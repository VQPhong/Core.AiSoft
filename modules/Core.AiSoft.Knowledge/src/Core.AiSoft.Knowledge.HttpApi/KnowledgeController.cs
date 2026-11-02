using Core.AiSoft.Knowledge.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Core.AiSoft.Knowledge;

public abstract class KnowledgeController : AbpControllerBase
{
    protected KnowledgeController()
    {
        LocalizationResource = typeof(KnowledgeResource);
    }
}
