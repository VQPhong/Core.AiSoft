using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.AiSoft.Knowledge.PostIndexing;

public interface IKnowledgeVectorStore
{
    Task ReplaceBlogPostAsync(KnowledgePostChunks post, CancellationToken cancellationToken = default);

    Task DeleteByBlogPostAsync(Guid blogPostId, CancellationToken cancellationToken = default);
}
