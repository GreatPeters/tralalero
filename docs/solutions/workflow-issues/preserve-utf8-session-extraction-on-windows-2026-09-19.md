---
title: Preserve UTF-8 session extraction on Windows
date: 2026-09-19
category: workflow-issues
module: Session reference recovery
problem_type: workflow_issue
component: assistant
severity: low
applies_when:
  - Recovering a numbered image reference from a Korean Codex session
  - Passing UTF-8 source text to Python through Windows PowerShell
tags: [sessions, windows, utf8, imagegen, reference-recovery]
---

# Preserve UTF-8 session extraction on Windows

## Context

Recovering the user's recent UI 7 reference exposed two encoding boundaries: Python defaulted to cp949 while reading UTF-8 JSONL, and a Windows PowerShell text pipeline replaced Korean with question marks before the skeleton extractor received it. The extractor reported zero parse errors despite the lost text. A similar pipeline silently prevented Korean string replacements in a copied gallery builder.

## Guidance

- Set `PYTHONUTF8=1` before launching metadata/extraction scripts.
- Pass source bytes directly with `subprocess.run(..., stdin=source.open('rb'))`, not `Get-Content | python`.
- Write scripts with Korean text through `apply_patch` or an explicit UTF-8 file writer, then run the saved script. Console output encoding alone does not guarantee native-command input encoding.
- Locate the named previous thread via session index, exclude the current thread, and extract only the bounded relevant session with the supplied skill scripts. Do not place raw JSONL or image base64 in the model context.
- Check meaningful Korean content as well as JSON parse status. The recovered prior final identified the latest gallery's 07 open orbit ring; its local PNG was inspected before generation.

## Why This Matters

A parseable skeleton can still lose the words needed to resolve the user's reference. An apparently successful string replacement can leave the old gallery title intact.

## Examples and verification

The corrected extraction retained readable Korean and the latest numbered concept links. An explicit patch corrected the new gallery title after a browser snapshot exposed the unchanged heading. The new ten-image batch is documented in [ring variants](../../../map-concepts/bonus-ring-variants-2026-09-19/README.md).

Related: [external gallery and native preview limits](protect-codex-image-clicks-without-changing-thread-state-2026-09-19.md), [earlier connector encoding boundary](run-unity-scene-generation-through-cli-connector-exec-when-editor-reload-is-stale-2026-06-21.md).

Captured with ce-compound mode:headless. Research and overlap review ran sequentially per AGENTS.md. No additional session scan or external issue search was needed for this local encoding failure.
