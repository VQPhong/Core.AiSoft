using Volo.Abp.Modularity;

namespace Core.AiSoft;

/* Inherit from this class for your domain layer tests. */
public abstract class AiSoftDomainTestBase<TStartupModule> : AiSoftTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
