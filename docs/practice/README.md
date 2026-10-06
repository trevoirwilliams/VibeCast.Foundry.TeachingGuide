# Backend practice: Deploy the AI-Enabled .NET Application to Azure

## Start here

Use the root [README](../../README.md) for this branch's setup and pinned package versions. Supporting UI and application code are supplied so you can focus on the backend tasks below. This enriched scaffold may contain supporting files that appear later in the recording.

Watch the feature explanation, pause before the implementation, and try the matching task. If you have already watched it, attempt the task before opening the solution. Read the numbered comments first; expand an optional hint only when you need it. You may ask Copilot to explain one unfamiliar API or failed check.

Search for `PRACTICE S` in C# files. Each named `NotImplementedException` is an intentional gap. Replace it with real behavior, not dummy output. Task-returning stubs omit `async`; add it when using `await`. Streaming tasks need an async iterator, `yield return` and the cancellation attribute described in their hints.

Save or commit your work before switching branches. Each section starts independently; your unfinished code does not carry forward automatically.

<a id="s09-01-saveasync"></a>
### S09-01: SaveAsync

- **File:** [src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs](../../src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs)
- **Attempt before:** **Implement Azurite for Development File Storage**.
- **Already provided / prerequisites:** BlobContainerClient, Helpers.BuildOwnerKey and StoredBlob are supplied; PostgreSQL/AppHost changes are outside this gap.

1. Validate inputs and separate the display filename from the storage key.
2. Build an owner-scoped generated key using the supplied owner helper.
3. Upload the stream with content type and media metadata.
4. Read the stored length and map it to StoredBlob.
5. Translate provider failures to the existing safe application error.

<details>
<summary>Optional API hint</summary>

Use Path.GetFileName/Path.GetExtension and Helpers.BuildOwnerKey(ownerId). The key shape is owners/{ownerKey}/media/{new-guid}{extension}. GetBlobClient selects the destination. BlobUploadOptions carries BlobHttpHeaders.ContentType and metadata ownerKey/storagePurpose (media). Await UploadAsync, then GetPropertiesAsync; map properties.Value.ContentLength to SizeBytes. Catch RequestFailedException and wrap in SafeApplicationException; do not swallow cancellation.

</details>

**Check your result:**

- Save known bytes in Azurite → StoredBlob reports their length and content type.
- Different owners produce different key prefixes; the original name does not choose the destination path.
- Invalid inputs fail before upload.

**Explain:** Why use the stored content length rather than assume every input stream exposes Length?

<a id="s09-02-openreadasync"></a>
### S09-02: OpenReadAsync

- **File:** [src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs](../../src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs)
- **Attempt before:** **Complete Azurite Implementation and Test**.
- **Already provided / prerequisites:** The container client and SafeApplicationException are supplied.

1. Resolve a blob client from the supplied storage key.
2. Open a readable stream with cancellation and disallow concurrent modification.
3. Return the open stream for the caller to consume and dispose.
4. Translate provider failures without turning cancellation into success.

<details>
<summary>Optional API hint</summary>

Use containerClient.GetBlobClient, BlobOpenReadOptions(allowModifications: false) and blobClient.OpenReadAsync. Return Stream; do not wrap it in a using that disposes it before returning. Catch RequestFailedException for safe error translation. Later errors while reading a returned stream remain the caller's responsibility.

</details>

**Check your result:**

- Save then open → reading the returned stream yields the original bytes.
- The stream is usable after this method returns; the caller disposes it.
- Opening a missing blob fails rather than returning an empty replacement stream.

**Explain:** Who owns the returned stream, and when should it be disposed?

<a id="s09-03-deleteasync"></a>
### S09-03: DeleteAsync

- **File:** [src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs](../../src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs)
- **Attempt before:** **Complete Azurite Implementation and Test**.
- **Already provided / prerequisites:** The container client and safe application error type are supplied.

1. Resolve a blob client from the storage key.
2. Delete if present, including snapshots, with caller cancellation.
3. Treat an already-missing blob as a successful no-op.
4. Translate provider failures to the existing safe application error.

<details>
<summary>Optional API hint</summary>

Use DeleteIfExistsAsync with DeleteSnapshotsOption.IncludeSnapshots, null conditions and the caller token. The method returns Task, so no application result object is needed. Catch RequestFailedException; keep cancellation separate.

</details>

**Check your result:**

- Save, delete, then delete again → both deletes complete.
- After deletion, the blob cannot be opened as existing content.

**Explain:** Why is an idempotent delete useful when cleanup is retried?

## Storage roundtrip

Use the configured Azurite instance: save a short known byte sequence, check StoredBlob metadata, open and read it, dispose the returned stream, delete it, and delete again. These storage checks do not need a live AI call. Running the full application may still require the other registered service settings described in the root README.

This checkpoint covers PostgreSQL, Azurite and local containers. It does not represent a completed hosted Azure deployment.

## Build, compare and review

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet test VibeCast.sln --configuration Release --no-build
```

An intentional exercise exception is expected when an unfinished path runs; a compiler error is not the exercise. Some tests cover other gaps, so use a focused filter where supplied. The examples above are acceptance checks, not a claim that every case has an automated test. Use local fakes for deterministic model responses; use the configured storage emulator for storage integration.

After your attempt, compare the relevant method with the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/96a17cfb477d9ecee0860b6461e44144a222bb6a). Explain behavioral differences instead of matching every line. Change one input, predict the result, then check your prediction.

<details>
<summary>Instructor validation status</summary>

This revision repairs misplaced exercise bodies and adds staged guidance. Full build, restored-solution tests and cloud smoke checks must be verified; the revision is not a certification that those checks passed. Before release, build the untouched scaffold, restore the missing implementations in a disposable copy, and run the tests. Distinguish expected exercise failures from unrelated failures. Lesson references use titles; exact transcript pause times remain unverified.

</details>
