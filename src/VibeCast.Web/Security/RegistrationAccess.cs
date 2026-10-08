namespace VibeCast.Web.Security;

public static class RegistrationAccess
{
    public static bool IsOpen(IConfiguration configuration, IHostEnvironment environment) =>
        configuration.GetValue<bool>("Registration:Enabled") &&
        (environment.IsDevelopment() ||
         !string.IsNullOrWhiteSpace(configuration["Registration:AllowedEmail"]));

    public static bool AllowsEmail(IConfiguration configuration, IHostEnvironment environment, string? email) =>
        IsOpen(configuration, environment) &&
        (environment.IsDevelopment() ||
         string.Equals(email?.Trim(), configuration["Registration:AllowedEmail"]?.Trim(),
             StringComparison.OrdinalIgnoreCase));
}
