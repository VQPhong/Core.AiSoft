using System;
using Volo.Abp.BlobStoring;

namespace Core.AiSoft.BlobStoring.SeaweedFs;

public static class SeaweedFsBlobContainerConfigurationExtensions
{
    public static SeaweedFsBlobProviderConfiguration GetSeaweedFsConfiguration(
        this BlobContainerConfiguration containerConfiguration)
    {
        return new SeaweedFsBlobProviderConfiguration(containerConfiguration);
    }

    public static BlobContainerConfiguration UseSeaweedFs(
        this BlobContainerConfiguration containerConfiguration,
        Action<SeaweedFsBlobProviderConfiguration>? configureAction = null)
    {
        containerConfiguration.ProviderType = typeof(SeaweedFsBlobProvider);

        configureAction?.Invoke(new SeaweedFsBlobProviderConfiguration(containerConfiguration));

        return containerConfiguration;
    }
}
