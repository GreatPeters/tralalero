# Road chapter reference visual pass

## Scope and evidence

Continue the saved HighWay/RestStop scenes without recreating either scene. The two user references were opened directly from `output/meshy_images/stage_02_1_highway_concept_batch_v1.png` and `stage_03_3_rest_stop_concept_batch_v1.png`. Prior pattern screenshots were also inspected. This is an authored scenery pass, not a claim of faithful reproduction or new balance acceptance.

## Design decisions (assistant choices)

- Highway: continuous gray/green acoustic panels, layered trees and city buildings, blue daylight sky; retain all curves, both recovery forks, traffic warnings and existing encounters.
- RestStop: repeat recognizable service courtyards along the existing route, beginning with fuel on the left and convenience frontage on the right. Use existing TRELLIS buildings, vehicles, vending, chargers and furniture, with green service trim, planted sidewalk edges and red vending accents.
- Food hall: retain its current dimensions, central cross, four doors, police pool and camera behavior; improve floor, perimeter and food-counter identity.
- Prefer asset placement and scene-specific material copies over rebuilding models. New scenery has no colliders or gameplay behaviours. Original materials remain untouched.

## Sequence and gates

1. Fresh scene/file hashes and PlayerPrefs snapshot; capture identical camera positions before/after.
2. Apply scene-local changes through official Unity Pipeline `run_script`; preserve a before-state fingerprint of gameplay transforms and serialized components.
3. Review actual renders, correct obvious clashes and perform three directed live checks (Highway main, recovery fork, RestStop approach/holdout).
4. Verify scene structure and hashes, build checks appropriate to changed source, restore and compare the fresh preferences snapshot, return to saved HighWay Edit Mode.

## Status

- Official CLI 1.0.0-beta.9/Pipeline 0.6.0-exp.1 reachable; original Unity 6000.2.6f1, HighWay saved Edit Mode.
- Baseline inspection: existing 102 acoustic wall pieces are approximately 4.4m long with approximately 12m spacing. Existing RestStop meshes are usable but their composition and predominantly navy finish differ from the reference.
- Completed native authoring and repeated visual corrections: continuity, camera-visible service fascias, imported Cubemap sky, lit foliage, red vending and quiet stone flooring.
- Six directed live checks completed at TimeScale1: RestStop opening72s, two holdout completions, final frontage12s, Highway main58s and recovery54s. All used ATT37/HP46/attack-speed30 with no continuous health correction. This is scoped upgraded/section-entry evidence, not full balance acceptance.
- Both scenes pass gameplay snapshot comparison after Play Mode/reload, with no missing scripts and no new decorative colliders. Runtime/Editor builds and harness validation passed. Prior123tests were not rerun.
- Fresh63-key preferences restored and compared without mismatch; coin3007/jewel0. Workbook, signed runtime archive and SR18 hashes remain unchanged. Final original Editor: saved HighWay, Edit Mode, no console errors.
- Completed headless compound by updating the existing stage-prefab reference-matching solution; frontmatter validator passed. Repo instructions already expose docs/solutions; no instruction-file edit needed. GitHub issue search skipped because gh was unavailable. No session-history lookup or agent delegation was needed under the repository tool mapping.
- See `map-concepts/road-reference-visuals-2026-09-13/README.md` and `final-verification.json` for artifacts, exact conditions and remaining reference/device/cohort limits.
