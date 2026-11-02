namespace Core.AiSoft.Knowledge.Documents;

public static class KnowledgeDocumentConsts
{
    public const int MaxFileNameLength = 256;

    public const int MaxExtensionLength = 16;

    public const int MaxContentTypeLength = 128;

    public const int Sha256Length = 64;

    public const int MaxBlobNameLength = 128;

    public const long DefaultMaxFileSize = 50 * 1024 * 1024;

    public const string AllowedExtensionsForUpload = ".md,.txt,.docx,.pdf";
}
