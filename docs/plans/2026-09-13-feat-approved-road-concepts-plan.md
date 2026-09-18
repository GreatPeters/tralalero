---
title: Apply approved road and open rest-stop concepts
date: 2026-09-13
status: completed
---

# Approved road and open rest-stop implementation

The user explicitly authorized applying the reviewed concepts and all subsequent corrections. Reuse existing assets where suitable; use the installed TRELLIS workflow for missing models. Preserve the heavily modified project and existing gameplay. Scene/prefab edits use official Unity Pipeline.

## Accepted design

- U42: all chapter NPCs are human and approximately three heads tall, with visibly longer bodies/legs. Preserve nine existing combat roles, animations and ranged equipment.
- U43: Highway has two lanes per direction and a yellow central divider. Preserve the curved route and both recovery branches. Keep the existing forward-carriageway gameplay coordinates; add the opposing carriageway on its left and repaint the existing surface into two forward lanes.
- U44/U45: restaurant defense retains the ordinary camera position, rotation, FOV and player screen scale. Player location remains fixed, while its visual body and newly fired shots turn toward approaching police through360 degrees. Preserve the current automatic nearest-target behavior; no new manual control scheme was requested.
- U46: a broad covered rest-stop hall with shops/counters at the outer perimeter and a continuously open central floor. Refer directly to the latest open-hall images, not the rejected narrow-corridor or spectator-camera versions.
- Keep the selected green/blue highway and blue/cream/timber service palette, warm daylight and visibly dense perimeter composition. Do not claim pixel-identical reference reproduction.

## Implementation units

1. Preserve current state: new scene/file snapshots, exact preferences snapshot and hashes; keep all pre-existing modifications.
2. Asset craft: inspect existing nine human rigs; re-proportion source geometry/rest bones without losing materials/actions/equipment. Inspect neutral proportions and animated exports before native import. Produce missing indoor food-counter modules using TRELLIS, then Blender cleanup/validation.
3. Runtime: keep the ordinary holdout camera fixed during all phases; retain bounded police reuse, visual-only yaw, firing and cleanup. Add focused camera/rotation regression tests.
4. Native scenery: author four-lane road paint/opposing deck, maintain branch clearance and gameplay placements; author an open covered hall and perimeter storefronts while preserving encounter ownership and entrances. Update only scoped visual hierarchies.
5. Verification: native scene structure and route support checks, camera/rotation tests, relevant combat integration tests, directed main/bypass/holdout play passes and visual refinements. Full chapter traversal uses explicitly labeled health correction only if needed to isolate route safety; do not label that balance certification.
6. Restore fresh user preferences, verify source workbook/archive/SR18 preservation or explain any deliberate scoped change, return original Editor to saved HighWay Edit Mode. Update architecture, quality and evidence records; compound reusable corrections.

## Modeling contract

Human assets retain their original stylized human faces, UVs, nine role identities and18-bone/six-action rigs. Lengthen body/legs relative to head; target approximately3-head silhouette at game distance, preserving planted feet, held equipment and projectile sockets. Use existing surface topology where it supports the reshaping; do not substitute whole-object Y scale for corrected head/body proportion. Export editable Blender source, GLB and full-key FBX plus fresh-import evidence.

New perimeter food counters must be freestanding, textured, readable from the gameplay camera, have plausible counter/display construction, and omit baked floor slabs or blocking full-room roofs. Aim for roughly15–25k triangles per visible module and one/few material sets; verify evaluated exports rather than treating count as artistic acceptance. Structural hall floor/roof/walls use native reusable architectural geometry with clean edges and finished materials.

## Preservation and limits

No whole-scene recreation, no previous installer rerun, no old preference restore, no unrelated cleanups. Workbook edits are unnecessary unless measured integration demands them; any such edit requires regenerating the signed archive. New art does not by itself certify Android performance or20-attempt progression. Existing route, encounter identities, bonuses, first/repeat rewards and opening movies remain functional.

Execution progress lives in `docs/exec-plans/active/approved-road-concepts-2026-09-13.md` and `map-concepts/approved-road-concepts-2026-09-13/`.
