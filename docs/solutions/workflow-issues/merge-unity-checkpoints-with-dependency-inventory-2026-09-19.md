---
title: Merge Unity checkpoints using explicit files and scene dependencies
date: 2026-09-19
category: workflow-issues
module: Unity source control and generated asset checkpoints
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - A user requests merging the current Unity project after multiple uncommitted asset-authoring sessions
  - The working directory includes large raw generation archives and live Editor-generated files
tags: [unity, git, lfs, merge, asset-dependencies, checkpoint]
---

# Merge Unity checkpoints using explicit files and scene dependencies

## Context

The current game comprised thousands of new authored files while raw generation outputs and previews added several more GB. A blanket `git add -A` would mix playable project state with caches and intermediate archives. The user's “main branch” was actually named `master`.

## Guidance

1. Resolve the remote default and verify ancestry. Preserve the user's current source state; concept approval and merging the existing game are different actions.
2. Inventory changed/untracked files by path, size and role before staging. Build an explicit NUL-delimited pathspec for Assets, required package archives, project settings, tools and durable docs. Leave raw local output archives/captures alone.
3. Scan selected text paths for credential patterns without printing matched secrets. Keep native signing material excluded.
4. Use Unity's `AssetDatabase.GetDependencies` for the shipping scenes, then compare those paths and their metadata against the staged index. A source file count does not prove referenced models/textures were included.
5. If adding LFS patterns for an existing extension, renormalize the already-tracked files of that extension as well. This changes Git storage, not the media payload. Use UTF-8 without BOM for commit message files in Windows PowerShell.
6. Build the existing runtime/editor projects. If the user is currently playing, do not stop their run merely to repeat tests that already passed for unchanged logic.
7. For an ancestor-only integration, push without force. Advance the local default ref only after publication succeeds, then switch between identical trees. This avoids the expensive and disruptive old-assets/new-assets import cycle in a live Unity workspace.

## Why this matters

Explicit inventory plus dependency checks provides a reviewable source snapshot while preserving local originals. A readable default-branch name, remote ref verification and non-force updates prevent treating a local commit as completed publication.

## Related

- [Native-frame verification and editor lifecycle](../ui-bugs/verify-mobile-sdf-and-world-presentation-in-native-frames-2026-09-16.md)
- [Execution record](../../exec-plans/completed/current-game-main-integration-2026-09-19.md)
