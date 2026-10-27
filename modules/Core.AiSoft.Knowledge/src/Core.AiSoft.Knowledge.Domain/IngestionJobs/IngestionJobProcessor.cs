using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Documents;
using Core.AiSoft.Knowledge.Extraction;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Core.AiSoft.Knowledge.IngestionJobs;

/// <summary>
/// Runs one ingestion job. Transport-agnostic: the RabbitMQ worker (Infrastructure) calls it per delivery.
/// Each status change is committed in its own unit of work so the job list shows progress while the heavy work
/// runs outside any transaction.
/// </summary>
public class IngestionJobProcessor : DomainService
{
    private readonly IRepository<IngestionJob, Guid> _jobRepository;
    private readonly IRepository<KnowledgeDocument, Guid> _documentRepository;
    private readonly IBlobContainer<KnowledgeDocumentContainer> _blobContainer;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IEnumerable<ITextExtractor> _extractors;

    public IngestionJobProcessor(
        IRepository<IngestionJob, Guid> jobRepository,
        IRepository<KnowledgeDocument, Guid> documentRepository,
        IBlobContainer<KnowledgeDocumentContainer> blobContainer,
        IUnitOfWorkManager unitOfWorkManager,
        IEnumerable<ITextExtractor> extractors)
    {
        _jobRepository = jobRepository;
        _documentRepository = documentRepository;
        _blobContainer = blobContainer;
        _unitOfWorkManager = unitOfWorkManager;
        _extractors = extractors;
    }

    public virtual async Task ProcessAsync(IngestDocumentRequestedEto request, int attempt, CancellationToken cancellationToken = default)
    {
        using (CurrentTenant.Change(request.TenantId))
        {
            if (!await TryStartAsync(request.JobId, attempt, cancellationToken))
            {
                return;
            }

            var document = await _documentRepository.GetAsync(request.DocumentId, cancellationToken: cancellationToken);

            var extracted = await ExtractAsync(document, cancellationToken);

            Logger.LogInformation(
                "Extracted {CharacterCount} characters in {SectionCount} sections (headings: {HasHeadings}) from document {DocumentId} ({FileName})",
                extracted.CharacterCount, extracted.Sections.Count, extracted.HasHeadings, document.Id, document.FileName);

            // TODO: chunking -> embedding -> indexing stages consume `extracted` here.

            await UpdateJobAsync(request.JobId, job => job.Succeed(chunkCount: 0, Clock.Now), cancellationToken);
        }
    }


    protected virtual async Task<ExtractedDocument> ExtractAsync(KnowledgeDocument document, CancellationToken cancellationToken)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanExtract(document.Extension))
                        ?? throw new BusinessException(KnowledgeErrorCodes.ExtractorNotFound)
                            .WithData("extension", document.Extension);

        await using var content = await _blobContainer.GetOrNullAsync(document.BlobName, cancellationToken)
                                  ?? throw new BusinessException(KnowledgeErrorCodes.BlobNotFound)
                                      .WithData("blobName", document.BlobName);

        return await extractor.ExtractAsync(content, cancellationToken);
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
            await UpdateJobAsync(jobId, job => job.Fail(errorCode, errorMessage, Clock.Now), cancellationToken);
        }
    }

    /// <returns>False when the job no longer exists or has already finished.</returns>
    protected virtual async Task<bool> TryStartAsync(Guid jobId, int attempt, CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

        var job = await _jobRepository.FindAsync(jobId, cancellationToken: cancellationToken);
        if (job == null || job.IsFinished)
        {
            Logger.LogInformation("Skipping ingestion job {JobId}: {Reason}", jobId, job == null ? "not found" : $"already {job.Status}");
            return false;
        }

        job.StartAttempt(attempt, Clock.Now);
        await _jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
        await uow.CompleteAsync(cancellationToken);
        return true;
    }

    protected virtual async Task UpdateJobAsync(Guid jobId, Action<IngestionJob> change, CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

        var job = await _jobRepository.FindAsync(jobId, cancellationToken: cancellationToken);
        if (job == null || job.IsFinished)
        {
            return;
        }

        change(job);
        await _jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
        await uow.CompleteAsync(cancellationToken);
    }
}
