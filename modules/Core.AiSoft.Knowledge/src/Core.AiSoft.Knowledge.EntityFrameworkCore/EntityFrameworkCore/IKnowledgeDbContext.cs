using Core.AiSoft.Knowledge.Documents;
using Core.AiSoft.Knowledge.IngestionJobs;
using Core.AiSoft.Knowledge.PostIndexing;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Core.AiSoft.Knowledge.EntityFrameworkCore;

[ConnectionStringName(KnowledgeDbProperties.ConnectionStringName)]
public interface IKnowledgeDbContext : IEfCoreDbContext
{
    DbSet<KnowledgeDocument> KnowledgeDocuments { get; }

    DbSet<IngestionJob> IngestionJobs { get; }

    DbSet<KnowledgePostIndexState> PostIndexStates { get; }
}
