using VibeCast.Domain.Common;

namespace VibeCast.Domain.Users;

public sealed class UserProfile : Entity
{
    private UserProfile() { }

    private UserProfile(string identityUserId, string displayName)
    {
        IdentityUserId = identityUserId;
        DisplayName = displayName;
    }

    public string IdentityUserId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string TimeZoneId { get; private set; } = "UTC";

    public static UserProfile Create(string identityUserId, string displayName) =>
        new(identityUserId, displayName);
}
