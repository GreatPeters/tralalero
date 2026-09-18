---
title: Match the selected lobby reference and correct merchant staging
date: 2026-09-17
status: complete
---

## Authorized scope

Match the supplied concept against the actual lobby, including TMP outline materials and visual proportions. Make the merchant visibly larger and correctly seated at his shop. Separate the start gesture into arrows beside the upper text and an independent hand swaying below, as in the third supplied image. Apply the common lobby correction across all three chapters.

## Findings and implementation

- Existing outline keyword was enabled, but .065 width and 9-pixel SDF padding were for small body labels. The start prompt needs its own display material and sufficiently padded atlas.
- Previous refinement reduced chapter/name/nav/wallet to 34/59/36/42 and used 2.8 PPU multiplier, reducing visible border width. Restore reference-sized hierarchy and heavier original borders.
- Combined Swipe PNG includes arrows and hand. Reuse the separate hand sprite and separate arrow images; animate only the hand with unscaled time.
- Merchant was only 2.6 m tall at 66.6 m camera depth, with posts overlapping him. Move shop, deck and merchant together closer to spawn; enlarge the merchant and verify floor support, visible seated and greeting poses in native frames.
- Fresh current scenes, scripts/materials and prefs preserved in `tmp/backups/harbor-reference-fidelity-2026-09-17`; retain prior user changes (13 scene roots at baseline).

## Acceptance

Native three-scene captures, independent hand motion and reset checks, explicit display material/atlas checks, supported merchant feet/stool and camera framing, relevant regressions/builds, fresh preference restore, documented correction of prior visual acceptance.

## Result

Implemented and visually inspected in all three scenes. 13 native tests pass; runtime/Editor builds pass. Full-cycle hand movement, fixed arrows, re-enable centering, actual seated/greeting merchant, clear face sightline and deck support verified. Updated the earlier SDF/presentation learning to acknowledge the user's rejection of the previous result. Evidence and state-recovery record: `map-concepts/harbor-reference-fidelity-2026-09-17/README.md`.
