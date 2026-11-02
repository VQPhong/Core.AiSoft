using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OllamaSharp.Models;

namespace Core.AiSoft.Ai;

public class OllamaChatDefaultsClient : DelegatingChatClient
{
    private readonly int _contextLength;

    public OllamaChatDefaultsClient(IChatClient innerClient, int contextLength)
        : base(innerClient)
    {
        _contextLength = contextLength;
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(messages, WithDefaults(options), cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(messages, WithDefaults(options), cancellationToken);

    private ChatOptions WithDefaults(ChatOptions? options)
    {
        var result = options?.Clone() ?? new ChatOptions();
        result.AddOllamaOption(OllamaOption.Think, false);
        result.AddOllamaOption(OllamaOption.NumCtx, _contextLength);
        return result;
    }
}
