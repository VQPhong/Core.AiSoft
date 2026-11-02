using System;
using Volo.Abp.Application.Dtos;

namespace Core.AiSoft.Knowledge.IngestionJobs;

/// <summary>
/// Results are always ordered by creation time, newest first.
/// </summary>
public class GetIngestionJobsDto : PagedResultRequestDto
{
    public Guid? BlogPostId { get; set; }

    public IngestionJobStatus? Status { get; set; }

    public DateTime? CreationTimeFrom { get; set; }

    public DateTime? CreationTimeTo { get; set; }
}
