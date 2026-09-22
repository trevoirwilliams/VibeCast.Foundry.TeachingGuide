---
name: validate-structured-output
description: Design and verify safe structured AI output for VibeCast. Use when a model returns typed plans, classifications, extraction results, tool arguments, or workflow decisions.
---

# Validate Structured Output

Treat model output as a candidate, not accepted application state.

For every structured result:

1. Define a closed C# contract.
2. Parse without arbitrary polymorphic type creation.
3. Enforce required fields, lengths, ranges, and collection limits.
4. Enforce cross-field business rules.
5. Validate the result before persistence or downstream use.
6. If repair is part of the use case, keep it explicitly bounded.
7. Revalidate the replacement result from scratch.
8. Reject the workflow when the final candidate still fails validation.
9. Never execute tool arguments before authorization and validation.
10. Preserve evidence about whether repair occurred when that matters to the domain or audit trail.

Representative deterministic tests should cover the application rules actually implemented, such as:

- valid plan accepted without repair;
- invalid plan followed by valid repair;
- invalid plan followed by invalid repair and rejection;
- invalid evidence requirement rejected;
- invalid tool result rejected before domain use;
- unsupported or malformed media rejected before AI processing.

Do not turn semantic quality questions into brittle string assertions.
