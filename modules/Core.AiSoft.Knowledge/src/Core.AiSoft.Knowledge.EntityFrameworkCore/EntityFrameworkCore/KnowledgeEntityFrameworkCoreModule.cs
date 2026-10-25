using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Core.AiSoft.Knowledge.EntityFrameworkCore;

[DependsOn(
    typeof(KnowledgeDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class KnowledgeEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<KnowledgeDbContext>(options =>
        {
            options.AddDefaultRepositories<IKnowledgeDbContext>(includeAllEntities: true);
            
            /* Add custom repositories here. Example:
            * options.AddRepository<Question, EfCoreQuestionRepository>();
            */
        });
    }
}
