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
    public Task<StoredBlob> SaveAsync(
        Stream content,
        string ownerId,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // PRACTICE S09-01: Store media using an owner-scoped generated key, a safe display filename and content metadata. Return the stored length and translate provider failures safely.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S09-01: implement SaveAsync.");
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        // PRACTICE S09-02: Open the stored blob as a readable stream, preserve cancellation and translate storage failures into the existing safe application error.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S09-02: implement OpenReadAsync.");
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        // PRACTICE S09-03: Delete an existing blob and its snapshots without failing when it is already absent. Preserve cancellation and safe error translation.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S09-03: implement DeleteAsync.");
    }
}
