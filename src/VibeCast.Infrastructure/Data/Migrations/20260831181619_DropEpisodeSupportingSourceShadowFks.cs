using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropEpisodeSupportingSourceShadowFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeSupportingSources_MediaAssets_MediaAssetId1",
                table: "EpisodeSupportingSources");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeSupportingSources_MediaAssetId1",
                table: "EpisodeSupportingSources");

            migrationBuilder.DropColumn(
                name: "MediaAssetId1",
                table: "EpisodeSupportingSources");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MediaAssetId1",
                table: "EpisodeSupportingSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeSupportingSources_MediaAssetId1",
                table: "EpisodeSupportingSources",
                column: "MediaAssetId1");

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeSupportingSources_MediaAssets_MediaAssetId1",
                table: "EpisodeSupportingSources",
                column: "MediaAssetId1",
                principalTable: "MediaAssets",
                principalColumn: "Id");
        }
    }
}
