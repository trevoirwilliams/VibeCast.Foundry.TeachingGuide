---
applyTo: "src/VibeCast.Infrastructure/**/*.cs,src/VibeCast.Infrastructure/**/*.csproj"
---

# Infrastructure Instructions

- Implement contracts owned by Application.
- Keep provider-specific models internal to Infrastructure.
- Bind configuration through strongly typed options and validate it at startup.
- Do not read configuration repeatedly inside request methods.
- Do not store credentials in options that may be rendered or logged.
- Use `HttpClientFactory` or supported SDK client registration.
- Register telemetry handlers before provider execution.
- Preserve cancellation through database, storage, network, and model calls.
- Apply retries only around idempotent and retryable operations.
- Do not hide provider failures behind empty successful results.
- Map provider errors to explicit application outcomes.
- Local filesystem and Channel implementations are development adapters, not durable
  production infrastructure.