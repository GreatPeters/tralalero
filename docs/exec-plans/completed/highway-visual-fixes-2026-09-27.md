---
title: Repair Highway vehicle presentation and fork readability
date: 2026-09-27
status: completed
---

User screenshots request: remove ugly car deformation, fix crossing fork lines to a Korean highway split with left green/right red guidance, increase early traffic, reveal cars 1–2 seconds earlier, fix buried-looking vehicles, and add modest vehicle variety (4–5 new Meshy models only if needed). This supersedes the previous no-road-guidance requirement for this scene. Other chapter scenes remain protected; no commits.

1. Back up the current authored scene/workbook/shared files. Measure native vehicle support and current asset variants; preserve user saves.
2. Repair accident body geometry and vehicle road support with native Unity authoring and runtime changes as required. Validate actual geometry, not only root transforms.
3. Replace overlapping fork paint with continuous branch-aware lane boundaries, green left/red right guidance and a visible separation area. Preserve the two choices and collision support.
4. Increase early traffic in Data.xlsx while retaining a passage and first-entry accident access. Advance appearance by 1.5 seconds without making contact unexpectedly earlier.
5. Add distinct usable existing vehicle silhouettes where possible. If new Meshy models remain necessary, first deliver the exact list/budget and Codex concepts for user confirmation; do not spend before confirmation.
6. Native before/after captures, focused builds/tests, normal-stat entry and both fork paths, preference byte restoration and protected-scene hashes. Update requirements/reliability and compound the lessons.

Evidence: outputs/highway-visual-fixes-2026-09-27. Starting working copy is already dirty from earlier tasks; do not reset it.

Completion: all requested local fixes are saved. Existing Meshy compact/SUV models provide two additional silhouettes without paid generation.72focused tests, Runtime/Editor builds and harness validation pass. play-v2 passes first-entry and clears jam/hi-pass plus open/cash; play-v3 verifies the final asphalt/guide cosmetic finish through750m. All preferences restore byte-identically, the original HighWay start scene is restored and four protected other-chapter scene hashes match. Failed/interrupted evidence and the shader-diagnostic error pause remain documented. Native comparison: http://127.0.0.1:8776/highway/fixes/. No commit. Human difficulty/earned economy/phone performance are outside this acceptance.
