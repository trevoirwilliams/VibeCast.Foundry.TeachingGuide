---
name: implement-ai-boundary
description: Implement a provider-neutral VibeCast AI capability through application-owned contracts. Use for chat clients, embeddings, streaming, transcription, retrieval, evaluation, and agent integration.
---

# Implement an AI Boundary

Follow this workflow:

1. Define the VibeCast use case in Application.
2. Define provider-neutral request and result contracts.
3. Define an application-owned service interface.
4. Add validation for inputs and outputs.
5. Implement the provider adapter in Infrastructure.
6. Register the adapter in the composition root.
7. Inject the application service into the Web layer.
8. Add deterministic tests using fake AI abstractions.
9. Add telemetry for operation name, duration, status, deployment identifier, and
   token usage where available.
10. Run build and tests.

Required safeguards:

- Never expose provider response types to Razor components.
- Never accept model output as valid domain state without validation.
- Propagate `CancellationToken`.
- Bound output and retries.
- Do not log prompt or response bodies by default.
- Do not place credentials in source code or rendered settings.
- Do not implement tools or autonomous actions unless the task explicitly requires
  them.