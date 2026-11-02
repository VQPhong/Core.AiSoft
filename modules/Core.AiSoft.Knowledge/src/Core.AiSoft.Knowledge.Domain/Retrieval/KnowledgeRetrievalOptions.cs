namespace Core.AiSoft.Knowledge.Retrieval;

public class KnowledgeRetrievalOptions
{
    public int TopK { get; set; } = 5;

    public const int MaxTopK = 20;

    public float MinScore { get; set; } = 0.45f;
}
