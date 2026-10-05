# VibeCast — Build Multimodal AI Features with Microsoft Foundry

**Branch:** `section-06-episode-media-start`  
**Checkpoint:** Section starting checkpoint

Analyze artwork, generate validated promotional images, transcribe recordings and assess supporting documents for an episode.

## What this checkpoint contains

The structured planner, domain validation, bounded repair, editorial-policy tool and accepted-plan persistence from Section 5 are present.

The current section's new implementation is the work to add as you follow the mapped lessons below. Files marked “introduced later” are intentionally absent. Existing files may still require changes during the section.

## Lesson and code map

| Current Udemy lesson | Relevant code in this checkpoint | Engineering objective |
|---|---|---|
| Implement Foundry Image Understanding Model Call | `src/VibeCast.Infrastructure/AI/FoundryArtworkAnalysisService.cs` — introduced later in this section | Combine image content and episode context; return typed analysis. |
| Generate and Validate Promotional Artwork | `src/VibeCast.Infrastructure/AI/FoundryEpisodeArtworkGenerationService.cs` — introduced later in this section | Generate artwork and validate it before storage. |
| Transcribe and Attach an Episode Recording | `src/VibeCast.Infrastructure/AI/FoundryEpisodeTranscriptionService.cs` — introduced later in this section | Transcribe owned episode audio and persist the result. |
| Add Content Understanding Configurations and Services | `src/VibeCast.Infrastructure/AI/FoundryEpisodeResourceAnalysisService.cs` — introduced later in this section | Extract document content and assess relevance to the accepted plan. |

The map follows the live Udemy lesson titles and inspected code. Lecture numbers can change when practice articles are inserted. Exact spoken sequencing and timestamps have not been verified against the reattached transcript archive.

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
| [src/VibeCast.Infrastructure/Options/FoundryOptions.cs](src/VibeCast.Infrastructure/Options/FoundryOptions.cs) | `ProjectEndpoint`, `ChatModelDeployment`, `ApiKey`, `MaxRetries` |

Use the matching configuration section names declared in these files. Environment variables use double underscores instead of colons. Keep credentials outside committed files. Startup validation may require settings for registered services even if your current task calls only one of them.

The local teaching account is defined in [SeedData.cs](src/VibeCast.Infrastructure/Data/SeedData.cs). Use it only in the local Development environment. [Program.cs](src/VibeCast.Web/Program.cs) and [DependencyInjection.cs](src/VibeCast.Infrastructure/DependencyInjection.cs) are authoritative for startup and registered services.

## Verify the learning objective

```bash
dotnet test VibeCast.sln --configuration Release --no-build
```

Establish the starting test result before adding the section's behavior. Then test the new behavior independently of live AI where possible. A starting branch is not expected to perform features introduced later in the section.

Check an owned image, a generated PNG, a short audio clip and a relevant/irrelevant document. Confirm invalid or foreign-owned assets do not become accepted episode content.

## Checkpoint boundaries

- Navigation pages are not a feature checklist. Workflow, approval, evaluation or observability screens can remain presentation scaffolds; inspect their service calls before treating them as implemented capabilities.
- The channel queue is local and non-durable. Its presence does not provide a hosted message broker or durable execution.

## Related checkpoints

- [section-06-episode-media-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-complete)
- [section-06-episode-media-complete-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-complete-ui)
- [section-06-episode-media-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-practice)
- [section-06-episode-media-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-start) — this branch
- [section-06-episode-media-start-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-start-ui)

Return to the [course branch index](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/main). The earlier generic branch-strategy/local-development documents may describe older snapshots; use this README and the linked source files for this branch's current setup.
