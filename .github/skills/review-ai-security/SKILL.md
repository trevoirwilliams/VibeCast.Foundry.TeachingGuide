---
name: review-ai-security
description: Review VibeCast AI-related changes for security, privacy, prompt injection, authorization, data leakage, unsafe tool execution, and operational weaknesses.
---

# Review AI Security

Review the diff and trace every trust boundary.

Check for:

- credentials, connection strings, tokens, or secrets;
- prompts or responses written to logs;
- direct provider calls from UI code;
- missing authentication or ownership checks;
- tools executable without explicit authorization;
- model-selected URLs, commands, paths, or resource identifiers;
- unvalidated structured output;
- unlimited retries or output;
- missing cancellation or timeout;
- retrieval content treated as trusted instructions;
- citations that are not tied to retrieved evidence;
- uploaded filenames used as filesystem paths;
- unbounded queues or concurrency;
- unsafe HTML or Markdown rendering;
- sensitive data sent to an unintended provider or geography;
- tests that require production credentials.

Report findings using:

- Severity: Critical, High, Medium, Low
- File and line
- Exploit or failure scenario
- Required remediation
- Verification test

Do not edit production code while using this skill unless explicitly instructed.