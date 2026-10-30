using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.AiSoft.Knowledge.PostIndexing;

public interface IKnowledgeVectorStore
{
    Task DeleteByBlogPostAsync(Guid blogPostId, CancellationToken cancellationToken = default);
}
