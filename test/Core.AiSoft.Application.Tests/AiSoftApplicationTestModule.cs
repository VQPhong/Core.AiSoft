using Volo.Abp.Modularity;

namespace Core.AiSoft;

[DependsOn(
    typeof(AiSoftApplicationModule),
    typeof(AiSoftDomainTestModule)
)]
public class AiSoftApplicationTestModule : AbpModule
{

}
