using System;
using Volo.Abp.Application.Dtos;

namespace Core.AiSoft.Knowledge.PostIndexing;

public class GetKnowledgePostsDto : PagedResultRequestDto
{
    public Guid? BlogId { get; set; }

    public string? Filter { get; set; }
}
