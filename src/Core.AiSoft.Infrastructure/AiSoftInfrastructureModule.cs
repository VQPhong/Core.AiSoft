using System;
using Core.AiSoft.Ai;
using Core.AiSoft.BlobStoring.SeaweedFs;
using Core.AiSoft.HealthChecks;
using Core.AiSoft.Knowledge;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Modularity;

namespace Core.AiSoft;

[DependsOn(
    typeof(AiSoftDomainModule),
    typeof(AbpBlobStoringModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AbpExceptionHandlingModule)
)]
public class AiSoftInfrastructureModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        var seaweedFsSection = configuration.GetSection("SeaweedFs");
        Configure<SeaweedFsOptions>(seaweedFsSection);

        ConfigureSeaweedFsHttpClient(context, seaweedFsSection.Get<SeaweedFsOptions>() ?? new SeaweedFsOptions());

        var ollamaSection = configuration.GetSection("Ollama");
        Configure<OllamaOptions>(ollamaSection);
        ConfigureOllama(context, ollamaSection.Get<OllamaOptions>() ?? new OllamaOptions());

        context.Services.AddHealthChecks()
            .AddCheck<SeaweedFsFilerHealthCheck>("seaweedfs-filer", tags: ["ready"])
            .AddCheck<QdrantHealthCheck>("qdrant", tags: ["ready"])
            .AddCheck<OllamaHealthCheck>("ollama", tags: ["ready"]);

        // RabbitMQ job workers (one hosted service per job). Turn one off per host with Jobs:{JobName}:Enabled=false.
        context.Services.AddHostedService<IngestPostJobWorker>();
    }

    private static void ConfigureOllama(ServiceConfigurationContext context, OllamaOptions options)
    {
#pragma warning disable EXTEXP0001
        context.Services.AddHttpClient(OllamaOptions.HttpClientName, client => client.Timeout = options.RequestTimeout + TimeSpan.FromSeconds(30))
            .RemoveAllResilienceHandlers()
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.Retry.DisableForUnsafeHttpMethods();
                resilience.AttemptTimeout.Timeout = options.RequestTimeout;
                resilience.TotalRequestTimeout.Timeout = options.RequestTimeout;
                resilience.CircuitBreaker.SamplingDuration = options.RequestTimeout * 2;
            });
#pragma warning restore EXTEXP0001

        context.Services.AddTransient<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            sp.GetRequiredService<OllamaClientFactory>().Create(OllamaOptions.EmbeddingConnectionName));

        context.Services.AddTransient<IChatClient>(sp =>
            new OllamaChatDefaultsClient(
                sp.GetRequiredService<OllamaClientFactory>().Create(OllamaOptions.ChatConnectionName),
                options.ChatContextLength));
    }

    private static void ConfigureSeaweedFsHttpClient(ServiceConfigurationContext context, SeaweedFsOptions options)
    {
#pragma warning disable EXTEXP0001
        context.Services.AddHttpClient(SeaweedFsOptions.HttpClientName, client =>
            {
                if (string.IsNullOrWhiteSpace(options.FilerUrl))
                {
                    throw new AbpException("SeaweedFs:FilerUrl is not configured. Run the application through Core.AiSoft.AppHost.");
                }

                client.BaseAddress = new Uri(options.FilerUrl);
                client.Timeout = options.RequestTimeout + TimeSpan.FromSeconds(30);
            })
            .RemoveAllResilienceHandlers()
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.Retry.DisableForUnsafeHttpMethods();
                resilience.AttemptTimeout.Timeout = options.RequestTimeout;
                resilience.TotalRequestTimeout.Timeout = options.RequestTimeout;
                resilience.CircuitBreaker.SamplingDuration = options.RequestTimeout * 2;
            });
#pragma warning restore EXTEXP0001
    }
}
