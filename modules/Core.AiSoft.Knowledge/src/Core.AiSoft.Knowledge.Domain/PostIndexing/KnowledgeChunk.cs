using System;
using System.Collections.Generic;

namespace Core.AiSoft.Knowledge.PostIndexing;

public record KnowledgeChunk(int Index, IReadOnlyList<string> HeadingPath, string Text, int? PageNumber = null);

public record KnowledgePostChunks(Guid BlogPostId, Guid? TenantId, int Version, IReadOnlyList<KnowledgeChunk> Chunks);
