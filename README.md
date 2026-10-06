# VibeCast — Foundations of Generative AI Development With .NET

**Branch:** `section-04-ai-client-practice`  
**Checkpoint:** Backend practice — instructor review draft

Connect an application-owned AI service to Microsoft Foundry, generate an episode concept and stream text through IChatClient.

## What this checkpoint contains

The authenticated Blazor shell, Identity, SQLite, domain/application projects, local blob abstraction, channel queue and test projects are supplied.

This branch imports the completed section's supporting UI, contracts and configuration, then leaves **2 backend tasks** intentionally unfinished; S04-01 is a supplied request-building example. Open [PRACTICE.md](PRACTICE.md) and the [task guide](docs/practice/README.md) before running a target feature. Named `NotImplementedException` failures identify work to implement; the branch is not a finished application release.

## Lesson and code map

| Current Udemy lesson | Relevant code in this checkpoint | Engineering objective |
|---|---|---|
| Connect VibeCast to Microsoft Foundry with IChatClient | [src/VibeCast.Infrastructure/DependencyInjection.cs](src/VibeCast.Infrastructure/DependencyInjection.cs) | Register the provider adapter and application-facing client. |
| Generate the First VibeCast Episode Concept with Foundry | [src/VibeCast.Infrastructure/AI/FoundryEpisodeConceptGenerator.cs](src/VibeCast.Infrastructure/AI/FoundryEpisodeConceptGenerator.cs) | Construct editorial messages and return a nonempty concept. |
| Streaming The Foundry Response to the Client | [src/VibeCast.Infrastructure/AI/FoundryEpisodeConceptGenerator.cs](src/VibeCast.Infrastructure/AI/FoundryEpisodeConceptGenerator.cs) | Stream updates and propagate cancellation. |

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

Tests that exercise an unfinished task can fail with its named exception. Use the task guide to distinguish expected gaps from regressions, and compare behavior with the pinned reference after attempting the implementation. Existing tests do not automatically cover every acceptance criterion. The scaffold's Release build has passed in GitHub Actions. See the task guide's validation status for expected exercise failures and remaining checks; completed solutions and live cloud behavior have not been fully verified.

Generate a concept from a brief, reject an empty fake response and check streamed update order and cancellation.

## Checkpoint boundaries

- Navigation pages are not a feature checklist. Workflow, approval, evaluation or observability screens can remain presentation scaffolds; inspect their service calls before treating them as implemented capabilities.
- The channel queue is local and non-durable. Its presence does not provide a hosted message broker or durable execution.

## Related checkpoints

- [section-04-ai-client-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-complete)
- [section-04-ai-client-complete-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-complete-ui)
- [section-04-ai-client-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-practice) — this branch
- [section-04-ai-client-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-start)
- [section-04-ai-client-start-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-start-ui)

Return to the [course branch index](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/main). The earlier generic branch-strategy/local-development documents may describe older snapshots; use this README and the linked source files for this branch's current setup.
