# VibeCast — Deploy the AI-Enabled .NET Application to Azure

**Branch:** `section-09-prod-prep-complete`  
**Checkpoint:** Section completion reference

Move the application's relational and file storage to PostgreSQL and Blob Storage, then run the application locally in containers.

## What this checkpoint contains

The AI features and Section 8 engineering checkpoint are supplied; the starting checkpoint still uses SQLite and LocalBlobStorage for media.

PostgreSQL migrations/configuration, AzureBlobStorage, Azurite orchestration, shared Data Protection storage and Dockerfile hosting are present. AppHost runs the web container in Development.

## Lesson and code map

| Current Udemy lesson | Relevant code in this checkpoint | Engineering objective |
|---|---|---|
| Move Relational Data to PostgreSQL for Production | [src/VibeCast.Infrastructure/DependencyInjection.cs](src/VibeCast.Infrastructure/DependencyInjection.cs) | Replace the SQLite provider with the PostgreSQL configuration in this section. |
| Implement Azurite for Development File Storage | [src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs](src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs) | Implement the media storage abstraction over Blob Storage. |
| Complete Azurite Implementation and Test | [VibeCast.AppHost/AppHost.cs](VibeCast.AppHost/AppHost.cs) | Connect the local emulator and verify media roundtrips. |
| Normalize Container Configurations and Run Locally | [Dockerfile](Dockerfile) | Run the container with AppHost-provided database, storage and configuration. |

The map follows the live Udemy lesson titles and inspected code. Lecture numbers can change when practice articles are inserted. Exact spoken sequencing and timestamps have not been verified against the reattached transcript archive.

## Setup for this branch

Use **.NET 10 / C# 14** and the package versions checked into this branch. Review [Directory.Packages.props](Directory.Packages.props) before changing dependencies. Restore/build the checkpoint before diagnosing a cloud issue.

### PostgreSQL, Azurite and the web container

Start a Docker-compatible container runtime. [AppHost.cs](VibeCast.AppHost/AppHost.cs) provisions PostgreSQL, Azurite and media/Data Protection containers, then builds the web Dockerfile. It injects the database and storage references into the web container. The default local web endpoint is `http://localhost:8080`; the AppHost resource output is authoritative.

Configure the AppHost parameters using its user-secrets store or the environment. Its [project file](VibeCast.AppHost/VibeCast.AppHost.csproj) declares the user-secrets ID. The current AppHost parameter names are:

- `Parameters:foundry-project-endpoint`
- `Parameters:foundry-chat-model-deployment`
- `Parameters:foundry-image-model-deployment`
- `Parameters:foundry-api-key`
- `Parameters:speech-endpoint`
- `Parameters:speech-api-key`
- `Parameters:content-understanding-endpoint`
- `Parameters:content-understanding-api-key`
- `Parameters:knowledge-storage-service-uri`
- `Parameters:knowledge-search-endpoint`

These become the Foundry, Speech, ContentUnderstanding and KnowledgeStorage settings in the web container. Media uses local Azurite here; the knowledge-source URI and Search endpoint still point to the configured external knowledge services. User secrets from the web project are not automatically the container's configuration.

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet run --project VibeCast.AppHost
```

Do not use the older SQLite path or the generic direct-web launch as this branch's default setup. AppHost explicitly selects Development; this is a local production-preparation checkpoint, not evidence of a completed hosted Azure deployment. Development startup applies migrations and seeds data. DataProtection production configuration additionally uses BlobUri and KeyVaultKeyIdentifier; inspect [DataProtectionExtensions.cs](src/VibeCast.Web/Security/DataProtectionExtensions.cs) for the environment-specific behavior.

### Configuration reference

| Options file | Settings declared by this checkpoint |
|---|---|
| [src/VibeCast.Infrastructure/Options/ContentUnderstandingOptions.cs](src/VibeCast.Infrastructure/Options/ContentUnderstandingOptions.cs) | `Endpoint`, `ApiKey` |
| [src/VibeCast.Infrastructure/Options/DataProtectionStorageOptions.cs](src/VibeCast.Infrastructure/Options/DataProtectionStorageOptions.cs) | `ApplicationName`, `BlobName`, `BlobUri`, `KeyVaultKeyIdentifier` |
| [src/VibeCast.Infrastructure/Options/FoundryOptions.cs](src/VibeCast.Infrastructure/Options/FoundryOptions.cs) | `ProjectEndpoint`, `ChatModelDeployment`, `ImageModelDeployment`, `ApiKey`, `MaxRetries`, `ChatTimeoutSeconds`, `MaxConcurrentChatRequests`, `ChatQueueLimit` |
| [src/VibeCast.Infrastructure/Options/KnowledgeStorageOptions.cs](src/VibeCast.Infrastructure/Options/KnowledgeStorageOptions.cs) | `ServiceUri`, `ContainerName`, `SearchEndpoint`, `KnowledgeBaseName`, `KnowledgeSourceName`, `SourcePathField` |
| [src/VibeCast.Infrastructure/Options/SpeechOptions.cs](src/VibeCast.Infrastructure/Options/SpeechOptions.cs) | `Endpoint`, `ApiKey` |

Use the matching configuration section names declared in these files. Environment variables use double underscores instead of colons. Keep credentials outside committed files. Startup validation may require settings for registered services even if your current task calls only one of them.

Knowledge storage and retrieval use the configured Azure identity through DefaultAzureCredential. Ensure that identity can access the configured storage and search resources; uploading a file does not guarantee indexing has finished.

The local teaching account is defined in [SeedData.cs](src/VibeCast.Infrastructure/Data/SeedData.cs). Use it only in the local Development environment. [Program.cs](src/VibeCast.Web/Program.cs) and [DependencyInjection.cs](src/VibeCast.Infrastructure/DependencyInjection.cs) are authoritative for startup and registered services.

## Verify the learning objective

```bash
dotnet test VibeCast.sln --configuration Release --no-build
```

Use existing tests as regression checks, then perform the relevant feature smoke check below. “Complete” identifies a teaching reference; this documentation update does not certify a fresh build, passing test run or deployed environment.

Roundtrip bytes through Azurite, verify owner-scoped storage keys, restart the local containers and confirm PostgreSQL-backed state and Data Protection storage behave as expected.

## Checkpoint boundaries

- Navigation pages are not a feature checklist. Workflow, approval, evaluation or observability screens can remain presentation scaffolds; inspect their service calls before treating them as implemented capabilities.
- The channel queue is local and non-durable. Its presence does not provide a hosted message broker or durable execution.
- The recorded image-generation integration uses IImageGenerator with MEAI001 suppressed. Keep the checkpoint's dependency set when following the video; treat API migration as separate work.

## Related checkpoints

- [section-09-prod-prep-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-complete) — this branch
- [section-09-prod-prep-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-practice)
- [section-09-prod-prep-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-start)

Return to the [course branch index](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/main). The earlier generic branch-strategy/local-development documents may describe older snapshots; use this README and the linked source files for this branch's current setup.
