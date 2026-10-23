using Core.AiSoft.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Core.AiSoft.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AiSoftEntityFrameworkCoreModule),
    typeof(AiSoftApplicationContractsModule)
)]
public class AiSoftDbMigratorModule : AbpModule
{
}
