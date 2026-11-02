using System;
using System.Text;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.IngestionJobs;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Distributed;
using Volo.CmsKit.Blogs;

namespace Core.AiSoft.Knowledge.PostIndexing;

public class PostIngestionManager : DomainService
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IBlogRepository _blogRepository;
    private readonly IRepository<KnowledgePostIndexState, Guid> _stateRepository;
    private readonly IRepository<IngestionJob, Guid> _jobRepository;
    private readonly IBlobContainer<KnowledgeSnapshotContainer> _snapshotContainer;
    private readonly IDistributedEventBus _distributedEventBus;

    public PostIngestionManager(
        IBlogPostRepository blogPostRepository,
        IBlogRepository blogRepository,
        IRepository<KnowledgePostIndexState, Guid> stateRepository,
        IRepository<IngestionJob, Guid> jobRepository,
        IBlobContainer<KnowledgeSnapshotContainer> snapshotContainer,
        IDistributedEventBus distributedEventBus)
    {
        _blogPostRepository = blogPostRepository;
        _blogRepository = blogRepository;
        _stateRepository = stateRepository;
        _jobRepository = jobRepository;
        _snapshotContainer = snapshotContainer;
        _distributedEventBus = distributedEventBus;
    }

    public virtual async Task<IngestionJob> EnqueueAsync(Guid blogPostId, bool force)
    {
        var post = await _blogPostRepository.FindAsync(blogPostId)
                   ?? throw new BusinessException(KnowledgeErrorCodes.PostNotFound).WithData("blogPostId", blogPostId);

        post.EnsureIngestible(await GetKnowledgeBlogIdAsync());

        var contentHash = post.ComputeContentHash();
        var state = await GetOrCreateStateAsync(blogPostId);

        state.EnsureCanEnqueue(contentHash, force);

        var jobId = GuidGenerator.Create();
        var job = new IngestionJob(
            jobId,
            CurrentTenant.Id,
            blogPostId,
            post.EntityVersion,
            contentHash,
            force,
            snapshotBlobName: $"{blogPostId:N}/{post.EntityVersion}-{jobId:N}.md");

        state.BeginJob(job.Id, job.Version);

        await _jobRepository.InsertAsync(job);
        await _stateRepository.UpdateAsync(state);

        await _distributedEventBus.PublishAsync(new IngestPostRequestedEto
        {
            JobId = job.Id,
            BlogPostId = blogPostId,
            Version = job.Version,
            TenantId = job.TenantId
        });

        await _snapshotContainer.SaveAsync(job.SnapshotBlobName, Encoding.UTF8.GetBytes(post.Content ?? string.Empty));

        return job;
    }

    public virtual async Task<Guid> GetKnowledgeBlogIdAsync()
    {
        try
        {
            return (await _blogRepository.GetBySlugAsync(KnowledgeBlogConsts.BlogSlug)).Id;
        }
        catch (EntityNotFoundException)
        {
            throw new BusinessException(KnowledgeErrorCodes.PostNotInKnowledgeBlog);
        }
    }

    protected virtual async Task<KnowledgePostIndexState> GetOrCreateStateAsync(Guid blogPostId)
    {
        var state = await _stateRepository.FirstOrDefaultAsync(s => s.BlogPostId == blogPostId);
        if (state != null)
        {
            return state;
        }

        return await _stateRepository.InsertAsync(
            new KnowledgePostIndexState(GuidGenerator.Create(), CurrentTenant.Id, blogPostId),
            autoSave: true);
    }
}
