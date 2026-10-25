using Core.AiSoft.Knowledge.Documents;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Core.AiSoft.Knowledge;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class KnowledgeDocumentToKnowledgeDocumentDtoMapper : MapperBase<KnowledgeDocument, KnowledgeDocumentDto>
{
    public override partial KnowledgeDocumentDto Map(KnowledgeDocument source);

    public override partial void Map(KnowledgeDocument source, KnowledgeDocumentDto destination);
}
