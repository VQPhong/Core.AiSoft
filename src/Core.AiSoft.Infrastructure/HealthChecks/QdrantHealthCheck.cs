using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qdrant.Client;

namespace Core.AiSoft.HealthChecks;

public class QdrantHealthCheck : IHealthCheck
{
    private readonly QdrantClient _qdrantClient;

    public QdrantHealthCheck(QdrantClient qdrantClient)
    {
        _qdrantClient = qdrantClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var reply = await _qdrantClient.HealthAsync(cancellationToken);
            return HealthCheckResult.Healthy($"Qdrant {reply.Version}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Qdrant is unreachable.", ex);
        }
    }
}
