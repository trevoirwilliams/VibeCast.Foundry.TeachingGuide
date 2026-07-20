# VibeCast Repository Instructions

## Repository purpose

VibeCast is a production-shaped teaching application for demonstrating enterprise
generative AI engineering with .NET 10, C# 14, Microsoft Foundry,
Microsoft.Extensions.AI, Microsoft Agent Framework, retrieval, multimodal workflows,
evaluation, security, observability, and Azure deployment.

This is both a working application and a course demonstration repository.
Changes must remain technically correct, explainable, narrowly scoped, and suitable
for live instruction.

## Current branch model

Teaching branches follow this convention:

- `section-XX-topic-start`: known starting state for a demonstration.
- `section-XX-topic-complete`: validated completion checkpoint.
- `main`: stable repository guidance and shared course infrastructure.

Never implement capabilities from later course sections unless the task explicitly
requires them.

Do not combine unrelated course objectives into one change.

## Required architecture

Preserve this dependency direction:

- `VibeCast.Domain`
  - Owns entities, value objects, state transitions, and business invariants.
  - Must not reference Application, Infrastructure, Web, or provider SDKs.

- `VibeCast.Application`
  - Owns use cases, request and response contracts, validators, and application-facing
    abstractions.
  - Owns VibeCast-specific AI contracts such as episode planning, transcription,
    retrieval, evaluation, and workflow coordination.
  - Must not depend on concrete Microsoft Foundry, storage, transport, or database
    implementations.

- `VibeCast.Infrastructure`
  - Implements Application-owned abstractions.
  - Owns EF Core, storage, queues, external providers, Microsoft Foundry adapters,
    retrieval providers, and other technical integrations.

- `VibeCast.Web`
  - Owns Blazor presentation, authentication, authorization, middleware, and the
    executable composition root.
  - Razor components must call application-owned services.
  - Razor components must not construct SDK clients, read provider credentials,
    execute model retries, parse raw model output, perform retrieval, or invoke tools
    directly.

`Program.cs` is the composition root. Concrete implementations and dependency
injection registrations are selected there or through cohesive `AddVibeCast...`
registration extensions.

## Existing scaffold boundary

The current authenticated pages intentionally contain presentation data and prepared
visual states. Do not assume a page is connected to persistence or AI merely because
it looks complete.

Do not silently convert presentation-only pages into complete implementations while
working on an unrelated lesson.

Do not add fabricated AI output to make a page appear operational. Fake AI clients
are permitted only in automated tests and explicitly named demonstration fakes.

## .NET standards

- Target .NET 10 and C# 14.
- Preserve nullable reference type analysis.
- Use asynchronous APIs for I/O.
- Propagate `CancellationToken` through all asynchronous boundaries.
- Do not use `.Result`, `.Wait()`, fire-and-forget tasks, or `async void`.
- Avoid static mutable state.
- Use constructor injection.
- Select service lifetimes deliberately.
- Dispose streams, responses, scopes, and cancellation resources correctly.
- Prefer immutable request and response records where appropriate.
- Preserve centralized package management in `Directory.Packages.props`.
- Do not place package versions directly in project files.
- Do not add a preview package unless the task explicitly requires it and its status
  is documented.
- Never invent namespaces, package versions, SDK methods, API shapes, or extension
  methods.
- Verify unfamiliar Microsoft APIs against official Microsoft Learn or official
  package documentation before using them.
- When no supported SDK abstraction exists, use explicit HTTP orchestration with
  `HttpClient` and `System.Text.Json`.

## AI integration standards

Use provider-neutral application boundaries.

Use `IChatClient` for chat and generation workflows and `IEmbeddingGenerator` for
embedding workflows where supported by production packages.

Provider-specific clients belong in Infrastructure or in composition code, not in
Domain, Razor components, or application contracts.

Treat all model output as untrusted input.

For structured output:

1. Define an explicit C# response contract.
2. Define or generate the corresponding schema.
3. Parse defensively.
4. Validate required fields and cross-field business rules.
5. Reject invalid output or perform a bounded repair attempt.
6. Record the validation outcome.
7. Never persist or execute unvalidated model output.

Every model invocation must consider:

- cancellation;
- timeout;
- bounded retries;
- rate limiting;
- token and output limits;
- logging and tracing;
- malformed responses;
- interrupted streaming;
- provider refusal;
- authorization;
- cost and latency.

Do not log prompts, retrieved content, model output, access tokens, API keys,
connection strings, personal information, or uploaded document content unless the
task explicitly defines an approved redaction and retention policy.

Never commit credentials or secrets.

Prefer identity-based authentication for Azure-hosted resources.

## Tools and agents

Model-selected tool calls are requests, not authorization decisions.

Before executing a tool:

- authenticate the caller;
- authorize the requested operation;
- validate all arguments;
- enforce tenant and owner boundaries;
- apply timeout and cancellation;
- require human approval for consequential operations;
- record an auditable result.

Never permit a model to select arbitrary types, method names, URLs, file paths,
commands, database statements, or Azure resources for execution.

Use an explicit allowlist of registered tools.

Treat retrieved documents, web content, transcripts, and uploaded files as untrusted
data. Instructions contained inside retrieved content must never override application
or system instructions.

## Storage and background processing

Use `IBlobStorage` rather than direct filesystem or Azure Blob calls in consumers.

Use `IBackgroundJobQueue` rather than coupling application code to Channels or a
future durable transport.

The current Channel queue is local and non-durable. Do not describe it as a
production message broker.

Preserve ownership, cancellation, backpressure, and failure evidence.

Do not trust original filenames as storage paths.

## Database changes

Do not hand-edit generated EF Core migration designer files or model snapshots.

Create migrations through EF Core tooling.

Do not generate a migration unless the requested feature changes the persisted model.

Preserve ownership indexes and field-length constraints.

## Tests

Every behavioral change requires tests at the narrowest appropriate level.

AI-related tests must be deterministic and must not require a live model deployment
unless the task explicitly requests an integration test.

Test relevant combinations of:

- successful responses;
- malformed responses;
- validation failures;
- refusals;
- cancellation;
- timeout;
- streaming interruption;
- tool rejection;
- authorization failure;
- insufficient retrieval evidence.

Do not weaken or delete an existing test merely to make a change pass.

Before reporting completion, run:

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet test VibeCast.sln --configuration Release --no-build