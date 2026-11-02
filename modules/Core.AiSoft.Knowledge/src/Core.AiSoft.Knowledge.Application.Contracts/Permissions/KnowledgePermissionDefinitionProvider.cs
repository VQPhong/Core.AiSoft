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
        documents.AddChild(KnowledgePermissions.Documents.Delete, L("Permission:Documents.Delete"));

        knowledgeGroup.AddPermission(KnowledgePermissions.Chat.Default, L("Permission:Chat"));

        var ingestionJobs = knowledgeGroup.AddPermission(KnowledgePermissions.IngestionJobs.Default, L("Permission:IngestionJobs"));
        var createJob = ingestionJobs.AddChild(KnowledgePermissions.IngestionJobs.Create, L("Permission:IngestionJobs.Create"));
        createJob.AddChild(KnowledgePermissions.IngestionJobs.Force, L("Permission:IngestionJobs.Force"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<KnowledgeResource>(name);
    }
}
