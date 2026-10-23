using System.Threading.Tasks;

namespace Core.AiSoft.Data;

public interface IAiSoftDbSchemaMigrator
{
    Task MigrateAsync();
}
