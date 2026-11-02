using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace Core.AiSoft.Knowledge.Chat;

/// <summary>
/// TODO: replace with a database table.
/// </summary>
public class InMemoryChatConversationStore : ISingletonDependency
{
    public const int MaxConversationsPerUser = 20;
    public const int MaxMessagesPerConversation = 50;

    private readonly ConcurrentDictionary<Guid, List<ChatConversation>> _conversations = new();

    public virtual IReadOnlyList<ChatConversation> GetList(Guid userId)
    {
        var list = GetUserList(userId);
        lock (list)
        {
            return list.OrderByDescending(c => c.LastMessageTime).ToList();
        }
    }

    public virtual ChatConversation? Find(Guid userId, Guid id)
    {
        var list = GetUserList(userId);
        lock (list)
        {
            return list.FirstOrDefault(c => c.Id == id);
        }
    }

    public virtual ChatConversation Create(Guid userId, Guid id, string title, DateTime now)
    {
        var conversation = new ChatConversation(id, title, now);
        var list = GetUserList(userId);
        lock (list)
        {
            list.Add(conversation);
            if (list.Count > MaxConversationsPerUser)
            {
                list.Remove(list.MinBy(c => c.LastMessageTime)!);
            }
        }

        return conversation;
    }

    public virtual void Delete(Guid userId, Guid id)
    {
        var list = GetUserList(userId);
        lock (list)
        {
            list.RemoveAll(c => c.Id == id);
        }
    }

    private List<ChatConversation> GetUserList(Guid userId) => _conversations.GetOrAdd(userId, _ => []);
}

public class ChatConversation
{
    private readonly List<ChatMessageDto> _messages = [];

    public Guid Id { get; }

    public string Title { get; }

    public DateTime LastMessageTime { get; private set; }

    public ChatConversation(Guid id, string title, DateTime creationTime)
    {
        Id = id;
        Title = title;
        LastMessageTime = creationTime;
    }

    public IReadOnlyList<ChatMessageDto> GetMessages()
    {
        lock (_messages)
        {
            return _messages.ToList();
        }
    }

    public void Add(ChatMessageDto question, ChatMessageDto answer)
    {
        lock (_messages)
        {
            _messages.Add(question);
            _messages.Add(answer);
            if (_messages.Count > InMemoryChatConversationStore.MaxMessagesPerConversation)
            {
                _messages.RemoveRange(0, _messages.Count - InMemoryChatConversationStore.MaxMessagesPerConversation);
            }

            LastMessageTime = answer.CreationTime;
        }
    }
}
