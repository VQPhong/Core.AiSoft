using System;
using Volo.Abp.EventBus;

namespace Core.AiSoft.Knowledge.IngestionJobs;

[EventName(EventName)]
public class IngestPostRequestedEto
{
    public const string EventName = "Knowledge.IngestPostRequested";

    public Guid JobId { get; set; }

    public Guid BlogPostId { get; set; }

    public int Version { get; set; }

    public Guid? TenantId { get; set; }
}
