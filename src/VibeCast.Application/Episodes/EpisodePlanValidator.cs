using VibeCast.Application.Validation;

namespace VibeCast.Application.Episodes;

public sealed class EpisodePlanValidator : IValidator<EpisodePlan>
{
    private const int MinimumTargetDurationMinutes = 18;
    private const int MaximumTargetDurationMinutes = 30;

    private const int MinimumSegmentCount = 3;
    private const int MaximumSegmentCount = 5;

    private const int MinimumTalkingPointsPerSegment = 2;
    private const int MaximumTalkingPointsPerSegment = 6;

    private const int MinimumKeyMessageCount = 2;
    private const int MaximumKeyMessageCount = 5;

    public ValidationResult Validate(EpisodePlan instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        ValidationResult result = new();

        ValidateRequiredText(
            instance.Summary,
            propertyName: nameof(instance.Summary),
            maximumLength: 600,
            result);

        if (instance.TargetDurationMinutes is
            < MinimumTargetDurationMinutes or
            > MaximumTargetDurationMinutes)
        {
            result.Add(
                nameof(instance.TargetDurationMinutes),
                $"The target duration must be between " +
                $"{MinimumTargetDurationMinutes} and " +
                $"{MaximumTargetDurationMinutes} minutes.");
        }

        EpisodePlanSegment[] segments =
            instance.Segments ?? [];

        if (segments.Length is
            < MinimumSegmentCount or
            > MaximumSegmentCount)
        {
            result.Add(
                nameof(instance.Segments),
                $"The plan must contain between " +
                $"{MinimumSegmentCount} and " +
                $"{MaximumSegmentCount} segments.");
        }

        int totalSegmentDuration = 0;

        for (int index = 0; index < segments.Length; index++)
        {
            EpisodePlanSegment? segment = segments[index];
            string segmentPath = $"Segments[{index}]";

            if (segment is null)
            {
                result.Add(
                    segmentPath,
                    "A segment cannot be null.");

                continue;
            }

            int expectedSequence = index + 1;

            if (segment.Sequence != expectedSequence)
            {
                result.Add(
                    $"{segmentPath}.Sequence",
                    $"The segment sequence must be " +
                    $"{expectedSequence} at this position.");
            }

            ValidateRequiredText(
                segment.Title,
                propertyName: $"{segmentPath}.Title",
                maximumLength: 160,
                result);

            ValidateRequiredText(
                segment.Purpose,
                propertyName: $"{segmentPath}.Purpose",
                maximumLength: 500,
                result);

            if (segment.DurationMinutes <= 0)
            {
                result.Add(
                    $"{segmentPath}.DurationMinutes",
                    "A segment duration must be greater than zero.");
            }
            else
            {
                totalSegmentDuration += segment.DurationMinutes;
            }

            ValidateStringCollection(
                segment.TalkingPoints,
                propertyName: $"{segmentPath}.TalkingPoints",
                minimumCount: MinimumTalkingPointsPerSegment,
                maximumCount: MaximumTalkingPointsPerSegment,
                maximumItemLength: 300,
                result);
        }

        if (totalSegmentDuration !=
            instance.TargetDurationMinutes)
        {
            result.Add(
                "Segments.TotalDuration",
                $"Segment durations total " +
                $"{totalSegmentDuration} minutes, but the target " +
                $"duration is {instance.TargetDurationMinutes} minutes.");
        }

        ValidateStringCollection(
            instance.KeyMessages,
            propertyName: nameof(instance.KeyMessages),
            minimumCount: MinimumKeyMessageCount,
            maximumCount: MaximumKeyMessageCount,
            maximumItemLength: 400,
            result);

        ValidateStringCollection(
            instance.EvidenceRequirements,
            propertyName: nameof(instance.EvidenceRequirements),
            minimumCount: 0,
            maximumCount: 8,
            maximumItemLength: 500,
            result);

        ValidateStringCollection(
            instance.MediaRequirements,
            propertyName: nameof(instance.MediaRequirements),
            minimumCount: 0,
            maximumCount: 8,
            maximumItemLength: 500,
            result);

        ValidateStringCollection(
            instance.EditorialRisks,
            propertyName: nameof(instance.EditorialRisks),
            minimumCount: 0,
            maximumCount: 8,
            maximumItemLength: 500,
            result);

        return result;
    }

    private static void ValidateRequiredText(
        string? value,
        string propertyName,
        int maximumLength,
        ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Add(
                propertyName,
                "A value is required.");

            return;
        }

        if (value.Trim().Length > maximumLength)
        {
            result.Add(
                propertyName,
                $"The value cannot exceed " +
                $"{maximumLength} characters.");
        }
    }

    private static void ValidateStringCollection(
        string[]? values,
        string propertyName,
        int minimumCount,
        int maximumCount,
        int maximumItemLength,
        ValidationResult result)
    {
        if (values is null)
        {
            result.Add(
                propertyName,
                "The collection is required.");

            return;
        }

        if (values.Length < minimumCount)
        {
            result.Add(
                propertyName,
                $"The collection must contain at least " +
                $"{minimumCount} item(s).");
        }

        if (values.Length > maximumCount)
        {
            result.Add(
                propertyName,
                $"The collection cannot contain more than " +
                $"{maximumCount} item(s).");
        }

        for (int index = 0; index < values.Length; index++)
        {
            string? item = values[index];
            string itemPath = $"{propertyName}[{index}]";

            if (string.IsNullOrWhiteSpace(item))
            {
                result.Add(
                    itemPath,
                    "The item cannot be empty.");

                continue;
            }

            if (item.Trim().Length > maximumItemLength)
            {
                result.Add(
                    itemPath,
                    $"The item cannot exceed " +
                    $"{maximumItemLength} characters.");
            }
        }
    }
}
