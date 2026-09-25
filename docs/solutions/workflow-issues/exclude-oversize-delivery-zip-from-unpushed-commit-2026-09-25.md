---
title: Exclude oversized delivery archives from an unpushed commit
date: 2026-09-25
category: workflow-issues
module: Unity asset delivery and Git LFS
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - GitHub LFS rejects a generated archive with a per-file size error
  - The archive was introduced by the last unpublished commit
tags: [git, lfs, delivery, archive, amend, working-tree]
---

# Exclude oversized delivery archives from an unpushed commit

## Context

The `reststop-assets-r1.zip` delivery bundle was 6,319,598,606 bytes. Its LFS object ID matched the rejected `2c4a81ce...` object in SourceTree's HTTP 422 error, which specified a 2,147,483,648-byte per-file maximum. An LFS progress line reporting 100% did not mean the branch push succeeded.

The bundle was introduced by local commit `10cd1292c` (`최신 모델 적용`). A live `git ls-remote` check found the remote branch at its parent, `514a7186b`. Other Unity/source work was still modified or untracked and was outside this repair.

## Guidance

1. Match the error's object ID to the committed LFS pointer and verify its declared size. Inspect all current LFS file sizes for other oversized objects.
2. Check the actual remote ref, the introducing commit and staged changes before choosing an amendment. This procedure applies to a single unpublished tip; an older or published introducing commit requires a different history plan.
3. Keep the local archive. Use `git rm --cached -- <archive>` to remove only its Git entry, then add a narrow ignore rule for the generated delivery bundle family. Individual models, textures, scenes and production records remain versioned.
4. Add only the intended repair files. Confirm the staged file list before `git commit --amend --no-edit`. Preserve the existing message, author, parent and every unrelated tree entry. Never stage the whole dirty workspace for a push repair.
5. Verify the resulting tree delta against the original commit, the archive's continued presence and ignored status, and the absence of oversized LFS objects in the outgoing history. Push the explicit branch normally; a replacement child of the unchanged remote tip needs no force push.
6. Confirm the remote ref equals the repaired local tip after the push. Keep a local receipt of old/new commit IDs and worktree preservation checks.

Adding a later deletion commit alone leaves the oversized pointer reachable in the outgoing history. Removing `.gitattributes` LFS rules is also not a solution: it would send the large binary through ordinary Git instead.

## Prevention

The repository ignores `/outputs/reststop-production-*/reststop-assets-*.zip`. These combined delivery archives duplicate already-versioned assets and are retained locally for download. GitHub's per-file LFS limits are separate from total storage capacity: [official limits](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage).

## Related

- [Merge Unity checkpoints using explicit files and scene dependencies](merge-unity-checkpoints-with-dependency-inventory-2026-09-19.md)
