# Backend practice: Prompt Engineering, Structured Outputs, and Typed C# Tools

## Start here

Use the root [README](../../README.md) for this branch's setup and pinned package versions. Supporting UI and application code are supplied so you can focus on the backend tasks below. This enriched scaffold may contain supporting files that appear later in the recording.

Watch the feature explanation, pause before the implementation, and try the matching task. If you have already watched it, attempt the task before opening the solution. Read the numbered comments first; expand an optional hint only when you need it. You may ask Copilot to explain one unfamiliar API or failed check.

Search for `PRACTICE S` in C# files. Each named `NotImplementedException` is an intentional gap. Replace it with real behavior, not dummy output. Task-returning stubs omit `async`; add it when using `await`. Streaming tasks need an async iterator, `yield return` and the cancellation attribute described in their hints.

Save or commit your work before switching branches. Each section starts independently; your unfinished code does not carry forward automatically.

<a id="s05-01-requesttypedplanasync"></a>
### S05-01: RequestTypedPlanAsync

- **File:** [src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs)
- **Attempt before:** **Generate and Render a Typed Episode Plan with Structured Outputs**.
- **Already provided / prerequisites:** JsonOptions, ChatResponseCompletionGuard and EpisodePlan are supplied.

1. Use the supplied messages and JSON options to request a typed episode plan.
2. Set the output limit and pass the caller token.
3. Check the completion state before extracting the typed result.
4. Reject a missing result; return the EpisodePlan rather than response text.

<details>
<summary>Optional API hint</summary>

Use GetResponseAsync<EpisodePlan> with JsonOptions, MaxOutputTokens = 3_500 and useJsonSchemaResponseFormat: true. Call ChatResponseCompletionGuard.EnsureUsableCompletion with FinishReason and operationName, then TryGetResult. Reject a false result or null plan with InvalidOperationException.

</details>

**Check your result:**

- A usable structured response → EpisodePlan.
- A truncated/refused completion or no usable typed result → rejection.

**Explain:** Why is a successfully parsed plan still subject to S05-02 validation?

<a id="s05-02-validate"></a>
### S05-02: Validate

- **File:** [src/VibeCast.Application/Episodes/EpisodePlanValidator.cs](../../src/VibeCast.Application/Episodes/EpisodePlanValidator.cs)
- **Attempt before:** **Validate the Episode Plan and Perform One Bounded Repair**.
- **Already provided / prerequisites:** ValidationResult, validation helpers, range constants and EpisodePlanValidatorTests are supplied.

1. Guard the plan and create a validation result to collect failures.
2. Use the supplied helpers for required text and collection rules.
3. Check segment count, sequence and positive durations; total the durations.
4. Compare that total with the target and return all recorded failures.

<details>
<summary>Optional API hint</summary>

Use ValidationResult.Add with property paths, ValidateRequiredText and ValidateStringCollection. Sequence is index + 1; treat missing segments as empty and report null entries. Text limits: Summary 600, segment Title 160, Purpose 500, talking point 300, key message 400. EvidenceRequirements, MediaRequirements and EditorialRisks allow 0–8 entries of at most 500 characters. Reuse the named constants for other ranges. Report a duration mismatch as Segments.TotalDuration.

</details>

**Check your result:**

- A valid 20-minute plan with durations 7, 7, 6 → IsValid.
- Durations 5, 5, 5 for that target → Segments.TotalDuration failure.
- Wrong sequence or a nonpositive duration → the corresponding segment property failure.

**Explain:** Why collect multiple failures instead of throwing on the first invalid field?

<a id="s05-03-generateasync"></a>
### S05-03: GenerateAsync

- **File:** [src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningService.cs)
- **Attempt before:** **Validate the Episode Plan and Perform One Bounded Repair**.
- **Already provided / prerequisites:** Complete S05-01 and S05-02 first. Prompt builders and EpisodePlanningResult are supplied.

1. Validate the request and use the supplied helpers to build initial messages.
2. Request and validate the first plan; return it if valid.
3. Otherwise build repair messages from the first plan and its validation errors.
4. Request and validate one repair; reject a second invalid plan.
5. Return the accepted plan with accurate prompt and repair metadata.

<details>
<summary>Optional API hint</summary>

Use CreateEditorialBriefJson → CreateInitialMessages → RequestTypedPlanAsync → planValidator.Validate. Only the invalid path calls CreateRepairMessages and RequestTypedPlanAsync again. Use EpisodePlanValidationException after a failed repair. Preserve EpisodePlannerPrompt.Version, RepairVersion, GeneratedAtUtc and RepairAttempted; RepairPromptVersion is null when no repair occurred. Guard Guid.Empty as well as a null request.

</details>

**Check your result:**

- Valid first plan → one generation request, RepairAttempted false.
- Invalid then valid → two generation requests, RepairAttempted true and repair version set.
- Invalid twice → EpisodePlanValidationException, with no third generation request.

**Explain:** How does a semantic repair differ from retrying a failed HTTP request?

<a id="s05-04-requesttypedplanasync"></a>
### S05-04: RequestTypedPlanAsync

- **File:** [src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningWithToolService.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodePlanningWithToolService.cs)
- **Attempt before:** **Perform Episode Planning with Editorial Policy Tool Call**.
- **Already provided / prerequisites:** GenerateAsync, ValidateCandidate, the policy provider, context and guidance state are supplied.

1. Capture the tool invocation count before requesting a plan.
2. Expose a policy function that uses trusted context and the supplied provider.
3. Record invocation and reuse policy guidance already stored in guidanceState.
4. Attach the function to the typed request and pass cancellation.
5. Reject unusable completion, missing tool invocation or missing typed plan.

<details>
<summary>Optional API hint</summary>

Create an AIFunction with AIFunctionFactory.Create, EpisodeFormatGuidanceTool.Name/Description and JsonOptions. The local function increments InvocationCount, returns cached Guidance or calls formatPolicyProvider.GetCurrentAsync. Add it to ChatOptions.Tools with ChatToolMode.Auto and MaxOutputTokens = 3_500. Use GetResponseAsync<EpisodePlan>, the completion guard and TryGetResult. Compare invocation counts for this request. Return EpisodePlan; let the supplied caller validate it.

</details>

**Check your result:**

- A tool invocation plus usable typed plan → accepted for caller validation.
- A typed response without a new policy-tool invocation → InvalidOperationException.
- Owner/policy context comes from the application, never from model-selected identity arguments.

**Explain:** Why is a previously cached policy insufficient evidence that this request invoked the tool?

## Work in dependency order

Complete S05-01 and S05-02 before running the S05-03 workflow. S05-04 is the tool-enabled variant. Use the supplied validator tests while working on S05-02:

```bash
dotnet test tests/VibeCast.Application.Tests/VibeCast.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~EpisodePlanValidatorTests"
```

Count logical plan-generation requests when checking the one-repair bound. Tool exchanges and transport retries are separate from that bound.

## Build, compare and review

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet test VibeCast.sln --configuration Release --no-build
```

An intentional exercise exception is expected when an unfinished path runs; a compiler error is not the exercise. Some tests cover other gaps, so use a focused filter where supplied. The examples above are acceptance checks, not a claim that every case has an automated test. Use local fakes for deterministic model responses; use the configured storage emulator for storage integration.

After your attempt, compare the relevant method with the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/7dd380550af0d1bb21027a8b8c63f530efe7e7d5). Explain behavioral differences instead of matching every line. Change one input, predict the result, then check your prediction.

<details>
<summary>Instructor validation status</summary>

This revision repairs misplaced exercise bodies and adds staged guidance. Full build, restored-solution tests and cloud smoke checks must be verified; the revision is not a certification that those checks passed. Before release, build the untouched scaffold, restore the missing implementations in a disposable copy, and run the tests. Distinguish expected exercise failures from unrelated failures. Lesson references use titles; exact transcript pause times remain unverified.

</details>
