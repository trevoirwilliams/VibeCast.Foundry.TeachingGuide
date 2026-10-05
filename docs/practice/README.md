# Backend practice: Production-Ready AI Application Engineering

> Instructor review draft. C# syntax checked; full solution build, behavior tests, cloud smoke tests and transcript timing are not verified in this environment. Run the release gates below before assigning this branch to students.

## Choose your route

- Practice route: attempt these tasks before watching the matching implementation video. Use the video as a worked solution afterward.
- Guided route: watch the explanation first, pause before implementation, then attempt the task. Do not simply copy the finished code.
- Already watched? Rebuild each missing behavior without opening the reference, then explain one failure case.

This branch is based on `section-08-ai-engineering-start` at `babc81553d043db4262f00cb7041b428871a3a0d`. Supporting files, UI, contracts, registration and migrations come from `section-08-ai-engineering-complete` at `30c56863165a043b0e42c06ef78f321ba38ddd0c`, with the core exercise implementations removed. It is an enriched section-start snapshot, so some supporting code appears earlier than in the video. Original start/complete branches are unchanged. Start each section independently; unfinished exercises do not carry forward.

Use the setup in `docs/local-development.md` and this checkpoint's pinned package files. Do not upgrade packages while solving an exercise. Cloud credentials and model deployments are still needed for live features; deterministic tests should not use them. Preserve all ownership and validation checks supplied in surrounding code.

## Your tasks

Find `PRACTICE S` in C# files. Each intentional `NotImplementedException` identifies an unfinished behavior. These failures are expected until that task is implemented; do not replace them with dummy output, skip tests or suppress assertions. When implementing an asynchronous stub, restore `async` when needed; streaming implementations also need iterator syntax and appropriate cancellation handling.

### S08-01: GetResponseAsync

- File: [src/VibeCast.Infrastructure/AI/ChatResilienceClient.cs](../../src/VibeCast.Infrastructure/AI/ChatResilienceClient.cs)
- Attempt before: **Add Timeouts, Cancellation, Rate Limits, and Bounded Retries** (lecture 106).
- Target practice time: 25 minutes, an initial estimate to calibrate with learners.
- Task: Link caller cancellation to a timeout, acquire a rate-limit lease, reject unavailable capacity and pass the bounded token to the inner client. Dispose the resources.
- Done when: Caller cancellation, timeout and rejected capacity cannot become a successful response; permits are released.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

### S08-02: GetStreamingResponseAsync

- File: [src/VibeCast.Infrastructure/AI/ChatResilienceClient.cs](../../src/VibeCast.Infrastructure/AI/ChatResilienceClient.cs)
- Attempt before: **Add Timeouts, Cancellation, Rate Limits, and Bounded Retries** (lecture 106).
- Target practice time: 25 minutes, an initial estimate to calibrate with learners.
- Task: Apply the same cancellation and capacity boundary for the whole stream lifetime. Yield updates in order and release resources when enumeration ends.
- Done when: Cancellation and early disposal release the lease; emitted chunks are not replayed.
- Evidence to keep: a test result or reproducible input/output observation, plus two sentences explaining a rejected case.

## Optional hint

Use a linked CancellationTokenSource and a disposable RateLimitLease. For streaming, the lease must survive until enumeration ends. Provider retries are already configured in DependencyInjection; do not add a second retry loop.

## Validate and compare

1. Run `dotnet restore VibeCast.sln` and `dotnet build VibeCast.sln --configuration Release --no-restore`. Exercise failures should be behavioral; missing dependencies or compile errors are setup/implementation problems.
2. Run `dotnet test VibeCast.sln --configuration Release --no-build`. Existing tests remain supplied. Tests exercising gaps may fail with the named exercise exception. This pack does not claim that every criterion already has an automated test.
3. Add or run a focused test for the task's success and rejection paths. Use local fakes for model behavior. Use Azurite for storage integration. Run cloud smoke checks only after deterministic checks; avoid repeated paid calls as a debugging loop.
4. Compare your implementation with the same file at the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/30c56863165a043b0e42c06ef78f321ba38ddd0c). Compare behavior and explain differences; matching every line is unnecessary.
5. Change one input or failure mode and predict the outcome before running it. Record whether the prediction was correct.

After finishing, use `git diff` to review your own changes. Commit your work before changing branches. To see one reference file without replacing your work, use `git show section-08-ai-engineering-complete:path/to/file.cs` with a file path from the task list.

## Using Copilot

Try the task first. Ask for an explanation or a hint about one failed case. In the testing lesson, ask Copilot to propose behavior cases, inspect its assertions, then deliberately break the relevant behavior in a disposable local copy and confirm the test detects it. Do not ask an agent to fill every practice gap in one pass. No new agent instructions or skills are installed by this pack.

## Instructor release gates

- Build the untouched scaffold and classify expected exercise failures separately from regressions.
- Restore the reference implementation for each gap and run the full test suite; all tests must pass.
- Ensure every core acceptance criterion has a deterministic test or an explicit integration checklist before release.
- Check the actual video sequence and add pause cues; lecture mapping is based on live titles and repository code, not a verified transcript review.
- Verify a fresh learner can set up the branch and reach the first gap; document credentials, quotas and costs separately.
- Pilot with a learner who has not seen the solution, then adjust task size and hints.

## Baseline correction and observability checkpoint

The original Section 8 completed DependencyInjection created ChatResilienceClient but did not use it in the returned pipeline. This practice snapshot connects that wrapper. Section 9's completed registration already includes the wrapper. Before comparing with Section 8's reference, retain this correction.

After **Trace AI Operations with OpenTelemetry**, run a successful call, a cancelled call and a failed call. Verify the VibeCast.AI source is subscribed, observe duration/status and confirm prompt/output content is not emitted. Explain why a provider-call timeout is not necessarily an end-to-end tool-workflow deadline. Provider retries and one semantic repair are separate budgets.
