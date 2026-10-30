using System;
using Core.AiSoft.Knowledge.IngestionJobs;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Core.AiSoft.Knowledge.PostIndexing;

public class KnowledgePostIndexState : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; protected set; }

    public virtual Guid BlogPostId { get; protected set; }

    public virtual int? IndexedVersion { get; protected set; }

    public virtual string? IndexedContentHash { get; protected set; }

    public virtual DateTime? IndexedAt { get; protected set; }

    public virtual int ChunkCount { get; protected set; }

    public virtual Guid? ActiveJobId { get; protected set; }

    public virtual int? ActiveVersion { get; protected set; }

    public virtual Guid? LastJobId { get; protected set; }

    protected KnowledgePostIndexState()
    {
    }

    internal KnowledgePostIndexState(Guid id, Guid? tenantId, Guid blogPostId)
        : base(id)
    {
        TenantId = tenantId;
        BlogPostId = blogPostId;
    }

    public virtual bool HasActiveJob => ActiveJobId.HasValue;

    public virtual PostIndexStatus GetStatus(string currentContentHash)
    {
        if (HasActiveJob)
        {
            return PostIndexStatus.Running;
        }

        if (IndexedContentHash == null)
        {
            return PostIndexStatus.NeverIndexed;
        }

        return IndexedContentHash == currentContentHash ? PostIndexStatus.UpToDate : PostIndexStatus.Stale;
    }

    public virtual void EnsureCanEnqueue(string contentHash, bool force)
    {
        if (HasActiveJob)
        {
            throw new BusinessException(KnowledgeErrorCodes.IngestionAlreadyRunning)
                .WithData("jobId", ActiveJobId!.Value);
        }

        if (!force && IndexedContentHash == contentHash)
        {
            throw new BusinessException(KnowledgeErrorCodes.ContentUnchanged)
                .WithData("indexedVersion", IndexedVersion!.Value);
        }
    }

    public virtual void EnsureCanDelete()
    {
        if (HasActiveJob)
        {
            throw new BusinessException(KnowledgeErrorCodes.IngestionAlreadyRunning)
                .WithData("jobId", ActiveJobId!.Value);
        }
    }

    public virtual void BeginJob(Guid jobId, int version)
    {
        if (HasActiveJob)
        {
            throw new BusinessException(KnowledgeErrorCodes.IngestionAlreadyRunning)
                .WithData("jobId", ActiveJobId!.Value);
        }

        ActiveJobId = jobId;
        ActiveVersion = version;
        LastJobId = jobId;
    }

    public virtual IngestionJobSkipReason? ShouldSkip(IngestionJob job)
    {
        // Lastest without duplicated
        if (ActiveJobId != job.Id || job.Version < IndexedVersion)
        {
            return IngestionJobSkipReason.Superseded;
        }

        // Force ingest
        if (!job.IsForced && job.ContentHash == IndexedContentHash)
        {
            return IngestionJobSkipReason.Unchanged;
        }

        return null;
    }

    public virtual bool ApplyResult(IngestionJob job, int chunkCount, DateTime now)
    {
        if (job.Version < IndexedVersion)
        {
            return false;
        }

        IndexedVersion = job.Version;
        IndexedContentHash = job.ContentHash;
        IndexedAt = now;
        ChunkCount = chunkCount;
        return true;
    }

    public virtual void EndJob(Guid jobId)
    {
        if (ActiveJobId != jobId)
        {
            return;
        }

        ActiveJobId = null;
        ActiveVersion = null;
    }
}
