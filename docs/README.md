# Docs Index

This directory is the repo-local record for agent work.

Use it as a map, not a dump.

Available documents:
- `QUALITY_SCORE.md`: current quality assessment and next leverage points.
- `RELIABILITY.md`: operational failure modes and recovery notes.
- `SECURITY.md`: trust boundaries and risky assumptions.
- `solutions/`: reusable fixes, workflow notes, and scene-generation patterns discovered during agent work.
- `design/stage_prop_rebuild_20260510.md`: stage-reference prop reuse and missing-image rebuild record.
- `exec-plans/active/codex-harness-foundation.md`: current repo-shaping plan for agent harness engineering.

Editor shortcuts:
- `Tools/Design Reference/Open Page`: opens a Unity Editor page for the design spreadsheet folder and generated Meshy image folder.
- `Tools/MeshyAI/Build Stage01 Noryangjin Auto Draft Scene`: regenerates the generated Stage01 Noryangjin reference scene.
- `Tools/MeshyAI/Build Stage01_2 Noryangjin Auto Draft Scene`: regenerates the generated Stage01_2 open-harbor pier reference scene.
- `Tools/MeshyAI/Build Noryangjin MapTool Concept Layout`: regenerates the generated `Concept` layout inside the Noryangjin map-tool scene. It uses registered map-tool roads and Stage01 Noryangjin palette props to draft a playable S-shaped harbor-market route while leaving non-`Concept` manual placements in place.
- `Tools/MeshyAI/Build Noryangjin MapTool Mode 2 Layout`: regenerates only `Prop_Layout2_*` dressing in `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity`. It is fail-closed to that sibling scene, requires the preserved 19-road W-to-N-to-E skeleton, keeps the original map-tool scene untouched, and writes its validation report and previews under `Temp/`.
- `Tools/MeshyAI/노량진 맵툴`: opens the Korean RTS-style palette for imported Noryangjin road modules, buildings, props, decorations, and backgrounds. The default screen intentionally shows only category filters (`전체/도로/건물/소품/장식/배경`) and prefab thumbnails, so layout work can be built back up one step at a time. Per-prefab defaults for `기본 크기`, `기본 회전 Y`, and `높이 오프셋` are saved in `Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset`.
- The Noryangjin map-tool background palette also includes `Assets/JH/Prefab/water.prefab`, displayed as `물`. It is placed as the single low-cost `Water/Background_Water` backdrop instead of a grid-managed prop, so repeated clicks update one water plane rather than accumulating water objects in `Props`.
- Grid-managed background palette items use their own placement layer: background items block only other background items, while roads, buildings, props, and decorations can still be placed over the background.
- `Tools/MeshyAI/노량진 맵툴 씬 열기 또는 생성`: opens or creates the editor-only map-tool scene at `Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity`. Use this scene for layout work instead of placing directly in a gameplay scene. The scene now includes an editor work floor, physical cell grid, origin marker, and auto-framed Scene view so placement starts from a readable surface.
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
