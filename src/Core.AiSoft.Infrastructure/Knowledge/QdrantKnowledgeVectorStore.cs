using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.PostIndexing;
using Qdrant.Client;
using Volo.Abp.DependencyInjection;
using static Qdrant.Client.Grpc.Conditions;

namespace Core.AiSoft.Knowledge;

public class QdrantKnowledgeVectorStore : IKnowledgeVectorStore, ITransientDependency
{
    public const string CollectionPrefix = "knowledge_";
    public const string BlogPostIdField = "blog_post_id";

    private readonly QdrantClient _qdrantClient;

    public QdrantKnowledgeVectorStore(QdrantClient qdrantClient)
    {
        _qdrantClient = qdrantClient;
    }

    public virtual async Task DeleteByBlogPostAsync(Guid blogPostId, CancellationToken cancellationToken = default)
    {
        var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);

        foreach (var collection in collections.Where(c => c.StartsWith(CollectionPrefix, StringComparison.Ordinal)))
        {
            await _qdrantClient.DeleteAsync(
                collection,
                MatchKeyword(BlogPostIdField, blogPostId.ToString()),
                wait: true,
                cancellationToken: cancellationToken);
        }
    }
}
