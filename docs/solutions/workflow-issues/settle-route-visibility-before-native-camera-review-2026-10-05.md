---
title: Settle route visibility and inspect native pixels before accepting camera fixtures
date: 2026-10-05
module: Chapters1-5 / Unity verification
problem_type: workflow_issue
tags: [camera, native-capture, billboard, visibility, preservation, route]
---

# Settle route visibility and inspect native pixels before accepting camera fixtures

The five-chapter composition revision exposed three ways a verification helper could misrepresent the authored game. The final source and evidence are documented in `docs/reviews/chapters1-5-composition-2026-10-05.md` and `outputs/composition-2026-10-05/`.

## Reproduce the production state transition

Calling `Chapter45Route.SampleSegment` alone does not include the chosen street branch. At Ch4 600/850m this placed a fixture between the walkable paths. Select the branch and sample through the director's current-deck method before evaluating trees, gardens, road width or center visibility.

Likewise, a directed elevator fixture must settle source-floor scenery visibility before `BeginLift`. Otherwise the next visibility pass can rewrite renderer flags after the cabin has cached them, producing a glass crossing absent from natural gameplay. V4 corrects fixture ordering; actual final cinema gameplay and a native renderer probe corroborate the result. Do not repair the product solely to satisfy an invalid fixture. A private-method abort proves restoration properties only when its synthetic world position is not a real interruption state.

## Measure a real approach, then read the rendered glyphs

A forward-distance test alone included bonus portals on parallel rest-stop roads. They consumed capture keys before the player approached the target. V2 also filters lateral distance and captures in closer four-metre intervals. Renderer bounds/`isVisible` remain navigation aids, not proof of readability or absence of occlusion. B13 acceptance uses native near32/28/24/20 PNGs that show both actual values; the far value clips at near16. Report the usable approach and its limits explicitly.

## Separate derived Edit state from serialized changes

`WallStatCanvasBillboard` has `[ExecuteAlways]` and faces the camera on enable, validate and late update. A changed camera therefore changes loaded Canvas rotations even when the scene YAML and prefab overrides do not change. The preservation audit resolves each differing Transform to its exact unchanged active billboard component, allows only that derived rotation, and separately checks the saved-scene diff and source hash. Do not ignore every Canvas or expand a generic transform allowlist.

For large repetitive Unity YAML, native `git diff --no-index` finishes promptly; Python `difflib` can spend a long time matching repeated records. Preserve partial diagnostics and switch to structural/native comparisons instead of silently overwriting them.

## Finish validation with the current session's recovery baseline

Actual Play can modify shared dynamic font/decal assets. Capture minimal byte-identical recovery copies before Play, retain the changed after-Play bytes, restore only known drift from the current manifest, and reimport only those assets. Compare all game preferences, approved Editor keys, locale, display settings, protected components, material definitions, lighting and existing deletions. Read Unity-generated native registry session counters for evidence; do not write them back. Release the preservation guard only after those checks pass.

The low-camera end-road defect was real in both old and new same-pose images. It was corrected by extending only existing road renderers past the authored goal, with no new collisions, route length or completion changes, then checked in both fixed captures and an ordinary full route. Keep this product defect separate from the fixture defects above.
