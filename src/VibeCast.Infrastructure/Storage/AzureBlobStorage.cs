using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Common;
using VibeCast.Infrastructure.Options;

namespace VibeCast.Infrastructure.Storage;

public sealed class AzureBlobStorage(
    BlobContainerClient containerClient,
    ILogger<AzureBlobStorage> logger) : IBlobStorage
{
    public async Task<StoredBlob> SaveAsync(
        Stream content,
        string ownerId,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("An authenticated owner is required.",
                nameof(ownerId));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("The original file name is required.",
                nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("The content type is required.",
                nameof(contentType));
        }

        string displayName = Path.GetFileName(originalFileName);
        string extension = Path.GetExtension(displayName)
                .ToLowerInvariant();
        string ownerKey = Helpers.BuildOwnerKey(ownerId);
        string storageKey = $"owners/{ownerKey}/media/{Guid.NewGuid():N}{extension}";

        BlobClient blobClient = containerClient.GetBlobClient(storageKey);
        BlobUploadOptions uploadOptions = new()
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            },
            Metadata = new Dictionary<string, string>
            {
                ["ownerKey"] = ownerKey,
                ["storagePurpose"] = "media"
            }
        };

        try
        {
            await blobClient.UploadAsync(
                content,
                uploadOptions,
                cancellationToken);

            Response<BlobProperties> properties = await blobClient.GetPropertiesAsync(
                    cancellationToken: cancellationToken);

            logger.LogInformation("Stored media blob {StorageKey}.", storageKey);

            return new StoredBlob(
                StorageKey: storageKey,
                OriginalFileName: displayName,
                ContentType: contentType,
                SizeBytes:properties.Value.ContentLength);
        }
        catch (RequestFailedException exception)
        {
            logger.LogError(exception, "Blob Storage rejected media write for {StorageKey}.", storageKey);

            throw new SafeApplicationException("VibeCast could not store the media file.", exception);
        }
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = containerClient.GetBlobClient(storageKey);

        try
        {
            return await blobClient.OpenReadAsync(
                new BlobOpenReadOptions(
                    allowModifications: false),
                cancellationToken);
        }
        catch (RequestFailedException exception)
        {
            logger.LogError(exception, "Blob Storage could not open media blob {StorageKey}.", storageKey);

            throw new SafeApplicationException("VibeCast could not open the media file.", exception);
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = containerClient.GetBlobClient(storageKey);

        try
        {
            await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                conditions: null,
                cancellationToken: cancellationToken);

            logger.LogInformation("Deleted media blob {StorageKey}.",
                storageKey);
        }
        catch (RequestFailedException exception)
        {
            logger.LogError(exception, "Blob Storage could not delete media blob {StorageKey}.", storageKey);

            throw new SafeApplicationException("VibeCast could not delete the media file.", exception);
        }
    }
}
