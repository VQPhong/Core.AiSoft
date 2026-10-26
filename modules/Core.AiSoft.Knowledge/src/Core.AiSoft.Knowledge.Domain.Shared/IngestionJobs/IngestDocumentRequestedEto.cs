using System;
using Volo.Abp.EventBus;

namespace Core.AiSoft.Knowledge.IngestionJobs;

[EventName(EventName)]
public class IngestDocumentRequestedEto
{
    public const string EventName = "Knowledge.IngestDocumentRequested";

    public Guid JobId { get; set; }

    public Guid DocumentId { get; set; }

    public Guid? TenantId { get; set; }
}
