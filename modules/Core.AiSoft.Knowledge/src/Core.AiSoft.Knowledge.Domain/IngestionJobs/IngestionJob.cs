using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Core.AiSoft.Knowledge.IngestionJobs;

/// <summary>
/// By default, create one job that ref to <see cref="Documents.KnowledgeDocument"/>. 
/// Every upload or re-ingest creates a new job; keep run history.
/// </summary>
public class IngestionJob : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; protected set; }

    public virtual Guid DocumentId { get; protected set; }

    public virtual IngestionJobStatus Status { get; protected set; }

    public virtual int AttemptCount { get; protected set; }

    public virtual DateTime? StartedAt { get; protected set; }

    public virtual DateTime? FinishedAt { get; protected set; }

    public virtual int ChunkCount { get; protected set; }

    public virtual string? ErrorCode { get; protected set; }

    public virtual string? ErrorMessage { get; protected set; }

    protected IngestionJob()
    {
    }

    internal IngestionJob(Guid id, Guid? tenantId, Guid documentId)
        : base(id)
    {
        TenantId = tenantId;
        DocumentId = documentId;
        Status = IngestionJobStatus.Queued;
    }

    public virtual bool IsFinished => Status is IngestionJobStatus.Succeeded or IngestionJobStatus.Failed;

    public virtual void StartAttempt(int attempt, DateTime now)
    {
        EnsureNotFinished();

        AttemptCount = attempt;
        StartedAt ??= now;
        Status = IngestionJobStatus.Extracting;
    }

    public virtual void Succeed(int chunkCount, DateTime now)
    {
        EnsureNotFinished();

        ChunkCount = chunkCount;
        FinishedAt = now;
        ErrorCode = null;
        ErrorMessage = null;
        Status = IngestionJobStatus.Succeeded;
    }

    public virtual void Fail(string? errorCode, string? errorMessage, DateTime now)
    {
        EnsureNotFinished();

        ErrorCode = Truncate(errorCode, IngestionJobConsts.MaxErrorCodeLength);
        ErrorMessage = Truncate(errorMessage, IngestionJobConsts.MaxErrorMessageLength);
        FinishedAt = now;
        Status = IngestionJobStatus.Failed;
    }

    private void EnsureNotFinished()
    {
        if (IsFinished)
        {
            throw new InvalidOperationException($"Ingestion job {Id} has already finished with status {Status}.");
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value == null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
