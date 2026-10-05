## VibeCast Architect

Review the current branch and create the smallest test plan that gives meaningful regression protection for the current codebase.
Do not edit files.
Constraints:
•	maximum four proposed tests;
•	use existing MSTest projects;
•	use existing public service boundaries;
•	use hand-written deterministic fakes where necessary;
•	no live Microsoft Foundry deployment;
•	no User Secrets;
•	no new package;
•	no mocking framework;
•	no bUnit or UI test framework;
•	no production abstraction solely for testability;
•	no tests for private methods;
•	no exact generated-prose assertions.
For each proposed test include:
•	exact test name;
•	production behavior protected;
•	test project;
•	required setup;
•	key assertions;
•	regression it would catch;
•	required or optional classification.
Begin with a no-production-change plan.

## VibeCast Security Reviewer
Perform a focused read-only review of the current implementation.

Review only:

- owner-scoped episode save and reload;
- prompt and model-output logging;
- cancellation and timeout propagation;
- model-selected tool execution;
- persistence of accepted model output.

Do not edit files.

Report only material findings supported by the code.

For each finding include:

- severity;
- file and line;
- concrete failure scenario;
- smallest remediation;
- regression test.

Prefer a direct query, guard clause, or existing boundary over a new policy,
repository, wrapper, or service.

When no material issue exists for a category, state that explicitly.


## VibeCast Test Specialist
Review the current codebase before editing.

Inspect:

- EpisodeWorkspace
- FoundryEpisodePlanningService
- FoundryEpisodePlanningWithToolService
- EpisodePlanValidator
- EfEpisodeService
- current Application and Domain tests

First produce a coverage inventory and propose no more than four high-value
behavioral tests.

Prefer existing public seams and the current MSTest projects.

Do not propose:

- live AI integration tests;
- UI test frameworks;
- mocking packages;
- shared fixture hierarchies;
- new production abstractions;
- tests for private methods;
- exact generated-prose assertions.

Wait for explicit approval before editing.

After approval, implement only the selected tests, run targeted tests, and report the diff and results.
