# VibeCast — Section 04 AI Client Starter

This branch contains the production-shaped starter application for **Section 04: Foundations of Enterprise Generative AI for .NET**.

The conventional .NET plumbing is already implemented so that course demonstrations can concentrate on AI client abstractions, Microsoft Foundry connectivity, model calls, streaming, telemetry, resilience, and deterministic tests.

## Included

- .NET 10 and C# 14
- Blazor Web App using Interactive Server rendering
- ASP.NET Core Identity with SQLite
- Authenticated navigation, dashboard, forms, episode management, media upload, and job status screens
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

## Deliberately not implemented

The starter branch does **not** configure an AI provider or register `IChatClient` / `IEmbeddingGenerator`. Those are the observable changes developed during Section 04.

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
