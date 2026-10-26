using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Documents;
using Core.AiSoft.Knowledge.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace Core.AiSoft.Knowledge.IngestionJobs;

[Authorize(KnowledgePermissions.IngestionJobs.Default)]
public class IngestionJobAppService : KnowledgeAppService, IIngestionJobAppService
{
    private readonly IRepository<IngestionJob, Guid> _jobRepository;
    private readonly IRepository<KnowledgeDocument, Guid> _documentRepository;

    public IngestionJobAppService(
        IRepository<IngestionJob, Guid> jobRepository,
        IRepository<KnowledgeDocument, Guid> documentRepository)
    {
        _jobRepository = jobRepository;
        _documentRepository = documentRepository;
    }

    public virtual async Task<IngestionJobDto> GetAsync(Guid id)
    {
        var query = (await CreateQueryAsync()).Where(j => j.Id == id);

        var job = await AsyncExecuter.FirstOrDefaultAsync(query)
                  ?? throw new EntityNotFoundException(typeof(IngestionJob), id);

        return WithDuration(job);
    }

    public virtual async Task<PagedResultDto<IngestionJobDto>> GetListAsync(GetIngestionJobsDto input)
    {
        var filter = input.Filter?.Trim().ToLower();

        var query = (await CreateQueryAsync())
            .WhereIf(!filter.IsNullOrEmpty(), j => j.FileName.ToLower().Contains(filter!))
            .WhereIf(input.Status.HasValue, j => j.Status == input.Status)
            .WhereIf(input.DocumentId.HasValue, j => j.DocumentId == input.DocumentId)
            .WhereIf(input.CreationTimeFrom.HasValue, j => j.CreationTime >= input.CreationTimeFrom)
            .WhereIf(input.CreationTimeTo.HasValue, j => j.CreationTime <= input.CreationTimeTo);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var jobs = await AsyncExecuter.ToListAsync(query
            .OrderByDescending(j => j.CreationTime)
            .ThenByDescending(j => j.Id)
            .PageBy(input));

        return new PagedResultDto<IngestionJobDto>(totalCount, jobs.Select(WithDuration).ToList());
    }

    protected virtual async Task<IQueryable<IngestionJobDto>> CreateQueryAsync()
    {
        var jobs = await _jobRepository.GetQueryableAsync();
        var documents = await _documentRepository.GetQueryableAsync();

        return from job in jobs
            join document in documents on job.DocumentId equals document.Id
            select new IngestionJobDto
            {
                Id = job.Id,
                DocumentId = job.DocumentId,
                FileName = document.FileName,
                Status = job.Status,
                AttemptCount = job.AttemptCount,
                StartedAt = job.StartedAt,
                FinishedAt = job.FinishedAt,
                ChunkCount = job.ChunkCount,
                ErrorCode = job.ErrorCode,
                ErrorMessage = job.ErrorMessage,
                CreationTime = job.CreationTime
            };
    }

    private static IngestionJobDto WithDuration(IngestionJobDto job)
    {
        if (job.StartedAt.HasValue && job.FinishedAt.HasValue)
        {
            job.DurationMs = (long)(job.FinishedAt.Value - job.StartedAt.Value).TotalMilliseconds;
        }

        return job;
    }
}
