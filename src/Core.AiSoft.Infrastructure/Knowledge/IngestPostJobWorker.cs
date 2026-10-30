using System;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.IngestionJobs;
using Core.AiSoft.Knowledge.PostIndexing;
using Core.AiSoft.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Core.AiSoft.Knowledge;

public class IngestPostJobWorker : RabbitMqJobWorkerBase<IngestPostRequestedEto>
{
    public const string Name = "Knowledge.IngestPost";

    public IngestPostJobWorker(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    protected override string JobName => Name;

    protected override void ConfigureDefaults(RabbitMqJobWorkerOptions options)
    {
        options.MaxConcurrency = 3;
    }

    protected override Task ProcessAsync(
        IServiceProvider services,
        IngestPostRequestedEto message,
        JobContext context,
        CancellationToken cancellationToken)
    {
        return services.GetRequiredService<PostIngestionProcessor>().ProcessAsync(message, context.Attempt, cancellationToken);
    }

    protected override Task OnFailedAsync(
        IServiceProvider services,
        IngestPostRequestedEto message,
        JobFailure failure,
        CancellationToken cancellationToken)
    {
        return services.GetRequiredService<PostIngestionProcessor>()
            .FailAsync(message.JobId, message.TenantId, failure.ErrorCode, failure.ErrorMessage, cancellationToken);
    }
}
