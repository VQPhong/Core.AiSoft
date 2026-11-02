namespace Core.AiSoft.Knowledge;

public static class KnowledgeDbProperties
{
    public static string DbTablePrefix { get; set; } = "Knowledge";

    public static string? DbSchema { get; set; } = null;

    public const string ConnectionStringName = "Knowledge";
}
