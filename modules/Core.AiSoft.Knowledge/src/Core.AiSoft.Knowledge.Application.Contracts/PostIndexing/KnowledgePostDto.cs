using System;
using Volo.Abp.Application.Dtos;

namespace Core.AiSoft.Knowledge.PostIndexing;

/// <summary>
/// A Knowledge blog post with its index state.
/// </summary>
public class KnowledgePostDto : EntityDto<Guid>
{
    public Guid BlogId { get; set; }

    public string Title { get; set; } = default!;

    public string Slug { get; set; } = default!;

    public bool IsPublished { get; set; }

    public bool IsIngestible { get; set; }

    public int Version { get; set; }

    public DateTime CreationTime { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public PostIndexStatus IndexStatus { get; set; }

    public int? IndexedVersion { get; set; }

    public DateTime? IndexedAt { get; set; }

    public int ChunkCount { get; set; }

    public Guid? ActiveJobId { get; set; }

    public Guid? LastJobId { get; set; }
}
