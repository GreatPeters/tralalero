---
status: completed
date: 2026-09-11
---

# Two-chapter presentation, Highway production and playtest refinement

User request: implement all16 follow-up items. Continue the existing map2 working tree and preserve prior local changes. This plan is the acceptance record; progress/evidence is maintained in the companion work log.

Completed2026-09-11. All U1–U7 requirements are implemented and verified. [Request-by-request acceptance and artifacts](../../../map-concepts/two-chapter-2026-09-11/README.md).65 related tests pass; both builds and the harness check pass; all57 original preference keys are restored. No commit or push was requested.

## Direction

- Presentation: readable mobile game UI with a calm deep-blue foundation, restrained warm-gold actions and cyan jewel accents. Large character/item artwork, consistent spacing and type, explicit ownership/price states. Remove noisy repeated parchment and undersized copy.
- Story: actual generated character/environment motion for the stolen shoes, curse, offering demand and Noryangjin departure. Four locally generated Wan2.2 shots form the exported movie. Keep public replay/skip/start gating; a pan/zoom slideshow does not satisfy the request.
- Highway: new `HighWay` gameplay scene inspired by the five `output/meshy_images/stage_02_*_highway_concept_batch_v1.png` references: asphalt, guardrails, Korean road signs, traffic, roadworks, tollgate and tunnel. Retain the Noryangjin scene and use its runtime systems, not its market layout.

## Units and acceptance

| Unit | Requests | Work | Required verification |
|---|---|---|---|
|U1 Tool workflow|4,7,8,12|Editor-only coin/jewel grant; generic map-tool entry; chapter palette selector left of Delete All; explicit scene navigation|Grant validation/live wallet refresh; both palettes distinct; navigation preserves dirty work|
|U2 Source inventory and six enemies|5,6|Hash/identity comparison of TRELLIS Highway props; import latest assets; complete6 distinct enemy models and usable combat prefabs|Source manifest, material/scale/collider inspection, all6 spawn/attack/die|
|U3 Presentation|1,2,3,9|Rebuild shop and story layouts; produce real animated story; preserve shop persistence and gem economy|Portrait screenshots, buy/equip/reload, full sequence/skip/replay and start gating|
|U4 Highway|12,14|Create authored HighWay route, scenery, encounters, readable Highway mechanics and chapter completion flow|Route support/corners, playable lanes, contacts/projectiles, defeat/replay/chapter transition|
|U5 Data|13|HighWay enemy/bonus/gimmick and growth controls in canonical Data.xlsx, protected archive refresh|Workbook formulas/schema, live/run-start ownership, chapter-specific data applied|
|U6 Noryangjin refinement|10,11,15|Repeated ordinary play, improve measured balance/readability, natural prop clusters, selected unused Noryangjin mechanics|Retain routes/camera clearance; no impossible lanes; repeated deaths/progress records|
|U7 Integration|16|Review both chapters, runtime/editor builds, targeted tests, player state restoration, durable docs|Evidence-backed acceptance table, remaining limitations explicit|

## Scope and operational constraints

- No purchasing third-party services or publishing needed. Use installed TRELLIS2 and local assets where possible.
- Raw workbook remains `Assets/ShooterSurvival/GameData/Editor/Data.xlsx`.
- All scene/prefab/asset authoring through reachable official Unity CLI/Pipeline. No legacy MCP bridge or raw YAML edits.
- Preserve user balances/owned levels/equipment around automated tests; cheat grants occur only through explicit editor controls or temporary verified test fixtures.
- New scenery must leave visible reaction space and playable lanes. Collision/gameplay bounds must be verified separately from render bounds.
- Record failed visual candidates and technical failures that provide a reusable lesson through ce-compound before completion.
