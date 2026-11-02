using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.PostIndexing;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Services;
using Volo.CmsKit.Blogs;

namespace Core.AiSoft.Knowledge.Retrieval;

public class KnowledgeRetriever : DomainService
{
    private readonly IKnowledgeVectorStore _vectorStore;
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly PostIngestionManager _postIngestionManager;
    private readonly KnowledgeRetrievalOptions _options;

    public KnowledgeRetriever(
        IKnowledgeVectorStore vectorStore,
        IBlogPostRepository blogPostRepository,
        PostIngestionManager postIngestionManager,
        IOptions<KnowledgeRetrievalOptions> options)
    {
        _vectorStore = vectorStore;
        _blogPostRepository = blogPostRepository;
        _postIngestionManager = postIngestionManager;
        _options = options.Value;
    }

    public virtual async Task<IReadOnlyList<KnowledgeSource>> SearchAsync(
        string query,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var topK = Math.Clamp(limit ?? _options.TopK, 1, KnowledgeRetrievalOptions.MaxTopK);
        var matches = await _vectorStore.SearchAsync(query.Trim(), CurrentTenant.Id, topK, _options.MinScore, cancellationToken);
        if (matches.Count == 0)
        {
            return [];
        }

        var knowledgeBlogId = await _postIngestionManager.GetKnowledgeBlogIdAsync();
        var posts = new Dictionary<Guid, BlogPost>();
        foreach (var postId in matches.Select(m => m.BlogPostId).Distinct())
        {
            var post = await _blogPostRepository.FindAsync(postId, cancellationToken: cancellationToken);
            if (post != null && post.BlogId == knowledgeBlogId && post.Status == BlogPostStatus.Published)
            {
                posts[postId] = post;
            }
        }

        return matches
            .Where(m => posts.ContainsKey(m.BlogPostId))
            .Select(m =>
            {
                var post = posts[m.BlogPostId];
                return new KnowledgeSource(post.Id, post.Title, post.Slug, m.ChunkIndex, m.HeadingPath, m.Text, m.Score);
            })
            .ToList();
    }
}

public record KnowledgeSource(
    Guid BlogPostId,
    string Title,
    string Slug,
    int ChunkIndex,
    IReadOnlyList<string> HeadingPath,
    string Text,
    float Score);
