using Volo.Abp.BlobStoring;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Core.AiSoft.Knowledge;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(AbpBlobStoringModule),
    typeof(KnowledgeDomainSharedModule)
)]
public class KnowledgeDomainModule : AbpModule
{

}
