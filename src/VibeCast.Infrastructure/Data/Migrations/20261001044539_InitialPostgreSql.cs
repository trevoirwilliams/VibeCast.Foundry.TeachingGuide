using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeFormatPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Tone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    AudienceKeyword = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    TargetDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PacingGuidance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeFormatPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TargetAudience = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Objective = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Tone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Language = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PlannedPublishDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledForUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedPlanJson = table.Column<string>(type: "text", nullable: true),
                    PlanPromptVersion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PlanGeneratedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PlanRepairAttempted = table.Column<bool>(type: "boolean", nullable: false),
                    PlanRepairPromptVersion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PlanFormatPolicyVersion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProposedAltText = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AcceptedAltText = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ArtworkSummary = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    ArtworkVisibleText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ArtworkPromptVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ArtworkAnalyzedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArtworkAcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GenerationModelDeployment = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    GenerationPromptVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TranscriptText = table.Column<string>(type: "text", nullable: true),
                    TranscriptionLocale = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TranscribedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsKnowledgeSource = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    JobType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SubjectReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentityUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeSupportingSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RelevanceRationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RelevantPointsJson = table.Column<string>(type: "text", nullable: false),
                    MatchedEvidenceRequirementsJson = table.Column<string>(type: "text", nullable: false),
                    AnalyzerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RelevancePromptVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AnalyzedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeFormatPolicies_IsActive_EffectiveFromUtc",
                table: "EpisodeFormatPolicies",
                columns: new[] { "IsActive", "EffectiveFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeFormatPolicies_Version",
                table: "EpisodeFormatPolicies",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_OwnerId_CreatedAtUtc",
                table: "Episodes",
                columns: new[] { "OwnerId", "CreatedAtUtc" });

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
                name: "IX_EpisodeSupportingSources_OwnerId_EpisodeId",
                table: "EpisodeSupportingSources",
                columns: new[] { "OwnerId", "EpisodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_OwnerId_IsKnowledgeSource",
                table: "MediaAssets",
                columns: new[] { "OwnerId", "IsKnowledgeSource" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_StorageKey",
                table: "MediaAssets",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_OwnerId_CreatedAtUtc",
                table: "ProcessingJobs",
                columns: new[] { "OwnerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_IdentityUserId",
                table: "UserProfiles",
                column: "IdentityUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "EpisodeFormatPolicies");

            migrationBuilder.DropTable(
                name: "EpisodeSupportingSources");

            migrationBuilder.DropTable(
                name: "ProcessingJobs");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Episodes");

            migrationBuilder.DropTable(
                name: "MediaAssets");
        }
    }
}
