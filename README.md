# VibeCast — course branch index

Teaching repository for **Generative AI for .NET Developers with Microsoft Foundry**.

`main` contains repository navigation, not the runnable VibeCast application. Choose a section branch before running .NET commands. The course uses .NET 10 / C# 14; each runnable branch pins its own dependencies and setup.

## Choose a learning route

- **start**: follow the existing demonstration from its starting state.
- **practice**: attempt the backend implementation using prepared supporting files and named gaps. These are instructor-review drafts; read their validation status before use.
- **complete**: inspect the section's reference implementation after attempting the work.
- **ui variants**: alternate presentation checkpoints. Some include backend/service/test changes; their READMEs list the actual differences.
- **source-gallery-finish**: an intermediate gallery checkpoint, not the full knowledge-grounded generation solution.

## Section checkpoints

| Course section | Starting branch | Backend practice | Reference |
|---|---|---|---|
| 4 — Foundations of Generative AI Development With .NET | [section-04-ai-client-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-start) | [section-04-ai-client-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-practice) | [section-04-ai-client-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-complete) |
| 5 — Prompt Engineering, Structured Outputs, and Typed C# Tools | [section-05-episode-planner-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-start) | [section-05-episode-planner-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-practice) | [section-05-episode-planner-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-complete) |
| 6 — Build Multimodal AI Features with Microsoft Foundry | [section-06-episode-media-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-start) | [section-06-episode-media-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-practice) | [section-06-episode-media-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-complete) |
| 7 — Build Knowledge-Grounded Content Generation with Foundry IQ and Azure AI Search | [section-07-source-gallery-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-07-source-gallery-start) | [section-07-source-gallery-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-07-source-gallery-practice) | [section-07-source-gallery-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-07-source-gallery-complete) |
| 8 — focused behavior-testing lesson | [section-08-ai-behavior-tests-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-08-ai-behavior-tests-start) | [section-08-ai-behavior-tests-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-08-ai-behavior-tests-practice) | [section-08-ai-behavior-tests-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-08-ai-behavior-tests-complete) |
| 8 — Production-Ready AI Application Engineering | [section-08-ai-engineering-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-08-ai-engineering-start) | [section-08-ai-engineering-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-08-ai-engineering-practice) | [section-08-ai-engineering-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-08-ai-engineering-complete) |
| 9 — Deploy the AI-Enabled .NET Application to Azure | [section-09-prod-prep-start](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-start) | [section-09-prod-prep-practice](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-practice) | [section-09-prod-prep-complete](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-09-prod-prep-complete) |

## Other checkpoints

- [section-04-ai-client-complete-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-complete-ui)
- [section-04-ai-client-start-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-04-ai-client-start-ui)
- [section-05-episode-planner-complete-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-complete-ui)
- [section-05-episode-planner-start-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-05-episode-planner-start-ui)
- [section-06-episode-media-complete-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-complete-ui)
- [section-06-episode-media-start-ui](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-06-episode-media-start-ui)
- [section-07-source-gallery-finish](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/section-07-source-gallery-finish)

## Setup changes through the course

Sections 4–7 use SQLite and local media storage; the full Section 7 checkpoint adds Azure-backed knowledge-source storage and retrieval. Section 8 complete adds an Aspire launch path for tracing. Section 9 complete uses PostgreSQL, Azurite and a Dockerfile-hosted web app through Aspire. Read the selected branch's README; one generic launch command does not describe all of these states.

## Known checkpoint distinctions

- Section 8 ai-engineering-complete constructs a resilience wrapper that is not included in the returned pipeline. Its practice branch connects the wrapper; Section 9 complete also includes it.
- The dedicated behavior-tests-complete branch contains tests absent from the general engineering-complete tree. Use the dedicated branch for the testing lesson.
- A completed reference is a teaching checkpoint, not a production certification. Practice branches intentionally contain runtime gaps.
- The README lesson maps use current Udemy titles and inspected code. Reattached transcript download remains blocked; exact timestamps and spoken sequence are not verified.

## Maintenance branch

[copilot/fix-github-actions-build-job](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/copilot/fix-github-actions-build-job) currently contains only a README. It is not a runnable course checkpoint and does not currently contain a workflow to inspect or execute.

## Working with the branches

Save and commit your work before switching checkpoints. Each section can be studied independently; incomplete exercise code need not carry forward. Preserve the section's package versions while following the recorded implementation. Use deterministic tests before paid model smoke tests, and keep cloud credentials outside committed files.
