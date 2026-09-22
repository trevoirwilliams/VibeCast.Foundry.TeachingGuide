---
name: test-ai-behavior
description: Design and implement representative deterministic tests for behaviors VibeCast guarantees around AI-generated data, retrieval, evidence, tools, media, and persistence.
---

# Test AI Behavior

Start from the feature behavior, not from generic AI failure modes.

## 1. Inventory application promises

Inspect the current implementation and list concrete guarantees, for example:

- an invalid episode plan receives at most one bounded repair;
- a repaired plan must independently pass validation;
- a model cannot invent evidence requirements outside the accepted plan;
- every user-selected knowledge source must be both authorized and available;
- invalid media cannot enter downstream AI processing;
- a required planning tool cannot be skipped;
- artwork requires an AI proposal before human acceptance.

## 2. Build a behavior matrix

For each candidate capture:

| Field | Meaning |
|---|---|
| Feature | Planner, evidence, RAG, media, artwork, tool calling |
| Guarantee | What VibeCast promises |
| Seam | Service, validator, domain method, or endpoint |
| Fake/fixture | Deterministic dependency required |
| Regression consequence | What breaks if this behavior changes |
| Priority | Critical, high, medium, low |

Prefer a small set of tests tied to real product behavior over generic provider edge cases.

## 3. Choose deterministic assertions

Good assertions include:

- repair attempted exactly once;
- repaired plan accepted only when valid;
- final invalid plan rejected;
- unknown evidence requirement rejected;
- unauthorized or unavailable selected source rejects the whole selection;
- invalid upload leaves no persisted media asset;
- cleanup occurs after failed temporary storage;
- required tool invocation cannot be bypassed;
- accepted artwork moves to the expected reviewed state.

Avoid exact generated prose unless the application contract defines exact text.

## 4. Use existing seams first

Prefer:

- fake `IChatClient`;
- existing validators;
- in-memory SQLite for persistence rules;
- local fake storage implementations;
- existing application services;
- `WebApplicationFactory<Program>` for HTTP integration behavior.

Do not modify production code solely for test convenience.

## 5. Separate evaluations from tests

If the requirement is semantic—relevance, groundedness, fluency, completeness, or safety scoring—mark it as a model-quality evaluation candidate. Do not disguise it as a deterministic unit test.

## 6. Run in layers

Run the focused test project or class first, then:

```bash
dotnet test VibeCast.sln --configuration Release
```

Report cases implemented, cases deferred, and whether any behavior still needs a live integration or model-quality evaluation.
