using Volo.Abp;

namespace Core.AiSoft.BlobStoring.SeaweedFs;

public class SeaweedFsException : AbpException
{
    public SeaweedFsException(string message)
        : base(message)
    {
    }
}
