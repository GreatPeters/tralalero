# Docs Index

This directory is the repo-local record for agent work.

Use it as a map, not a dump.

Available documents:

- `QUALITY_SCORE.md`: current quality assessment and next leverage points.
- `RELIABILITY.md`: operational failure modes and recovery notes.
- `SECURITY.md`: trust boundaries and risky assumptions.
- `solutions/`: reusable fixes, workflow notes, and scene-generation patterns discovered during agent work.
- `noryangjin-gameplay-maptool.md`: Forward gameplay installation and route-turn authoring guide for the Noryangjin map-tool scene.
- `noryangjin-enemy-movement.md`: per-enemy movement modes and player-trigger authoring for Forward/Noryangjin encounters.
- `noryangjin-map2-authored-scene.md`: reconciled reference contract, authored-scene composition, and verification record for Noryangjin Map 2.
- `noryangjin-mobile-optimization.md`: safe Static classification, low-poly water, Android texture budgets, camera overrides, and measured Map 1/2 results.
- `player-character-defaults.md`: Player defaults, Excel precedence, and the absolute missile speed + duration model.
- `game-data-workbook.md`: the Editor-only Excel source, map-tool shortcuts, protected runtime archive, build guard, and security boundary.
- `firebase-analytics-bigquery.md`: Firebase Unity SDK setup, retention/playtime and round-event contract, BigQuery export/query workflow, and telemetry trust boundary.
- `exec-plans/completed/protected-game-data-workbook.md`: completed implementation and verification record for the protected workbook workflow.
- `exec-plans/active/firebase-analytics-bigquery.md`: analytics implementation, verification status, and the remaining external Firebase/BigQuery console steps.
- `design/stage_prop_rebuild_20260510.md`: stage-reference prop reuse and missing-image rebuild record.
- `exec-plans/active/codex-harness-foundation.md`: current repo-shaping plan for agent harness engineering.

Editor shortcuts:

- The Noryangjin map tool's `편의` tab opens or locates the shared game-data Excel file and regenerates or validates its protected runtime archive. The same actions are available under `Tools/Data`.
- `Tools/Analytics/Firebase 대상 고정`: records the reviewed Firebase project ID, app ID, and Android package under `ProjectSettings` so later config swaps fail the build.
- `Tools/Analytics/Firebase 설정 검증`: checks pinned SDK archive hashes, Android privacy defaults, the Android config, and the reviewed destination before a device build.
- `Tools/Analytics/Firebase 연결 문서 열기`: opens the Firebase/BigQuery setup and query guide.
- `Tools/맵 제작 도구/자료`: opens the asset design folder, the Noryangjin map-plan and preview folder, or the generated Meshy image folder.
- `Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기`: opens the Noryangjin map-tool palette.
- `Tools/맵 제작 도구/노량진 맵 제작/맵툴 씬 열기 또는 생성`: opens or creates the authored map-tool scene.
- `Tools/맵 제작 도구/노량진 맵 제작/게임플레이/Forward 기능 연결`: installs the Forward gameplay setup into the authored map-tool scene.
- `Tools/맵 제작 도구/노량진 맵 제작/게임플레이/적 이동 기능 연결`: repairs the five Forward enemy movement components and the map-tool trigger prefab without saving the open scene.
- `Tools/맵 제작 도구/노량진 맵 제작/최적화/현재 씬 모바일 최적화`: applies the safe mobile bake to the open Noryangjin map scene without saving that scene automatically.
- `Tools/맵 제작 도구/노량진 맵 제작/최적화/맵 1·2 모바일 최적화`: applies and saves the same idempotent bake to both authored map scenes; it refuses to overwrite a loaded dirty target scene.
- `Tools/맵 제작 도구/유지보수`: contains destructive or rarely used repair and bridge-conversion commands.
- `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity` is the baked Map 2 implementation of `outputs/chapter_campaign_reference_orthogonal_20min`. It is edited with the normal map-tool workflow and has no regeneration command.
- `Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기`: opens the Korean RTS-style palette for imported Noryangjin road modules, buildings, props, decorations, and backgrounds. The default screen intentionally shows only category filters (`전체/도로/건물/소품/장식/배경`) and prefab thumbnails, so layout work can be built back up one step at a time. Per-prefab defaults for `기본 크기`, `기본 회전 Y`, and `높이 오프셋` are saved in `Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset`.
- The Noryangjin map-tool background palette also includes `Assets/JH/Prefab/water.prefab`, displayed as `물`. It is placed as the single low-cost `Water/Background_Water` backdrop instead of a grid-managed prop, so repeated clicks update one water plane rather than accumulating water objects in `Props`.
- Grid-managed background palette items use their own placement layer: background items block only other background items, while roads, buildings, props, and decorations can still be placed over the background.
- `Tools/맵 제작 도구/노량진 맵 제작/맵툴 씬 열기 또는 생성`: opens or creates the authored map-tool scene at `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity`. It contains the editable layout surface and, after Forward gameplay is connected, also runs as the Noryangjin gameplay scene. Do not treat it as a disposable generated preview.
- `Tools/맵 제작 도구/노량진 맵 제작/게임플레이/Forward 기능 연결`: copies the configured Forward player/weapon rig, pre-start/shop UI, Managers, EventSystem, and upgrade services into the open Noryangjin map-tool scene while preserving its visible `Original` character. It also keeps Forward at Build Index `0` and enables Noryangjin at Build Index `1`.
- The map-tool palette's rendererless `회전 스팟` item marks route corners. Select one to set its absolute target Y yaw and turn duration; see `noryangjin-gameplay-maptool.md` for placement, yaw convention, and Play Mode verification.
- The rendererless `적 발동 스팟` item starts linked enemies configured for forward movement or a side entrance. Select the trigger, then click enemies in SceneView to toggle their mapping; `Esc` clears the spot selection. Direct target-list editing is hidden from the Inspector; see `noryangjin-enemy-movement.md`.
- In the Noryangjin map tool's selection mode, each placed object's `Y` height label is a clickable selection target. The `설치 조정` preview follows the selected placement root first and falls back to the object under the map cursor when nothing is selected.
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
