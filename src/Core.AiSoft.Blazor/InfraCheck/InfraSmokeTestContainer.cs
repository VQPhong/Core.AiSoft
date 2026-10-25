using Volo.Abp.BlobStoring;

namespace Core.AiSoft.Blazor.InfraCheck;

/// <summary>
/// Blob container used only by the development infra check; mapped to SeaweedFS.
/// </summary>
[BlobContainerName("infra-smoke-test")]
public class InfraSmokeTestContainer
{
}
