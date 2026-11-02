namespace Core.AiSoft.Knowledge.IngestionJobs;

public static class IngestionJobConsts
{
    public const int MaxErrorCodeLength = 128;

    public const int MaxErrorMessageLength = 2048;

    public const int ContentHashLength = 64;

    public const int MaxSnapshotBlobNameLength = 256;

    public static readonly IngestionJobStatus[] ActiveStatuses =
    [
        IngestionJobStatus.Queued,
        IngestionJobStatus.Extracting,
        IngestionJobStatus.Chunking,
        IngestionJobStatus.Embedding,
        IngestionJobStatus.Indexing
    ];
}
