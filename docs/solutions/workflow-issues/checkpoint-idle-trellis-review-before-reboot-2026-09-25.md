---
title: Checkpoint an idle TRELLIS review gate before reboot
date: 2026-09-25
category: workflow-issues
module: Rest-stop TRELLIS production lifecycle
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A user requests stopping generation after the current stage before reboot
  - An unattended runner waits for a visual verdict without an external stop control
tags: [trellis, checkpoint, reboot, pause, process-ownership]
---

# Checkpoint an idle TRELLIS review gate before reboot

## Context

The production runner was waiting for S10's first shape review. The shape and five images were saved; ComfyUI's queue was empty. The installed engine exposes `pause()`, but the already-running wrapper offered no external control and its review loop never checked the stop event. Editing that script on disk would not change the running process.

## Guidance

1. Do not write a fake pass, rejection or uncertain verdict to unblock shutdown: each changes quality state or permits another stage.
2. Verify successful prompt history, generated ledger state, absent texture attempts, existing model and review images, and an empty queue. Snapshot state/settings/ledgers and the completed stage with hashes; inventory selected results separately.
3. If no cooperative control exists, preserve the unanswered gate and stop only exactly identified idle task processes. Report OS termination accurately instead of calling it an application-level graceful shutdown. Never cancel an active stage under a request to finish that stage first.
4. Verify both launcher/worker PIDs and the dedicated listener are gone. Classify HTTP gallery servers separately; a broad `reststop` command-line match produced a false positive in the first scan. Preserve that observation and the corrected receipt.
5. A reboot pause must survive process death. The task runner now rejects startup while `pause-request.json` exists without explicit `--resume`. The resume wrapper checks saved stage/ledger hashes, port and process ownership, keeps the three deferred repair IDs and chooses fresh logs.
6. For subsequent runs, the main loop requests the engine pause, and review/startup boundaries observe the pause file. `Paused` follows the staged engine's existing persistence path without creating a verdict or resetting budgets. A currently running generation is allowed to finish before the review boundary stops it.

## Evidence and limits

`outputs/reststop-production-2026-09-24/checkpoints/reboot-20260925-031512/` retains 185 file snapshots, 69 selected model records, successful S10 history and the shutdown receipt. Eight rigs and 72 clips were already saved. No new generation was submitted for shutdown or verification.

`python -X utf8 tools/test-reststop-pause.py` passed five tests: immediate pause, pause while awaiting review, simultaneous pause/verdict, pause with an existing verdict, and normal reuse of an existing verdict. The PowerShell resume `-CheckOnly` path also passed without starting a process. This is not an end-to-end generation test of the newly added pause behavior, and it does not retroactively make the legacy process shutdown cooperative.

## Related

- [Preserve attempts when cancelling a heavy texture](partition-glazed-trellis-props-without-resetting-attempts-2026-09-24.md)
- [Recheck completed prompt history before replay](../integration-issues/recheck-comfy-history-before-replaying-lost-prompts-2026-09-24.md)
