using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using VibeCast.Infrastructure.Options;

namespace VibeCast.Web.Security;

public static class DataProtectionExtensions
{
    private const string ContainerClientKey = "data-protection";
    public static void AddVibeCastDataProtection(this WebApplicationBuilder builder, TokenCredential azureCredential)
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
                        // PRACTICE S09-05A: Select the development key-ring blob.
                        // Lesson: Complete Azurite Implementation and Test.
                        // 1. Resolve the keyed BlobContainerClient using ContainerClientKey.
                        // 2. Select options.BlobName from that container and return a BlobClient.
                        //    Return the client, not its URI or downloaded content.
                        // The outer call persists the key ring; creation of the local blob is supplied below.
                        // Optional API hint: docs/practice/README.md#s09-05-data-protection
                        throw new NotImplementedException("S09-05A: select the Data Protection blob.");
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

        // PRACTICE S09-05B: Persist and protect the production key ring.
        // 1. Configure dataProtection to persist keys at the validated blobUri.
        // 2. On that builder, configure Key Vault protection using keyIdentifier.
        // 3. Pass the supplied azureCredential to both calls. No return object is needed here.
        // Blob Storage holds the Data Protection key ring; Key Vault protects those keys.
        // Keep the supplied environment guards so Development does not require Key Vault.
        // Check in Azure: the key-ring blob persists across app restarts and can be read by the app.
        // Optional API hint: docs/practice/README.md#s09-05-data-protection
        throw new NotImplementedException("S09-05B: persist and protect production keys.");
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
