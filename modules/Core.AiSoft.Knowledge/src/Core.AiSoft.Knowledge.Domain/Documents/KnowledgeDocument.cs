using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Core.AiSoft.Knowledge.Documents;

public class KnowledgeDocument : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; protected set; }

    public virtual string FileName { get; protected set; } = default!;

    public virtual string Extension { get; protected set; } = default!;

    public virtual string ContentType { get; protected set; } = default!;

    public virtual long Size { get; protected set; }

    /// <summary>
    /// Lower-case hex SHA-256 of the file content - for duplication check.
    /// </summary>
    public virtual string Sha256 { get; protected set; } = default!;

    public virtual string BlobName { get; protected set; } = default!;

    public virtual KnowledgeDocumentStatus Status { get; protected set; }

    public virtual int ChunkCount { get; protected set; }

    public virtual Guid? LastJobId { get; protected set; }

    public virtual Guid? BlogPostId { get; protected set; }

    protected KnowledgeDocument()
    {
    }

    internal KnowledgeDocument(
        Guid id,
        Guid? tenantId,
        string fileName,
        string extension,
        string contentType,
        long size,
        string sha256,
        string blobName)
        : base(id)
    {
        TenantId = tenantId;
        FileName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName), KnowledgeDocumentConsts.MaxFileNameLength);
        Extension = Check.NotNullOrWhiteSpace(extension, nameof(extension), KnowledgeDocumentConsts.MaxExtensionLength).ToLower();
        ContentType = Check.NotNullOrWhiteSpace(contentType, nameof(contentType), KnowledgeDocumentConsts.MaxContentTypeLength);
        Size = size;
        Sha256 = Check.NotNullOrWhiteSpace(sha256, nameof(sha256), KnowledgeDocumentConsts.Sha256Length);
        BlobName = Check.NotNullOrWhiteSpace(blobName, nameof(blobName), KnowledgeDocumentConsts.MaxBlobNameLength);
        Status = KnowledgeDocumentStatus.Pending;
    }

    internal void AttachPost(Guid blogPostId)
    {
        BlogPostId = blogPostId;
    }

    internal void AssignJob(Guid jobId)
    {
        LastJobId = jobId;
        Status = KnowledgeDocumentStatus.Pending;
    }
}
