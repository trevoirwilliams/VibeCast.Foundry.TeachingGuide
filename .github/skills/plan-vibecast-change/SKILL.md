---
name: plan-vibecast-change
description: Plan a scoped VibeCast implementation before coding. Use for new features, lesson demonstrations, refactors, AI integrations, retrieval work, agent workflows, or infrastructure changes.
---

# Plan a VibeCast Change

Before editing code:

1. Identify the active teaching branch.
2. Read:
   - `README.md`
   - `docs/architecture.md`
   - `docs/branch-strategy.md`
   - relevant project files and tests.
3. Restate the requested observable outcome.
4. Identify the owning layer:
   - Domain for business invariants.
   - Application for use cases and contracts.
   - Infrastructure for technical implementations.
   - Web for presentation and composition.
5. Trace the expected request path.
6. Identify security, cancellation, validation, telemetry, and testing requirements.
7. Identify capabilities that belong to later course sections and exclude them.
8. Produce a plan containing:
   - files to create;
   - files to modify;
   - tests to add;
   - commands to run;
   - assumptions;
   - unresolved API or package questions.

Do not edit files while performing this skill unless the user explicitly requests
implementation after the plan.