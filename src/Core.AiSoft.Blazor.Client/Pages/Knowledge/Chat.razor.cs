using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Chat;
using Core.AiSoft.Knowledge.Localization;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Radzen;

namespace Core.AiSoft.Blazor.Client.Pages.Knowledge;

/// <summary>
/// Simple RAG chat: one request per question (no streaming), answers rendered as Markdown with their sources.
/// </summary>
public partial class Chat
{
    // Raw HTML from the model is escaped, not rendered.
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    [Inject] protected IKnowledgeChatAppService ChatAppService { get; set; } = default!;
    [Inject] protected IStringLocalizer<KnowledgeResource> KL { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;

    protected List<ChatConversationDto> Conversations { get; set; } = [];
    protected List<ChatMessageDto> Messages { get; set; } = [];
    protected Guid? ConversationId { get; set; }
    protected string? Input { get; set; }
    protected bool IsSending { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadConversationsAsync();
    }

    protected virtual async Task LoadConversationsAsync()
    {
        try
        {
            Conversations = (await ChatAppService.GetConversationsAsync()).Items.ToList();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    protected virtual void NewConversation()
    {
        ConversationId = null;
        Messages = [];
        Input = null;
    }

    protected virtual async Task OpenConversationAsync(Guid id)
    {
        if (IsSending)
        {
            return;
        }

        try
        {
            var conversation = await ChatAppService.GetConversationAsync(id);
            ConversationId = conversation.Id;
            Messages = conversation.Messages;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
            await LoadConversationsAsync();
        }
    }

    protected virtual async Task DeleteConversationAsync(Guid id)
    {
        if (await DialogService.Confirm(KL["DeleteConversation"] + "?", KL["DeleteConversation"]) != true)
        {
            return;
        }

        try
        {
            await ChatAppService.DeleteConversationAsync(id);
            if (ConversationId == id)
            {
                NewConversation();
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }

        await LoadConversationsAsync();
    }

    protected virtual async Task SendAsync()
    {
        var question = Input?.Trim();
        if (IsSending || string.IsNullOrEmpty(question))
        {
            return;
        }

        // Shown right away; replaced by the stored pair when the answer arrives.
        var pending = new ChatMessageDto { IsUser = true, Content = question, CreationTime = DateTime.Now };
        Messages.Add(pending);
        Input = null;
        IsSending = true;

        try
        {
            var reply = await ChatAppService.SendAsync(new SendChatMessageDto { ConversationId = ConversationId, Message = question });

            Messages.Remove(pending);
            Messages.Add(reply.Question);
            Messages.Add(reply.Answer);

            ConversationId = reply.ConversationId;
            await LoadConversationsAsync();
        }
        catch (Exception ex)
        {
            Messages.Remove(pending);
            Input = question;
            await HandleErrorAsync(ex);
        }
        finally
        {
            IsSending = false;
        }
    }

    protected static MarkupString RenderMarkdown(string content)
        => new(Markdown.ToHtml(content, MarkdownPipeline));
}
