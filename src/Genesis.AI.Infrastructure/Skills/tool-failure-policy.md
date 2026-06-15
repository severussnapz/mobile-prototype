---
name: tool-failure-policy
description: 'Use this skill in all pipeline stages. Defines the retry-twice rule, failure reason emission protocol, and prohibition on advancing phase after tool failure.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Tool Failure Policy

## Rule 1 — Retry twice, then stop

If a tool call fails (returns an error or unexpected result):
1. Retry once with the same parameters.
2. If it fails again, retry once with corrected parameters (if the error suggests a parameter issue).
3. If it fails a third time, stop and report.

Do not retry more than twice for the same tool call.

## Rule 2 — Emit failure reason

When stopping after retries, always emit a plain-language failure reason:

```
⚠️ Tool failure: [tool_name] failed after 2 retries.
Error: [error message or description]
Impact: [what this failure prevents]
Action required: [what the human needs to do, or "system error — please retry the conversation"]
```

## Rule 3 — No phase advance on failure

If a tool call failure prevents completion of the current phase:
- Do NOT call `advance_phase`.
- Do NOT proceed as if the phase completed.
- Add a CRITICAL parking lot item.
- Wait for human intervention.

The only exception: if the tool failure is for a non-blocking tool (e.g. `update_progress`) and the substantive phase work is complete, you may note the failure and continue.

## Rule 4 — `save_artefact` failures are always blocking

`save_artefact` and `edit_artefact` failures are ALWAYS blocking regardless of context.
A failed artefact write means the output does not exist in the system.
Never advance the phase if the artefact write failed.

## Rule 5 — Idempotent retry

Before retrying `save_artefact`, call `list_artefacts` to check whether the file was actually written despite the error.
If it exists with the expected content, the failure was a false negative — proceed without retry.
