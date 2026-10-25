using Core.AiSoft.Knowledge.Documents;
using Core.AiSoft.Knowledge.IngestionJobs;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Core.AiSoft.Knowledge.EntityFrameworkCore;

[ConnectionStringName(KnowledgeDbProperties.ConnectionStringName)]
public class KnowledgeDbContext : AbpDbContext<KnowledgeDbContext>, IKnowledgeDbContext
{
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }

    public DbSet<IngestionJob> IngestionJobs { get; set; }

    public KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureKnowledge();
    }
}
