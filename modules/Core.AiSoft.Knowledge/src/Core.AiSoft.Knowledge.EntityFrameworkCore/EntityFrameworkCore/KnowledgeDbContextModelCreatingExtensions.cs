using Core.AiSoft.Knowledge.Documents;
using Core.AiSoft.Knowledge.IngestionJobs;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Core.AiSoft.Knowledge.EntityFrameworkCore;

public static class KnowledgeDbContextModelCreatingExtensions
{
    public static void ConfigureKnowledge(
        this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<KnowledgeDocument>(b =>
        {
            b.ToTable(KnowledgeDbProperties.DbTablePrefix + "Documents", KnowledgeDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(d => d.FileName).IsRequired().HasMaxLength(KnowledgeDocumentConsts.MaxFileNameLength);
            b.Property(d => d.Extension).IsRequired().HasMaxLength(KnowledgeDocumentConsts.MaxExtensionLength);
            b.Property(d => d.ContentType).IsRequired().HasMaxLength(KnowledgeDocumentConsts.MaxContentTypeLength);
            b.Property(d => d.Sha256).IsRequired().HasMaxLength(KnowledgeDocumentConsts.Sha256Length).IsFixedLength();
            b.Property(d => d.BlobName).IsRequired().HasMaxLength(KnowledgeDocumentConsts.MaxBlobNameLength);

            b.HasIndex(d => new { d.TenantId, d.Sha256 });
            b.HasIndex(d => d.Status);
            b.HasIndex(d => d.CreationTime);
        });

        builder.Entity<IngestionJob>(b =>
        {
            b.ToTable(KnowledgeDbProperties.DbTablePrefix + "IngestionJobs", KnowledgeDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(j => j.ErrorCode).HasMaxLength(IngestionJobConsts.MaxErrorCodeLength);
            b.Property(j => j.ErrorMessage).HasMaxLength(IngestionJobConsts.MaxErrorMessageLength);

            b.HasOne<KnowledgeDocument>().WithMany().HasForeignKey(j => j.DocumentId).IsRequired();

            b.HasIndex(j => j.DocumentId);
            b.HasIndex(j => new { j.Status, j.CreationTime });
        });
    }
}
