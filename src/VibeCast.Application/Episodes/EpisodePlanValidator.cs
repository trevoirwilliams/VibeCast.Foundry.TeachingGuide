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
        // PRACTICE S05-02: Validate
        // 1. Guard the plan and create a validation result to collect failures.
        // 2. Use the supplied helpers for required text and collection rules.
        // 3. Check segment count, sequence and positive durations; total the durations.
        // 4. Compare that total with the target and return all recorded failures.
        // Optional API hints and checks: docs/practice/README.md#s05-02-validate
        throw new NotImplementedException("S05-02: implement Validate.");
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
