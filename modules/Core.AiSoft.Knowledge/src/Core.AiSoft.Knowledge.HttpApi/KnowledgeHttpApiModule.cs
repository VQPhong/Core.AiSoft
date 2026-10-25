using Localization.Resources.AbpUi;
using Core.AiSoft.Knowledge.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.AiSoft.Knowledge;

[DependsOn(
    typeof(KnowledgeApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class KnowledgeHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(KnowledgeHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<KnowledgeResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
