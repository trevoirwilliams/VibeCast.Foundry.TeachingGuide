# VibeCast — Deploy the AI-Enabled .NET Application to Azure

**Branch:** `section-09-prod-prep-start`  
**Checkpoint:** Section starting checkpoint

Move the application's relational and file storage to PostgreSQL and Blob Storage, then run the application locally in containers.

## What this checkpoint contains

The AI features and Section 8 engineering checkpoint are supplied; the starting checkpoint still uses SQLite and LocalBlobStorage for media.

The current section's new implementation is the work to add as you follow the mapped lessons below. Files marked “introduced later” are intentionally absent. Existing files may still require changes during the section.

## Lesson and code map

| Current Udemy lesson | Relevant code in this checkpoint | Engineering objective |
|---|---|---|
| Move Relational Data to PostgreSQL for Production | [src/VibeCast.Infrastructure/DependencyInjection.cs](src/VibeCast.Infrastructure/DependencyInjection.cs) | Replace the SQLite provider with the PostgreSQL configuration in this section. |
| Implement Azurite for Development File Storage | `src/VibeCast.Infrastructure/Storage/AzureBlobStorage.cs` — introduced later in this section | Implement the media storage abstraction over Blob Storage. |
| Complete Azurite Implementation and Test | [VibeCast.AppHost/AppHost.cs](VibeCast.AppHost/AppHost.cs) | Connect the local emulator and verify media roundtrips. |
| Normalize Container Configurations and Run Locally | [Dockerfile](Dockerfile) | Run the container with AppHost-provided database, storage and configuration. |

The map follows the live Udemy lesson titles and inspected code. Lecture numbers can change when practice articles are inserted. Exact spoken sequencing and timestamps have not been verified against the reattached transcript archive.

### Known checkpoint limitation

This snapshot constructs ChatResilienceClient in DependencyInjection but returns a pipeline that bypasses it. Presence of the class does not mean its timeout/capacity policy runs. The Section 8 practice branch connects the wrapper, and Section 9 complete also uses it. This README update does not change the recorded implementation.

## Setup for this branch

Use **.NET 10 / C# 14** and the package versions checked into this branch. Review [Directory.Packages.props](Directory.Packages.props) before changing dependencies. Restore/build the checkpoint before diagnosing a cloud issue.

The web application uses SQLite and LocalBlobStorage for media in this checkpoint. Their configured paths can be relative to the process/content root; inspect the branch's appsettings and options rather than assuming every branch shares the same data directory. Save your work before changing checkpoints, and use a database appropriate to that branch's migrations.

Supply Foundry configuration to the web project using .NET user secrets or environment variables. `Foundry:ProjectEndpoint` is the repository's property name: its value is passed to AzureOpenAIClient, so use the endpoint expected by that client, not an arbitrary portal/project URL. The existing adapter uses `Foundry:ApiKey` and deployment names from the branch options.

```bash
dotnet dev-certs https --trust
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet run --project src/VibeCast.Web --launch-profile https
```

For the Aspire tracing route, run `dotnet run --project VibeCast.AppHost` after supplying the web project's settings. This AppHost launches the web project; it does not provision PostgreSQL or Azurite in this checkpoint.

### Configuration reference

| Options file | Settings declared by this checkpoint |
|---|---|
| [src/VibeCast.Infrastructure/Options/BlobStorageOptions.cs](src/VibeCast.Infrastructure/Options/BlobStorageOptions.cs) | `RootPath`, `Capacity` |
| [src/VibeCast.Infrastructure/Options/ContentUnderstandingOptions.cs](src/VibeCast.Infrastructure/Options/ContentUnderstandingOptions.cs) | `Endpoint`, `ApiKey` |
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

Establish the starting test result before adding the section's behavior. Then test the new behavior independently of live AI where possible. A starting branch is not expected to perform features introduced later in the section.

Roundtrip bytes through Azurite, verify owner-scoped storage keys, restart the local containers and confirm PostgreSQL-backed state and Data Protection storage behave as expected.

## Checkpoint boundaries

- Navigation pages are not a feature checklist. Workflow, approval, evaluation or observability screens can remain presentation scaffolds; inspect their service calls before treating them as implemented capabilities.
- The channel queue is local and non-durable. Its presence does not provide a hosted message broker or durable execution.
- The recorded image-generation integration uses IImageGenerator with MEAI001 suppressed. Keep the checkpoint's dependency set when following the video; treat API migration as separate work.

## Related checkpoints

- [section-09-prod-prep-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-complete)
- [section-09-prod-prep-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-practice)
- [section-09-prod-prep-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-start) — this branch

Return to the [course branch index](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/main). The earlier generic branch-strategy/local-development documents may describe older snapshots; use this README and the linked source files for this branch's current setup.
