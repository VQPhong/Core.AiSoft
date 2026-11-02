using System;

namespace Core.AiSoft.Knowledge.IngestionJobs;

public class CreateIngestionJobDto
{
    public Guid BlogPostId { get; set; }

    public bool Force { get; set; }
}
