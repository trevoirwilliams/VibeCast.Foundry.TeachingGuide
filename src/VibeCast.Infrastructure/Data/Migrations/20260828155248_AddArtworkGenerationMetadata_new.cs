using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkGenerationMetadata_new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GeneratedAtUtc",
                table: "MediaAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationModelDeployment",
                table: "MediaAssets",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationPromptVersion",
                table: "MediaAssets",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedAtUtc",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "GenerationModelDeployment",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "GenerationPromptVersion",
                table: "MediaAssets");
        }
    }
}
