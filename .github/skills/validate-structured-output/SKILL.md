---
name: validate-structured-output
description: Design and verify safe structured AI output for VibeCast. Use when a model returns JSON, typed plans, classifications, extraction results, tool arguments, or workflow decisions.
---

# Validate Structured Output

For every structured model result:

1. Define a closed C# record or class hierarchy.
2. Reject unknown or unsupported discriminator values.
3. Enforce required fields and maximum lengths.
4. Enforce numeric ranges and collection-size limits.
5. Enforce cross-field business constraints.
6. Parse without permitting arbitrary polymorphic type creation.
7. Return a validation result that distinguishes:
   - parse failure;
   - schema failure;
   - domain failure;
   - policy failure.
8. Permit at most the explicitly configured number of repair attempts.
9. Never execute tool arguments before authorization and validation.
10. Add tests for:
    - valid output;
    - invalid JSON;
    - missing fields;
    - excessive collection sizes;
    - invalid enum values;
    - contradictory fields;
    - malicious strings;
    - cancelled repair.