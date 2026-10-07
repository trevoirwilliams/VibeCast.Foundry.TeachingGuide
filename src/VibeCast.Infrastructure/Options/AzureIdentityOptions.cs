namespace VibeCast.Infrastructure.Options;

public sealed class AzureIdentityOptions
{
    public const string SectionName = "AzureIdentity";

    public string ManagedIdentityClientId { get; init; } = string.Empty;
}
