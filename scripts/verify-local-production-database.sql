DO $verify$
BEGIN
    IF (SELECT count(*) FROM public."__EFMigrationsHistory") <> 1
       OR (SELECT count(*) FROM public."EpisodeFormatPolicies") <> 1
       OR NOT EXISTS (SELECT 1 FROM public."EpisodeFormatPolicies" WHERE "Rationale" = 'preserve administrator edit') THEN
        RAISE EXCEPTION 'Rerun overwrote data or duplicated schema/reference data';
    END IF;
    IF has_schema_privilege('id-vibecast-prod', 'public', 'CREATE')
       OR has_table_privilege('id-vibecast-prod', 'public.unrelated', 'SELECT,INSERT,UPDATE,DELETE')
       OR has_table_privilege('id-vibecast-prod', 'public."__EFMigrationsHistory"', 'SELECT,INSERT,UPDATE,DELETE')
       OR has_table_privilege('id-vibecast-prod', 'public."EpisodeFormatPolicies"', 'INSERT,UPDATE,DELETE') THEN
        RAISE EXCEPTION 'Runtime has excessive privileges';
    END IF;
END
$verify$;
SET ROLE "id-vibecast-prod";
SELECT count(*) FROM public."EpisodeFormatPolicies";
INSERT INTO public."AspNetRoles" ("Id", "Name") VALUES ('test-role', 'Temporary test role');
UPDATE public."AspNetRoles" SET "Name" = 'Updated' WHERE "Id" = 'test-role';
INSERT INTO public."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") VALUES ('test-role', 'test', 'test');
DELETE FROM public."AspNetRoleClaims" WHERE "RoleId" = 'test-role';
DELETE FROM public."AspNetRoles" WHERE "Id" = 'test-role';
DO $denied$
BEGIN
    BEGIN
        DELETE FROM public."__EFMigrationsHistory";
        RAISE EXCEPTION 'Migration history delete unexpectedly allowed';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
    BEGIN
        UPDATE public."EpisodeFormatPolicies" SET "IsActive" = false;
        RAISE EXCEPTION 'Policy update unexpectedly allowed';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
    BEGIN
        CREATE TABLE public.runtime_should_not_create(id integer);
        RAISE EXCEPTION 'Runtime DDL unexpectedly allowed';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
END
$denied$;
RESET ROLE;
