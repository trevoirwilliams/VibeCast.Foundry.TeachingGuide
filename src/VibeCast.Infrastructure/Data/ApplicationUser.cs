using Microsoft.AspNetCore.Identity;

namespace VibeCast.Infrastructure.Data;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
