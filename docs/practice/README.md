# Backend practice: Production-Ready AI Application Engineering

## Start here

Use the root [README](../../README.md) for this branch's setup and pinned package versions. Supporting UI and application code are supplied so you can focus on the backend tasks below. This enriched scaffold may contain supporting files that appear later in the recording.

Watch the feature explanation, pause before the implementation, and try the matching task. If you have already watched it, attempt the task before opening the solution. Read the numbered comments first; expand an optional hint only when you need it. You may ask Copilot to explain one unfamiliar API or failed check.

Search for `PRACTICE S` in C# files. Each named `NotImplementedException` is an intentional gap. Replace it with real behavior, not dummy output. Task-returning stubs omit `async`; add it when using `await`. Streaming tasks need an async iterator, `yield return` and the cancellation attribute described in their hints.

Save or commit your work before switching branches. Each section starts independently; your unfinished code does not carry forward automatically.

<a id="s08-01-getresponseasync"></a>
### S08-01: GetResponseAsync

- **File:** [src/VibeCast.Infrastructure/AI/ChatResilienceClient.cs](../../src/VibeCast.Infrastructure/AI/ChatResilienceClient.cs)
- **Attempt before:** **Add Timeouts, Cancellation, Rate Limits, and Bounded Retries**.
- **Already provided / prerequisites:** The inner client, timeout, rate limiter and provider retry configuration are supplied.

1. Link the caller's cancellation token and start the configured timeout.
2. Acquire one capacity permit using the linked token; reject an unacquired lease.
3. Forward messages and options to the inner client with that token.
4. Await completion before disposing the lease and token source.

<details>
<summary>Optional API hint</summary>

Use CancellationTokenSource.CreateLinkedTokenSource, CancelAfter(timeout), rateLimiter.AcquireAsync and lease.IsAcquired. Invoke base.GetResponseAsync to reach the wrapped client rather than recursively calling this override. Await the response inside the using scope. Return ChatResponse unchanged; do not add another retry loop.

</details>

**Check your result:**

- A response completes → lease is released.
- Denied capacity → inner client is not called.
- Cancellation or timeout → failure propagates and capacity is released.

**Explain:** Why must the method await the inner task before leaving the lease's using scope?

<a id="s08-02-getstreamingresponseasync"></a>
### S08-02: GetStreamingResponseAsync

- **File:** [src/VibeCast.Infrastructure/AI/ChatResilienceClient.cs](../../src/VibeCast.Infrastructure/AI/ChatResilienceClient.cs)
- **Attempt before:** **Add Timeouts, Cancellation, Rate Limits, and Bounded Retries**.
- **Already provided / prerequisites:** Complete S08-01 first. Corrected pipeline wiring and provider retries are supplied.

1. Apply the same linked timeout and capacity acquisition as the regular call.
2. Keep the lease alive while enumerating the inner response stream.
3. Yield each update unchanged and in order.
4. Release resources on completion, failure, cancellation or early disposal.

<details>
<summary>Optional API hint</summary>

Use an async iterator and add [EnumeratorCancellation] to its token parameter when implementing it. Use base.GetStreamingResponseAsync and WithCancellation with the linked token. Place using scopes around the await foreach, not just stream creation. Do not replay already-yielded updates or add a retry loop.

</details>

**Check your result:**

- Read one update and dispose the enumerator → the permit becomes available again.
- Cancellation while waiting for another update → resources released.
- Updates are yielded once, in their original order.

**Explain:** Why is returning a stream different from consuming it within a resource lifetime?

## Supplied pipeline and observability

Keep the corrected client wiring in this practice branch: the original Section 8 completed registration created the resilience wrapper but bypassed it in the returned pipeline. Do not undo this correction when comparing files. Provider retries are supplied in DependencyInjection; do not add a second retry loop.

After the OpenTelemetry lesson, observe a successful, cancelled and failed operation with the supplied telemetry. Check duration/status and that raw prompt/output content is not logged. Explain why a provider-call timeout is not necessarily a deadline for an entire tool workflow.

## Build, compare and review

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet test VibeCast.sln --configuration Release --no-build
```

An intentional exercise exception is expected when an unfinished path runs; a compiler error is not the exercise. Some tests cover other gaps, so use a focused filter where supplied. The examples above are acceptance checks, not a claim that every case has an automated test. Use local fakes for deterministic model responses; use the configured storage emulator for storage integration.

After your attempt, compare the relevant method with the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/30c56863165a043b0e42c06ef78f321ba38ddd0c). Explain behavioral differences instead of matching every line. Change one input, predict the result, then check your prediction.

<details>
<summary>Instructor validation status</summary>

Checked on 2026-10-06: The scaffold Release build, migration check and all 21 existing tests passed in GitHub Actions after correcting the supplied host's duplicate health route. Existing tests do not cover the full lifetime/cancellation behavior of the resilience gaps; use the checks above.

[Scaffold CI run](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/actions/runs/37504667212). A restored-solution test run and live cloud checks have not been performed for this revision. Those are still needed before claiming that every completed exercise is verified. Lesson references use titles; exact transcript pause times remain unverified.

</details>
