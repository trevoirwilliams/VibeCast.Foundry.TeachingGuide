# Backend practice: Build Knowledge-Grounded Content Generation with Foundry IQ and Azure AI Search

## Start here

Use the root [README](../../README.md) for this branch's setup and pinned package versions. Supporting UI and application code are supplied so you can focus on the backend tasks below. This enriched scaffold may contain supporting files that appear later in the recording.

Watch the feature explanation, pause before the implementation, and try the matching task. If you have already watched it, attempt the task before opening the solution. Read the numbered comments first; expand an optional hint only when you need it. You may ask Copilot to explain one unfamiliar API or failed check.

Search for `PRACTICE S` in C# files. Each named `NotImplementedException` is an intentional gap. Replace it with real behavior, not dummy output. Task-returning stubs omit `async`; add it when using `await`. Streaming tasks need an async iterator, `yield return` and the cancellation attribute described in their hints.

Save or commit your work before switching branches. Each section starts independently; your unfinished code does not carry forward automatically.

<a id="s07-01-buildsourcefilter"></a>
### S07-01: BuildSourceFilter

- **File:** [src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs](../../src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs)
- **Attempt before:** **Fix Reference and Filter Condition Implementation**.
- **Already provided / prerequisites:** Ownership resolution and EscapeODataString are supplied. This helper returns the filter string only.

1. Use only the source URLs already resolved by the caller.
2. Remove blank and duplicate values; reject an empty allowed set.
3. Escape each URL as an OData string literal using the supplied helper.
4. Build a search.in filter on the configured source field from the escaped values.

<details>
<summary>Optional API hint</summary>

Use StringComparer.Ordinal for deduplication. EscapeODataString doubles apostrophes. This checkpoint uses search.in: join the escaped URL values with a pipe delimiter and include that delimiter in the filter. For field blob_url and one URL, the shape is search.in(blob_url, 'https://example.test/a.pdf', '|'). For an empty set throw SafeApplicationException; do not return an empty filter that could broaden retrieval.

</details>

**Check your result:**

- Duplicate URLs → one entry per distinct URL in the filter.
- A URL containing O'Brien.pdf → O''Brien.pdf inside the quoted literal.
- No usable URLs → rejection, never an unrestricted query.

**Explain:** Why is an empty filter unsafe even when ownership was checked earlier?

<a id="s07-02-readevidencereferences"></a>
### S07-02: ReadEvidenceReferences

- **File:** [src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs](../../src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs)
- **Attempt before:** **Fix Reference and Filter Condition Implementation**.
- **Already provided / prerequisites:** GroundedEvidenceReference and CreateSnippet are supplied. Generation and empty-evidence rejection remain in the caller.

1. Parse grounding as a JSON array; treat unusable input as no evidence.
2. Read each reference ID, accepting strings and numbers as strings.
3. Skip entries without a usable ID and create snippets from content.
4. Return the reference array; the caller rejects an empty array.

<details>
<summary>Optional API hint</summary>

Use JsonDocument.Parse and JsonValueKind. For ref_id, use GetString for strings or GetRawText for numbers. Validate an item is an object before TryGetProperty; content should be a string or use an empty snippet input. Catch JsonException and return an empty array. Do not renumber IDs.

</details>

**Check your result:**

- `[{"ref_id":1,"content":"Alpha"},{"ref_id":"doc-2","content":"Beta"}]` → IDs "1" and "doc-2".
- Invalid JSON, a non-array root or no usable IDs → empty references.
- Malformed entries such as null, or a numeric content field, do not crash parsing.

**Explain:** Why must a numeric reference ID retain its identity when represented as a string?

<a id="s07-03-validateblog"></a>
### S07-03: ValidateBlog

- **File:** [src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs](../../src/VibeCast.Infrastructure/AI/FoundryGroundedBlogGenerationService.cs)
- **Attempt before:** **Generate a Grounded Blog from Knowledge Source**.
- **Already provided / prerequisites:** The generated draft and retrieved ID set are passed in. This method validates/mutates the draft and returns void.

1. Check the required title, introduction and conclusion.
2. Check section and takeaway counts against this feature's bounds.
3. Normalize the cited ID list by removing blank values and duplicates.
4. Reject no citations or any ID outside the supplied evidence set.
5. Assign the accepted IDs back to the draft.

<details>
<summary>Optional API hint</summary>

Require 3–6 sections and 3–6 key takeaways. Use StringComparer.Ordinal for ID comparisons and deduplication. Check SourceReferenceIds against availableReferenceIds; use SafeApplicationException for invalid drafts. Citation membership does not establish that each claim is supported.

</details>

**Check your result:**

- Available IDs "1", "doc-2"; citations "1", "1" → one accepted "1".
- Citation "invented" or no citations → rejection.
- Missing title or only two sections → rejection.

**Explain:** What can reference membership validation prove, and what still needs evidence review?

## Check helpers before making paid calls

The three tasks are independent helpers. Use the small inputs above to reason through filtering, parsing and reference membership before trying the whole gallery workflow. Keep helpers private; exercise them through the service in tests or use a debugger to inspect their inputs and outputs.

The completed checkpoint is a worked reference, not a substitute for these acceptance checks. In particular, malformed JSON entry shapes need explicit handling; the original reference parser does not cover every shape listed above.

## Build, compare and review

```bash
dotnet restore VibeCast.sln
dotnet build VibeCast.sln --configuration Release --no-restore
dotnet test VibeCast.sln --configuration Release --no-build
```

An intentional exercise exception is expected when an unfinished path runs; a compiler error is not the exercise. Some tests cover other gaps, so use a focused filter where supplied. The examples above are acceptance checks, not a claim that every case has an automated test. Use local fakes for deterministic model responses; use the configured storage emulator for storage integration.

After your attempt, compare the relevant method with the [pinned reference solution](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/tree/b892798b5926d055a7211125f23fd350a438205d). Explain behavioral differences instead of matching every line. Change one input, predict the result, then check your prediction.

<details>
<summary>Instructor validation status</summary>

Checked on 2026-10-06: The scaffold Release build, migration check and existing test suite passed in GitHub Actions. The helper exercises still need the acceptance checks above; a passing existing suite does not mean those gaps are complete.

[Scaffold CI run](https://github.com/trevoirwilliams/VibeCast.Foundry.TeachingGuide/actions/runs/37504309077). A restored-solution test run and live cloud checks have not been performed for this revision. Those are still needed before claiming that every completed exercise is verified. Lesson references use titles; exact transcript pause times remain unverified.

</details>
