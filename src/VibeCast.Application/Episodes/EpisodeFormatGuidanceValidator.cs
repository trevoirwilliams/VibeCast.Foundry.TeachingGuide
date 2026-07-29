using VibeCast.Application.Validation;

namespace VibeCast.Application.Episodes;

public sealed class EpisodeFormatGuidanceValidator
    : IValidator<EpisodeFormatGuidanceValidationRequest>
{
    public ValidationResult Validate(
        EpisodeFormatGuidanceValidationRequest instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        ValidationResult result = new();

        if (instance.Plan.TargetDurationMinutes !=
            instance.Guidance.TargetDurationMinutes)
        {
            result.Add(
                nameof(EpisodePlan.TargetDurationMinutes),
                $"The target duration must be " +
                $"{instance.Guidance.TargetDurationMinutes} minutes " +
                $"as selected by format policy " +
                $"{instance.Guidance.PolicyVersion}.");
        }

        return result;
    }
}
