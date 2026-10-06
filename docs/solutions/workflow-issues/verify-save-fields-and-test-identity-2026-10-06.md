---
title: Verify save field coverage and the identity of shared Editor test results
date: 2026-10-06
category: workflow-issues
module: Account progress persistence
problem_type: workflow_issue
component: tooling
severity: medium
applies_when:
  - "Progression features and cloud save integration change concurrently."
  - "Multiple sessions use one Unity Editor test runner."
tags: [unity, saved-games, progression, testing]
---

# Verify save fields and test identity

## Context

The explicit settings save review found that the newly added chapter best-progress preferences were absent from the cloud whitelist. A later poll of the shared Editor test runner returned successful EssentialProposalsTests results instead of the requested account tests. Neither a local save nor an unrelated green test suite proves account save coverage.

## Guidance

Compare gameplay persistence keys with `GameProgressStore.Keys()` whenever new persistent progression is added. Chapter 1–5 best-progress keys now participate in the snapshot and validate the normalized 0..1 bounds. Keep device-only settings outside the cloud whitelist.

Verify returned test FullName values and counts before attributing a result to a requested suite. Re-run the narrow suite when another session has replaced the shared result, and save the matching receipt immediately. The account service suite returned 17 matching passing tests after the retry.

## Evidence and limits

`PlayAccountServiceTests` checks guest device save, linked lobby upload through an injected backend, mid-run local-only save and deletion journal blocking. `GameProgressSnapshotTests` checks normalized best-progress bounds. Editor layout previews use sample status text and prove neither real Google authentication nor server writes.

See [account setup](../../google-play-account-setup.md) and [preference snapshot preservation](round-trip-check-preference-snapshots-before-unity-play-2026-10-06.md).
