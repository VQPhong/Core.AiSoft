using Core.AiSoft.Localization;
using Volo.Abp.AspNetCore.Components;

namespace Core.AiSoft.Blazor.Client;

public abstract class AiSoftComponentBase : AbpComponentBase
{
    protected AiSoftComponentBase()
    {
        LocalizationResource = typeof(AiSoftResource);
    }
}
