using Core.AiSoft.Knowledge;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.TenantManagement;
using Volo.CmsKit.Admin;
using Volo.CmsKit.Public;

namespace Core.AiSoft;

[DependsOn(
    typeof(KnowledgeApplicationModule),
    typeof(AiSoftDomainModule),
    typeof(AiSoftApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
[DependsOn(typeof(CmsKitAdminApplicationModule), typeof(CmsKitPublicApplicationModule))]
public class AiSoftApplicationModule : AbpModule
{

}
