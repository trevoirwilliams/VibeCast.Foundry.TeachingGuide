using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEpisodeSupportingSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodeSupportingSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EpisodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaAssetId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    RelevanceRationale = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RelevantPointsJson = table.Column<string>(type: "TEXT", nullable: false),
                    MatchedEvidenceRequirementsJson = table.Column<string>(type: "TEXT", nullable: false),
                    AnalyzerId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RelevancePromptVersion = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    AnalyzedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeSupportingSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeSupportingSources_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeSupportingSources_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeSupportingSources_MediaAssets_MediaAssetId1",
                        column: x => x.MediaAssetId1,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeSupportingSources_EpisodeId",
                table: "EpisodeSupportingSources",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeSupportingSources_MediaAssetId",
                table: "EpisodeSupportingSources",
                column: "MediaAssetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeSupportingSources_MediaAssetId1",
                table: "EpisodeSupportingSources",
                column: "MediaAssetId1");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeSupportingSources_OwnerId_EpisodeId",
                table: "EpisodeSupportingSources",
                columns: new[] { "OwnerId", "EpisodeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodeSupportingSources");
        }
    }
}
