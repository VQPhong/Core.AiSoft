using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;

namespace Core.AiSoft.Knowledge;

[DependsOn(
    typeof(KnowledgeApplicationContractsModule),
    typeof(AbpHttpClientModule))]
public class KnowledgeHttpApiClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(KnowledgeApplicationContractsModule).Assembly,
            KnowledgeRemoteServiceConsts.RemoteServiceName
        );
    }
}
