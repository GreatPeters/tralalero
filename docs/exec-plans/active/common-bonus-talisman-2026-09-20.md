# Common folded talisman Bonus presentation

Status: completed, 2026-09-20.

User-authorized implementation of the selected `03. 접힌 부적 · 모든 강화는 몸으로`, across every Bonus type, authored placements and enemy drops. Existing stats, choice rules, rarity, spawns and rewards stay authoritative. All types share torso absorption; icon/text differ.

## Work and verification

1. Inventory current Bonus prefabs and scene paths, inspect live scale/UI and snapshot pre-existing changes.
2. Build one reproducible native mesh/material talisman and common transient torso effect; connect the shared WallScript lifecycle and icon updates, retaining negative walls.
3. Install on scene placements and canonical/all Bonus prefab sources through Unity Editor APIs, including future authored/dropped instances.
4. Compile, run focused Bonus regression tests, inspect native three-scene idle and actual pickup frames, and verify transient cleanup/restart and representative normal/rare/unique types.
5. Record implementation, constraints and reusable corrections.

## Progress

- Unity Pipeline reachable, original active SR18 scene clean, Play Mode state being inspected.
- Working branch `feat/common-bonus-talisman`; pre-existing unrelated tracked changes retained. No external download is currently needed.
- Completed: shared native art/effect, 21 source prefabs and 150 placements, authoring/runtime/drop paths, 37 passing tests, native reward/helper/drop/reset checks, three-scene captures and clean saved-scene audits. Runtime/editor builds and harness validation passed. Original SR18 clean Edit Mode restored.
- Result and verification commands: `map-concepts/common-talisman-applied-2026-09-20/README.md`. Android device performance was not part of this Editor acceptance.
