using System;
using Volo.Abp.Application.Dtos;

namespace Core.AiSoft.Knowledge.IngestionJobs;

public class IngestionJobDto : EntityDto<Guid>
{
    public Guid DocumentId { get; set; }

    public string FileName { get; set; } = default!;

    public IngestionJobStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Time from the first start to the end of the job
    /// </summary>
    public long? DurationMs { get; set; }

    public int ChunkCount { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreationTime { get; set; }
}
