# Backend practice: Production-Ready AI Application Engineering

## Start here

Use the root [README](../../README.md) for this branch's setup and pinned package versions. Supporting UI and application code are supplied so you can focus on the backend tasks below. This enriched scaffold may contain supporting files that appear later in the recording.

Watch the feature explanation, pause before the implementation, and try the matching task. If you have already watched it, attempt the task before opening the solution. Read the numbered comments first; expand an optional hint only when you need it. You may ask Copilot to explain one unfamiliar API or failed check.

Search for `PRACTICE S` in C# files. Each named `NotImplementedException` is an intentional gap. Replace it with real behavior, not dummy output. Task-returning stubs omit `async`; add it when using `await`. Streaming tasks need an async iterator, `yield return` and the cancellation attribute described in their hints.

Save or commit your work before switching branches. Each section starts independently; your unfinished code does not carry forward automatically.

<a id="s08t-01-generateasync_wheninitialplanisinvalidandrepairisvalid_returnsrepairedplan"></a>
### S08T-01: GenerateAsync_WhenInitialPlanIsInvalidAndRepairIsValid_ReturnsRepairedPlan

- **File:** [tests/VibeCast.Application.Tests/FoundryEpisodePlanningServiceBehaviorTests.cs](../../tests/VibeCast.Application.Tests/FoundryEpisodePlanningServiceBehaviorTests.cs)
- **Attempt before:** **Test AI Behavior with Representative Cases**.
- **Already provided / prerequisites:** CreatePlan, CreateRequest and SequenceChatClient are supplied; no live model is needed.

1. Arrange an invalid timing plan, then a valid repair using the supplied factory.
2. Queue both responses in the fake client and construct the real planning service.
3. Call the service once with the supplied request factory.
4. Assert two model calls, the repaired plan and accurate repair metadata.

<details>
<summary>Optional API hint</summary>

CreatePlan targets 20 minutes: try [5, 5, 5] then [7, 7, 6]. SequenceChatClient returns plans in constructor order and exposes CallCount. Use EpisodePlanValidator and NullLogger<FoundryEpisodePlanningService>.Instance. Check RepairAttempted, RepairPromptVersion, result.Plan.Summary and that the returned plan passes validation.

</details>

**Check your result:**

- The test passes with the supplied service.
- In a disposable local change, return the invalid initial plan or wrong repair metadata → this test fails; then undo that change.

**Explain:** Why does asserting only CallCount leave important behavior unchecked?

<a id="s08t-02-generateasync_whenrepairisstillinvalid_throwsafteronerepairattempt"></a>
### S08T-02: GenerateAsync_WhenRepairIsStillInvalid_ThrowsAfterOneRepairAttempt

- **File:** [tests/VibeCast.Application.Tests/FoundryEpisodePlanningServiceBehaviorTests.cs](../../tests/VibeCast.Application.Tests/FoundryEpisodePlanningServiceBehaviorTests.cs)
- **Attempt before:** **Test AI Behavior with Representative Cases**.
- **Already provided / prerequisites:** Use the same factories, fake and service construction as S08T-01.

1. Arrange two plans whose segment durations do not match the target.
2. Queue both in the fake and construct the real planning service.
3. Assert that calling the service throws the domain validation exception.
4. Assert exactly two model calls and the relevant failure and repair evidence.

<details>
<summary>Optional API hint</summary>

Try [5, 5, 5] and [6, 6, 6] for the 20-minute target. Use Assert.ThrowsExactlyAsync<EpisodePlanValidationException>. Check CallCount == 2, exception.RepairAttempted and a Failures entry with PropertyName == "Segments.TotalDuration". The fake throws if a third response is requested; do not accept that unrelated exception as success.

</details>

**Check your result:**

- Two invalid responses → EpisodePlanValidationException with repair evidence.
- Allowing another generation request, or accepting the second invalid plan → the test fails.

**Explain:** Why must the test assert the exception type as well as the call count?

<a id="s08t-03-validate_whenassessmentreferencesunknownevidencerequirement_isinvalid"></a>
### S08T-03: Validate_WhenAssessmentReferencesUnknownEvidenceRequirement_IsInvalid

- **File:** [tests/VibeCast.Application.Tests/SupportingSourceAssessmentValidatorTests.cs](../../tests/VibeCast.Application.Tests/SupportingSourceAssessmentValidatorTests.cs)
- **Attempt before:** **Test AI Behavior with Representative Cases**.
- **Already provided / prerequisites:** SupportingSourceAssessment, its validation request and validator are supplied.

1. Create an otherwise-valid assessment with one unknown evidence requirement.
2. Pass it with the plan's allowed requirements to the real validator.
3. Assert invalidity and an error for the matched-evidence field.
4. Ensure unrelated invalid fields cannot explain the test passing.

<details>
<summary>Optional API hint</summary>

Use SupportingSourceAssessmentValidationRequest(assessment, allowedRequirements). Keep rationale, summary and relevant points valid. Include one known and one unknown MatchedEvidenceRequirements entry. Assert !result.IsValid and an Errors entry whose PropertyName equals nameof(SupportingSourceAssessment.MatchedEvidenceRequirements).

</details>

**Check your result:**

- Unknown requirement → the specific field failure.
- Temporarily remove membership validation in a disposable local change → this test fails; undo afterward.

**Explain:** Why is IsValid == false alone too weak for this test?

## Run only these behavior tests

```bash
dotnet test tests/VibeCast.Application.Tests/VibeCast.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~FoundryEpisodePlanningServiceBehaviorTests|FullyQualifiedName~SupportingSourceAssessmentValidatorTests"
```

For this lesson, you may ask Copilot to draft or improve a test after you identify the behavior. Check its setup and assertions yourself. Temporarily break that behavior in a disposable local change and confirm the test fails, then undo the change. Do not replace the real validator/service with a fake that merely repeats the assertion.

## Build, compare and review

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet test VibeCast.sln --configuration Release --no-build
```

An intentional exercise exception is expected when an unfinished path runs; a compiler error is not the exercise. Some tests cover other gaps, so use a focused filter where supplied. The examples above are acceptance checks, not a claim that every case has an automated test. Use local fakes for deterministic model responses; use the configured storage emulator for storage integration.

After your attempt, compare the relevant method with the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/0ff1ad858e510eff9bce8b85868ff83cc03a8631). Explain behavioral differences instead of matching every line. Change one input, predict the result, then check your prediction.

<details>
<summary>Instructor validation status</summary>

This revision repairs misplaced exercise bodies and adds staged guidance. Full build, restored-solution tests and cloud smoke checks must be verified; the revision is not a certification that those checks passed. Before release, build the untouched scaffold, restore the missing implementations in a disposable copy, and run the tests. Distinguish expected exercise failures from unrelated failures. Lesson references use titles; exact transcript pause times remain unverified.

</details>
