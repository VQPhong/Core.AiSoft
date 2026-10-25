using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;

namespace Core.AiSoft.Knowledge;

[DependsOn(
    typeof(KnowledgeDomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule)
    )]
public class KnowledgeApplicationContractsModule : AbpModule
{

}
