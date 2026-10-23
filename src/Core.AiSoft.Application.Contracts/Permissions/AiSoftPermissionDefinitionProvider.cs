using Core.AiSoft.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace Core.AiSoft.Permissions;

public class AiSoftPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AiSoftPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(AiSoftPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AiSoftResource>(name);
    }
}
