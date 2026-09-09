---
status: ready-for-publication
date: 2026-09-10
---

# Noryangjin release, customization and Highway concepts

Finish the user's eight-part request on map2, retaining existing work and publishing the completed source/assets/data to Git.

Implementation and verification are complete. Final step: commit and push the reviewed project changes on map2, preserving the local backups/caches. No Highway automatic transition or completed Highway map is claimed.

Completed: 9999/fast-lateral controls; five documented long runs including boosted clears and a normal-stat level-3 clear; late-ramp E23 placement fix; current/max HP and helper-percentage fixes; workbook balance/control/catalog sheets; reachable reference-art upgrade UI; 12-item part shop with purchase/equip/reload verification; five Highway concept PNGs grounded in 29 available source models and existing road prefabs. Rollback of customization was unnecessary.

Verification: runtime/editor builds, harness validation, 76 focused passing checks plus two legacy SR18 source-hash failures. The broad isolated suite was 606/629; one title failure was corrected and retested, other historical contracts/fixtures remain listed in the release test report. Test options, speed and all 44 captured purchase keys were restored. `ce-compound mode:headless` learning is recorded in the docs index.

Artifacts: `map-concepts/noryangjin-release-2026-09-10/README.md` and `map-concepts/highway-examples-2026-09-10/README.md`.

## Units

1. Add map-tool Convenience toggles for9999 health/attack and faster lateral movement. Store preferences in the Editor session; apply on run start, restore normal values when disabled/Play exits, and keep the workbook/player build unchanged by test controls.
2. Play Noryangjin repeatedly with the requested toggles. Address observed awkwardness, pacing and hazard readability. Tune the original workbook's enemy/bonus/permanent-upgrade economy using the existing progression design. Keep boosted traversal and normal-stat balance evidence separate.
3. Apply the supplied shoe-workshop reference to the reachable existing upgrade UI, preserving real purchase/value bindings. Build an initially cosmetic-only coin shop with independent skin/shoes/hat slots, preview, purchase, equip and persistence. Store its catalog in Data.xlsx. Take a separate pre-customization snapshot; revert only this unit if it cannot be made functional.
4. After the Noryangjin work, inspect/import selected Highway models from the specified Trellis result folder. Reuse an existing road if suitable. Deliver five reference-grounded concept images: enemies, gimmicks, props, and two overall compositions. Preserve Bonus Walls. The request is for examples before a full Highway map build.
5. Verify affected logic, saved data, visuals and restart/purchase flows; document decisions and limits; commit and push to the existing remote branch. Preserve local backups/caches while excluding generated dependency trees from publication.

## Evidence and recovery

Baseline patch: `tmp/backups/sr18-release-20260910/before-project.patch`.
Reference: the user's `generated_images/019f22f4-e2cc-73b1-8fdf-68dd8b36147a/ig_05779c8238eb2b38016a4be9ce52c08191b2b6d9feb7c8f996.png`.
Primary implementation/result record: `map-concepts/noryangjin-release-2026-09-10/README.md`.
Test-earned currency, upgrades used only for verification, and Editor test settings must be restored before completion.
