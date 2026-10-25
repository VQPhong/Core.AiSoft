using Core.AiSoft.Knowledge.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Core.AiSoft.Knowledge.Permissions;

public class KnowledgePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var knowledgeGroup = context.AddGroup(KnowledgePermissions.GroupName, L("Permission:Knowledge"));

        var documents = knowledgeGroup.AddPermission(KnowledgePermissions.Documents.Default, L("Permission:Documents"));
        documents.AddChild(KnowledgePermissions.Documents.Upload, L("Permission:Documents.Upload"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<KnowledgeResource>(name);
    }
}
