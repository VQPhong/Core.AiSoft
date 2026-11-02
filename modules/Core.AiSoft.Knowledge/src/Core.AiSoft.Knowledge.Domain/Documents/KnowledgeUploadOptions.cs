using System.Collections.Generic;

namespace Core.AiSoft.Knowledge.Documents;

public class KnowledgeUploadOptions
{
    public Dictionary<string, string> AllowedExtensions { get; } = new()
    {
        [".md"] = "text/markdown",
        [".txt"] = "text/plain",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".pdf"] = "application/pdf"
    };

    public long MaxFileSize { get; set; } = KnowledgeDocumentConsts.DefaultMaxFileSize;
}
