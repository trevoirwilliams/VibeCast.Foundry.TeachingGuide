# VibeCast — Build Knowledge-Grounded Content Generation with Foundry IQ and Azure AI Search

**Branch:** `section-07-source-gallery-start`  
**Checkpoint:** Section starting checkpoint

Upload reusable documents and generate a blog from selected, owner-resolved knowledge sources with validated reference IDs.

## What this checkpoint contains

The episode media and document-relevance services from Section 6 are present.

The current section's new implementation is the work to add as you follow the mapped lessons below. Files marked “introduced later” are intentionally absent. Existing files may still require changes during the section.

## Lesson and code map

| Current Udemy lesson | Relevant code in this checkpoint | Engineering objective |
|---|---|---|
| Upgrade the VibeCast Source Gallery | [src/VibeCast.Infrastructure/Media/EfMediaAssetService.cs](src/VibeCast.Infrastructure/Media/EfMediaAssetService.cs) | Prepare reusable knowledge sources beyond one episode. |
| Configure Knowledge Source Upload to Azure Blob Storage | `src/VibeCast.Infrastructure/Storage/AzureBlobKnowledgeSourceStorage.cs` — introduced later in this section | Store documents for the knowledge-source ingestion flow. |
| Generate a Grounded Blog from Knowledge Source | `src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs` — introduced later in this section | Retrieve selected-source evidence and generate a blog. |
| Fix Reference and Filter Condition Implementation | `src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs` — introduced later in this section | Apply source allowlists and validate returned reference IDs. |

The map follows the live Udemy lesson titles and inspected code. Lecture numbers can change when practice articles are inserted. Exact spoken sequencing and timestamps have not been verified against the reattached transcript archive.

The completed workflow is knowledge-base-backed **blog generation**. The gallery-only checkpoint and the full retrieval checkpoint are distinct. Do not infer an application-managed chunker, embedding loop or an agent workflow merely from a RAG label. Reference-ID membership does not by itself prove that every generated claim is supported.

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

### Configuration reference

| Options file | Settings declared by this checkpoint |
|---|---|
| [src/VibeCast.Infrastructure/Options/BlobStorageOptions.cs](src/VibeCast.Infrastructure/Options/BlobStorageOptions.cs) | `RootPath`, `Capacity` |
| [src/VibeCast.Infrastructure/Options/ContentUnderstandingOptions.cs](src/VibeCast.Infrastructure/Options/ContentUnderstandingOptions.cs) | `Endpoint`, `ApiKey` |
| [src/VibeCast.Infrastructure/Options/FoundryOptions.cs](src/VibeCast.Infrastructure/Options/FoundryOptions.cs) | `ProjectEndpoint`, `ChatModelDeployment`, `ImageModelDeployment`, `ApiKey`, `MaxRetries` |
| [src/VibeCast.Infrastructure/Options/SpeechOptions.cs](src/VibeCast.Infrastructure/Options/SpeechOptions.cs) | `Endpoint`, `ApiKey` |

Use the matching configuration section names declared in these files. Environment variables use double underscores instead of colons. Keep credentials outside committed files. Startup validation may require settings for registered services even if your current task calls only one of them.

The local teaching account is defined in [SeedData.cs](src/VibeCast.Infrastructure/Data/SeedData.cs). Use it only in the local Development environment. [Program.cs](src/VibeCast.Web/Program.cs) and [DependencyInjection.cs](src/VibeCast.Infrastructure/DependencyInjection.cs) are authoritative for startup and registered services.

## Verify the learning objective

```bash
dotnet test VibeCast.sln --configuration Release --no-build
```

Establish the starting test result before adding the section's behavior. Then test the new behavior independently of live AI where possible. A starting branch is not expected to perform features introduced later in the section.

Select owned documents, verify retrieval is restricted to those sources and reject missing/unknown evidence references. Distinguish indexing delay from implementation failure.

## Checkpoint boundaries

- Navigation pages are not a feature checklist. Workflow, approval, evaluation or observability screens can remain presentation scaffolds; inspect their service calls before treating them as implemented capabilities.
- The channel queue is local and non-durable. Its presence does not provide a hosted message broker or durable execution.
- The recorded image-generation integration uses IImageGenerator with MEAI001 suppressed. Keep the checkpoint's dependency set when following the video; treat API migration as separate work.

## Related checkpoints

- [section-07-source-gallery-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-07-source-gallery-complete)
- [section-07-source-gallery-finish](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-07-source-gallery-finish)
- [section-07-source-gallery-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-07-source-gallery-practice)
- [section-07-source-gallery-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-07-source-gallery-start) — this branch

Return to the [course branch index](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/main). The earlier generic branch-strategy/local-development documents may describe older snapshots; use this README and the linked source files for this branch's current setup.
