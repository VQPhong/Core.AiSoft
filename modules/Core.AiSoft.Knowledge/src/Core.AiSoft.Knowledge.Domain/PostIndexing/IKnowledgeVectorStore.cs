using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.AiSoft.Knowledge.PostIndexing;

public interface IKnowledgeVectorStore
{
    Task ReplaceBlogPostAsync(KnowledgePostChunks post, CancellationToken cancellationToken = default);

    Task DeleteByBlogPostAsync(Guid blogPostId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Most similar chunks
    /// </summary>
    Task<IReadOnlyList<KnowledgeChunkMatch>> SearchAsync(
        string query,
        Guid? tenantId,
        int limit,
        float minScore,
        CancellationToken cancellationToken = default);
}

public record KnowledgeChunkMatch(Guid BlogPostId, int ChunkIndex, IReadOnlyList<string> HeadingPath, string Text, float Score);
