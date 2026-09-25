---
title: Preserve Python runtime and Unicode across PowerShell boundaries
date: 2026-09-25
category: workflow-issues
module: Rest-stop TRELLIS production automation
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Resuming a Python production workflow after a reboot
  - Sending quoted code or non-ASCII text from Windows PowerShell to Python
  - Writing review decisions consumed by a running generation process
tags: [powershell, python, unicode, encoding, trellis, atomic-write, checkpoints]
---

# Preserve Python runtime and Unicode across PowerShell boundaries

## Context

The rest-stop batch resumed correctly from its saved checkpoint, but supporting commands exposed three separate boundaries:

- Bare `python` did not have Pillow, so the contact-sheet watcher failed with `ModuleNotFoundError: No module named 'PIL'`.
- A Python `-c` command lost embedded quotation marks while crossing the PowerShell/native argument boundary. It raised `SyntaxError` before writing a review.
- Piping a script containing Korean through the default PowerShell encoding replaced the Korean with question marks. Python exited successfully and wrote valid UTF-8 files containing the already-corrupted text. `-X utf8` alone could not restore the lost characters.

The affected Korean text was limited to two newly appended execution-log sections. Those sections were restored with `apply_patch` and read back successfully. Model files and the ASCII review JSON were unaffected.

## Guidance

1. Use the interpreter recorded by the working production launcher. For this batch it is `C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe`. A healthy generation process does not establish that bare `python` has the same dependencies. Do not install packages into an unrelated interpreter merely to make a helper run.
2. Prefer a repository script or a literal here-string sent to Python standard input over nested `-c` quoting. Shell quoting and JSON serialization are different operations.
3. Set the sending PowerShell process's `$OutputEncoding` to UTF-8 when the input contains Unicode. Python's decoding setting only controls the receiving side. Alternatively, serialize JSON using ASCII Unicode escapes before transport and decode it inside Python. Use `-X utf8` for helper scripts that otherwise read UTF-8 files with the local default encoding; the frontmatter checker initially raised a `cp949` decoding error without this flag.
4. Use `apply_patch` for Korean documentation changes, then read the affected section with `Get-Content -Encoding UTF8`. A zero exit code or valid JSON proves neither the intended Unicode content nor its meaning.
5. Write each completed review to a temporary file beside its destination, then publish it with `os.replace`. Refuse to overwrite an existing verdict. Preserve the observed gate and verdict in the orchestration state when a write fails; clear them only after a successful exit.

## Why this matters

Interpreter selection, command-line parsing, byte encoding and atomic publication are independent concerns. Fixing the receiving Python encoding does not fix text already lost in the shell. Similarly, a successful helper invocation does not authorize a visual pass: the actual generated views still require inspection before publishing a verdict.

## When to apply

Use this pattern for Windows generation helpers, review gates, multilingual documentation and checkpoint recovery. Preserve the original failed attempts and do not rerun a costly generation request to fix a supporting script's transport failure.

## Verified example

This real PowerShell-to-Python round trip passed during the resumed batch:

```powershell
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
@'
sample = '휴게소 재개'
assert sample == '\ud734\uac8c\uc18c \uc7ac\uac1c'
print('UTF-8 PowerShell stdin round trip verified')
'@ | & 'C:/AI/TRELLIS2-AMD/venv/Scripts/python.exe' -X utf8 -
```

The decoded code-point assertion catches sender-side corruption. Printing the same damaged value back would not provide that check.

## Evidence

- [Production launcher](../../../tools/resume-reststop-production.ps1) fixes the runtime and validates the checkpoint before starting processes.
- [Review watcher](../../../tools/watch-reststop-review.py) waits for a real gate and builds the contact sheet; it never approves the result.
- [Execution log](../../../map-concepts/reststop-production-2026-09-24/execution-log.md) records the observed failures, restored sections and actual model decisions.

Captured with `ce-compound mode:headless`, using sequential research under the project's tool mapping. No overlapping solution was found. Existing AGENTS.md guidance already makes the solution store discoverable; no instruction-file change was needed.
