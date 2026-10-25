using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Core.AiSoft.BlobStoring.SeaweedFs;

/// <summary>
/// Stores blobs through the SeaweedFS Filer HTTP API with a plain HttpClient (no S3 / AWS SDK).
/// Layout: /{RootPath}/{host | tenants/{tenantId}}/{containerName}/{blobName}
/// </summary>
public class SeaweedFsBlobProvider : BlobProviderBase, ITransientDependency
{
    protected IHttpClientFactory HttpClientFactory { get; }
    protected ICurrentTenant CurrentTenant { get; }

    public SeaweedFsBlobProvider(IHttpClientFactory httpClientFactory, ICurrentTenant currentTenant)
    {
        HttpClientFactory = httpClientFactory;
        CurrentTenant = currentTenant;
    }

    public override async Task SaveAsync(BlobProviderSaveArgs args)
    {
        var path = GetBlobPath(args);
        var client = CreateClient();

        if (!args.OverrideExisting && await ExistsAsync(client, path, args.CancellationToken))
        {
            throw new BlobAlreadyExistsException(
                $"Saving BLOB '{args.BlobName}' does already exists in the container '{args.ContainerName}'! Set {nameof(args.OverrideExisting)} if it should be overwritten.");
        }

        // Multipart POST to the full file path; the filer creates missing parent directories and overwrites an existing file.
        using var streamContent = new StreamContent(args.BlobStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var form = new MultipartFormDataContent();
        form.Add(streamContent, "file", GetFileName(args.BlobName));

        using var response = await client.PostAsync(path, form, args.CancellationToken);
        await EnsureSuccessAsync(response, "save", path, args.CancellationToken);
    }

    public override async Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
    {
        var path = GetBlobPath(args);
        var client = CreateClient();

        if (!await ExistsAsync(client, path, args.CancellationToken))
        {
            return false;
        }

        using var response = await client.DeleteAsync(path, args.CancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessAsync(response, "delete", path, args.CancellationToken);
        return true;
    }

    public override Task<bool> ExistsAsync(BlobProviderExistsArgs args)
    {
        return ExistsAsync(CreateClient(), GetBlobPath(args), args.CancellationToken);
    }

    public override async Task<Stream?> GetOrNullAsync(BlobProviderGetArgs args)
    {
        var path = GetBlobPath(args);
        var client = CreateClient();

        var response = await client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, args.CancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        await EnsureSuccessAsync(response, "get", path, args.CancellationToken);

        // Streamed, not buffered: disposing the returned stream releases the connection.
        return await response.Content.ReadAsStreamAsync(args.CancellationToken);
    }

    protected virtual async Task<bool> ExistsAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, path);
        using var response = await client.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessAsync(response, "check", path, cancellationToken);
        return true;
    }

    protected virtual HttpClient CreateClient()
    {
        return HttpClientFactory.CreateClient(SeaweedFsOptions.HttpClientName);
    }

    protected virtual string GetBlobPath(BlobProviderArgs args)
    {
        var configuration = args.Configuration.GetSeaweedFsConfiguration();

        var tenantSegment = CurrentTenant.Id == null
            ? "host"
            : $"tenants/{CurrentTenant.Id.Value:D}";

        var segments = $"{configuration.RootPath}/{tenantSegment}/{args.ContainerName}/{args.BlobName}"
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);

        return "/" + string.Join('/', segments);
    }

    protected virtual string GetFileName(string blobName)
    {
        var fileName = blobName.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return string.IsNullOrEmpty(fileName) ? blobName : fileName;
    }

    protected virtual async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        string path,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = response.Content == null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        throw new SeaweedFsException(
            $"SeaweedFS filer failed to {operation} '{path}': {(int)response.StatusCode} {response.ReasonPhrase}. {body}".Trim());
    }
}
