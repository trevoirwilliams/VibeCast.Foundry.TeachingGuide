# Backend practice: Build Knowledge-Grounded Content Generation with Foundry IQ and Azure AI Search

> Instructor review draft. C# syntax checked; full solution build, behavior tests, cloud smoke tests and transcript timing are not verified in this environment. Run the release gates below before assigning this branch to students.

## Choose your route

- Practice route: attempt these tasks before watching the matching implementation video. Use the video as a worked solution afterward.
- Guided route: watch the explanation first, pause before implementation, then attempt the task. Do not simply copy the finished code.
- Already watched? Rebuild each missing behavior without opening the reference, then explain one failure case.

This branch is based on `section-07-source-gallery-start` at `f31647d1cbf70c2c49487ede7dc1cb1122d83d1b`. Supporting files, UI, contracts, registration and migrations come from `section-07-source-gallery-complete` at `b892798b5926d055a7211125f23fd350a438205d`, with the core exercise implementations removed. It is an enriched section-start snapshot, so some supporting code appears earlier than in the video. Original start/complete branches are unchanged. Start each section independently; unfinished exercises do not carry forward.

Use the setup in `docs/local-development.md` and this checkpoint's pinned package files. Do not upgrade packages while solving an exercise. Cloud credentials and model deployments are still needed for live features; deterministic tests should not use them. Preserve all ownership and validation checks supplied in surrounding code.

## Your tasks

Find `PRACTICE S` in C# files. Each intentional `NotImplementedException` identifies an unfinished behavior. These failures are expected until that task is implemented; do not replace them with dummy output, skip tests or suppress assertions. When implementing an asynchronous stub, restore `async` when needed; streaming implementations also need iterator syntax and appropriate cancellation handling.

### S07-01: BuildSourceFilter

- File: [src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs](../../src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs)
- Attempt before: **Fix Reference and Filter Condition Implementation** (lecture 95).
- Target practice time: 20 minutes, an initial estimate to calibrate with learners.
- Task: Build a retrieval filter from the resolved source URLs. Escape OData values, remove duplicates and reject an empty allowed set; never broaden to every source.
- Done when: Only selected, owner-resolved URLs are allowed; apostrophes are escaped; an empty set fails closed.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S07-02: ReadEvidenceReferences

- File: [src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs](../../src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs)
- Attempt before: **Fix Reference and Filter Condition Implementation** (lecture 95).
- Target practice time: 20 minutes, an initial estimate to calibrate with learners.
- Task: Parse retrieved evidence into reference IDs and snippets. Support the reference ID shapes used by this checkpoint and return no references for malformed evidence.
- Done when: String and numeric IDs are retained as strings; malformed or missing references prevent generation.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S07-03: ValidateBlog

- File: [src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs](../../src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs)
- Attempt before: **Generate a Grounded Blog from Knowledge Source** (lecture 94).
- Target practice time: 20 minutes, an initial estimate to calibrate with learners.
- Task: Validate the draft structure and cited reference IDs against the retrieved evidence. Reject missing or invented citations; normalize accepted references.
- Done when: Incomplete drafts and unknown citations fail; valid references are deduplicated. This does not prove claim-level factual support.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

## Optional hint

The service already resolves selected sources through IMediaAssetService with ownerId. The filter consumes that resolved allowlist. EscapeODataString and CreateSnippet are supplied. Citation membership is only one grounding check.

## Validate and compare

1. Run `dotnet restore VibeCast.sln` and `dotnet build VibeCast.sln --configuration Release --no-restore`. Exercise failures should be behavioral; missing dependencies or compile errors are setup/implementation problems.
2. Run `dotnet test VibeCast.sln --configuration Release --no-build`. Existing tests remain supplied. Tests exercising gaps may fail with the named exercise exception. This pack does not claim that every criterion already has an automated test.
3. Add or run a focused test for the task's success and rejection paths. Use local fakes for model behavior. Use Azurite for storage integration. Run cloud smoke checks only after deterministic checks; avoid repeated paid calls as a debugging loop.
4. Compare your implementation with the same file at the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/b892798b5926d055a7211125f23fd350a438205d). Compare behavior and explain differences; matching every line is unnecessary.
5. Change one input or failure mode and predict the outcome before running it. Record whether the prediction was correct.

After finishing, use `git diff` to review your own changes. Commit your work before changing branches. To see one reference file without replacing your work, use `git show section-07-source-gallery-complete:path/to/file.cs` with a file path from the task list.

## Using Copilot

Try the task first. Ask for an explanation or a hint about one failed case. In the testing lesson, ask Copilot to propose behavior cases, inspect its assertions, then deliberately break the relevant behavior in a disposable local copy and confirm the test detects it. Do not ask an agent to fill every practice gap in one pass. No new agent instructions or skills are installed by this pack.

## Instructor release gates

- Build the untouched scaffold and classify expected exercise failures separately from regressions.
- Restore the reference implementation for each gap and run the full test suite; all tests must pass.
- Ensure every core acceptance criterion has a deterministic test or an explicit integration checklist before release.
- Check the actual video sequence and add pause cues; lecture mapping is based on live titles and repository code, not a verified transcript review.
- Verify a fresh learner can set up the branch and reach the first gap; document credentials, quotas and costs separately.
- Pilot with a learner who has not seen the solution, then adjust task size and hints.

## Retrieval scope

Use source-gallery-complete as the reference, not source-gallery-finish. This checkpoint uses KnowledgeBaseRetrievalClient and generates a blog. It does not ask you to build an unrelated application-managed chunker or embedding pipeline. Add a negative integration check with two owners: selecting another owner's source must never broaden retrieval. Indexing delays are separate from code failures.
