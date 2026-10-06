# Chapters 1–4 toon extension — completed

Completed 2026-10-04T17:55:40.436807+00:00. All four scenes applied and scoped native checks completed; see [final implementation and validation report](../../reviews/chapters1-4-toon-implementation-2026-10-04.md). Earlier pending paragraphs below are chronological records, superseded by this completion and its exact limits.

## Implementation history

User instruction supplied by parent: “1~4챕터도 모두 적용해줘” (2026-10-04 15:03 UTC). Continue the approved Chapter 5 visual direction across the four existing scenes. Chapter 1 strengths stay; Chapter 4 should read as a young Korean shopping street, distinct from the highway. No new story, chapter structure, paid generation, installation, APK, commits or pushes.

## Recovery and scope

Root is the only Unity writer. Independent read-only art review is delegated to ch5_toon_visual_review. Official Unity Pipeline controls PID 83488; other editors are untouched. Scene copies + metadata, hashes of five scenes and all dependencies, product source/settings, exact observed preferences and editor state were captured under outputs/ch1to4-toon-2026-10-04/preflight and validation before edits. Chapter 5 and its owned assets remain protected baseline inputs.

## Applied, validation pending

- Scene-owned FlatKit materials: Ch1 52 / 4,126 renderers; Ch2 29 / 2,204; Ch3 84 / 1,412; Ch4 83 / 1,141. No shared originals edited.
- Ch2: 212 wall cap groups. Planned apartment replacements initially matched zero after material names changed; correction pending.
- Ch3: 19 rounded shop fascias, eight perimeter piers, quiet side floor lines. Missing title glyph identified for correction.
- Ch4: 73 decorated fronts with four types, six four-person shop queues, reused existing CH07–09 visuals and owned idle controllers; additions have no colliders. Tower and road geometry retained.
- 31 before and 31 first-pass after screenshots captured from actual Unity Play using sampled poses and existing cameras. These are composition comparisons, not natural traversal or combat passes. Root and independent reviewer inspect pixels before final gameplay.

## Remaining gates

1. Resolve concrete visual review findings and recapture affected views.
2. Run ordinary native movement/combat through each affected chapter and branch; record limits rather than treating directed poses as clears.
3. Compare original protected components/transforms and dependency hashes; verify Chapter 5 byte-identical.
4. Restore and verify exact observed game-owned preferences, editor selectors/start scene, locale, PT_ResourcesCleanup and callbacks. Never write engine session registry keys.
5. Publish local gallery, verification receipts and final review. No completion claim before all four chapters are applied and checked.

Historical 1,083/1,135 result and 52 failures are not current passes. Full suite not repeated because unrelated asset/font side effects require separate recovery work. This task uses scoped native checks plus preservation audits.

## Visual revision and structural audit

Independent v1 review rejected the desaturated Ch1 harbor, road-material boundaries in Ch2, and a missing Ch3 title glyph. All three were corrected and independently accepted in v2. The actual visible Ch1 market floor comprises 32 WetFloor renderers (the IndoorTileSurface is hidden supporting geometry); its two owned materials were corrected in v3 and the reviewer accepted 03/04/06.

Ch2 now uses 32 simple authored apartment envelopes/window modules, preserving original transforms and colliders while disabling only original visual renderers. An additional 91 asphalt surfaces use the same palette. Ch4 now includes 13 low garden/bench groups and three small kiosks outside the walking corridor. No purchased/generated service was used in this extension: native Unity geometry, isolated copies of existing materials, existing civilian rigs/idle clips.

All 31 comparison poses have exactly identical saved player position, camera position and FOV before/after. Original protected components (13,177) and their ancestor transforms (10,920) are unchanged. The only additional protected components are 24 queue Animators; lighting is unchanged in all four scenes.

Play validation is still running. Baseline Ch1 (ATT8/HP60) died at 34.8 seconds from EnemyContact before its fork; this is not a clear. Ch2's first jam/Hi-Pass route cleared in 298.3 seconds using the existing authored growth cohort (ATT level20, HP level24, fire-rate level10). Remaining routes and Ch1/3 growth coverage, Ch4 ordinary routes/connection are pending.

Preservation scan currently covers 12,666 dependency files and 622 source/settings files. Only the four approved scenes and one shared material serialization rounding changed. The material difference is `_Color.r: 0.055 -> 0.054999977`; two older local recovery copies exactly match the current preflight SHA256, and a verified copy is preserved for exact restoration after Play. No other shared asset or source/settings drift was observed. This is an outstanding restoration step, not a waived change.


## Native validation and scoped follow-up

- Ch2: all four road/exit combinations cleared at normal time scale with the existing 20/24/10 growth cohort. These are real input/combat routes, not zero-upgrade balance passes. All four have zero wrong-direction, vehicle-overlap, hidden-body and invisible-contact-damage observations. Aggregate narrow-row diagnostics were nonzero for a few end-of-route frames (3–5); they are retained, not silently called zero.
- Ch2: a separate open/cash clear waited through the actual chapter movie and entered the RestStop lobby automatically, healthy and not running. Native result-panel.png is the final lobby image; two same-frame capture requests meant the alternate arrival filename was not written. The independent reviewer approved the actual lobby pixels.
- Ch2 choice pictures now use the current scene: congestion uses a native driving capture; the open road uses an illustrative native scene/camera render with enemy visuals and world UI omitted only during capture and all states restored before saving. This is a UI illustration, not a screenshot proving an enemy-free run. Rejected intermediate images are retained, not selected.
- Ch1 baseline ATT8/HP60 died from EnemyContact at 34.8s; preserved as a failure. Existing growth cohort 37/46/30 (ATT156/HP11330) cleared the indoor route at 298.3s, HP1452; health was not pinned.
- Independent native review found a pre-existing translucent roof veil at the market entry. Added one scene-local instance of existing RestStopBuildingVisibility, targeting only 1,312 V2Roof/CeilingJointBacking renderers in the market footprint; hides them while inside and restores original forceRenderingOff states outside. No product source, collision or movement changes. Same 70.1s actual-play comparison was independently approved after correction. Full indoor rerun and outdoor route are underway; final comparison uses after-v4 for Ch1.
- Ch3, Ch4 final native coverage and the final restoration/hash audit remain pending. No all-chapter completion claim yet.
