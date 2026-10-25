using Core.AiSoft.Knowledge.Localization;
using Volo.Abp.Application.Services;

namespace Core.AiSoft.Knowledge;

public abstract class KnowledgeAppService : ApplicationService
{
    protected KnowledgeAppService()
    {
        LocalizationResource = typeof(KnowledgeResource);
        ObjectMapperContext = typeof(KnowledgeApplicationModule);
    }
}
