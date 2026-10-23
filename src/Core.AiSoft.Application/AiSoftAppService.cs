using Core.AiSoft.Localization;
using Volo.Abp.Application.Services;

namespace Core.AiSoft;

/* Inherit your application services from this class.
 */
public abstract class AiSoftAppService : ApplicationService
{
    protected AiSoftAppService()
    {
        LocalizationResource = typeof(AiSoftResource);
    }
}
