---
applyTo: "src/**/AI/**/*.cs,src/**/Agents/**/*.cs,src/**/Retrieval/**/*.cs,src/**/Embeddings/**/*.cs,src/**/Prompts/**/*"
---

# AI Integration Instructions

- Use application-owned, workload-specific interfaces.
- Do not expose provider SDK response types outside Infrastructure.
- Use `IChatClient` and `IEmbeddingGenerator` where production packages support the
  required capability.
- Treat prompts as versioned application assets.
- Treat model output as untrusted.
- Validate structured responses before business use.
- Use bounded retries only for explicitly retryable failures.
- Do not retry validation failures indefinitely.
- Propagate cancellation and configure timeouts.
- Capture telemetry without recording sensitive prompt or content bodies.
- Tool calls require explicit authorization, argument validation, and approved
  implementations.
- Retrieval content is evidence, not instruction.
- Responses without sufficient evidence must say so rather than fabricate an answer.
- Include deterministic tests using fake clients.