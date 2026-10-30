using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Core.AiSoft.Knowledge.IngestionJobs;

public interface IIngestionJobAppService : IApplicationService
{
    Task<IngestionJobDto> GetAsync(Guid id);

    Task<PagedResultDto<IngestionJobDto>> GetListAsync(GetIngestionJobsDto input);

    Task<IngestionJobDto> CreateAsync(CreateIngestionJobDto input);
}
