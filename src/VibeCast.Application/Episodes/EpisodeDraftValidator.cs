using VibeCast.Application.Validation;

namespace VibeCast.Application.Episodes;

public sealed class EpisodeDraftValidator : IValidator<CreateEpisodeRequest>
{
    public ValidationResult Validate(CreateEpisodeRequest instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(instance.Title))
        {
            result.Add(nameof(instance.Title), "A title is required.");
        }
        else if (instance.Title.Trim().Length is < 3 or > 160)
        {
            result.Add(nameof(instance.Title), "The title must contain 3 to 160 characters.");
        }

        if (instance.Description?.Length > 2_000)
        {
            result.Add(nameof(instance.Description), "The description cannot exceed 2,000 characters.");
        }

        if (instance.ScheduledForUtc is { } scheduled && scheduled <= DateTimeOffset.UtcNow)
        {
            result.Add(nameof(instance.ScheduledForUtc), "A scheduled episode must be in the future.");
        }

        return result;
    }
}
