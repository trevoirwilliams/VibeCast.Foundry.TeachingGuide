using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Common;

namespace VibeCast.Infrastructure.Storage;

public sealed class AzureBlobKnowledgeSourceStorage(
    BlobContainerClient containerClient,
    ILogger<AzureBlobKnowledgeSourceStorage> logger) : IKnowledgeSourceStorage
{
    public async Task<StoredKnowledgeSource> SaveAsync(
        Guid mediaAssetId,
        string ownerId,
        string originalFileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException("A media asset identifier is required.",
                nameof(mediaAssetId));
        }

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

        ArgumentNullException.ThrowIfNull(content);

        string storageKey = BuildStorageKey(
            mediaAssetId,
            ownerId,
            originalFileName);

        BlobClient blobClient = containerClient.GetBlobClient(storageKey);

        BlobUploadOptions uploadOptions = new()
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            },
            Metadata = new Dictionary<string, string>
            {
                ["mediaAssetId"] = mediaAssetId.ToString("D"),
                ["ownerKey"] = BuildOwnerKey(ownerId)
            }
        };

        try
        {
            await blobClient.UploadAsync(
                content,
                uploadOptions,
                cancellationToken);

            logger.LogInformation("Stored knowledge-source copy {StorageKey} for media asset {MediaAssetId}.",
                storageKey,
                mediaAssetId);

            return new StoredKnowledgeSource(
                StorageKey: storageKey,
                BlobUri: blobClient.Uri);
        }
        catch (RequestFailedException exception)
        {
            logger.LogError(
                exception,
                "Azure Blob Storage rejected knowledge-source promotion for media asset {MediaAssetId}.",
                mediaAssetId);

            throw new SafeApplicationException(
                "VibeCast could not store the knowledge-source copy in Azure Blob Storage.",
                exception);
        }
    }

    public async Task DeleteAsync(
        Guid mediaAssetId,
        string ownerId,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        string storageKey = BuildStorageKey(
            mediaAssetId,
            ownerId,
            originalFileName);

        BlobClient blobClient = containerClient.GetBlobClient(storageKey);

        try
        {
            await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                conditions: null,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Removed knowledge-source copy {StorageKey} for media asset {MediaAssetId}.",
                storageKey,
                mediaAssetId);
        }
        catch (RequestFailedException exception)
        {
            logger.LogError(
                exception,
                "Azure Blob Storage rejected knowledge-source withdrawal for media asset {MediaAssetId}.",
                mediaAssetId);

            throw new SafeApplicationException(
                "VibeCast could not withdraw the knowledge-source copy from Azure Blob Storage.",
                exception);
        }
    }

    public Uri GetUri(Guid mediaAssetId, string ownerId, string originalFileName)
    {
        string storageKey = BuildStorageKey(
            mediaAssetId,
            ownerId,
            originalFileName);

        return containerClient.GetBlobClient(storageKey).Uri;
    }

    private static string BuildStorageKey(
        Guid mediaAssetId,
        string ownerId,
        string originalFileName)
    {
        string extension = Path.GetExtension(
                    Path.GetFileName(originalFileName))
                .ToLowerInvariant();

        if (extension is not ".pdf" and not ".txt")
        {
            throw new SafeApplicationException("Only PDF and TXT files can be promoted to the knowledge store.");
        }

        return $"owners/{BuildOwnerKey(ownerId)}/sources/{mediaAssetId:N}{extension}";
    }

    private static string BuildOwnerKey(string ownerId)
    {
        byte[] ownerBytes = Encoding.UTF8.GetBytes(ownerId);

        byte[] hash = SHA256.HashData(ownerBytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
