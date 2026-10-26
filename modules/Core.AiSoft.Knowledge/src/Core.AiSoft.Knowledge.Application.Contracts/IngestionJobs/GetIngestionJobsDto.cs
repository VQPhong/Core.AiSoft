using System;
using Volo.Abp.Application.Dtos;

namespace Core.AiSoft.Knowledge.IngestionJobs;

/// <summary>
/// Results are always ordered by creation time, newest first.
/// </summary>
public class GetIngestionJobsDto : PagedResultRequestDto
{
    /// <summary>
    /// Part of the document file name.
    /// </summary>
    public string? Filter { get; set; }

    public IngestionJobStatus? Status { get; set; }

    public Guid? DocumentId { get; set; }

    public DateTime? CreationTimeFrom { get; set; }

    public DateTime? CreationTimeTo { get; set; }
}
