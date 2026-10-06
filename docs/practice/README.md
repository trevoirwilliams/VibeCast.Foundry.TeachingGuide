# Backend practice: Foundations of Generative AI Development With .NET

## Start here

Use the root [README](../../README.md) for this branch's setup and pinned package versions. Supporting UI and application code are supplied so you can focus on the backend tasks below. This enriched scaffold may contain supporting files that appear later in the recording.

Watch the feature explanation, pause before the implementation, and try the matching task. If you have already watched it, attempt the task before opening the solution. Read the numbered comments first; expand an optional hint only when you need it. You may ask Copilot to explain one unfamiliar API or failed check.

Search for `PRACTICE S` in C# files. Each named `NotImplementedException` is an intentional gap. Replace it with real behavior, not dummy output. Task-returning stubs omit `async`; add it when using `await`. Streaming tasks need an async iterator, `yield return` and the cancellation attribute described in their hints.

Save or commit your work before switching branches. Each section starts independently; your unfinished code does not carry forward automatically.

### S04-01: Supplied request-building example

`CreateModelRequest` is complete. Read it to see how system instructions, user data and output limits form a request. Reuse it in both exercises; you do not need to implement it again.

<a id="s04-02-generateasync"></a>
### S04-02: GenerateAsync

- **File:** [src/VibeCast.Infrastructure/AI/FoundryEpisodeConceptGenerator.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodeConceptGenerator.cs)
- **Attempt before:** **Generate the First VibeCast Episode Concept with Foundry**.
- **Already provided / prerequisites:** CreateModelRequest supplies messages and options; EpisodeConceptResult is the application return type.

1. Guard against a null request, then reuse the supplied request builder.
2. Request one completed response with the messages, options and caller token.
3. Extract and trim the combined text; reject empty or whitespace output.
4. Return an EpisodeConceptResult containing the accepted text; do not log it.

<details>
<summary>Optional API hint</summary>

Use CreateModelRequest, then await chatClient.GetResponseAsync. ChatResponse.Text is the combined text for this plain-text feature; Messages is the message collection. EpisodeConceptResult takes the accepted string in its Content constructor parameter. Use InvalidOperationException for unusable output.

</details>

**Check your result:**

- Response text `  A useful concept  ` → result.Content is `A useful concept`.
- Empty or whitespace-only text → InvalidOperationException.
- An already-cancelled caller token reaches the fake client → cancellation, not a successful concept.

**Explain:** Why does the application return EpisodeConceptResult instead of exposing ChatResponse?

<a id="s04-03-streamasync"></a>
### S04-03: StreamAsync

- **File:** [src/VibeCast.Infrastructure/AI/FoundryEpisodeConceptGenerator.cs](../../src/VibeCast.Infrastructure/AI/FoundryEpisodeConceptGenerator.cs)
- **Attempt before:** **Streaming The Foundry Response to the Client**.
- **Already provided / prerequisites:** The same request builder and client used by S04-02 are supplied.

1. Guard the request and reuse the supplied request builder.
2. Enumerate streamed updates with the caller token.
3. Yield each nonempty text fragment unchanged and in order; keep spaces.
4. After enumeration, reject a stream that delivered no characters.

<details>
<summary>Optional API hint</summary>

Use an async iterator, await foreach, chatClient.GetStreamingResponseAsync and update.Text. Skip null/empty fragments, not whitespace fragments. Use yield return and count emitted characters. When implementing the iterator, add [System.Runtime.CompilerServices.EnumeratorCancellation] to its token parameter so cancellation passed to enumeration also works.

</details>

**Check your result:**

- Updates `Hello`, ``, ` `, `world` → fragments `Hello`, ` `, `world`; combined text `Hello world`.
- No updates, or only empty updates → InvalidOperationException when enumerated.
- Cancellation during enumeration → cancellation propagates; do not return a fabricated final response.

**Explain:** Why would trimming every fragment damage the result?

## Focused checks without a live model

The supplied `EpisodeConceptPracticeTests` uses an in-memory fake client. No credentials or model calls are needed for these tests. The named tests fail until you implement S04-02/S04-03; their failure messages and assertions show the expected behavior.

```bash
dotnet test tests/VibeCast.Application.Tests/VibeCast.Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~EpisodeConceptPracticeTests"
```

To work on only one exercise, append `&FullyQualifiedName~GenerateAsync` or `&FullyQualifiedName~StreamAsync` inside the filter quotes.

## Build, compare and review

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet test VibeCast.sln --configuration Release --no-build
```

An intentional exercise exception is expected when an unfinished path runs; a compiler error is not the exercise. Some tests cover other gaps, so use a focused filter where supplied. The examples above are acceptance checks, not a claim that every case has an automated test. Use local fakes for deterministic model responses; use the configured storage emulator for storage integration.

After your attempt, compare the relevant method with the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/fab04059d970dbaebe2c28c95c06c651bccbd95b). Explain behavioral differences instead of matching every line. Change one input, predict the result, then check your prediction.

<details>
<summary>Instructor validation status</summary>

This revision repairs misplaced exercise bodies and adds staged guidance. Full build, restored-solution tests and cloud smoke checks must be verified; the revision is not a certification that those checks passed. Before release, build the untouched scaffold, restore the missing implementations in a disposable copy, and run the tests. Distinguish expected exercise failures from unrelated failures. Lesson references use titles; exact transcript pause times remain unverified.

</details>
