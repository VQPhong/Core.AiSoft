using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.AiSoft.Migrations
{
    /// <inheritdoc />
    public partial class Added_Document_BlogPostId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BlogPostId",
                table: "KnowledgeDocuments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlogPostId",
                table: "KnowledgeDocuments");
        }
    }
}
