# Rest-stop production assets in RestStop and HighWay

## Scope

2026-09-25: The user selected installation into RestStop first, then HighWay. Use the completed 90-asset delivery from `outputs/reststop-production-2026-09-24/delivery-r1`. Preserve gameplay roots, encounter bindings, collision routes, progression and existing authored changes. Install relevant shared models in HighWay; do not put indoor fixtures on the highway merely to reach a count.

## Acceptance

- Import reviewed low-detail models, preserving multiple materials, alpha, normals and the eight rigs with nine clips each.
- Replace appropriate scenery and character visuals through native Unity authoring, preserving gameplay references and collision behavior.
- Record source selection, target dimensions and installed scene coverage.
- Reopen both saved scenes, verify missing references/materials and gameplay bindings, and inspect native rendered views plus moving character poses.
- Keep pre-install scene backups and repeatable installation scripts. No commit or push is part of this request.

## Status

- Source delivery: complete (90 models, 8 rigs).
- Scene backups: `outputs/reststop-scene-integration-2026-09-25/backups/`.
- Native import, 8 facility assemblies, RestStop installation and HighWay installation: complete.
- Native asset checks: 90 models, 99 material regions, 72 clips. Source FBX hashes match all 90 selected originals.
- Saved/reopened scene coverage: RestStop 90 types; HighWay 83 types. Gameplay/collider/camera/bonus contracts preserved; missing scripts/materials zero; one Animator per enemy.
- Native normal movement/contact plus controlled H03/H08 shots: pass. Player preferences restored byte-for-byte.
- Test results: RestStop filter 20/26 passed, with six baseline-confirmed unrelated old UI/workbook assertions; HighwayRebuildContractTests 3/3 passed.
- Gallery: 12 native scene images and 4 runtime videos, HTTP/length checks pass. Device performance and a complete level playthrough were not measured.
- Full report: `map-concepts/reststop-scene-integration-2026-09-25/README.md`.
