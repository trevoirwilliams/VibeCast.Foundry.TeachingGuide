# Teaching Branch Strategy

- `main`: repository guide and stable course metadata.
- `section-04-ai-client-start`: complete non-AI application plumbing; no model provider configured.
- `section-04-ai-client-complete`: planned checkpoint after `IChatClient`, `IEmbeddingGenerator`, Foundry connectivity, streaming, telemetry, and tests are implemented.
- Later sections should repeat the `section-XX-topic-start` / `section-XX-topic-complete` pattern.

Each demonstration starts from a named branch and ends by running the application or tests. Learners can reset to a known checkpoint without replaying unrelated .NET setup.
