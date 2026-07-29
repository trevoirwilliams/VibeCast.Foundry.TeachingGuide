using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEpisodeSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedPlanJson",
                table: "Episodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanFormatPolicyVersion",
                table: "Episodes",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlanGeneratedAtUtc",
                table: "Episodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanPromptVersion",
                table: "Episodes",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlanRepairAttempted",
                table: "Episodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlanRepairPromptVersion",
                table: "Episodes",
                type: "TEXT",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedPlanJson",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "PlanFormatPolicyVersion",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "PlanGeneratedAtUtc",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "PlanPromptVersion",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "PlanRepairAttempted",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "PlanRepairPromptVersion",
                table: "Episodes");
        }
    }
}
