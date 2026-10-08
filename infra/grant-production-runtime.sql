BEGIN;
GRANT CONNECT ON DATABASE "vibecast" TO "id-vibecast-prod";
GRANT USAGE ON SCHEMA public TO "id-vibecast-prod";
-- Exact initial-migration tables; unrelated and future tables receive no grants.
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    public."AspNetRoles", public."AspNetUsers", public."AspNetRoleClaims",
    public."AspNetUserClaims", public."AspNetUserLogins", public."AspNetUserRoles",
    public."AspNetUserTokens", public."Episodes", public."MediaAssets",
    public."ProcessingJobs", public."UserProfiles", public."EpisodeSupportingSources"
TO "id-vibecast-prod";
REVOKE ALL ON TABLE public."EpisodeFormatPolicies" FROM "id-vibecast-prod";
GRANT SELECT ON TABLE public."EpisodeFormatPolicies" TO "id-vibecast-prod";
REVOKE ALL ON TABLE public."__EFMigrationsHistory" FROM "id-vibecast-prod";
-- The only numeric identity columns in InitialPostgreSql are Identity claims.
GRANT USAGE, SELECT ON SEQUENCE
    public."AspNetRoleClaims_Id_seq", public."AspNetUserClaims_Id_seq"
TO "id-vibecast-prod";
COMMIT;
