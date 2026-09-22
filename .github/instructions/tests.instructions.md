---
applyTo: "tests/**/*.cs"
---

# Test Instructions

- Use MSTest, matching the existing repository.
- Keep tests deterministic, isolated, and fast by default.
- Prefer fakes over live AI, Azure, network, clock, or filesystem dependencies.
- Name tests for observable behavior and expected result.
- Do not assert against unstable generated prose.
- Assert contracts, validation outcomes, call counts, tool use, ownership boundaries,
  evidence identity, persistence, cleanup, and other application guarantees.
- Do not place production-only secrets or endpoints in test settings.
- Integration tests may use `WebApplicationFactory<Program>`.
- Never weaken existing assertions solely to make a new implementation pass.

## Prioritize VibeCast behavior

When testing AI-backed features, begin with the behavior the application promises:

- invalid episode plans receive at most one bounded repair;
- a still-invalid repaired plan is rejected;
- evidence requirements must exist in the accepted episode plan;
- selected knowledge sources must all be authorized and available;
- media bytes must match the claimed file type;
- generated artwork must pass media validation before persistence;
- artwork cannot be accepted before analysis;
- required application tools and policies cannot be silently skipped.

Provider mechanics such as raw finish reasons, token counts, or exact prompt wording are secondary unless the production behavior specifically depends on them.

## Test levels

1. **Deterministic behavior tests — default**
   Exercise application services, validators, provider adapters, and domain state through fakes or in-memory infrastructure.

2. **Integration tests — when wiring matters**
   Verify DI, HTTP boundaries, persistence, authentication, or local infrastructure.

3. **Model-quality evaluations — opt in**
   Use for semantic properties such as relevance, groundedness, completeness, fluency, or safety scoring. Keep these separate from deterministic correctness tests.
