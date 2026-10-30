using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Volo.Abp;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.RabbitMQ;

namespace Core.AiSoft.Messaging;

// TODO: review this later
public abstract class RabbitMqJobWorkerBase<TMessage> : BackgroundService
    where TMessage : class
{
    public const string AttemptHeader = "x-aisoft-attempt";
    public const string LastErrorHeader = "x-aisoft-last-error";

    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected IServiceScopeFactory ScopeFactory { get; }
    protected IConnectionPool ConnectionPool { get; }
    protected IRabbitMqSerializer Serializer { get; }
    protected AbpRabbitMqEventBusOptions EventBusOptions { get; }
    protected RabbitMqJobWorkerOptions Options { get; }
    protected ILogger Logger { get; }

    protected abstract string JobName { get; }

    protected virtual string RoutingKey => EventNameAttribute.GetNameOrDefault<TMessage>();

    protected string QueueName => $"{EventBusOptions.ExchangeName}.Jobs.{JobName}";
    protected string RetryQueueName => QueueName + ".retry";
    protected string DeadQueueName => QueueName + ".dead";

    protected RabbitMqJobWorkerBase(IServiceProvider serviceProvider)
    {
        ScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        ConnectionPool = serviceProvider.GetRequiredService<IConnectionPool>();
        Serializer = serviceProvider.GetRequiredService<IRabbitMqSerializer>();
        EventBusOptions = serviceProvider.GetRequiredService<IOptions<AbpRabbitMqEventBusOptions>>().Value;
        Logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

        Options = new RabbitMqJobWorkerOptions();
        ConfigureDefaults(Options);
        serviceProvider.GetRequiredService<IConfiguration>().GetSection("Jobs").GetSection(JobName).Bind(Options);
    }

    private int _inFlight;

    /// <summary>
    /// Per-job defaults, applied before the "Jobs:{JobName}" configuration section (which overrides them).
    /// Called from the constructor: must only set values on <paramref name="options"/>.
    /// </summary>
    protected virtual void ConfigureDefaults(RabbitMqJobWorkerOptions options)
    {
    }

    /// <summary>
    /// How the message processed.
    /// Throw a <see cref="BusinessException"/> for perrmanent errors;
    /// temporary exception is retried until <see cref="RabbitMqJobWorkerOptions.MaxAttempts"/>.
    /// </summary>
    protected abstract Task ProcessAsync(IServiceProvider services, TMessage message, JobContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Called once when the job will not be processed again: business error / last attempt failed.
    /// </summary>
    protected virtual Task OnFailedAsync(IServiceProvider services, TMessage message, JobFailure failure, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Options.Enabled)
        {
            Logger.LogInformation("Job worker {JobName} is disabled; messages stay in {Queue}", JobName, QueueName);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilChannelClosesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Job worker {JobName} lost its RabbitMQ channel; reconnecting in {Delay}", JobName, ReconnectDelay);
            }

            try
            {
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    protected virtual async Task ConsumeUntilChannelClosesAsync(CancellationToken stoppingToken)
    {
        var concurrency = (ushort)Math.Clamp(Options.MaxConcurrency, 1, RabbitMqJobWorkerOptions.MaxConcurrencyLimit);

        var connection = await ConnectionPool.GetAsync(EventBusOptions.ConnectionName);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: false,
                publisherConfirmationTrackingEnabled: false,
                consumerDispatchConcurrency: concurrency),
            stoppingToken);

        await DeclareTopologyAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: concurrency, global: false, stoppingToken);

        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        channel.ChannelShutdownAsync += (_, _) =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);

        Logger.LogInformation(
            "Job worker {JobName} consuming {Queue} (routing key {RoutingKey}, concurrency {Concurrency}, max attempts {MaxAttempts})",
            JobName, QueueName, RoutingKey, concurrency, Options.MaxAttempts);

        await closed.Task.WaitAsync(stoppingToken);
    }

    protected virtual async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            EventBusOptions.ExchangeName,
            EventBusOptions.GetExchangeTypeOrDefault(),
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(QueueName, EventBusOptions.ExchangeName, RoutingKey, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = QueueName
            },
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(DeadQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
    }

    protected virtual async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var attempt = ReadAttempt(delivery.BasicProperties);
        TMessage? message = null;
        Exception? error = null;

        var inFlight = Interlocked.Increment(ref _inFlight);
        Logger.LogDebug("Job {JobName} started message {MessageId} ({InFlight} in flight)", JobName, delivery.BasicProperties.MessageId, inFlight);

        try
        {
            message = (TMessage)Serializer.Deserialize(delivery.Body.ToArray(), typeof(TMessage));

            using var scope = ScopeFactory.CreateScope();
            await ProcessAsync(
                scope.ServiceProvider,
                message,
                new JobContext(attempt, Options.MaxAttempts, delivery.BasicProperties.MessageId),
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await TryNackAsync(channel, delivery.DeliveryTag);
            return;
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }

        // Unreadable -> always be error
        var decision = message == null
            ? JobDeliveryDecision.Dead
            : JobRetryPolicy.Decide(error, attempt, Options.MaxAttempts);

        try
        {
            switch (decision)
            {
                case JobDeliveryDecision.Fail:
                    Logger.LogWarning(error, "Job {JobName} failed (attempt {Attempt}); not retrying", JobName, attempt);
                    await NotifyFailedAsync(message!, error!, attempt, retriesExhausted: false, stoppingToken);
                    break;

                case JobDeliveryDecision.Retry:
                    Logger.LogWarning(error, "Job {JobName} attempt {Attempt}/{MaxAttempts} failed; retrying in {Delay}",
                        JobName, attempt, Options.MaxAttempts, Options.RetryDelay);
                    await RepublishAsync(channel, delivery, RetryQueueName, attempt + 1, Options.RetryDelay, error, stoppingToken);
                    break;

                case JobDeliveryDecision.Dead:
                    Logger.LogError(error, "Job {JobName} gave up after attempt {Attempt}; moving to {DeadQueue}", JobName, attempt, DeadQueueName);
                    if (message != null)
                    {
                        await NotifyFailedAsync(message, error!, attempt, retriesExhausted: true, stoppingToken);
                    }

                    await RepublishAsync(channel, delivery, DeadQueueName, attempt, delay: null, error, stoppingToken);
                    break;
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            // Could not record the outcome - temporary error, just retry later.
            Logger.LogError(ex, "Job {JobName} could not settle message {MessageId}; requeueing", JobName, delivery.BasicProperties.MessageId);
            await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
            await TryNackAsync(channel, delivery.DeliveryTag);
        }
    }

    protected virtual async Task NotifyFailedAsync(TMessage message, Exception error, int attempt, bool retriesExhausted, CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var (code, text) = DescribeError(scope.ServiceProvider, error);

        await OnFailedAsync(scope.ServiceProvider, message, new JobFailure(error, code, text, attempt, retriesExhausted), cancellationToken);
    }

    protected virtual (string? Code, string? Message) DescribeError(IServiceProvider services, Exception error)
    {
        if (error is IBusinessException)
        {
            var info = services.GetRequiredService<IExceptionToErrorInfoConverter>().Convert(error);
            return (info.Code, info.Message);
        }

        return (null, $"{error.GetType().Name}: {error.Message}");
    }

    protected virtual async Task RepublishAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        string queueName,
        int attempt,
        TimeSpan? delay,
        Exception? error,
        CancellationToken cancellationToken)
    {
        var source = delivery.BasicProperties;
        var headers = source.Headers == null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(source.Headers);

        headers[AttemptHeader] = attempt;
        if (error != null)
        {
            var text = $"{error.GetType().Name}: {error.Message}";
            headers[LastErrorHeader] = text.Length > 1000 ? text[..1000] : text;
        }

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = source.MessageId,
            CorrelationId = source.CorrelationId,
            ContentType = source.ContentType,
            Headers = headers,
            Expiration = delay.HasValue ? ((long)delay.Value.TotalMilliseconds).ToString() : null
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: delivery.Body,
            cancellationToken: cancellationToken);
    }

    private async Task TryNackAsync(IChannel channel, ulong deliveryTag)
    {
        try
        {
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Could not nack message on job {JobName}", JobName);
        }
    }

    private static int ReadAttempt(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers == null || !properties.Headers.TryGetValue(AttemptHeader, out var value) || value == null)
        {
            return 1;
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            _ => 1
        };
    }
}
