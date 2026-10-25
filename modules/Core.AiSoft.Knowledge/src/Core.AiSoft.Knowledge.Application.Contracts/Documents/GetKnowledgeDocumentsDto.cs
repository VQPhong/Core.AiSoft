using System;
using Volo.Abp.Application.Dtos;

namespace Core.AiSoft.Knowledge.Documents;

/// <summary>
/// Results are always ordered by upload time, newest first.
/// </summary>
public class GetKnowledgeDocumentsDto : PagedResultRequestDto
{
    /// <summary>
    /// Part of the file name
    /// </summary>
    public string? Filter { get; set; }

    public KnowledgeDocumentStatus? Status { get; set; }

    public DateTime? CreationTimeFrom { get; set; }

    public DateTime? CreationTimeTo { get; set; }
}
