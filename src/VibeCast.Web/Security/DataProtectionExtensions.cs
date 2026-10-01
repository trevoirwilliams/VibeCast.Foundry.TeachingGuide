using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using VibeCast.Infrastructure.Options;

namespace VibeCast.Web.Security;

public static class DataProtectionExtensions
{
    private const string ContainerClientKey = "data-protection";
    public static void AddVibeCastDataProtection(this WebApplicationBuilder builder)
    {
        DataProtectionStorageOptions options = builder.Configuration
                .GetSection(DataProtectionStorageOptions.SectionName)
                .Get<DataProtectionStorageOptions>()
            ?? new DataProtectionStorageOptions();

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            throw new InvalidOperationException("DataProtection:ApplicationName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.BlobName))
        {
            throw new InvalidOperationException("DataProtection:BlobName is required.");
        }

        IDataProtectionBuilder dataProtection = builder.Services
                .AddDataProtection()
                .SetApplicationName(options.ApplicationName);

        if (builder.Environment.IsDevelopment())
        {
            dataProtection.PersistKeysToAzureBlobStorage(
                    serviceProvider =>
                    {
                        BlobContainerClient container = serviceProvider
                                .GetRequiredKeyedService<BlobContainerClient>(
                                    ContainerClientKey);

                        return container.GetBlobClient(options.BlobName);
                    });

            return;
        }

        if (!builder.Environment.IsProduction())
        {
            return;
        }

        if (!Uri.TryCreate(options.BlobUri, UriKind.Absolute,
                out Uri? blobUri) || blobUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("DataProtection:BlobUri must be an absolute HTTPS URI in Production.");
        }

        if (!Uri.TryCreate(options.KeyVaultKeyIdentifier, UriKind.Absolute,
                out Uri? keyIdentifier) || keyIdentifier.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("DataProtection:KeyVaultKeyIdentifier must be an absolute HTTPS URI in Production.");
        }

        DefaultAzureCredential credential = new();

        dataProtection.PersistKeysToAzureBlobStorage(
                blobUri,
                credential)
            .ProtectKeysWithAzureKeyVault(
                keyIdentifier,
                credential);
    }

    public static async Task EnsureDevelopmentDataProtectionBlobAsync(
            this WebApplication app,
            CancellationToken cancellationToken = default)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        DataProtectionStorageOptions options = app.Configuration
                .GetSection(DataProtectionStorageOptions.SectionName)
                .Get<DataProtectionStorageOptions>()
            ?? new DataProtectionStorageOptions();

        BlobContainerClient container = app.Services.GetRequiredKeyedService<
                    BlobContainerClient>(
                    ContainerClientKey);

        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        BlobClient blob = container.GetBlobClient(options.BlobName);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            await blob.UploadAsync(BinaryData.FromString("<repository></repository>"),
                overwrite: false,
                cancellationToken);
        }
    }
}
