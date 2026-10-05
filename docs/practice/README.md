# Backend practice: Build Multimodal AI Features with Microsoft Foundry

> Instructor review draft. C# syntax checked; full solution build, behavior tests, cloud smoke tests and transcript timing are not verified in this environment. Run the release gates below before assigning this branch to students.

## Choose your route

- Practice route: attempt these tasks before watching the matching implementation video. Use the video as a worked solution afterward.
- Guided route: watch the explanation first, pause before implementation, then attempt the task. Do not simply copy the finished code.
- Already watched? Rebuild each missing behavior without opening the reference, then explain one failure case.

This branch is based on `section-06-episode-media-start` at `7dd380550af0d1bb21027a8b8c63f530efe7e7d5`. Supporting files, UI, contracts, registration and migrations come from `section-06-episode-media-complete` at `bbfc6caec2296d66624a8556d6e3092ad598bbee`, with the core exercise implementations removed. It is an enriched section-start snapshot, so some supporting code appears earlier than in the video. Original start/complete branches are unchanged. Start each section independently; unfinished exercises do not carry forward.

Use the setup in `docs/local-development.md` and this checkpoint's pinned package files. Do not upgrade packages while solving an exercise. Cloud credentials and model deployments are still needed for live features; deterministic tests should not use them. Preserve all ownership and validation checks supplied in surrounding code.

## Your tasks

Find `PRACTICE S` in C# files. Each intentional `NotImplementedException` identifies an unfinished behavior. These failures are expected until that task is implemented; do not replace them with dummy output, skip tests or suppress assertions. When implementing an asynchronous stub, restore `async` when needed; streaming implementations also need iterator syntax and appropriate cancellation handling.

### S06-01: AnalyzeAsync

- File: [src/VibeCast.Infrastructure/AI/FoundryArtworkAnalysisService.cs](../../src/VibeCast.Infrastructure/AI/FoundryArtworkAnalysisService.cs)
- Attempt before: **Implement Foundry Image Understanding Model Call** (lecture 64).
- Target practice time: 25 minutes, an initial estimate to calibrate with learners.
- Task: Validate the supplied artwork, combine image data with episode context, request typed analysis and validate it before returning accessibility metadata.
- Done when: Unsupported images fail before invocation; a valid analysis has usable alt text and prompt metadata.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S06-02: GenerateAsync

- File: [src/VibeCast.Infrastructure/AI/FoundryEpisodeArtworkGenerationService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodeArtworkGenerationService.cs)
- Attempt before: **Generate and Validate Promotional Artwork** (lecture 70).
- Target practice time: 35 minutes, an initial estimate to calibrate with learners.
- Task: Generate artwork for an episode owned by the caller. Bound the call, validate the returned PNG, persist metadata and clean up a stored blob if persistence fails.
- Done when: Wrong owner, invalid content type or PNG signature cannot persist an asset; failed persistence cleans up.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S06-03: TranscribeAsync

- File: [src/VibeCast.Infrastructure/AI/FoundryEpisodeTranscriptionService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodeTranscriptionService.cs)
- Attempt before: **Transcribe and Attach an Episode Recording** (lecture 73).
- Target practice time: 25 minutes, an initial estimate to calibrate with learners.
- Task: Load an owned audio asset, choose the episode locale and transcribe it. Reject empty text and save the transcript with its metadata.
- Done when: Wrong-owner and nonaudio assets fail; nonempty text and locale persist; streams are disposed.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S06-04: AssessRelevanceAsync

- File: [src/VibeCast.Infrastructure/AI/FoundryEpisodeResourceAnalysisService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodeResourceAnalysisService.cs)
- Attempt before: **Add Content Understanding Configurations and Services** (lecture 79).
- Target practice time: 20 minutes, an initial estimate to calibrate with learners.
- Task: Use the prepared relevance prompt and extracted document context to request a typed assessment. Reject unusable completions and preserve cancellation.
- Done when: A usable assessment reaches the supplied validator; invalid or unrelated evidence cannot become accepted support.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

## Optional hint

For the first three challenges, the rest of each method is supplied. Implement the local request function using values already in scope. Keep supplied guards and persistence code. The resource-assessment challenge uses the existing prompt builder and completion guard.

## Validate and compare

1. Run `dotnet restore VibeCast.sln` and `dotnet build VibeCast.sln --configuration Release --no-restore`. Exercise failures should be behavioral; missing dependencies or compile errors are setup/implementation problems.
2. Run `dotnet test VibeCast.sln --configuration Release --no-build`. Existing tests remain supplied. Tests exercising gaps may fail with the named exercise exception. This pack does not claim that every criterion already has an automated test.
3. Add or run a focused test for the task's success and rejection paths. Use local fakes for model behavior. Use Azurite for storage integration. Run cloud smoke checks only after deterministic checks; avoid repeated paid calls as a debugging loop.
4. Compare your implementation with the same file at the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/bbfc6caec2296d66624a8556d6e3092ad598bbee). Compare behavior and explain differences; matching every line is unnecessary.
5. Change one input or failure mode and predict the outcome before running it. Record whether the prediction was correct.

After finishing, use `git diff` to review your own changes. Commit your work before changing branches. To see one reference file without replacing your work, use `git show section-06-episode-media-complete:path/to/file.cs` with a file path from the task list.

## Using Copilot

Try the task first. Ask for an explanation or a hint about one failed case. In the testing lesson, ask Copilot to propose behavior cases, inspect its assertions, then deliberately break the relevant behavior in a disposable local copy and confirm the test detects it. Do not ask an agent to fill every practice gap in one pass. No new agent instructions or skills are installed by this pack.

## Instructor release gates

- Build the untouched scaffold and classify expected exercise failures separately from regressions.
- Restore the reference implementation for each gap and run the full test suite; all tests must pass.
- Ensure every core acceptance criterion has a deterministic test or an explicit integration checklist before release.
- Check the actual video sequence and add pause cues; lecture mapping is based on live titles and repository code, not a verified transcript review.
- Verify a fresh learner can set up the branch and reach the first gap; document credentials, quotas and costs separately.
- Pilot with a learner who has not seen the solution, then adjust task size and hints.

## Checkpoint compatibility

The existing artwork implementation uses IImageGenerator and suppresses MEAI001. Treat that as an explicitly experimental dependency inherited from the recorded checkpoint; this pack does not relabel it as a stable API or replace it with IChatClient. Keep the pinned dependency set for video parity.
