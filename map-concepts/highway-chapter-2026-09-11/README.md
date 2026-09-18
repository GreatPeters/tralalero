# HighWay chapter

Authored scene: `Assets/ShooterSurvival/Scenes/Tools/HighWay.unity`.

Open `Tools > 맵 제작 도구 > 씬 이동 > 고속도로`, then `맵 툴 열기`. The selector left of `모두 삭제` switches the palette between `노량진 맵툴` and `고속도로 컨셉`. Scene navigation is separate from palette selection.

## Layout and gameplay

-2340m route with five turns, two shallow elevation changes,23 enemies across six models,12 left/right bonus pairs and29 hazards.40m of apron behind spawn supports the player and helpers.
- Districts progress from expressway entry through roadworks, traffic, toll lanes and tunnel ribs. New asphalt, road markings, supports, signs, roadside buildings/trees and the user's latest29 TRELLIS props are used.
- Roadblocks take projectile damage and open their lane when broken. Crossing cars show a warning before moving. Three toll barriers share the same workbook cycle so one lane stays open.
- Noryangjin's clear screen offers `고속도로로`. HighWay offers chapter replay; no unimplemented third chapter is advertised.

## Data and ownership

`Data.xlsx` remains the canonical editable file. `밸런스 조정!B28:D45` controls Highway damage/health growth, tier multipliers, hazard damage, durability and timing. `적 배치`, `보너스 배치`, `기믹 배치` contain64 HighWay rows. Scene identifiers are listed in [placements.json](placements.json).

The shared `Noryangjin_MapTool` hierarchy name is retained for route/tool consumers. The copied player and camera explicitly reference the new `Roads` transform. Scene/prefab changes were authored through Unity; subsequent work should refine the saved scene, not rerun `Create` over it.

## Verification

- Corrected ordinary-input cohort, ATT level8 / HP level6 / other permanent upgrades0: **292.70,292.72,292.68seconds, all clear**. Starting ATT33/HP130; final HP82/153.63/94.48; all12 bonus choices reached in each run. TimeScale3; no9999 or accelerated lateral movement. This is an automated steering result, not a human-difficulty guarantee.
- Earlier32-second/64-second failed controller trials are retained. A later invalid trace exposed a stale copied road reference and was stopped after passing the elevated turn; it is not counted as a valid traversal. Explicit road/camera rebinding fixed it.
- `HighwayChapterIntegrationTests` verifies six model families, every workbook placement, projected road heights, junction support, spawn support and build registration. Toll-cycle rules and currency controls have focused tests.
- Full original play traces are under `tmp/image-previews/sr18-presentation-progression-2026-09-10/highway-bound-route-20260911/`.
- After fixing projectile-to-roadblock delivery, a final ordinary-input run cleared in292.69seconds with106.49 HP and all12 choices. Directed checks separately verify an85-health block breaking after3 real shots, open/closed toll damage0/40 and traffic avoid/contact damage0/35.

See [refinement.json](refinement.json) for refinement counts and [enemy production](../highway-enemies-2026-09-11/README.md) for source assets and model verification.
