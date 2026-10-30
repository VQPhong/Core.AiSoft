using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Documents;
using Core.AiSoft.Knowledge.IngestionJobs;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;
using Volo.CmsKit.Blogs;

namespace Core.AiSoft.Knowledge.PostIndexing;

public class KnowledgePostDeletionManager : DomainService
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly PostIngestionManager _postIngestionManager;
    private readonly IRepository<KnowledgePostIndexState, Guid> _stateRepository;
    private readonly IRepository<IngestionJob, Guid> _jobRepository;
    private readonly IRepository<KnowledgeDocument, Guid> _documentRepository;
    private readonly IKnowledgeVectorStore _vectorStore;
    private readonly IBlobContainer<KnowledgeSnapshotContainer> _snapshotContainer;
    private readonly IBlobContainer<KnowledgeDocumentContainer> _documentContainer;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public KnowledgePostDeletionManager(
        IBlogPostRepository blogPostRepository,
        PostIngestionManager postIngestionManager,
        IRepository<KnowledgePostIndexState, Guid> stateRepository,
        IRepository<IngestionJob, Guid> jobRepository,
        IRepository<KnowledgeDocument, Guid> documentRepository,
        IKnowledgeVectorStore vectorStore,
        IBlobContainer<KnowledgeSnapshotContainer> snapshotContainer,
        IBlobContainer<KnowledgeDocumentContainer> documentContainer,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _blogPostRepository = blogPostRepository;
        _postIngestionManager = postIngestionManager;
        _stateRepository = stateRepository;
        _jobRepository = jobRepository;
        _documentRepository = documentRepository;
        _vectorStore = vectorStore;
        _snapshotContainer = snapshotContainer;
        _documentContainer = documentContainer;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public virtual async Task DeleteAsync(Guid blogPostId)
    {
        var post = await _blogPostRepository.FindAsync(blogPostId)
                   ?? throw new BusinessException(KnowledgeErrorCodes.PostNotFound).WithData("blogPostId", blogPostId);

        if (post.BlogId != await _postIngestionManager.GetKnowledgeBlogIdAsync())
        {
            throw new BusinessException(KnowledgeErrorCodes.PostNotInKnowledgeBlog);
        }

        var state = await _stateRepository.FirstOrDefaultAsync(s => s.BlogPostId == blogPostId);
        state?.EnsureCanDelete();

        await _vectorStore.DeleteByBlogPostAsync(blogPostId);

        var jobs = await _jobRepository.GetListAsync(j => j.BlogPostId == blogPostId);
        var documents = await _documentRepository.GetListAsync(d => d.BlogPostId == blogPostId);

        var blobsToDelete = jobs.Select(j => (Snapshot: true, Name: j.SnapshotBlobName))
            .Concat(documents.Select(d => (Snapshot: false, Name: d.BlobName)))
            .ToList();

        await _jobRepository.DeleteManyAsync(jobs);
        if (state != null)
        {
            await _stateRepository.DeleteAsync(state);
        }

        await _documentRepository.DeleteManyAsync(documents);
        await _blogPostRepository.DeleteAsync(post);

        _unitOfWorkManager.Current!.OnCompleted(() => DeleteBlobsAsync(blogPostId, blobsToDelete));
    }

    protected virtual async Task DeleteBlobsAsync(Guid blogPostId, IReadOnlyList<(bool Snapshot, string Name)> blobs)
    {
        foreach (var (snapshot, name) in blobs)
        {
            try
            {
                if (snapshot)
                {
                    await _snapshotContainer.DeleteAsync(name);
                }
                else
                {
                    await _documentContainer.DeleteAsync(name);
                }
            }
            catch (Exception ex)
            {
                // Rows are already gone: a leftover blob is only wasted space, so log and continue.
                Logger.LogWarning(ex, "Could not delete blob {BlobName} of deleted blog post {BlogPostId}", name, blogPostId);
            }
        }
    }
}
