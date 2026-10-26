using System;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.IngestionJobs;
using Core.AiSoft.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Core.AiSoft.Knowledge;

public class IngestDocumentJobWorker : RabbitMqJobWorkerBase<IngestDocumentRequestedEto>
{
    public IngestDocumentJobWorker(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    protected override string JobName => "Knowledge.IngestDocument";

    protected override Task ProcessAsync(
        IServiceProvider services,
        IngestDocumentRequestedEto message,
        JobContext context,
        CancellationToken cancellationToken)
    {
        return services.GetRequiredService<IngestionJobProcessor>().ProcessAsync(message, context.Attempt, cancellationToken);
    }

    protected override Task OnFailedAsync(
        IServiceProvider services,
        IngestDocumentRequestedEto message,
        JobFailure failure,
        CancellationToken cancellationToken)
    {
        return services.GetRequiredService<IngestionJobProcessor>()
            .FailAsync(message.JobId, message.TenantId, failure.ErrorCode, failure.ErrorMessage, cancellationToken);
    }
}
