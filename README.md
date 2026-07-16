# VibeCast — Section 04 AI Client Starter

This branch contains the production-shaped starter application for **Section 04: Foundations of Enterprise Generative AI for .NET**.

The application presents a complete authenticated product shell so demonstrations can begin at the AI integration boundary rather than spending course time on ordinary Blazor, CRUD, database, and page-layout work.

## Included

- .NET 10 and C# 14
- Blazor Web App using Interactive Server rendering
- ASP.NET Core Identity with SQLite
- Authenticated application shell with grouped navigation and responsive layout
- Dashboard, episode list, episode brief form, and tabbed episode workspace
- Media library and upload/processing form
- Processing jobs, knowledge sources, editorial workflows, and approval queue
- Evaluation, observability, and settings surfaces
- EF Core context, initial migration, and development seed data
- Episode, media asset, processing job, and user profile domain entities
- Local blob-storage implementation behind `IBlobStorage`
- Bounded channel-based background-job queue and hosted worker
- Strongly typed configuration options
- DataAnnotations plus application-level validators
- Structured logging and OpenTelemetry tracing/metrics baseline
- Multi-stage Dockerfile and Docker Compose configuration
- Domain, application, and integration test projects
- GitHub Actions CI

## UI-scaffold boundary

The authenticated pages intentionally use presentation data and visual states. They do **not** invoke EF Core business persistence, blob storage, background jobs, AI providers, retrieval, agents, evaluations, or telemetry queries.

The existing infrastructure remains available for later course checkpoints, but the Section 04 pages expose clear seams where learners will add:

- `IChatClient` and `IEmbeddingGenerator`
- Microsoft Foundry connectivity
- Structured output, validation, and repair
- Multimodal processing
- RAG ingestion and retrieval
- Microsoft Agent Framework workflows
- Human approval and durable execution
- Evaluation and OpenTelemetry evidence

## Authenticated routes

- `/dashboard`
- `/episodes`
- `/episodes/create`
- `/episodes/{id}`
- `/media`
- `/media/upload`
- `/jobs`
- `/knowledge`
- `/knowledge/create`
- `/workflows`
- `/workflows/{id}`
- `/approvals`
- `/evaluations`
- `/observability`
- `/settings`

## Run locally

```bash
dotnet restore
dotnet build --configuration Release
dotnet run --project src/VibeCast.Web
```

Development seed account:

- Email: `instructor@vibecast.local`
- Password: `VibeCast!12345`

The seed account is for local teaching use only. Replace or disable it outside the Development environment.

## Data locations

- SQLite: `.vibecast/vibecast.db`
- Uploaded media: `.vibecast/blobs`

## Docker

```bash
docker compose up --build
```

## Branch progression

See [`docs/branch-strategy.md`](docs/branch-strategy.md). The corresponding completion checkpoint should be created as `section-04-ai-client-complete` after the AI client pipeline is implemented.
