---
title: Verify BigQuery placeholders and native Sheets state when deploying a connector
date: 2026-09-12
category: integration-issues
module: Firebase player-log Google Sheets connector
problem_type: integration_issue
component: tooling
severity: medium
symptoms:
  - "A single replace updated a SQL comment instead of the FROM dataset placeholder"
  - "Google Sheets refused frozen columns crossing a merged title"
  - "Same-invocation SpreadsheetApp reads lagged advanced Sheets API writes"
root_cause: logic_error
resolution_type: code_fix
tags: [google-sheets, apps-script, bigquery, deployment, verification]
---

# Verify BigQuery placeholders and native Sheets state

The prepared round SQL contains its dataset placeholder in a comment and the FROM clause. JavaScript's single replace targeted only the comment. Use replacement of every occurrence, and assert both that no placeholder survives and that the actual FROM identifier is correct. Five connector scenarios pass with that assertion.

Native Google Sheets disallows freezing only part of a merged range. A title merged A:T conflicted with freezing A:D; merge the title within A:D or remove that merge. Inspect the partially created state before resuming instead of rerunning destructive initialization.

Browser editor paste/save events can lag. Read back the UI's full selected code after paste has settled and before treating Save as proof. Compare the canonical SQL content as well: nested string escaping can silently drop a regex backslash. A temporary per-character checksum detected the mismatch before live round queries were accepted.

Advanced Sheets API writes and SpreadsheetApp's read cache can disagree within one invocation. The temporary60-day diagnostic printed stale30-day dates/zero rows even though the native sheet contained60 days/one real round. Independent export/readback established the saved state. Do not replace valid data based on a cached diagnostic.

The completed Tra connection imports one actual exported round and has one verified daily trigger. Original empty-sheet backup and native exports remain under `tmp/tra-live-20260912`; aggregate-only evidence is in `map-concepts/player-logs-live-2026-09-12/README.md`.
