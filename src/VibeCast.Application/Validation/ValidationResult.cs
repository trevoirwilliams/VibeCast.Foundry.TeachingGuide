namespace VibeCast.Application.Validation;

public sealed record ValidationFailure(string PropertyName, string ErrorMessage);

public sealed class ValidationResult
{
    private readonly List<ValidationFailure> _errors = [];

    public IReadOnlyCollection<ValidationFailure> Errors => _errors;
    public bool IsValid => _errors.Count == 0;

    public void Add(string propertyName, string errorMessage) =>
        _errors.Add(new ValidationFailure(propertyName, errorMessage));
}

public interface IValidator<in T>
{
    ValidationResult Validate(T instance);
}
