# Docs Index

This directory is the repo-local record for agent work.

Use it as a map, not a dump.

Canonical project documents:

- `../GAME_DESIGN_OVERVIEW.md`: 기획서. 세계관, 캐릭터의 목적, 핵심 루프, 플레이 감정과 콘텐츠 방향을 관리한다.
- `../BALANCE_OVERVIEW.md`: 밸런스 문서. 조정 가능한 수치, 목표 체감, 조정 근거와 검증 결과를 관리한다.
- `../DEVELOPMENT_OVERVIEW.md`: 개발 문서. 구현 구조, 개발 상태, 제작 절차와 상세 기술 문서의 진입점을 관리한다.

새로운 내용은 성격에 맞는 기준 문서에 먼저 기록하고, 상세 구현이나 운영 절차가 길어지면 가까운 `docs/` 하위 문서로 분리한다.

Specialized planning document:

- `../MAP_DESIGN_OVERVIEW.md`: 맵 기획서. 길, 구역, 전투 비트, 기믹·보너스 배치와 맵 제작 규칙을 관리한다.
- `../map-concepts/noryangjin-expansion-2026-09-02/README.md`: 현재 51모듈에서 총 230모듈로 확장하며 기존·신규 길을 2~5번 입체 교차하는 SUPER RADICAL 30안과 도로 종류별 수량을 비교한다.

Available documents:

- `QUALITY_SCORE.md`: current quality assessment and next leverage points.
- `RELIABILITY.md`: operational failure modes and recovery notes.
- `SECURITY.md`: trust boundaries and risky assumptions.
- `solutions/`: reusable fixes, workflow notes, and scene-generation patterns discovered during agent work.
- `noryangjin-gameplay-maptool.md`: Forward gameplay installation and route-turn authoring guide for the Noryangjin map-tool scene.
- `noryangjin-enemy-movement.md`: simple per-enemy event modes, one-target movement, shared animation states, and activation-spot linking for Forward/Noryangjin encounters.
- `upgrade-shoe-workshop.md`: reference-driven shoe-workshop presentation for the existing nine permanent upgrades, including rebuild and sorting contracts.
- `Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/SOURCE.md`: source URL, CC0 license, archive hash, and retained Unity FBX record for Forward enemy walk/run locomotion.
- `noryangjin-map2-authored-scene.md`: reconciled reference contract, authored-scene composition, and verification record for Noryangjin Map 2.
- `noryangjin-mobile-optimization.md`: safe Static classification, low-poly water, Android texture budgets, camera overrides, and measured Map 1/2 results.
- `noryangjin-stage1-maptool-expansion-plan.md`: legacy Stage 1 enemy, gimmick, bonus, object, and map-tool UX ideas with four concept PNGs; its earlier route-length target is superseded by the current map plan and balance document.
- `mobile-ui-atlas-optimizer.md`: one-click Sprite Atlas V2 maintenance, exclusions, settings, validation contract, and measured UI batching results.
- `player-character-defaults.md`: Player defaults, Excel precedence, and the absolute missile speed + duration model.
- `game-data-workbook.md`: the Editor-only Excel source, map-tool shortcuts, protected runtime archive, build guard, and security boundary.
- `firebase-analytics-bigquery.md`: Firebase Unity SDK setup, retention/playtime and round-event contract, BigQuery export/query workflow, and telemetry trust boundary.
- `exec-plans/completed/protected-game-data-workbook.md`: completed implementation and verification record for the protected workbook workflow.
- `exec-plans/completed/noryangjin-enemy-event-controller.md`: completed migration from mixed movement/fire controls to the five-mode Enemy Event authoring and six-state animation contract.
- `exec-plans/active/firebase-analytics-bigquery.md`: analytics implementation, verification status, and the remaining external Firebase/BigQuery console steps.
- `design/stage_prop_rebuild_20260510.md`: stage-reference prop reuse and missing-image rebuild record.
- `exec-plans/active/codex-harness-foundation.md`: current repo-shaping plan for agent harness engineering.

Editor shortcuts:

- `Tools/맵 제작 도구/문서/기획서 열기`, `밸런스 문서 열기`, `개발 문서 열기`, `맵 기획서 열기`: opens the three canonical root documents and the specialized map plan with the operating system's default Markdown app.
- The Noryangjin map tool's `편의` tab can activate or deactivate scene-root screen UI without hiding nested world-space UI. Its `작업 그리드 범위` control also changes the horizontal X and vertical Z top-view radii independently from the default 300 up to 1200 cells without moving scene objects or changing placement coordinates. The same tab exposes only `Data.xlsx 열기`; saving the workbook reloads editor data automatically, and player builds generate and validate the protected runtime archive automatically.
- `Tools/Analytics/Firebase 대상 고정`: records the reviewed Firebase project ID, app ID, and Android package under `ProjectSettings` so later config swaps fail the build.
- `Tools/Analytics/Firebase 설정 검증`: checks pinned SDK archive hashes, Android privacy defaults, the Android config, and the reviewed destination before a device build.
- `Tools/Analytics/Firebase 연결 문서 열기`: opens the Firebase/BigQuery setup and query guide.
- `Tools/맵 제작 도구/자료/자료 위치 안내`: opens the consolidated reference-location window. Individual folder shortcuts are not exposed as separate menu commands.
- `Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기`: opens the Noryangjin map-tool palette.
- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity` is the baked Map 2 implementation of `outputs/chapter_campaign_reference_orthogonal_20min`. It is edited with the normal map-tool workflow and has no regeneration command.

Internal recovery APIs are deliberately not registered as map-production `MenuItem`s. Agents may invoke a documented static method through `unity command eval "<Type.Method>();" --project-path .` only after checking that method's scene and Play Mode preconditions. Examples include `NoryangjinForwardGameplayInstaller.InstallIntoOpenNoryangjinScene`, `ForwardEnemyMovementSetup.Configure`, and the two `NoryangjinMapStaticOptimizer` methods.

- `Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기`: opens the Korean RTS-style palette for imported Noryangjin road modules, buildings, props, decorations, and backgrounds. The default screen intentionally shows only category filters (`전체/도로/건물/소품/장식/배경`) and prefab thumbnails, so layout work can be built back up one step at a time. Per-prefab defaults for `기본 크기`, `기본 회전 Y`, and `높이 오프셋` are saved in `Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset`.
- The Noryangjin map-tool background palette also includes `Assets/JH/Prefab/water.prefab`, displayed as `물`. It is placed as the single low-cost `Water/Background_Water` backdrop instead of a grid-managed prop, so repeated clicks update one water plane rather than accumulating water objects in `Props`.
- Grid-managed background palette items use their own placement layer: background items block only other background items, while roads, buildings, props, and decorations can still be placed over the background.
- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity` is the authored map-tool and gameplay scene. Open it from the Project window before using the map-tool palette; it is not a disposable generated preview.
- The map-tool palette's rendererless `회전 스팟` item marks route corners. Select one to set its absolute target Y yaw and turn duration; see `noryangjin-gameplay-maptool.md` for placement, yaw convention, and Play Mode verification.
- The rendererless `적 발동 스팟` item stores only enemy links. Select it, click enemies in SceneView to toggle links, and press `Esc` to clear the spot selection. Each linked enemy's `Enemy Event Controller` independently chooses attack loop, shoot, move-then-attack, or start-to-target patrol; see `noryangjin-enemy-movement.md`.
- The map tool's `보너스` tab exposes one reusable `Box_left` altar, displayed as `운명의 제단`; legacy `Box_Right` and the older fixed-stat wall prefabs remain outside the palette. Choose `노멀`, `엘리트`, or `유니크` before placement, or select a placed altar and change the same grade in the map tool or `AuthoredBonusWall` Inspector. At runtime the altar reads the display label from `이름`, the effect key from `항목`, and the random value from `최소`/`최대` in `Data.xlsx`'s `보너스` sheet (`엘리트` uses the sheet's `Rare` rows). The data-first world UI places one large auto-sized value above a compact icon-and-stat badge, supporting `+999`, `+11%`, and localized labels through `ATK SPEED`. The pedestal warp combines counter-rotating water, a sparse compass layer, forward foam, and rising droplets; proximity focus enlarges only the approached choice. Only `Percent` rows render a `%` suffix; `Ratio` and `Value` rows render plain numbers. Percent-specific attack/health icons are replaced by the ordinary icons, and `attPercent`/`hpPercent` reuse the corresponding `att`/`hp` display names. Altars within the serialized `인접 판정 거리` (default 12 world units on XZ) exclude ability keys already rolled by their neighbors, so a close left/right pair cannot show the same ability. Bonus instances live under `Noryangjin_MapTool/Bonuses` on an occupancy layer separate from roads, scenery, and enemies. Rebuild the canonical prefab with `Tools/Shooter Survival/Bonus Choice Boxes/Build Box Prefabs`; refresh generated child overrides in the open scene with `Refresh Open Scene Altar Instances`; migrate legacy right-hand instances with `Migrate Open Scene To Single Altar`. Verify with `BonusAltarRulesTests` and the filtered `MonsterGrowthAndMapToolEnemyTests` suite.
- Forward enemy deaths instantiate that same `Box_left` at the enemy's ground position instead of the legacy `random_wall_normal`. The drop uses the map-tool altar's final `3 × 3 × 3` scale and `180°` Y orientation, keeps the prefab's Normal grade, and receives a removable `RuntimeBonusWall` marker, so stage preparation clears drops without deleting map-authored altars.
- In the Noryangjin map tool's selection mode, each placed object's `Y` height label is a clickable selection target. The `설치 조정` preview follows the selected placement root first and falls back to the object under the map cursor when nothing is selected. The blue footprint belongs only to the currently selected placement root (including a selected child); activation-trigger footprints follow the collider's current Y rotation. The selected-object `한 칸 이동` direction buttons move once when a press becomes active. Mouse-up blocks every following GUI frame until the next real mouse-down, so releasing cannot add another step. Holding the same button for two seconds starts repeating at four steps per second. Clearing selection also clears the blue footprint. The last-placed fallback remains limited to `이어 복붙`.
- The map-tool toolbar's `이어 복붙` action duplicates the selected placed object with its current transform and child setup. Roads advance by their manual fine-grid footprint, backgrounds retain freeform renderer placement with a one-fine-cell seam overlap, and ordinary objects meet at their renderer edges. The new copy becomes the selection, so repeated clicks continue the same placement chain.
- The selected-object card's `복사하기` action enters `붙여넣기 중` mode. Clicking any valid SceneView tile stamps another copy there while preserving the source instance's prefab connection, overrides, added children, rotation, scale, height, and within-tile offset. It can stamp multiple separated tiles without changing the clipboard source; click `붙여넣기 중` again or choose a palette item to cancel.
- The current Korean design list is `docs/design/tralalero_meshy_asset_plan_kr.xlsx`. `docs/design/tralalero_meshy_asset_plan.xlsx` is the English sibling. `old_트랄랄레오_MeshyAI_소품리스트_한글.xlsx` is an archived legacy workbook and is not part of the active rebuild flow.
- Meshy design PNGs use `{sequence}_{stage}_{kind}_{asset_number}_{english_name}.png`; run `python tools/rename_meshy_design_images.py --apply` to migrate older generated names.
- Active Meshy design sequences after the Stage01 map-tool road replacement: `001-048` Noryangjin, `049-092` Highway, `093-124` Rest Stop, `125-168` City, `169-211` Gangnam, `212-242` Common.
- `tools/redraw_modular_road_prefab_kit.py` is the previous procedural modular-road redraw flow; use it only when intentionally replacing the approved RnD road-only direction.
- Current `output/meshy_images/RnD` direction is road-only full 3D MeshyAI reference images: 9 road assets per stage, with `road_only_contact_sheet.png` and `road_only_manifest.jsonl`.
- Run `python tools/promote_rnd_road_only_assets.py --apply` to replace the active stage-specific `ROAD` modules with the approved RnD road-only images, expand each stage to 9 road variants, renumber the active queue, and update both design workbooks plus `meshy_image_prompts_kr.jsonl`.
- `tools/replace_noryangjin_roads_with_testfolder.py` replaced the previous 9 Noryangjin ROAD variants with the 3 imported map-tool modules from `Assets/ShooterSurvival/Models/MeshyAI/TestFolder`, then renumbered active PNGs and synced the workbooks/JSONL.
- `tools/generate_rnd_stage_road_examples.py` is legacy procedural RnD output and does not match the approved road-only 3D concept direction; it now requires `--allow-legacy-procedural` before it can clear and replace `RnD`.
- Use `python tools/update_redrawn_asset_notes.py` before syncing when the modular road images need their workbook visual notes and image briefs refreshed.
- Run `python tools/sync_active_meshy_design_lists.py` after moving/removing/renumbering generated PNGs so both design workbooks and `meshy_image_prompts_kr.jsonl` match the current root-level active image list.

Rules:
- Update docs when behavior, workflow, or expectations change.
- Prefer short, stable documents over one large manual.
- Link from the nearest durable index instead of repeating the same rules everywhere.
