using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;

namespace Core.AiSoft.Knowledge.Documents;

[Authorize(KnowledgePermissions.Documents.Default)]
public class KnowledgeDocumentAppService : KnowledgeAppService, IKnowledgeDocumentAppService
{
    private readonly KnowledgeDocumentManager _documentManager;
    private readonly KnowledgeFileValidator _fileValidator;
    private readonly IRepository<KnowledgeDocument, Guid> _documentRepository;
    private readonly KnowledgeUploadOptions _uploadOptions;

    public KnowledgeDocumentAppService(
        KnowledgeDocumentManager documentManager,
        KnowledgeFileValidator fileValidator,
        IRepository<KnowledgeDocument, Guid> documentRepository,
        IOptions<KnowledgeUploadOptions> uploadOptions)
    {
        _documentManager = documentManager;
        _fileValidator = fileValidator;
        _documentRepository = documentRepository;
        _uploadOptions = uploadOptions.Value;
    }

    [Authorize(KnowledgePermissions.Documents.Upload)]
    public virtual async Task<KnowledgeDocumentDto> UploadAsync(IRemoteStreamContent file)
    {
        Check.NotNull(file, nameof(file));

        if (file.ContentLength.HasValue)
        {
            _fileValidator.EnsureSize(file.ContentLength.Value);
        }

        var content = await ReadWithLimitAsync(file.GetStream(), _uploadOptions.MaxFileSize);
        var (document, _) = await _documentManager.CreateAsync(Path.GetFileName(file.FileName ?? string.Empty), content);

        return ObjectMapper.Map<KnowledgeDocument, KnowledgeDocumentDto>(document);
    }

    public virtual async Task<KnowledgeDocumentDto> GetAsync(Guid id)
    {
        var document = await _documentRepository.GetAsync(id);
        return ObjectMapper.Map<KnowledgeDocument, KnowledgeDocumentDto>(document);
    }

    public virtual async Task<PagedResultDto<KnowledgeDocumentDto>> GetListAsync(GetKnowledgeDocumentsDto input)
    {
        var filter = input.Filter?.Trim().ToLower();

        var query = (await _documentRepository.GetQueryableAsync())
            .WhereIf(!filter.IsNullOrEmpty(), d => d.FileName.ToLower().Contains(filter!))
            .WhereIf(input.Status.HasValue, d => d.Status == input.Status)
            .WhereIf(input.CreationTimeFrom.HasValue, d => d.CreationTime >= input.CreationTimeFrom)
            .WhereIf(input.CreationTimeTo.HasValue, d => d.CreationTime <= input.CreationTimeTo);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var documents = await AsyncExecuter.ToListAsync(query
            .OrderByDescending(d => d.CreationTime)
            .ThenByDescending(d => d.Id)
            .PageBy(input));

        return new PagedResultDto<KnowledgeDocumentDto>(
            totalCount,
            ObjectMapper.Map<List<KnowledgeDocument>, List<KnowledgeDocumentDto>>(documents));
    }

    protected virtual async Task<byte[]> ReadWithLimitAsync(Stream stream, long maxSize)
    {
        using var buffer = new MemoryStream();
        int chunkSize = 10 * 8 * 1024;              // 10 KB
        byte[] chunk = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(chunk)) > 0)
            {
                if (buffer.Length + read > maxSize)
                {
                    _fileValidator.EnsureSize(buffer.Length + read);
                }

                buffer.Write(chunk, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }

        return buffer.ToArray();
    }
}
