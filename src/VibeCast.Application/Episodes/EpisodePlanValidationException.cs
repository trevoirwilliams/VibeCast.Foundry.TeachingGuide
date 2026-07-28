using VibeCast.Application.Validation;

namespace VibeCast.Application.Episodes;

public sealed class EpisodePlanValidationException : Exception
{
    public EpisodePlanValidationException(
        IEnumerable<ValidationFailure> failures,
        bool repairAttempted)
        : base(
            repairAttempted
                ? "The episode plan remained invalid after one " +
                  "bounded repair attempt."
                : "The generated episode plan failed deterministic " +
                  "validation.")
    {
        ArgumentNullException.ThrowIfNull(failures);

        Failures = failures.ToArray();
        RepairAttempted = repairAttempted;
    }

    public IReadOnlyList<ValidationFailure> Failures { get; }

    public bool RepairAttempted { get; }
}
