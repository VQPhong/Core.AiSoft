using Volo.Abp.BlobStoring;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.CmsKit;
using Core.AiSoft.Knowledge.PostIndexing;
using Core.AiSoft.Knowledge.Retrieval;
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
        var configuration = context.Services.GetConfiguration();
        Configure<KnowledgeIngestionOptions>(configuration.GetSection("Knowledge:Ingestion"));
        Configure<KnowledgeRetrievalOptions>(configuration.GetSection("Knowledge:Retrieval"));
    }
}
