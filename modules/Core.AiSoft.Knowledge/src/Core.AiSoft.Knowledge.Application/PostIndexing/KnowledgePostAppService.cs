using System;
using System.Linq;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.CmsKit.Blogs;

namespace Core.AiSoft.Knowledge.PostIndexing;

/// <summary>
/// Posts of a blog (the Knowledge blog by default) with their index state, for the index management grid.
/// </summary>
[Authorize(KnowledgePermissions.IngestionJobs.Default)]
public class KnowledgePostAppService : KnowledgeAppService, IKnowledgePostAppService
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IRepository<KnowledgePostIndexState, Guid> _stateRepository;
    private readonly PostIngestionManager _postIngestionManager;
    private readonly KnowledgePostDeletionManager _deletionManager;

    public KnowledgePostAppService(
        IBlogPostRepository blogPostRepository,
        IRepository<KnowledgePostIndexState, Guid> stateRepository,
        PostIngestionManager postIngestionManager,
        KnowledgePostDeletionManager deletionManager)
    {
        _blogPostRepository = blogPostRepository;
        _stateRepository = stateRepository;
        _postIngestionManager = postIngestionManager;
        _deletionManager = deletionManager;
    }

    public virtual async Task<PagedResultDto<KnowledgePostDto>> GetListAsync(GetKnowledgePostsDto input)
    {
        var knowledgeBlogId = await _postIngestionManager.GetKnowledgeBlogIdAsync();
        var blogId = input.BlogId ?? knowledgeBlogId;
        var filter = input.Filter?.Trim();

        var totalCount = await _blogPostRepository.GetCountAsync(filter: filter, blogId: blogId);
        var posts = await _blogPostRepository.GetListAsync(
            filter: filter,
            blogId: blogId,
            maxResultCount: input.MaxResultCount,
            skipCount: input.SkipCount,
            sorting: $"{nameof(BlogPost.CreationTime)} desc");

        var postIds = posts.Select(p => p.Id).ToList();
        var states = (await _stateRepository.GetListAsync(s => postIds.Contains(s.BlogPostId)))
            .ToDictionary(s => s.BlogPostId);

        var items = posts.Select(post =>
        {
            states.TryGetValue(post.Id, out var state);
            var hash = post.ComputeContentHash();

            return new KnowledgePostDto
            {
                Id = post.Id,
                BlogId = post.BlogId,
                Title = post.Title,
                Slug = post.Slug,
                IsPublished = post.Status == BlogPostStatus.Published,
                IsIngestible = post.BlogId == knowledgeBlogId && post.Status == BlogPostStatus.Published,
                Version = post.EntityVersion,
                CreationTime = post.CreationTime,
                LastModificationTime = post.LastModificationTime,
                IndexStatus = state?.GetStatus(hash) ?? PostIndexStatus.NeverIndexed,
                IndexedVersion = state?.IndexedVersion,
                IndexedAt = state?.IndexedAt,
                ChunkCount = state?.ChunkCount ?? 0,
                ActiveJobId = state?.ActiveJobId,
                LastJobId = state?.LastJobId
            };
        }).ToList();

        return new PagedResultDto<KnowledgePostDto>(totalCount, items);
    }

    [Authorize(KnowledgePermissions.Documents.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        await _deletionManager.DeleteAsync(id);
    }
}
