namespace Core.AiSoft.Knowledge.IngestionJobs;

public enum IngestionJobSkipReason
{
    /// <summary>A newer version of the blog post was indexed or queued.</summary>
    Superseded = 1,

    /// <summary>The same content was already indexed (no forced).</summary>
    Unchanged = 2
}
