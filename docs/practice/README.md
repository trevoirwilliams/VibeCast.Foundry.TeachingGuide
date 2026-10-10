# Backend practice: Deploy the AI-Enabled .NET Application to Azure

## Start here

Use the root [README](../../README.md) for this branch's setup and pinned package versions. Supporting UI and application code are supplied so you can focus on the backend tasks below. This enriched scaffold may contain supporting files that appear later in the recording.

Watch the feature explanation, pause before the implementation, and try the matching task. If you have already watched it, attempt the task before opening the solution. Read the numbered comments first; expand an optional hint only when you need it. You may ask Copilot to explain one unfamiliar API or failed check.

Search for `PRACTICE S` in C# files. Each named `NotImplementedException` is an intentional gap. Replace it with real behavior, not dummy output. Task-returning storage stubs omit `async`; add it when using `await`. The other gaps configure existing builders or return clients; their comments identify which.

This branch includes the supporting production code from Complete at `c2815618038f285631c54fee3a5e42ce80e7b66f`. Production identity selection and AI client authentication are supplied, not additional identity lessons. Use the published Udemy lesson titles below.

Complete S09-04, S09-05A and S09-06 before starting the full local app. S09-01 to S09-03 are needed for media operations. S09-05B and S09-07 apply to Production. An exception naming one of these unfinished tasks is expected until you implement it.

| Published lesson | Activity |
|---|---|
| Clean Up and Stabilize for Production | Inspect the supplied provider → resilience → logging chain and shared telemetry registration. |
| Move Relational Data to PostgreSQL for Production | S09-04: configure the local provider; keep the supplied PostgreSQL migrations. |
| Implement Azurite for Development File Storage | S09-01 to S09-03: implement media upload, read and delete. |
| Complete Azurite Implementation and Test | S09-05: configure Data Protection persistence and protection; verify storage. |
| Normalize Container Configurations and Run Locally | S09-06: select the media client; configure AppHost parameters and inspect the correct container. |
| Provision Azure Infrastructure | Apply the published lesson's resource setup; no additional C# gap. |
| Publish and Deploy VibeCast to Azure Container Apps | Use the completed app and supply production configuration; verify an actual application operation. |
| Add Application Insights and Production Health Monitoring | S09-07: configure the production exporter; verify a request trace and health separately. |

The uploaded identity recordings are not listed as separate published lessons in the reviewed curriculum, so their plumbing remains supplied. Provisioning/deployment transcript filenames do not establish a match to the published videos; this update does not invent scripts or exact pause times for them.

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
- **Attempt before:** the matching method is demonstrated in **Implement Azurite for Development File Storage**; check the result during **Complete Azurite Implementation and Test**.
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
- **Attempt before:** the matching method is demonstrated in **Implement Azurite for Development File Storage**; check the result during **Complete Azurite Implementation and Test**.
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

<a id="s09-04-postgresql"></a>
### S09-04: Configure PostgreSQL

- **File:** [DependencyInjection.cs](../../src/VibeCast.Infrastructure/DependencyInjection.cs)
- **Lesson:** **Move Relational Data to PostgreSQL for Production**.
- **Supplied:** AppHost database provisioning, connection-string validation, the design-time factory, PostgreSQL migrations and production Entra authentication.

Configure the options inside the non-production DbContext factory callback. Use the supplied connection string and PostgreSQL provider; do not return a context or hardcode a password.

<details>
<summary>Optional API hint</summary>

Call UseNpgsql on the supplied options builder with connectionString. AddDbContextFactory already defines the factory registration. The design-time factory is a separate, supplied path used by EF tooling.

</details>

**Check:** After the other local startup gaps are complete, start AppHost, sign in and save an episode. Restart the resources without deleting volumes and confirm the episode remains. Retain the existing migrations: the recording's deletion of SQLite migrations was a one-time provider transition already reflected here.

**Explain:** Why does the runtime need AppHost configuration even when EF can construct a context at design time?

<a id="s09-05-data-protection"></a>
### S09-05: Persist and protect Data Protection keys

- **File:** [DataProtectionExtensions.cs](../../src/VibeCast.Web/Security/DataProtectionExtensions.cs)
- **Lesson:** **Complete Azurite Implementation and Test**.
- **Supplied:** Application name, environment guards, URI validation, credential, and development container/blob creation.

**Part A (Development):** In the existing persistence callback, resolve the keyed container and return the BlobClient selected by options.BlobName. Do not upload or download the key ring yourself here.

**Part B (Production):** Configure the existing dataProtection builder to persist at blobUri, then protect the keys using keyIdentifier. Both calls use azureCredential. The method itself returns void.

<details>
<summary>Optional API hint</summary>

Part A: GetRequiredKeyedService&lt;BlobContainerClient&gt;(ContainerClientKey), then GetBlobClient(options.BlobName).

Part B: PersistKeysToAzureBlobStorage(blobUri, azureCredential), then ProtectKeysWithAzureKeyVault(keyIdentifier, azureCredential).

</details>

**Check locally:** In Storage Explorer, inspect vibecast-dataprotection/keys.xml after signing in. Restart the application without clearing its storage and confirm the existing key ring remains usable.

**Check in Azure:** With the production identity permissions configured, verify that the key-ring blob persists across a restart and the application can still use it. This requires real Azure resources; Azurite does not emulate Key Vault.

**Explain:** Blob Storage persists the application's key ring; Key Vault protects those keys. Neither location is the media-upload container.

<a id="s09-06-media-client"></a>
### S09-06: Select the media container

- **File:** [DependencyInjection.cs](../../src/VibeCast.Infrastructure/DependencyInjection.cs)
- **Lesson:** **Normalize Container Configurations and Run Locally**.
- **Supplied:** Development and production keyed client registrations; the unkeyed knowledge container.

Inside the IBlobStorage factory, resolve the media container and logger, then return an AzureBlobStorage instance. Keep the service key "media"; using the unkeyed client can send uploads to knowledge storage.

<details>
<summary>Optional API hint</summary>

Resolve GetRequiredKeyedService&lt;BlobContainerClient&gt;("media") and GetRequiredService&lt;ILogger&lt;AzureBlobStorage&gt;&gt;(), then pass both to the AzureBlobStorage constructor. Return the storage implementation, not the container client.

</details>

**Check:** Upload a file and inspect vibecast-media in Storage Explorer. Its key should be owners/{ownerKey}/media/{generated-name}. A successful UI upload alone does not prove it reached the correct container.

**Explain:** Why can the wrong container registration produce a successful upload rather than an exception?

<a id="s09-07-telemetry"></a>
### S09-07: Export production telemetry

- **File:** [Extensions.cs](../../VibeCast.ServiceDefaults/Extensions.cs)
- **Lesson:** **Add Application Insights and Production Health Monitoring**.
- **Supplied:** Instrumentation, application activity sources, Production/configuration checks, local OTLP fallback, and health endpoints.

Inside the Production branch, add the Azure Monitor exporter to the existing openTelemetry builder and configure its connection string. Keep one telemetry pipeline.

<details>
<summary>Optional API hint</summary>

UseAzureMonitorExporter accepts an options callback. Set its ConnectionString from insightsConnectionString. The environment condition and UseOtlpExporter fallback are already implemented.

</details>

**Check locally:** Aspire continues receiving telemetry through the supplied OTLP configuration.

**Check in Azure:** Configure APPLICATIONINSIGHTS_CONNECTION_STRING, trigger a normal application operation, and inspect its trace in Application Insights. Separately request /health and /alive. These paths are excluded from request tracing and currently only exercise the application's self-check; they do not prove database, storage or AI access.

**Explain:** Why is a successful health response insufficient evidence that an AI request works?

## Storage roundtrip

Use the configured Azurite instance: save a short known byte sequence, check StoredBlob metadata, open and read it, dispose the returned stream, delete it, and delete again. Verify different owners use different key prefixes. These storage operations do not require an AI call; starting the full application still needs its registered service configuration.

## Build and check

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet test tests/VibeCast.Domain.Tests/VibeCast.Domain.Tests.csproj --configuration Release --no-build
dotnet test tests/VibeCast.Application.Tests/VibeCast.Application.Tests.csproj --configuration Release --no-build
```

A successful build checks the scaffold's syntax; the existing domain/application tests do not verify these storage and deployment exercises. Use the task-specific checks above after implementation.

The full-solution test command also starts HealthEndpointTests. Its existing fixture does not provision PostgreSQL/Azurite or inject their configuration. Starting AppHost separately does not automatically configure that test host. The inspected Complete checkpoint's CI fails there for a missing PostgreSQL connection string. Do not remove validation or comment out tests to hide this failure.

## Compare after your attempt

Compare the relevant block with the [pinned Complete checkpoint](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/c2815618038f285631c54fee3a5e42ce80e7b66f). All seven exercises fit into the same methods or callbacks as the reference. Explain behavioral differences instead of matching formatting. Preserve your work before switching branches.

Production identity, cloud AI client registrations and the initial schema SQL are supplied to match that checkpoint. The schema must be applied separately in Production; startup migration and seeding are Development-only. The repository snapshot does not include the infrastructure assets shown in every recording. This practice update covers the application code and does not establish a verified Azure deployment.
