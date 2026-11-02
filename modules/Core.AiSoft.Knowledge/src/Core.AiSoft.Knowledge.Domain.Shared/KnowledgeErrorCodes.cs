namespace Core.AiSoft.Knowledge;

public static class KnowledgeErrorCodes
{
    public const string FileTypeNotAllowed = "Knowledge:FileTypeNotAllowed";

    public const string FileContentMismatch = "Knowledge:FileContentMismatch";

    public const string FileTooLarge = "Knowledge:FileTooLarge";

    public const string EmptyFile = "Knowledge:EmptyFile";

    public const string DuplicateDocument = "Knowledge:DuplicateDocument";

    public const string BlobNotFound = "Knowledge:BlobNotFound";

    public const string ExtractorNotFound = "Knowledge:ExtractorNotFound";

    public const string FileUnreadable = "Knowledge:FileUnreadable";

    public const string NoTextExtracted = "Knowledge:NoTextExtracted";

    public const string PdfHasNoTextLayer = "Knowledge:PdfHasNoTextLayer";

    public const string PostNotFound = "Knowledge:PostNotFound";

    public const string PostNotInKnowledgeBlog = "Knowledge:PostNotInKnowledgeBlog";

    public const string PostNotPublished = "Knowledge:PostNotPublished";

    public const string IngestionAlreadyRunning = "Knowledge:IngestionAlreadyRunning";

    public const string ContentUnchanged = "Knowledge:ContentUnchanged";

    public const string SnapshotNotFound = "Knowledge:SnapshotNotFound";
}
