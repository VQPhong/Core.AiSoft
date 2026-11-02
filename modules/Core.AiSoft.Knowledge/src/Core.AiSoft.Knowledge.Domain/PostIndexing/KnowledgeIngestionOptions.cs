using System;

namespace Core.AiSoft.Knowledge.PostIndexing;

public class KnowledgeIngestionOptions
{
    public TimeSpan? DebugProcessingDelay { get; set; }

    public KnowledgeChunkingOptions Chunking { get; set; } = new();
}

public class KnowledgeChunkingOptions
{
    public const int MinMaxLength = 200;

    public int MaxLength { get; set; } = 1200;

    public int OverlapLength { get; set; } = 150;
}
