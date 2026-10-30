using Volo.Abp.BlobStoring;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.CmsKit;
using Core.AiSoft.Knowledge.PostIndexing;
using Microsoft.Extensions.DependencyInjection;

namespace Core.AiSoft.Knowledge;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(AbpBlobStoringModule),
    typeof(AbpDistributedLockingAbstractionsModule),
    typeof(CmsKitDomainModule),
    typeof(KnowledgeDomainSharedModule)
)]
public class KnowledgeDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<KnowledgeIngestionOptions>(context.Services.GetConfiguration().GetSection("Knowledge:Ingestion"));
    }
}
