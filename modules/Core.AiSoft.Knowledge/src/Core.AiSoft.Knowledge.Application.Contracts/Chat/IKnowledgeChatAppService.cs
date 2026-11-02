using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Core.AiSoft.Knowledge.Chat;

public interface IKnowledgeChatAppService : IApplicationService
{
    Task<ListResultDto<ChatConversationDto>> GetConversationsAsync();

    Task<ChatConversationDetailDto> GetConversationAsync(Guid id);

    Task<ChatReplyDto> SendAsync(SendChatMessageDto input);

    Task DeleteConversationAsync(Guid id);

    Task<ListResultDto<KnowledgeSourceDto>> SearchAsync(SearchKnowledgeDto input);
}
