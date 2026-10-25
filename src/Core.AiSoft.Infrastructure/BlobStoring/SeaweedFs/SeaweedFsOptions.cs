using System;

namespace Core.AiSoft.BlobStoring.SeaweedFs;

/// <summary>
/// Connection settings for the SeaweedFS Filer HTTP API, bound from the "SeaweedFs" configuration section.
/// </summary>
public class SeaweedFsOptions
{
    public const string HttpClientName = "SeaweedFsFiler";

    /// <summary>
    /// Base URL of the filer.
    /// </summary>
    public string? FilerUrl { get; set; }


    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
