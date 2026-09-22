---
name: review-ai-security
description: Review VibeCast AI-related changes for security, privacy, prompt injection, authorization, evidence scope, unsafe tool execution, and operational weaknesses.
---

# Review AI Security

Trace every trust boundary and focus on behavior that could violate an application guarantee.

Check for:

- credentials, connection strings, tokens, or secrets;
- prompts or responses written to logs;
- direct provider calls from UI code;
- missing authentication or ownership checks;
- selected knowledge sources not fully constrained to the authenticated owner;
- tools executable without explicit authorization;
- model-selected URLs, commands, paths, or resource identifiers;
- unvalidated structured output;
- unlimited repair or retry loops;
- retrieval content treated as trusted instructions;
- citations or evidence requirements not tied to approved application state;
- partial retrieval success silently widening or changing user-selected scope;
- uploaded filenames or claimed MIME types trusted without content validation;
- unsafe HTML or Markdown rendering;
- sensitive data sent to an unintended provider or geography.

For every material finding, identify a deterministic regression test when the behavior is observable.

Report:

- Severity
- File and line
- Trust boundary
- Failure or abuse scenario
- Required remediation
- Regression test

Do not edit production code while using this skill unless explicitly instructed.
