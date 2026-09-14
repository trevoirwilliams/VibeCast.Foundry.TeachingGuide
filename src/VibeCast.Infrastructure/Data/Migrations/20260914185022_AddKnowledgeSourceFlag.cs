using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeSourceFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsKnowledgeSource",
                table: "MediaAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_OwnerId_IsKnowledgeSource",
                table: "MediaAssets",
                columns: new[] { "OwnerId", "IsKnowledgeSource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_OwnerId_IsKnowledgeSource",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "IsKnowledgeSource",
                table: "MediaAssets");
        }
    }
}
