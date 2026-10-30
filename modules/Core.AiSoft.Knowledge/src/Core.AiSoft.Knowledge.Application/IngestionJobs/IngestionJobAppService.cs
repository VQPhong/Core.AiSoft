using System;
using System.Linq;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Permissions;
using Core.AiSoft.Knowledge.PostIndexing;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Core.AiSoft.Knowledge.IngestionJobs;

[Authorize(KnowledgePermissions.IngestionJobs.Default)]
public class IngestionJobAppService : KnowledgeAppService, IIngestionJobAppService
{
    private readonly IRepository<IngestionJob, Guid> _jobRepository;
    private readonly PostIngestionManager _postIngestionManager;

    public IngestionJobAppService(
        IRepository<IngestionJob, Guid> jobRepository,
        PostIngestionManager postIngestionManager)
    {
        _jobRepository = jobRepository;
        _postIngestionManager = postIngestionManager;
    }

    public virtual async Task<IngestionJobDto> GetAsync(Guid id)
    {
        return ToDto(await _jobRepository.GetAsync(id));
    }

    public virtual async Task<PagedResultDto<IngestionJobDto>> GetListAsync(GetIngestionJobsDto input)
    {
        var query = (await _jobRepository.GetQueryableAsync())
            .WhereIf(input.BlogPostId.HasValue, j => j.BlogPostId == input.BlogPostId)
            .WhereIf(input.Status.HasValue, j => j.Status == input.Status)
            .WhereIf(input.CreationTimeFrom.HasValue, j => j.CreationTime >= input.CreationTimeFrom)
            .WhereIf(input.CreationTimeTo.HasValue, j => j.CreationTime <= input.CreationTimeTo);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var jobs = await AsyncExecuter.ToListAsync(query
            .OrderByDescending(j => j.CreationTime)
            .ThenByDescending(j => j.Id)
            .PageBy(input));

        return new PagedResultDto<IngestionJobDto>(totalCount, jobs.Select(ToDto).ToList());
    }

    [Authorize(KnowledgePermissions.IngestionJobs.Create)]
    public virtual async Task<IngestionJobDto> CreateAsync(CreateIngestionJobDto input)
    {
        if (input.Force)
        {
            await AuthorizationService.CheckAsync(KnowledgePermissions.IngestionJobs.Force);
        }

        var job = await _postIngestionManager.EnqueueAsync(input.BlogPostId, input.Force);

        try
        {
            await CurrentUnitOfWork!.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is not BusinessException)
        {
            var activeJobId = await FindActiveJobIdAsync(input.BlogPostId);
            if (activeJobId == null)
            {
                throw;
            }

            throw new BusinessException(KnowledgeErrorCodes.IngestionAlreadyRunning).WithData("jobId", activeJobId.Value);
        }

        return ToDto(job);
    }

    protected virtual async Task<Guid?> FindActiveJobIdAsync(Guid blogPostId)
    {
        using var uow = UnitOfWorkManager.Begin(requiresNew: true, isTransactional: false);

        var query = (await _jobRepository.GetQueryableAsync())
            .Where(j => j.BlogPostId == blogPostId && IngestionJobConsts.ActiveStatuses.Contains(j.Status))
            .Select(j => (Guid?)j.Id);

        return await AsyncExecuter.FirstOrDefaultAsync(query);
    }

    protected virtual IngestionJobDto ToDto(IngestionJob job)
    {
        return new IngestionJobDto
        {
            Id = job.Id,
            BlogPostId = job.BlogPostId,
            Version = job.Version,
            ContentHash = job.ContentHash,
            IsForced = job.IsForced,
            DocumentId = job.DocumentId,
            Status = job.Status,
            SkipReason = job.SkipReason,
            AttemptCount = job.AttemptCount,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            DurationMs = job.StartedAt.HasValue && job.FinishedAt.HasValue
                ? (long)(job.FinishedAt.Value - job.StartedAt.Value).TotalMilliseconds
                : null,
            ChunkCount = job.ChunkCount,
            ErrorCode = job.ErrorCode,
            ErrorMessage = job.ErrorMessage,
            CreationTime = job.CreationTime
        };
    }
}
