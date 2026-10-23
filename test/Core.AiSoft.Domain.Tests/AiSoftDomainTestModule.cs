using Volo.Abp.Modularity;

namespace Core.AiSoft;

[DependsOn(
    typeof(AiSoftDomainModule),
    typeof(AiSoftTestBaseModule)
)]
public class AiSoftDomainTestModule : AbpModule
{

}
