using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Retrieval;
using Microsoft.Extensions.AI;
using Volo.Abp.Domain.Services;

namespace Core.AiSoft.Knowledge.Chat;

public class KnowledgeChatManager : DomainService
{
    private readonly KnowledgeRetriever _retriever;
    private readonly IChatClient _chatClient;

    public KnowledgeChatManager(KnowledgeRetriever retriever, IChatClient chatClient)
    {
        _retriever = retriever;
        _chatClient = chatClient;
    }

    public virtual async Task<KnowledgeChatAnswer> AnswerAsync(string question, CancellationToken cancellationToken = default)
    {
        var sources = await _retriever.SearchAsync(question, cancellationToken: cancellationToken);
        if (sources.Count == 0)
        {
            return new KnowledgeChatAnswer(null, []);
        }

        var response = await _chatClient.GetResponseAsync(
            KnowledgeChatPrompt.BuildMessages(question, sources),
            new ChatOptions { Temperature = 0.2f, MaxOutputTokens = 1024 },
            cancellationToken);

        var text = KnowledgeChatPrompt.CleanAnswer(response.Text);
        var cited = KnowledgeChatPrompt.GetCitedNumbers(text, sources.Count);

        // Uncited answer: still show what it was based on.
        var citations = (cited.Count > 0 ? cited : Enumerable.Range(1, sources.Count))
            .Select(n => new KnowledgeChatCitation(n, sources[n - 1]))
            .ToList();

        return new KnowledgeChatAnswer(text, citations);
    }
}

public record KnowledgeChatAnswer(string? Text, IReadOnlyList<KnowledgeChatCitation> Citations);

public record KnowledgeChatCitation(int Number, KnowledgeSource Source);
