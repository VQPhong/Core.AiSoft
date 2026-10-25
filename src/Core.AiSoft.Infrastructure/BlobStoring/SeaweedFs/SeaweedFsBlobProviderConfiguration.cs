using Volo.Abp.BlobStoring;

namespace Core.AiSoft.BlobStoring.SeaweedFs;

public class SeaweedFsBlobProviderConfiguration
{
    /// <summary>
    /// Filer directory that holds every container, e.g. "aisoft" stores blobs under /aisoft/...
    /// </summary>
    public string RootPath
    {
        get => _containerConfiguration.GetConfigurationOrDefault(SeaweedFsBlobProviderConfigurationNames.RootPath, "aisoft")!;
        set => _containerConfiguration.SetConfiguration(SeaweedFsBlobProviderConfigurationNames.RootPath, value);
    }

    private readonly BlobContainerConfiguration _containerConfiguration;

    public SeaweedFsBlobProviderConfiguration(BlobContainerConfiguration containerConfiguration)
    {
        _containerConfiguration = containerConfiguration;
    }
}

public static class SeaweedFsBlobProviderConfigurationNames
{
    public const string RootPath = "SeaweedFs.RootPath";
}
