using Core.AiSoft.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Core.AiSoft.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AiSoftController : AbpControllerBase
{
    protected AiSoftController()
    {
        LocalizationResource = typeof(AiSoftResource);
    }
}
