namespace VibeCast.Infrastructure.Options;

public sealed class DataProtectionStorageOptions
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; set; } = "VibeCast";

    public string BlobName { get; set; } = "keys.xml";

    public string BlobUri { get; set; } = string.Empty;

    public string KeyVaultKeyIdentifier { get; set; } = string.Empty;
}
