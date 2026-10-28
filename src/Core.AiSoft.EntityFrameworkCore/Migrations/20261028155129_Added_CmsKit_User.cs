using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.AiSoft.Migrations
{
    /// <inheritdoc />
    public partial class Added_CmsKit_User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CmsUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Surname = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PhoneNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CmsBlogPosts_AuthorId",
                table: "CmsBlogPosts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_CmsUsers_TenantId_Email",
                table: "CmsUsers",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_CmsUsers_TenantId_UserName",
                table: "CmsUsers",
                columns: new[] { "TenantId", "UserName" });

            migrationBuilder.AddForeignKey(
                name: "FK_CmsBlogPosts_CmsUsers_AuthorId",
                table: "CmsBlogPosts",
                column: "AuthorId",
                principalTable: "CmsUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CmsBlogPosts_CmsUsers_AuthorId",
                table: "CmsBlogPosts");

            migrationBuilder.DropTable(
                name: "CmsUsers");

            migrationBuilder.DropIndex(
                name: "IX_CmsBlogPosts_AuthorId",
                table: "CmsBlogPosts");
        }
    }
}
