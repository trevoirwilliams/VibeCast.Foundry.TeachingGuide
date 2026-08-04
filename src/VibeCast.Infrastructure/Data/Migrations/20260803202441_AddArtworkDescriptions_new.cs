using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkDescriptions_new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedAltText",
                table: "MediaAssets",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArtworkAcceptedAtUtc",
                table: "MediaAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArtworkAnalyzedAtUtc",
                table: "MediaAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArtworkPromptVersion",
                table: "MediaAssets",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArtworkSummary",
                table: "MediaAssets",
                type: "TEXT",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArtworkVisibleText",
                table: "MediaAssets",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedAltText",
                table: "MediaAssets",
                type: "TEXT",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAltText",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ArtworkAcceptedAtUtc",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ArtworkAnalyzedAtUtc",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ArtworkPromptVersion",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ArtworkSummary",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ArtworkVisibleText",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ProposedAltText",
                table: "MediaAssets");
        }
    }
}
