using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Extraction;
using Core.AiSoft.Knowledge.IngestionJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Core.AiSoft.Knowledge.PostIndexing;

/// <summary>
/// Runs one ingestion job for a blog post. Transport-agnostic: the RabbitMQ worker (Infrastructure) calls it per delivery.
/// A per-post distributed lock makes sure one post is processed by one worker at a time (redeliveries, manual messages);
/// the version / content checks are in <see cref="KnowledgePostIndexState"/>.
/// Each state change is committed in its own unit of work so the job list shows progress; heavy work runs outside transactions.
/// </summary>
public class PostIngestionProcessor : DomainService
{
    public static readonly TimeSpan LockWaitTimeout = TimeSpan.FromSeconds(5);

    private const string MarkdownExtension = ".md";

    private readonly IRepository<IngestionJob, Guid> _jobRepository;
    private readonly IRepository<KnowledgePostIndexState, Guid> _stateRepository;
    private readonly IBlobContainer<KnowledgeSnapshotContainer> _snapshotContainer;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IEnumerable<ITextExtractor> _extractors;
    private readonly KnowledgeIngestionOptions _options;

    public PostIngestionProcessor(
        IRepository<IngestionJob, Guid> jobRepository,
        IRepository<KnowledgePostIndexState, Guid> stateRepository,
        IBlobContainer<KnowledgeSnapshotContainer> snapshotContainer,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager,
        IEnumerable<ITextExtractor> extractors,
        IOptions<KnowledgeIngestionOptions> options)
    {
        _jobRepository = jobRepository;
        _stateRepository = stateRepository;
        _snapshotContainer = snapshotContainer;
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
        _extractors = extractors;
        _options = options.Value;
    }

    public virtual async Task ProcessAsync(IngestPostRequestedEto request, int attempt, CancellationToken cancellationToken = default)
    {
        using (CurrentTenant.Change(request.TenantId))
        {
            await using var handle = await _distributedLock.TryAcquireAsync(LockName(request.BlogPostId), LockWaitTimeout, cancellationToken);
            if (handle == null)
            {
                throw new IngestionLockNotAcquiredException(request.BlogPostId);
            }

            var job = await TryStartAsync(request.JobId, attempt, cancellationToken);
            if (job == null)
            {
                return;
            }

            var document = await ReadSnapshotAsync(job, cancellationToken);

            Logger.LogInformation(
                "Ingesting blog post {BlogPostId} v{Version} (job {JobId}): {CharacterCount} characters in {SectionCount} sections",
                job.BlogPostId, job.Version, job.Id, document.CharacterCount, document.Sections.Count);

            // TODO: chunk -> embed -> upsert into Qdrant here
            var chunkCount = document.Sections.Count;

            if (_options.DebugProcessingDelay is { } delay)
            {
                await Task.Delay(delay, cancellationToken);
            }

            await CompleteAsync(job.Id, chunkCount, cancellationToken);
        }
    }

    public virtual async Task FailAsync(
        Guid jobId,
        Guid? tenantId,
        string? errorCode,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        using (CurrentTenant.Change(tenantId))
        {
            await ChangeAsync(jobId, (job, state) =>
            {
                job.Fail(errorCode, errorMessage, Clock.Now);
                state?.EndJob(job.Id);
            }, cancellationToken);
        }
    }

    /// <returns>The started job, or null when it must not run (finished already, or skipped as superseded / unchanged).</returns>
    protected virtual async Task<IngestionJob?> TryStartAsync(Guid jobId, int attempt, CancellationToken cancellationToken)
    {
        IngestionJob? started = null;

        await ChangeAsync(jobId, (job, state) =>
        {
            var skipReason = state == null ? IngestionJobSkipReason.Superseded : state.ShouldSkip(job);
            if (skipReason.HasValue)
            {
                Logger.LogInformation("Skipping ingestion job {JobId} (post {BlogPostId} v{Version}): {Reason}",
                    job.Id, job.BlogPostId, job.Version, skipReason);

                job.Skip(skipReason.Value, Clock.Now);
                state?.EndJob(job.Id);
                return;
            }

            job.StartAttempt(attempt, Clock.Now);
            started = job;
        }, cancellationToken);

        return started;
    }

    protected virtual async Task CompleteAsync(Guid jobId, int chunkCount, CancellationToken cancellationToken)
    {
        await ChangeAsync(jobId, (job, state) =>
        {
            if (state != null && !state.ApplyResult(job, chunkCount, Clock.Now))
            {
                Logger.LogInformation("Ingestion job {JobId} finished but a newer version of post {BlogPostId} is already indexed",
                    job.Id, job.BlogPostId);
            }

            job.Succeed(chunkCount, Clock.Now);
            state?.EndJob(job.Id);
        }, cancellationToken);
    }

    protected virtual async Task<ExtractedDocument> ReadSnapshotAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanExtract(MarkdownExtension))
                        ?? throw new BusinessException(KnowledgeErrorCodes.ExtractorNotFound)
                            .WithData("extension", MarkdownExtension);

        await using var content = await _snapshotContainer.GetOrNullAsync(job.SnapshotBlobName, cancellationToken)
                                  ?? throw new BusinessException(KnowledgeErrorCodes.SnapshotNotFound)
                                      .WithData("blobName", job.SnapshotBlobName);

        return await extractor.ExtractAsync(content, cancellationToken);
    }

    /// <summary>
    /// Loads the job and its post state in a new transactional unit of work, applies <paramref name="change"/> and commits.
    /// Does nothing when the job is missing or already finished (redelivered message).
    /// </summary>
    protected virtual async Task ChangeAsync(
        Guid jobId,
        Action<IngestionJob, KnowledgePostIndexState?> change,
        CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

        var job = await _jobRepository.FindAsync(jobId, cancellationToken: cancellationToken);
        if (job == null || job.IsFinished)
        {
            Logger.LogInformation("Ingestion job {JobId}: {Reason}; nothing to do", jobId, job == null ? "not found" : $"already {job.Status}");
            return;
        }

        var state = await _stateRepository.FirstOrDefaultAsync(s => s.BlogPostId == job.BlogPostId, cancellationToken);

        change(job, state);

        await _jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
        if (state != null)
        {
            await _stateRepository.UpdateAsync(state, cancellationToken: cancellationToken);
        }

        await uow.CompleteAsync(cancellationToken);
    }

    protected static string LockName(Guid blogPostId) => $"knowledge:ingest:{blogPostId:N}";
}

/// <summary>
/// Transient: another worker is processing the same blog post. Not a business error, so the job is retried.
/// </summary>
public class IngestionLockNotAcquiredException : Exception
{
    public IngestionLockNotAcquiredException(Guid blogPostId)
        : base($"Blog post {blogPostId} is being ingested by another worker.")
    {
    }
}
