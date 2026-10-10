CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "AspNetRoles" (
        "Id" text NOT NULL,
        "Name" character varying(256),
        "NormalizedName" character varying(256),
        "ConcurrencyStamp" text,
        CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "AspNetUsers" (
        "Id" text NOT NULL,
        "DisplayName" text NOT NULL,
        "UserName" character varying(256),
        "NormalizedUserName" character varying(256),
        "Email" character varying(256),
        "NormalizedEmail" character varying(256),
        "EmailConfirmed" boolean NOT NULL,
        "PasswordHash" text,
        "SecurityStamp" text,
        "ConcurrencyStamp" text,
        "PhoneNumber" text,
        "PhoneNumberConfirmed" boolean NOT NULL,
        "TwoFactorEnabled" boolean NOT NULL,
        "LockoutEnd" timestamp with time zone,
        "LockoutEnabled" boolean NOT NULL,
        "AccessFailedCount" integer NOT NULL,
        CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "EpisodeFormatPolicies" (
        "Id" uuid NOT NULL,
        "Version" character varying(80) NOT NULL,
        "Tone" character varying(80),
        "AudienceKeyword" character varying(80),
        "TargetDurationMinutes" integer NOT NULL,
        "PacingGuidance" character varying(500) NOT NULL,
        "Rationale" character varying(500) NOT NULL,
        "Priority" integer NOT NULL,
        "EffectiveFromUtc" timestamp with time zone NOT NULL,
        "EffectiveToUtc" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EpisodeFormatPolicies" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "Episodes" (
        "Id" uuid NOT NULL,
        "Title" character varying(160) NOT NULL,
        "Description" character varying(2000),
        "OwnerId" character varying(450) NOT NULL,
        "TargetAudience" character varying(160) NOT NULL,
        "Objective" character varying(600) NOT NULL,
        "Tone" character varying(80) NOT NULL,
        "Language" character varying(80) NOT NULL,
        "PlannedPublishDate" date,
        "Status" integer NOT NULL,
        "ScheduledForUtc" timestamp with time zone,
        "AcceptedPlanJson" text,
        "PlanPromptVersion" character varying(120),
        "PlanGeneratedAtUtc" timestamp with time zone,
        "PlanRepairAttempted" boolean NOT NULL,
        "PlanRepairPromptVersion" character varying(120),
        "PlanFormatPolicyVersion" character varying(120),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Episodes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "MediaAssets" (
        "Id" uuid NOT NULL,
        "EpisodeId" uuid,
        "OwnerId" character varying(450) NOT NULL,
        "OriginalFileName" character varying(260) NOT NULL,
        "StorageKey" character varying(512) NOT NULL,
        "ContentType" character varying(128) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "Status" integer NOT NULL,
        "ProposedAltText" character varying(150),
        "AcceptedAltText" character varying(150),
        "ArtworkSummary" character varying(600),
        "ArtworkVisibleText" character varying(500),
        "ArtworkPromptVersion" character varying(80),
        "ArtworkAnalyzedAtUtc" timestamp with time zone,
        "ArtworkAcceptedAtUtc" timestamp with time zone,
        "GenerationModelDeployment" character varying(120),
        "GenerationPromptVersion" character varying(80),
        "GeneratedAtUtc" timestamp with time zone,
        "TranscriptText" text,
        "TranscriptionLocale" character varying(20),
        "TranscribedAtUtc" timestamp with time zone,
        "IsKnowledgeSource" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MediaAssets" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "ProcessingJobs" (
        "Id" uuid NOT NULL,
        "OwnerId" character varying(450) NOT NULL,
        "JobType" character varying(128) NOT NULL,
        "SubjectReference" character varying(512),
        "Status" integer NOT NULL,
        "ErrorMessage" character varying(2000),
        "StartedAtUtc" timestamp with time zone,
        "CompletedAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ProcessingJobs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "UserProfiles" (
        "Id" uuid NOT NULL,
        "IdentityUserId" character varying(450) NOT NULL,
        "DisplayName" character varying(120) NOT NULL,
        "TimeZoneId" character varying(100) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserProfiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "AspNetRoleClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "RoleId" text NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "AspNetUserClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "UserId" text NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "AspNetUserLogins" (
        "LoginProvider" text NOT NULL,
        "ProviderKey" text NOT NULL,
        "ProviderDisplayName" text,
        "UserId" text NOT NULL,
        CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
        CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "AspNetUserRoles" (
        "UserId" text NOT NULL,
        "RoleId" text NOT NULL,
        CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
        CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "AspNetUserTokens" (
        "UserId" text NOT NULL,
        "LoginProvider" text NOT NULL,
        "Name" text NOT NULL,
        "Value" text,
        CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
        CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE TABLE "EpisodeSupportingSources" (
        "Id" uuid NOT NULL,
        "EpisodeId" uuid NOT NULL,
        "MediaAssetId" uuid NOT NULL,
        "OwnerId" character varying(450) NOT NULL,
        "Summary" character varying(2000) NOT NULL,
        "RelevanceRationale" character varying(1000) NOT NULL,
        "RelevantPointsJson" text NOT NULL,
        "MatchedEvidenceRequirementsJson" text NOT NULL,
        "AnalyzerId" character varying(64) NOT NULL,
        "RelevancePromptVersion" character varying(80) NOT NULL,
        "AnalyzedAtUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EpisodeSupportingSources" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EpisodeSupportingSources_Episodes_EpisodeId" FOREIGN KEY ("EpisodeId") REFERENCES "Episodes" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_EpisodeSupportingSources_MediaAssets_MediaAssetId" FOREIGN KEY ("MediaAssetId") REFERENCES "MediaAssets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_EpisodeFormatPolicies_IsActive_EffectiveFromUtc" ON "EpisodeFormatPolicies" ("IsActive", "EffectiveFromUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "IX_EpisodeFormatPolicies_Version" ON "EpisodeFormatPolicies" ("Version");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_Episodes_OwnerId_CreatedAtUtc" ON "Episodes" ("OwnerId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_EpisodeSupportingSources_EpisodeId" ON "EpisodeSupportingSources" ("EpisodeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "IX_EpisodeSupportingSources_MediaAssetId" ON "EpisodeSupportingSources" ("MediaAssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_EpisodeSupportingSources_OwnerId_EpisodeId" ON "EpisodeSupportingSources" ("OwnerId", "EpisodeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_MediaAssets_OwnerId_IsKnowledgeSource" ON "MediaAssets" ("OwnerId", "IsKnowledgeSource");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "IX_MediaAssets_StorageKey" ON "MediaAssets" ("StorageKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE INDEX "IX_ProcessingJobs_OwnerId_CreatedAtUtc" ON "ProcessingJobs" ("OwnerId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "IX_UserProfiles_IdentityUserId" ON "UserProfiles" ("IdentityUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001044539_InitialPostgreSql') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261001044539_InitialPostgreSql', '10.0.12');
    END IF;
END $EF$;
COMMIT;

