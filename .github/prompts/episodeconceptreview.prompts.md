## Prompt 1 - Vibecast Architect review
Inspect the request path from #file:EpisodeCreate.razor through IEpisodeConceptGenerator, FoundryEpisodeConceptGenerator, IChatClient, AzureOpenAIClient, and Microsoft Foundry.

Do not edit files.

Verify:

Application, Infrastructure, and Web responsibilities;
provider types do not escape Infrastructure;
Razor does not own prompts or credentials;
cancellation and timeout propagation;
model-output validation;
testability;
concepts that should remain deferred to later sections.
Return only actionable findings, classified as required, recommended, or acceptable as implemented.

## Prompt 2 - VibeCast Security Review
Perform a focused security and privacy review of the AI client implementation.

Prioritize:

- API-key handling;
- prompt and generated-content logging;
- untrusted model output;
- unsafe Razor rendering;
- cancellation and resource exhaustion;
- exception disclosure;
- prompt injection through editorial form fields.

For every material finding, include the file, failure scenario, remediation,
and required regression test.

Do not edit files.

## Prompt 3 - VibeCast Test Specialist
Using the approved architecture and security findings, add the smallest
deterministic MSTest coverage necessary to close Section 04.

Use a hand-written fake IChatClient. Do not call Microsoft Foundry or read
User Secrets.

Cover:

- successful response mapping;
- system and user message roles;
- editorial brief content;
- MaxOutputTokens equals 2000;
- empty model response rejection;
- cancellation propagation.

Run the targeted tests after implementation.

## Prompt 4 - VibeCast AI Implementer
Apply only the required findings from the architecture, security, and
testing reviews.

Keep the public application contract unchanged.

Required priorities:

- remove prompt, title, or generated-content bodies from logs;
- retain MaxOutputTokens at 2000;
- preserve cancellation propagation;
- preserve provider-neutral IChatClient usage;
- avoid introducing later-section features.

Run restore, Release build, targeted tests, and the full solution tests.
Report every changed file.