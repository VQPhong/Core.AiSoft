using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace Core.AiSoft.Knowledge.Documents;

public interface IKnowledgeDocumentAppService : IApplicationService
{
    Task<KnowledgeDocumentDto> UploadAsync(IRemoteStreamContent file);

    Task<KnowledgeDocumentDto> GetAsync(Guid id);

    Task<PagedResultDto<KnowledgeDocumentDto>> GetListAsync(GetKnowledgeDocumentsDto input);
}
