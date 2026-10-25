using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.BlobStoring.SeaweedFs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Core.AiSoft.HealthChecks;

public class SeaweedFsFilerHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SeaweedFsFilerHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(SeaweedFsOptions.HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Head, "/");
            using var response = await client.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Filer responded {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Filer is unreachable.", ex);
        }
    }
}
