using VibeCast.Domain.Common;

namespace VibeCast.Domain.Episodes;

public sealed class EpisodeFormatPolicy : Entity
{
    private EpisodeFormatPolicy()
    {
    }

    private EpisodeFormatPolicy(
        string version,
        string? tone,
        string? audienceKeyword,
        int targetDurationMinutes,
        string pacingGuidance,
        string rationale,
        int priority,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc)
    {
        Version = RequireText(
            version,
            nameof(version),
            maximumLength: 80);

        Tone = NormalizeOptionalText(
            tone,
            nameof(tone),
            maximumLength: 80);

        AudienceKeyword = NormalizeOptionalText(
            audienceKeyword,
            nameof(audienceKeyword),
            maximumLength: 80);

        if (targetDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetDurationMinutes),
                "The target duration must be greater than zero.");
        }

        TargetDurationMinutes = targetDurationMinutes;

        PacingGuidance = RequireText(
            pacingGuidance,
            nameof(pacingGuidance),
            maximumLength: 500);

        Rationale = RequireText(
            rationale,
            nameof(rationale),
            maximumLength: 500);

        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(priority),
                "Priority cannot be negative.");
        }

        Priority = priority;
        EffectiveFromUtc = effectiveFromUtc;

        if (effectiveToUtc is not null &&
            effectiveToUtc < effectiveFromUtc)
        {
            throw new ArgumentException(
                "The policy end time cannot precede its start time.",
                nameof(effectiveToUtc));
        }

        EffectiveToUtc = effectiveToUtc;
    }

    public string Version { get; private set; } = string.Empty;

    public string? Tone { get; private set; }

    public string? AudienceKeyword { get; private set; }

    public int TargetDurationMinutes { get; private set; }

    public string PacingGuidance { get; private set; } = string.Empty;

    public string Rationale { get; private set; } = string.Empty;

    public int Priority { get; private set; }

    public DateTimeOffset EffectiveFromUtc { get; private set; }

    public DateTimeOffset? EffectiveToUtc { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static EpisodeFormatPolicy Create(
        string version,
        string? tone,
        string? audienceKeyword,
        int targetDurationMinutes,
        string pacingGuidance,
        string rationale,
        int priority,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc = null)
    {
        return new EpisodeFormatPolicy(
            version,
            tone,
            audienceKeyword,
            targetDurationMinutes,
            pacingGuidance,
            rationale,
            priority,
            effectiveFromUtc,
            effectiveToUtc);
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    private static string RequireText(
        string? value,
        string parameterName,
        int maximumLength)
    {
        string normalized = value?.Trim() ?? string.Empty;

        if (normalized.Length == 0 ||
            normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value must contain between 1 and " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        string parameterName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
