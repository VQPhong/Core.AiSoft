using Core.AiSoft.Knowledge.Documents;
using Core.AiSoft.Knowledge.IngestionJobs;
using Core.AiSoft.Knowledge.PostIndexing;
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

            b.Property(j => j.ContentHash).IsRequired().HasMaxLength(IngestionJobConsts.ContentHashLength).IsFixedLength();
            b.Property(j => j.SnapshotBlobName).IsRequired().HasMaxLength(IngestionJobConsts.MaxSnapshotBlobNameLength);
            b.Property(j => j.ErrorCode).HasMaxLength(IngestionJobConsts.MaxErrorCodeLength);
            b.Property(j => j.ErrorMessage).HasMaxLength(IngestionJobConsts.MaxErrorMessageLength);

            b.HasOne<KnowledgeDocument>().WithMany().HasForeignKey(j => j.DocumentId).IsRequired(false);

            b.HasIndex(j => j.BlogPostId);
            b.HasIndex(j => j.DocumentId);
            b.HasIndex(j => new { j.Status, j.CreationTime });
        });

        builder.Entity<KnowledgePostIndexState>(b =>
        {
            b.ToTable(KnowledgeDbProperties.DbTablePrefix + "PostIndexStates", KnowledgeDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(s => s.IndexedContentHash).HasMaxLength(IngestionJobConsts.ContentHashLength).IsFixedLength();

            b.HasIndex(s => new { s.TenantId, s.BlogPostId }).IsUnique();
        });
    }
}
