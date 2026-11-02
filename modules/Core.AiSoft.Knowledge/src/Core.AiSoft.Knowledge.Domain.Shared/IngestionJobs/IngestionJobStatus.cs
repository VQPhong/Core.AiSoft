namespace Core.AiSoft.Knowledge.IngestionJobs;

public enum IngestionJobStatus
{
    Queued = 0,
    Extracting = 1,
    Chunking = 2,
    Embedding = 3,
    Indexing = 4,
    Succeeded = 5,
    Failed = 6,
    Skipped = 7
}
