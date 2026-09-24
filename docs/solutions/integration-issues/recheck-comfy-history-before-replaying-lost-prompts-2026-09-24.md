---
title: Recheck ComfyUI completion history before replaying a supposedly lost prompt
date: 2026-09-24
category: integration-issues
module: Rest-stop TRELLIS production client
problem_type: integration_issue
component: background_job
symptoms:
  - A successful backend generation was marked LostPrompt by the local client
  - The staged batch halted with its shape attempt still recorded as submitted
root_cause: async_timing
resolution_type: code_fix
severity: medium
tags: [trellis, comfyui, polling, race-condition, recovery, attempt-ledger]
---

# Recheck ComfyUI completion history before replaying a supposedly lost prompt

## Problem

The installed client reads `/history/<id>` and then `/queue` separately. A prompt can complete between those reads, making it absent from both observations even though its successful completion is now recorded. R11 halted at that boundary after the backend had produced its GLB.

## Symptoms

- Prompt `a36a4fb6-2008-4b6f-a609-72c2fc1b57c3` completed successfully at timestamp1790249398890. The client recorded its halt about214ms later.
- The original shape ledger remained `submitted` with terminal `halted` and one shape attempt consumed.
- A subsequent read of that exact history ID returned success and the expected exported GLB. Its submitted graph matched the saved request. This evidence is consistent with the two-read race; the deterministic regression reproduces that ordering.

## What Didn't Work

Treating a negative queue observation as proof of a lost job is insufficient. Restarting or resubmitting immediately would risk duplicate generation and incorrect attempt accounting. Clearing the original halted ledger would also hide the failure.

## Solution

`tools/reststop_prompt_wait.py` wraps the unchanged installed client's wait method. Only after `LostPrompt`, it rechecks the **same** history ID up to three times with bounded0/.25/.5-second delays within the original deadline. It returns a recovered success, propagates a recorded generation error, or preserves the original missing-prompt exception. It never submits another prompt or resets a timeout.

The task runner's `TaskClient.wait`, the manual texture helper and cached-stage diagnostic use this wrapper. The installed engine/workflow files and saved generation values remain unchanged, preserving generation identities and existing ledgers. Regression checks in `tools/test-reststop-prompt-wait.py` cover ordinary success, the completion race, genuinely missing prompts, a recorded backend error, and deadline expiry before/during the grace period.

R11's existing output was copied to `manual/R11-recovered-m1`, with its exact history, original submitted graph, input hash and source GLB hash in `completed-prompt-recovery.json`. Five actual views passed shape review. The original halted ledger was retained. Manual texturing of such a halted source now requires matching recovery proof before using its remaining shared texture budget.

The main batch resumed with `--defer F03,F04,R11`, retaining all90 rows. Use the actual PID written to `runner.json`; on this Windows environment the venv launcher's Start-Process PID differed from the running Python worker PID. Resume logs are `runner-resume-3.log` and `.err.log`.

## Why This Works

The second history observation closes the gap between two non-atomic reads without guessing whether generation should be repeated. Exact-ID recovery and immutable attempt records separate recovering a completed result from authorizing a new attempt.

## Prevention

- Reconcile server history and on-disk exports before resubmitting any ambiguous job.
- Treat success and error history records distinctly; a recovered record is not automatically a pass.
- Keep the original failure and use a separate recovery receipt/selected-result override.
- Do not modify installed files solely to patch a task-specific client race when that would invalidate existing generation fingerprints.
- Run `C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe tools/test-reststop-prompt-wait.py` after changing this wrapper.

## Related Issues

- [Recover cached geometry before destructive postprocessing](../workflow-issues/recover-cached-trellis-shape-before-postprocessing-loss-2026-09-24.md).
- [Preserve generation budgets during thin-part repairs](../workflow-issues/repair-thin-trellis-parts-without-resetting-generation-budgets-2026-09-24.md).

Captured with `ce-compound mode:headless` using sequential local evidence and regression checks. Existing solution-store discoverability instructions were sufficient.
