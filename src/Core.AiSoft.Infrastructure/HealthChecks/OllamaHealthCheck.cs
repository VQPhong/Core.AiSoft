using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Ai;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Core.AiSoft.HealthChecks;

public class OllamaHealthCheck : IHealthCheck
{
    private readonly OllamaClientFactory _clientFactory;

    public OllamaHealthCheck(OllamaClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _clientFactory.Create(OllamaOptions.EmbeddingConnectionName);
            var installed = (await client.ListLocalModelsAsync(cancellationToken)).Select(m => m.Name).ToHashSet();

            var embeddingModel = client.SelectedModel;
            var chatModel = _clientFactory.GetConnection(OllamaOptions.ChatConnectionName).Model;

            if (!installed.Contains(embeddingModel))
            {
                return HealthCheckResult.Unhealthy($"Embedding model {embeddingModel} is not pulled.");
            }

            return installed.Contains(chatModel)
                ? HealthCheckResult.Healthy($"{embeddingModel}, {chatModel}")
                : HealthCheckResult.Degraded($"Chat model {chatModel} is not pulled.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Ollama is unreachable.", ex);
        }
    }
}
