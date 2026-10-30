using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Core.AiSoft.Knowledge.PostIndexing;

public interface IKnowledgePostAppService : IApplicationService
{
    Task<PagedResultDto<KnowledgePostDto>> GetListAsync(GetKnowledgePostsDto input);

    Task DeleteAsync(Guid id);
}
