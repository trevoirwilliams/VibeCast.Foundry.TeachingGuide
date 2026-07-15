using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeCast.Infrastructure.Data.Migrations;

[DbContext(typeof(VibeCastDbContext))]
[Migration("202607150001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AspNetRoles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AspNetUsers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Episodes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                OwnerId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ScheduledForUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Episodes", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MediaAssets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EpisodeId = table.Column<Guid>(type: "TEXT", nullable: true),
                OwnerId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                StorageKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MediaAssets", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ProcessingJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                OwnerId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                JobType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                SubjectReference = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ProcessingJobs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "UserProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                IdentityUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UserProfiles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                RoleId = table.Column<string>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey("FK_AspNetUserClaims_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey("FK_AspNetUserLogins_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                RoleId = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_AspNetUserRoles_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey("FK_AspNetUserTokens_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_AspNetRoleClaims_RoleId", "AspNetRoleClaims", "RoleId");
        migrationBuilder.CreateIndex("RoleNameIndex", "AspNetRoles", "NormalizedName", unique: true);
        migrationBuilder.CreateIndex("IX_AspNetUserClaims_UserId", "AspNetUserClaims", "UserId");
        migrationBuilder.CreateIndex("IX_AspNetUserLogins_UserId", "AspNetUserLogins", "UserId");
        migrationBuilder.CreateIndex("IX_AspNetUserRoles_RoleId", "AspNetUserRoles", "RoleId");
        migrationBuilder.CreateIndex("EmailIndex", "AspNetUsers", "NormalizedEmail");
        migrationBuilder.CreateIndex("UserNameIndex", "AspNetUsers", "NormalizedUserName", unique: true);
        migrationBuilder.CreateIndex("IX_Episodes_OwnerId_CreatedAtUtc", "Episodes", new[] { "OwnerId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_MediaAssets_StorageKey", "MediaAssets", "StorageKey", unique: true);
        migrationBuilder.CreateIndex("IX_ProcessingJobs_OwnerId_CreatedAtUtc", "ProcessingJobs", new[] { "OwnerId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_UserProfiles_IdentityUserId", "UserProfiles", "IdentityUserId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AspNetRoleClaims");
        migrationBuilder.DropTable("AspNetUserClaims");
        migrationBuilder.DropTable("AspNetUserLogins");
        migrationBuilder.DropTable("AspNetUserRoles");
        migrationBuilder.DropTable("AspNetUserTokens");
        migrationBuilder.DropTable("Episodes");
        migrationBuilder.DropTable("MediaAssets");
        migrationBuilder.DropTable("ProcessingJobs");
        migrationBuilder.DropTable("UserProfiles");
        migrationBuilder.DropTable("AspNetRoles");
        migrationBuilder.DropTable("AspNetUsers");
    }
}
