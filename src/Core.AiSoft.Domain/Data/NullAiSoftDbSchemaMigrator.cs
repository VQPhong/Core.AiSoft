using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Core.AiSoft.Data;

/* This is used if database provider does't define
 * IAiSoftDbSchemaMigrator implementation.
 */
public class NullAiSoftDbSchemaMigrator : IAiSoftDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
