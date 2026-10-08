-- Required application-owned reference data only. No demo users or episodes.
-- Run as the PostgreSQL Entra administrator AFTER applying EF Core migrations.
-- The unique Version constraint guarantees idempotency without overwriting edits.
INSERT INTO public."EpisodeFormatPolicies"
(
    "Id", "Version", "Tone", "AudienceKeyword",
    "TargetDurationMinutes", "PacingGuidance", "Rationale",
    "Priority", "EffectiveFromUtc", "EffectiveToUtc",
    "IsActive", "CreatedAtUtc", "UpdatedAtUtc"
)
VALUES
(
    gen_random_uuid(),
    'format-default-2026.1',
    NULL,
    NULL,
    24,
    'Use balanced pacing with clear transitions, practical explanations, and a concise recap.',
    'Default guidance applies when no more specific audience or tone policy matches.',
    0,
    TIMESTAMPTZ '2026-01-01 00:00:00+00',
    NULL,
    true,
    now(),
    now()
)
ON CONFLICT ("Version") DO NOTHING;

DO $verify$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM public."EpisodeFormatPolicies"
        WHERE "Version" = 'format-default-2026.1' AND "IsActive"
    ) THEN
        RAISE EXCEPTION 'Required active episode format policy is missing';
    END IF;
END
$verify$;
