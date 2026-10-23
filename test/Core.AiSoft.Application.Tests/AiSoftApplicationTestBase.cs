using Volo.Abp.Modularity;

namespace Core.AiSoft;

public abstract class AiSoftApplicationTestBase<TStartupModule> : AiSoftTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
