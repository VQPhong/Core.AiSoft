using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Core.AiSoft.Data;
using Volo.Abp.DependencyInjection;

namespace Core.AiSoft.EntityFrameworkCore;

public class EntityFrameworkCoreAiSoftDbSchemaMigrator
    : IAiSoftDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAiSoftDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the AiSoftDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AiSoftDbContext>()
            .Database
            .MigrateAsync();
    }
}
