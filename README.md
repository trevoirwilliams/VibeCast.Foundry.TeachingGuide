# VibeCast — Prompt Engineering, Structured Outputs, and Typed C# Tools

**Branch:** `section-05-episode-planner-start-ui`  
**Checkpoint:** Section starting checkpoint (UI variant)

Generate a typed episode plan, enforce domain rules, perform one bounded repair, use editorial-policy tools and save the accepted result.

## What this checkpoint contains

Episode-concept generation and text streaming from Section 4 are present.

The current section's new implementation is the work to add as you follow the mapped lessons below. Files marked “introduced later” are intentionally absent. Existing files may still require changes during the section.

### What the UI variant changes

Compared with [section-05-episode-planner-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-start), this checkpoint changes:

- [src/VibeCast.Web/Components/Layout/NavMenu.razor](src/VibeCast.Web/Components/Layout/NavMenu.razor)
- [src/VibeCast.Web/Components/Pages/EpisodeCreate.razor](src/VibeCast.Web/Components/Pages/EpisodeCreate.razor)
- [src/VibeCast.Web/Components/Pages/EpisodeWorkspace.razor](src/VibeCast.Web/Components/Pages/EpisodeWorkspace.razor)
- [src/VibeCast.Web/Components/Pages/Episodes.razor](src/VibeCast.Web/Components/Pages/Episodes.razor)
- [src/VibeCast.Web/Components/Pages/Home.razor](src/VibeCast.Web/Components/Pages/Home.razor)

These differences are presentation files; the core backend checkpoint is shared with the corresponding non-UI branch.

## Lesson and code map

| Current Udemy lesson | Relevant code in this checkpoint | Engineering objective |
|---|---|---|
| Persist the Editorial Brief and Open the Episode Workspace - Part 1 | `src/VibeCast.Infrastructure/Episodes/EfEpisodeService.cs` — introduced later in this section | Persist the editorial brief through the application boundary; continue with Part 2. |
| Generate and Render a Typed Episode Plan with Structured Outputs | `src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs` — introduced later in this section | Request an EpisodePlan rather than free-form text. |
| Validate the Episode Plan and Perform One Bounded Repair | `src/VibeCast.Application/Episodes/EpisodePlanValidator.cs` — introduced later in this section | Check domain rules and allow one semantic repair. |
| Perform Episode Planning with Editorial Policy Tool Call | `src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningWithToolService.cs` — introduced later in this section | Require trusted editorial-policy context through a typed tool. |
| Save and Reload the Accepted Episode Plan | `src/VibeCast.Infrastructure/Episodes/EfEpisodeService.cs` — introduced later in this section | Persist and retrieve the accepted plan and metadata. |

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
| [src/VibeCast.Infrastructure/Options/FoundryOptions.cs](src/VibeCast.Infrastructure/Options/FoundryOptions.cs) | `ProjectEndpoint`, `ChatModelDeployment`, `ApiKey` |

Use the matching configuration section names declared in these files. Environment variables use double underscores instead of colons. Keep credentials outside committed files. Startup validation may require settings for registered services even if your current task calls only one of them.

The local teaching account is defined in [SeedData.cs](src/VibeCast.Infrastructure/Data/SeedData.cs). Use it only in the local Development environment. [Program.cs](src/VibeCast.Web/Program.cs) and [DependencyInjection.cs](src/VibeCast.Infrastructure/DependencyInjection.cs) are authoritative for startup and registered services.

## Verify the learning objective

```bash
dotnet test VibeCast.sln --configuration Release --no-build
```

Establish the starting test result before adding the section's behavior. Then test the new behavior independently of live AI where possible. A starting branch is not expected to perform features introduced later in the section.

Accept a valid plan; reject invalid segment totals; allow one repair only; require the policy tool; save and reload an accepted plan.

## Checkpoint boundaries

- Navigation pages are not a feature checklist. Workflow, approval, evaluation or observability screens can remain presentation scaffolds; inspect their service calls before treating them as implemented capabilities.
- The channel queue is local and non-durable. Its presence does not provide a hosted message broker or durable execution.

## Related checkpoints

- [section-05-episode-planner-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-complete)
- [section-05-episode-planner-complete-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-complete-ui)
- [section-05-episode-planner-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-practice)
- [section-05-episode-planner-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-start)
- [section-05-episode-planner-start-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-start-ui) — this branch

Return to the [course branch index](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/main). The earlier generic branch-strategy/local-development documents may describe older snapshots; use this README and the linked source files for this branch's current setup.
