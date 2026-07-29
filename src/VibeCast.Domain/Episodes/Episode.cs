using VibeCast.Domain.Common;

namespace VibeCast.Domain.Episodes;

public sealed class Episode : Entity
{
    private Episode() { }

    private Episode(
        string title,
        string? description,
        string targetAudience,
        string objective,
        string tone,
        string language,
        DateOnly? plannedPublishDate,
        string ownerId)
    {
        SetTitle(title);
        Description = description?.Trim();
        OwnerId = string.IsNullOrWhiteSpace(ownerId)
            ? throw new ArgumentException("An owner is required.", nameof(ownerId))
            : ownerId;
        TargetAudience = targetAudience.Trim();
        Objective = objective.Trim();
        Tone = tone.Trim();
        Language = language.Trim();
        PlannedPublishDate = plannedPublishDate;
    }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public string TargetAudience { get; private set; } = string.Empty;
    public string Objective { get; private set; } = string.Empty;
    public string Tone { get; private set; } = string.Empty;
    public string Language { get; private set; } = string.Empty;
    public DateOnly? PlannedPublishDate { get; private set; }

    public EpisodeStatus Status { get; private set; } = EpisodeStatus.Draft;
    public DateTimeOffset? ScheduledForUtc { get; private set; }

    public string? AcceptedPlanJson { get; private set; }

    public string? PlanPromptVersion { get; private set; }

    public DateTimeOffset? PlanGeneratedAtUtc { get; private set; }

    public bool PlanRepairAttempted { get; private set; }

    public string? PlanRepairPromptVersion { get; private set; }

    public string? PlanFormatPolicyVersion { get; private set; }

    public static Episode Create(
        string title,
        string? description,
        string targetAudience,
        string objective,
        string tone,
        string language,
        DateOnly? plannedPublishDate,
        string ownerId)
    {
        return new Episode(
            title,
            description,
            targetAudience,
            objective,
            tone,
            language,
            plannedPublishDate,
            ownerId);
    }

    public void Rename(string title)
    {
        SetTitle(title);
        MarkUpdated();
    }

    public void Schedule(DateTimeOffset scheduledForUtc)
    {
        ScheduledForUtc = scheduledForUtc;
        Status = EpisodeStatus.ReadyForReview;
        MarkUpdated();
    }

    private void SetTitle(string title)
    {
        var value = title?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 160)
        {
            throw new ArgumentException("Episode title must contain 1 to 160 characters.", nameof(title));
        }

        Title = value;
    }

    public void SaveAcceptedPlan(
        string planJson,
        string promptVersion,
        DateTimeOffset generatedAtUtc,
        bool repairAttempted,
        string? repairPromptVersion,
        string? formatPolicyVersion)
    {
        if (string.IsNullOrWhiteSpace(planJson))
        {
            throw new ArgumentException(
                "The accepted plan JSON is required.",
                nameof(planJson));
        }

        if (string.IsNullOrWhiteSpace(promptVersion))
        {
            throw new ArgumentException(
                "The prompt version is required.",
                nameof(promptVersion));
        }

        if (generatedAtUtc == default)
        {
            throw new ArgumentException(
                "The plan generation timestamp is required.",
                nameof(generatedAtUtc));
        }

        AcceptedPlanJson = planJson;
        PlanPromptVersion = promptVersion.Trim();
        PlanGeneratedAtUtc = generatedAtUtc;
        PlanRepairAttempted = repairAttempted;

        PlanRepairPromptVersion =
            string.IsNullOrWhiteSpace(repairPromptVersion)
                ? null
                : repairPromptVersion.Trim();

        PlanFormatPolicyVersion =
            string.IsNullOrWhiteSpace(formatPolicyVersion)
                ? null
                : formatPolicyVersion.Trim();

        MarkUpdated();
    }
}
