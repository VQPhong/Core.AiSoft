using System;
using Volo.Abp.Application.Dtos;

namespace Core.AiSoft.Knowledge.Documents;

public class KnowledgeDocumentDto : EntityDto<Guid>
{
    public string FileName { get; set; } = default!;

    public string Extension { get; set; } = default!;

    public string ContentType { get; set; } = default!;

    public long Size { get; set; }

    public KnowledgeDocumentStatus Status { get; set; }

    public int ChunkCount { get; set; }

    /// <summary>
    /// Ingestion job Id.
    /// </summary>
    public Guid? LastJobId { get; set; }

    /// <summary>
    /// Draft blog post
    /// </summary>
    public Guid? BlogPostId { get; set; }

    public DateTime CreationTime { get; set; }
}
