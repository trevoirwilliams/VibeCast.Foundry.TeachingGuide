using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEpisodeEditorialPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodeFormatPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Tone = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    AudienceKeyword = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    TargetDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    PacingGuidance = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Rationale = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeFormatPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeFormatPolicies_IsActive_EffectiveFromUtc",
                table: "EpisodeFormatPolicies",
                columns: new[] { "IsActive", "EffectiveFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeFormatPolicies_Version",
                table: "EpisodeFormatPolicies",
                column: "Version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodeFormatPolicies");
        }
    }
}
