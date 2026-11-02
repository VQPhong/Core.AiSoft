using System;
using System.Linq;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Permissions;
using Core.AiSoft.Knowledge.Retrieval;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Users;

namespace Core.AiSoft.Knowledge.Chat;

[Authorize(KnowledgePermissions.Chat.Default)]
public class KnowledgeChatAppService : KnowledgeAppService, IKnowledgeChatAppService
{
    private const int MaxTitleLength = 60;

    private readonly KnowledgeChatManager _chatManager;
    private readonly KnowledgeRetriever _retriever;
    private readonly InMemoryChatConversationStore _store;

    public KnowledgeChatAppService(
        KnowledgeChatManager chatManager,
        KnowledgeRetriever retriever,
        InMemoryChatConversationStore store)
    {
        _chatManager = chatManager;
        _retriever = retriever;
        _store = store;
    }

    public virtual Task<ListResultDto<ChatConversationDto>> GetConversationsAsync()
    {
        var items = _store.GetList(CurrentUser.GetId())
            .Select(c => new ChatConversationDto { Id = c.Id, Title = c.Title, LastMessageTime = c.LastMessageTime })
            .ToList();

        return Task.FromResult(new ListResultDto<ChatConversationDto>(items));
    }

    public virtual Task<ChatConversationDetailDto> GetConversationAsync(Guid id)
    {
        var conversation = GetConversation(id);

        return Task.FromResult(new ChatConversationDetailDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            LastMessageTime = conversation.LastMessageTime,
            Messages = conversation.GetMessages().ToList()
        });
    }

    public virtual async Task<ChatReplyDto> SendAsync(SendChatMessageDto input)
    {
        var question = input.Message.Trim();
        var conversation = input.ConversationId.HasValue ? GetConversation(input.ConversationId.Value) : null;

        var questionMessage = new ChatMessageDto { IsUser = true, Content = question, CreationTime = Clock.Now };

        // Stateless: the stored conversation is only for display, the model sees the question alone.
        var answer = await _chatManager.AnswerAsync(question);

        var answerMessage = new ChatMessageDto
        {
            IsUser = false,
            Content = answer.Text ?? L["Chat:NoSourcesFound"],
            CreationTime = Clock.Now,
            HasSources = answer.Citations.Count > 0,
            Citations = answer.Citations.Select(c => new ChatCitationDto { Number = c.Number, Source = ToDto(c.Source) }).ToList()
        };

        conversation ??= _store.Create(CurrentUser.GetId(), GuidGenerator.Create(), ToTitle(question), Clock.Now);
        conversation.Add(questionMessage, answerMessage);

        return new ChatReplyDto { ConversationId = conversation.Id, Question = questionMessage, Answer = answerMessage };
    }

    public virtual Task DeleteConversationAsync(Guid id)
    {
        _store.Delete(CurrentUser.GetId(), id);
        return Task.CompletedTask;
    }

    public virtual async Task<ListResultDto<KnowledgeSourceDto>> SearchAsync(SearchKnowledgeDto input)
    {
        var sources = await _retriever.SearchAsync(input.Query, input.TopK);
        return new ListResultDto<KnowledgeSourceDto>(sources.Select(ToDto).ToList());
    }

    protected virtual ChatConversation GetConversation(Guid id)
        => _store.Find(CurrentUser.GetId(), id) ?? throw new EntityNotFoundException(typeof(ChatConversation), id);

    protected static KnowledgeSourceDto ToDto(KnowledgeSource source) => new()
    {
        BlogPostId = source.BlogPostId,
        Title = source.Title,
        Url = $"/blogs/{KnowledgeBlogConsts.BlogSlug}/{source.Slug}",
        ChunkIndex = source.ChunkIndex,
        HeadingPath = source.HeadingPath.ToList(),
        Text = source.Text,
        Score = source.Score
    };

    private static string ToTitle(string question)
    {
        var line = question.Split('\n')[0].Trim();
        return line.Length <= MaxTitleLength ? line : line[..MaxTitleLength].TrimEnd() + "…";
    }
}
