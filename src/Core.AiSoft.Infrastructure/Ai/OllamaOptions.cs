using System;

namespace Core.AiSoft.Ai;

public class OllamaOptions
{
    public const string HttpClientName = "Ollama";
    public const string EmbeddingConnectionName = "embedding-model";
    public const string ChatConnectionName = "chat-model";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
