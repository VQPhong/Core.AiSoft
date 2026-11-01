using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;
using Qdrant.Client;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.BlobStoring;
using Volo.Abp.EventBus.Distributed;

namespace Core.AiSoft.Blazor.InfraCheck;

/// <summary>
/// Dev-only.
/// TODO: Remove this after we're done.
/// </summary>
[Authorize]
[RemoteService(Name = "InfraCheck")]
[Area("dev")]
[Route("api/dev/infra-check")]
public class InfraCheckController : AbpControllerBase
{
    private readonly IBlobContainer<InfraSmokeTestContainer> _blobContainer;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly InfraPingTracker _pingTracker;
    private readonly QdrantClient _qdrantClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IChatClient _chatClient;

    public InfraCheckController(
        IBlobContainer<InfraSmokeTestContainer> blobContainer,
        IDistributedEventBus distributedEventBus,
        InfraPingTracker pingTracker,
        QdrantClient qdrantClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IChatClient chatClient)
    {
        _blobContainer = blobContainer;
        _distributedEventBus = distributedEventBus;
        _pingTracker = pingTracker;
        _qdrantClient = qdrantClient;
        _embeddingGenerator = embeddingGenerator;
        _chatClient = chatClient;
    }

    /// <summary>
    /// Save -> exists -> read back -> duplicate-save guard -> delete. Pass keep=true to leave the file in the filer for inspection.
    /// Stops at the first failed step; the test file is always removed when a step fails.
    /// </summary>
    [HttpPost("blob")]
    public async Task<InfraCheckResultDto> CheckBlobAsync(bool keep = false)
    {
        var result = new InfraCheckResultDto { Target = "seaweedfs" };
        var blobName = $"checks/{Guid.NewGuid():N}.txt";
        var content = $"infra check {Clock.Now:O}";
        var needsCleanup = false;

        try
        {
            if (!await RunStepAsync(result, "save", async () =>
                {
                    await _blobContainer.SaveAsync(blobName, Encoding.UTF8.GetBytes(content));
                    needsCleanup = true;
                    return Pass(blobName);
                }))
            {
                return result;
            }

            if (!await RunStepAsync(result, "exists", async () =>
                    await _blobContainer.ExistsAsync(blobName)
                        ? Pass()
                        : Fail("ExistsAsync returned false after save.")))
            {
                return result;
            }

            if (!await RunStepAsync(result, "read back", async () =>
                    Encoding.UTF8.GetString(await _blobContainer.GetAllBytesAsync(blobName)) == content
                        ? Pass()
                        : Fail("Content read back does not match what was saved.")))
            {
                return result;
            }

            if (!await RunStepAsync(result, "reject duplicate save", async () =>
                {
                    try
                    {
                        await _blobContainer.SaveAsync(blobName, Encoding.UTF8.GetBytes("overwrite"), overrideExisting: false);
                        return Fail("Saving an existing blob without overrideExisting did not throw.");
                    }
                    catch (BlobAlreadyExistsException)
                    {
                        return Pass();
                    }
                }))
            {
                return result;
            }

            if (keep)
            {
                needsCleanup = false;
                AddStep(result, "delete", Pass("skipped (keep = true)"));
                return result;
            }

            if (!await RunStepAsync(result, "delete", async () =>
                    await _blobContainer.DeleteAsync(blobName)
                        ? Pass()
                        : Fail("DeleteAsync returned false for an existing blob.")))
            {
                return result;
            }
            needsCleanup = false;

            await RunStepAsync(result, "gone after delete", async () =>
                await _blobContainer.ExistsAsync(blobName)
                    ? Fail("ExistsAsync still returns true after delete.")
                    : Pass());

            await RunStepAsync(result, "delete missing returns false", async () =>
                await _blobContainer.DeleteAsync(blobName)
                    ? Fail("DeleteAsync returned true for a missing blob.")
                    : Pass());

            return result;
        }
        finally
        {
            if (needsCleanup)
            {
                await TryDeleteBlobAsync(blobName);
            }
        }
    }

    /// <summary>
    /// Publishes a ping through the outbox. It reaches RabbitMQ after this request's unit of work commits,
    /// so poll GET event/{pingId} for the result.
    /// </summary>
    [HttpPost("event")]
    public async Task<InfraPingStatusDto> PublishPingAsync()
    {
        var pingId = Guid.NewGuid();
        await _distributedEventBus.PublishAsync(new InfraPingEto { PingId = pingId, SentAt = Clock.Now });

        return new InfraPingStatusDto { PingId = pingId, Received = false };
    }

    [HttpGet("event/{pingId}")]
    public InfraPingStatusDto GetPingStatus(Guid pingId)
    {
        var receivedAt = _pingTracker.GetReceivedAt(pingId);

        return new InfraPingStatusDto
        {
            PingId = pingId,
            Received = receivedAt.HasValue,
            ReceivedAt = receivedAt
        };
    }

    [HttpGet("qdrant")]
    public async Task<InfraCheckResultDto> CheckQdrantAsync()
    {
        var result = new InfraCheckResultDto { Target = "qdrant" };

        if (!await RunStepAsync(result, "health", async () =>
            {
                var health = await _qdrantClient.HealthAsync();
                return Pass($"{health.Title} {health.Version}");
            }))
        {
            return result;
        }

        await RunStepAsync(result, "list collections", async () =>
        {
            var collections = await _qdrantClient.ListCollectionsAsync();
            return Pass(collections.Count == 0 ? "no collections" : string.Join(", ", collections));
        });

        return result;
    }

    [HttpGet("ollama")]
    public async Task<InfraCheckResultDto> CheckOllamaAsync()
    {
        var result = new InfraCheckResultDto { Target = "ollama" };

        await RunStepAsync(result, "embed", async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var embedding = await _embeddingGenerator.GenerateAsync("Kho tri thức nội bộ của công ty.");
            return Pass($"{embedding.ModelId}: {embedding.Vector.Length} dimensions in {stopwatch.ElapsedMilliseconds} ms");
        });

        await RunStepAsync(result, "chat", async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var options = new ChatOptions { MaxOutputTokens = 64 };
            options.AddOllamaOption(OllamaOption.Think, false);

            var response = await _chatClient.GetResponseAsync("Trả lời bằng một câu ngắn: Thủ đô của Việt Nam là gì?", options);
            return Pass($"{response.ModelId} in {stopwatch.ElapsedMilliseconds} ms: {response.Text.Trim()}");
        });

        return result;
    }

    private async Task<bool> RunStepAsync(
        InfraCheckResultDto result,
        string name,
        Func<Task<(bool Success, string? Detail)>> step)
    {
        (bool Success, string? Detail) outcome;

        try
        {
            outcome = await step();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Infra check {Target} step '{Step}' threw", result.Target, name);
            outcome = Fail($"{ex.GetType().Name}: {ex.Message}");
        }

        AddStep(result, name, outcome);

        if (!outcome.Success)
        {
            Logger.LogWarning("Infra check {Target} step '{Step}' failed: {Detail}", result.Target, name, outcome.Detail);
        }

        return outcome.Success;
    }

    private static (bool Success, string? Detail) Pass(string? detail = null) => (true, detail);

    private static (bool Success, string? Detail) Fail(string detail) => (false, detail);

    private static void AddStep(InfraCheckResultDto result, string name, (bool Success, string? Detail) outcome)
    {
        result.Steps.Add(new InfraCheckStepDto { Name = name, Success = outcome.Success, Detail = outcome.Detail });
    }

    private async Task TryDeleteBlobAsync(string blobName)
    {
        try
        {
            await _blobContainer.DeleteAsync(blobName);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Infra check could not clean up test blob {BlobName}", blobName);
        }
    }
}
