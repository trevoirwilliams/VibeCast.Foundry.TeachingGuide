---
applyTo: "tests/**/*.cs"
---

# Test Instructions

- Use MSTest, matching the existing repository.
- Keep tests deterministic and isolated.
- Prefer fakes over live AI, Azure, network, clock, or filesystem dependencies.
- Name tests as observable behavior and expected result.
- Cover normal, failure, cancellation, malformed-output, and authorization paths.
- Do not assert against unstable generated prose.
- Assert contracts, statuses, tool calls, citations, validation outcomes, and
  persisted state.
- Do not place production-only secrets or endpoints in test settings.
- Integration tests may use `WebApplicationFactory<Program>`.
- Never reduce existing assertions solely to make a new implementation pass.