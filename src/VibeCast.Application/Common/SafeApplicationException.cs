namespace VibeCast.Application.Common;

/// <summary>
/// Represents an expected application condition whose message is safe to show
/// to an authenticated user.
/// </summary>
public sealed class SafeApplicationException : Exception
{
    public SafeApplicationException(string message)
        : base(message)
    {
    }

    public SafeApplicationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
