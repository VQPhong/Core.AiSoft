using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.AiSoft.Knowledge.Chat;

public class ChatConversationDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public DateTime LastMessageTime { get; set; }
}

public class ChatConversationDetailDto : ChatConversationDto
{
    public List<ChatMessageDto> Messages { get; set; } = [];
}

public class ChatMessageDto
{
    public bool IsUser { get; set; }

    public string Content { get; set; } = default!;

    public DateTime CreationTime { get; set; }

    public bool HasSources { get; set; }

    public List<ChatCitationDto> Citations { get; set; } = [];
}

public class ChatCitationDto
{
    public int Number { get; set; }

    public KnowledgeSourceDto Source { get; set; } = default!;
}

public class KnowledgeSourceDto
{
    public Guid BlogPostId { get; set; }

    public string Title { get; set; } = default!;

    public string Url { get; set; } = default!;

    public int ChunkIndex { get; set; }

    public List<string> HeadingPath { get; set; } = [];

    public string Text { get; set; } = default!;

    public float Score { get; set; }
}

public class SendChatMessageDto
{
    public Guid? ConversationId { get; set; }

    [Required]
    [StringLength(ChatConsts.MaxMessageLength)]
    public string Message { get; set; } = default!;
}

public class ChatReplyDto
{
    public Guid ConversationId { get; set; }

    public ChatMessageDto Question { get; set; } = default!;

    public ChatMessageDto Answer { get; set; } = default!;
}

public class SearchKnowledgeDto
{
    [Required]
    [StringLength(ChatConsts.MaxMessageLength)]
    public string Query { get; set; } = default!;

    [Range(1, 20)]
    public int? TopK { get; set; }
}

public static class ChatConsts
{
    public const int MaxMessageLength = 2000;
}
