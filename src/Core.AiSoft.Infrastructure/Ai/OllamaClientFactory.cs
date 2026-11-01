using System;
using System.Data.Common;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Core.AiSoft.Ai;

public class OllamaClientFactory : ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public OllamaClientFactory(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public virtual OllamaApiClient Create(string connectionName)
    {
        var (endpoint, model) = GetConnection(connectionName);

        var httpClient = _httpClientFactory.CreateClient(OllamaOptions.HttpClientName);
        httpClient.BaseAddress = endpoint;

        return new OllamaApiClient(httpClient, model);
    }

    public virtual (Uri Endpoint, string Model) GetConnection(string connectionName)
    {
        var connectionString = _configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new AbpException(
                $"ConnectionStrings:{connectionName} is not configured. Run the application through Core.AiSoft.AppHost.");
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        if (!builder.TryGetValue("Endpoint", out var endpoint) || !builder.TryGetValue("Model", out var model))
        {
            throw new AbpException($"ConnectionStrings:{connectionName} must contain Endpoint and Model.");
        }

        return (new Uri(endpoint.ToString()!), model.ToString()!);
    }
}
