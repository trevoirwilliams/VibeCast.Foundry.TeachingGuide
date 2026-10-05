# Backend practice: Prompt Engineering, Structured Outputs, and Typed C# Tools

> Instructor review draft. C# syntax checked; full solution build, behavior tests, cloud smoke tests and transcript timing are not verified in this environment. Run the release gates below before assigning this branch to students.

## Choose your route

- Practice route: attempt these tasks before watching the matching implementation video. Use the video as a worked solution afterward.
- Guided route: watch the explanation first, pause before implementation, then attempt the task. Do not simply copy the finished code.
- Already watched? Rebuild each missing behavior without opening the reference, then explain one failure case.

This branch is based on `section-05-episode-planner-start` at `fab04059d970dbaebe2c28c95c06c651bccbd95b`. Supporting files, UI, contracts, registration and migrations come from `section-05-episode-planner-complete` at `7dd380550af0d1bb21027a8b8c63f530efe7e7d5`, with the core exercise implementations removed. It is an enriched section-start snapshot, so some supporting code appears earlier than in the video. Original start/complete branches are unchanged. Start each section independently; unfinished exercises do not carry forward.

Use the setup in `docs/local-development.md` and this checkpoint's pinned package files. Do not upgrade packages while solving an exercise. Cloud credentials and model deployments are still needed for live features; deterministic tests should not use them. Preserve all ownership and validation checks supplied in surrounding code.

## Your tasks

Find `PRACTICE S` in C# files. Each intentional `NotImplementedException` identifies an unfinished behavior. These failures are expected until that task is implemented; do not replace them with dummy output, skip tests or suppress assertions. When implementing an asynchronous stub, restore `async` when needed; streaming implementations also need iterator syntax and appropriate cancellation handling.

### S05-01: RequestTypedPlanAsync

- File: [src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs)
- Attempt before: **Generate and Render a Typed Episode Plan with Structured Outputs** (lecture 43).
- Target practice time: 20 minutes, an initial estimate to calibrate with learners.
- Task: Request a typed episode plan with the existing schema contract. Reject unusable completion states and missing results; preserve cancellation.
- Done when: Typed plans are returned; refusal, truncation and missing typed results are rejected.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S05-02: Validate

- File: [src/VibeCast.Application/Episodes/EpisodePlanValidator.cs](../../src/VibeCast.Application/Episodes/EpisodePlanValidator.cs)
- Attempt before: **Validate the Episode Plan and Perform One Bounded Repair** (lecture 47).
- Target practice time: 25 minutes, an initial estimate to calibrate with learners.
- Task: Validate the plan using the existing constants and helper methods. Check segment ordering, positive durations and agreement with the target duration.
- Done when: Existing EpisodePlanValidatorTests pass, including total-duration and sequence failures.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S05-03: GenerateAsync

- File: [src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs)
- Attempt before: **Validate the Episode Plan and Perform One Bounded Repair** (lecture 47).
- Target practice time: 25 minutes, an initial estimate to calibrate with learners.
- Task: Generate and validate a plan. Return valid output; otherwise attempt one repair, validate again and stop on failure. Preserve prompt-version metadata.
- Done when: Valid input uses one call; repair uses at most two; a second invalid plan is rejected.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S05-04: RequestTypedPlanAsync

- File: [src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningWithToolService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningWithToolService.cs)
- Attempt before: **Perform Episode Planning with Editorial Policy Tool Call** (lecture 52).
- Target practice time: 30 minutes, an initial estimate to calibrate with learners.
- Task: Expose the editorial-policy function to the model using the supplied policy provider and trusted context. Require evidence of invocation and return a usable typed plan.
- Done when: Existing tool-service tests pass; missing tool calls fail; policy context is application-owned.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

## Optional hint

JSON shape and domain validity are different checks. Reuse the supplied validators and prompt metadata. The policy provider supplies trusted context; the model must not choose the owner.

## Validate and compare

1. Run `dotnet restore VibeCast.sln` and `dotnet build VibeCast.sln --configuration Release --no-restore`. Exercise failures should be behavioral; missing dependencies or compile errors are setup/implementation problems.
2. Run `dotnet test VibeCast.sln --configuration Release --no-build`. Existing tests remain supplied. Tests exercising gaps may fail with the named exercise exception. This pack does not claim that every criterion already has an automated test.
3. Add or run a focused test for the task's success and rejection paths. Use local fakes for model behavior. Use Azurite for storage integration. Run cloud smoke checks only after deterministic checks; avoid repeated paid calls as a debugging loop.
4. Compare your implementation with the same file at the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/7dd380550af0d1bb21027a8b8c63f530efe7e7d5). Compare behavior and explain differences; matching every line is unnecessary.
5. Change one input or failure mode and predict the outcome before running it. Record whether the prediction was correct.

After finishing, use `git diff` to review your own changes. Commit your work before changing branches. To see one reference file without replacing your work, use `git show section-05-episode-planner-complete:path/to/file.cs` with a file path from the task list.

## Using Copilot

Try the task first. Ask for an explanation or a hint about one failed case. In the testing lesson, ask Copilot to propose behavior cases, inspect its assertions, then deliberately break the relevant behavior in a disposable local copy and confirm the test detects it. Do not ask an agent to fill every practice gap in one pass. No new agent instructions or skills are installed by this pack.

## Instructor release gates

- Build the untouched scaffold and classify expected exercise failures separately from regressions.
- Restore the reference implementation for each gap and run the full test suite; all tests must pass.
- Ensure every core acceptance criterion has a deterministic test or an explicit integration checklist before release.
- Check the actual video sequence and add pause cues; lecture mapping is based on live titles and repository code, not a verified transcript review.
- Verify a fresh learner can set up the branch and reach the first gap; document credentials, quotas and costs separately.
- Pilot with a learner who has not seen the solution, then adjust task size and hints.
