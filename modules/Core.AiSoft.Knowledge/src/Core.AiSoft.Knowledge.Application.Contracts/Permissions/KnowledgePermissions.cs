using Volo.Abp.Reflection;

namespace Core.AiSoft.Knowledge.Permissions;

public class KnowledgePermissions
{
    public const string GroupName = "Knowledge";

    public static class Documents
    {
        public const string Default = GroupName + ".Documents";
        public const string Upload = Default + ".Upload";
    }

    public static class IngestionJobs
    {
        public const string Default = GroupName + ".IngestionJobs";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(KnowledgePermissions));
    }
}
