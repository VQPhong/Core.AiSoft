using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.IngestionJobs;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace Core.AiSoft.Knowledge.Documents;

public class KnowledgeDocumentManager : DomainService
{
    private readonly KnowledgeFileValidator _fileValidator;
    private readonly IRepository<KnowledgeDocument, Guid> _documentRepository;
    private readonly IRepository<IngestionJob, Guid> _jobRepository;
    private readonly IBlobContainer<KnowledgeDocumentContainer> _blobContainer;

    public KnowledgeDocumentManager(
        KnowledgeFileValidator fileValidator,
        IRepository<KnowledgeDocument, Guid> documentRepository,
        IRepository<IngestionJob, Guid> jobRepository,
        IBlobContainer<KnowledgeDocumentContainer> blobContainer)
    {
        _fileValidator = fileValidator;
        _documentRepository = documentRepository;
        _jobRepository = jobRepository;
        _blobContainer = blobContainer;
    }

    public virtual async Task<(KnowledgeDocument Document, IngestionJob Job)> CreateAsync(string fileName, byte[] content)
    {
        var (extension, contentType) = _fileValidator.Validate(fileName, content);

        var sha256 = Convert.ToHexStringLower(SHA256.HashData(content));
        var existing = await _documentRepository.FirstOrDefaultAsync(d => d.Sha256 == sha256);
        if (existing != null)
        {
            throw new BusinessException(KnowledgeErrorCodes.DuplicateDocument)
                .WithData("documentId", existing.Id);
        }

        var documentId = GuidGenerator.Create();
        var document = new KnowledgeDocument(
            documentId,
            CurrentTenant.Id,
            fileName,
            extension,
            contentType,
            content.LongLength,
            sha256,
            blobName: $"{documentId:N}{extension}");

        var job = new IngestionJob(GuidGenerator.Create(), CurrentTenant.Id, documentId);
        document.AssignJob(job.Id);

        await _documentRepository.InsertAsync(document);
        await _jobRepository.InsertAsync(job);

        // Upload later so it would handle rollback in case of failure
        await _blobContainer.SaveAsync(document.BlobName, content);

        return (document, job);
    }
}
