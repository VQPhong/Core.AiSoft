using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.AiSoft.Migrations
{
    /// <inheritdoc />
    public partial class Added_Post_Ingestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old jobs were per uploaded file (no blog post, no version): they cannot be mapped to the new model
            // and would all get an empty BlogPostId, breaking the unique "one active job per post" index below.
            migrationBuilder.Sql("DELETE FROM \"KnowledgeIngestionJobs\";");
            migrationBuilder.Sql("UPDATE \"KnowledgeDocuments\" SET \"LastJobId\" = NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeIngestionJobs_KnowledgeDocuments_DocumentId",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentId",
                table: "KnowledgeIngestionJobs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "BlogPostId",
                table: "KnowledgeIngestionJobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "KnowledgeIngestionJobs",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsForced",
                table: "KnowledgeIngestionJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SkipReason",
                table: "KnowledgeIngestionJobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotBlobName",
                table: "KnowledgeIngestionJobs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "KnowledgeIngestionJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "KnowledgePostIndexStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlogPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndexedVersion = table.Column<int>(type: "integer", nullable: true),
                    IndexedContentHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    IndexedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ChunkCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveVersion = table.Column<int>(type: "integer", nullable: true),
                    LastJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgePostIndexStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeIngestionJobs_BlogPostId",
                table: "KnowledgeIngestionJobs",
                column: "BlogPostId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgePostIndexStates_TenantId_BlogPostId",
                table: "KnowledgePostIndexStates",
                columns: new[] { "TenantId", "BlogPostId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeIngestionJobs_KnowledgeDocuments_DocumentId",
                table: "KnowledgeIngestionJobs",
                column: "DocumentId",
                principalTable: "KnowledgeDocuments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeIngestionJobs_KnowledgeDocuments_DocumentId",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.DropTable(
                name: "KnowledgePostIndexStates");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeIngestionJobs_BlogPostId",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.DropColumn(
                name: "BlogPostId",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.DropColumn(
                name: "IsForced",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.DropColumn(
                name: "SkipReason",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.DropColumn(
                name: "SnapshotBlobName",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "KnowledgeIngestionJobs");

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentId",
                table: "KnowledgeIngestionJobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeIngestionJobs_KnowledgeDocuments_DocumentId",
                table: "KnowledgeIngestionJobs",
                column: "DocumentId",
                principalTable: "KnowledgeDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
