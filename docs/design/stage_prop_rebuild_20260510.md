# Stage Prop Rebuild 2026-05-10

## Scope
- Source references: `output/meshy_images/stage_01_1_noryangjin.png` through `stage_05_5_gangnam.png`.
- Existing and rebuilt asset images are numbered by stage block in `output/meshy_images`.
- Rebuilt design files: `tralalero_meshy_asset_plan.xlsx`, `tralalero_meshy_asset_plan_kr.xlsx`, `meshy_image_prompts_kr.jsonl`.

## Reuse Decisions
- Highway buses are covered by `130_HWY-017_Bus_obstacle`.
- Highway toll structures are covered by `122_HWY-009_Tollgate_booth_module` and `123_HWY-010_Electronic_toll_gate`; smaller toll barrier, signal, and payment parts were split out as new assets.
- Rest-stop service signs, gas pumps, EV chargers, benches, vending machines, and parked cars are covered by existing `RST-*` assets.
- City bus stops, traffic lights, taxis, delivery scooters, crosswalks, and hydrants are covered by existing `CITY-*` assets.
- Gangnam sedans, storefronts, mannequins, shoe displays, department-store entrance, and final reward shoe are covered by existing `GNG-*`, `BLD-*`, and `GAME-*` assets.

## New Rows
The rebuilt list includes the prop and vehicle additions `NRY-036`, `NRY-037`, `HWY-023` through `HWY-031`, `RST-019` through `RST-023`, `CITY-024` through `CITY-029`, and `GNG-023` through `GNG-030`.

Stage-specific road modules were then added for all five stages:
- `NRY-038` through `NRY-046`: Noryangjin straight, S-curve, T-junction, left corner, right corner, cross intersection, hairpin, narrowing connector, and Y-split wet pier modules.
- `HWY-032` through `HWY-040`: Highway straight, elevated curve, Y-split, left corner, right corner, cross interchange, hairpin ramp, narrowing merge, and toll-lane approach modules.
- `RST-024` through `RST-032`: Rest-stop straight, S-curve, T-junction, left corner, right corner, cross intersection, roundabout, narrowing, and side parking bay modules.
- `CITY-030` through `CITY-038`: City straight crosswalk, 90-degree corner, cross intersection, T-junction, S-curve, Y-split, narrowing, roundabout, and bus-lane street modules.
- `GNG-031` through `GNG-039`: Gangnam straight, S-curve, Y-split, left corner, right corner, T-junction, cross plaza, hairpin valet loop, and narrowing luxury entrance modules.

Together with the previously listed common gameplay modules, the pre-cleanup queue contained 247 generated rows. After duplicate/reference pruning and renumbering, the active root queue contained 228 generated rows; the 2026-05-14 RnD road-only promotion expanded the current active root queue to 248 generated rows.

## Vehicle Follow-Up
The first pass treated vehicles as covered by existing generic entries:
- `HWY-016` passenger car, `HWY-017` bus, and `HWY-018` truck.
- `RST-015` parked car.
- `CITY-012` delivery scooter and `CITY-022` taxi.
- `GNG-018` black sedan.

To make the stage references easier to match one-to-one, explicit vehicle variants were then appended as `HWY-028` through `HWY-031`, `RST-022` through `RST-023`, `CITY-027` through `CITY-029`, and `GNG-029` through `GNG-030`.

## Generated Outputs
- Renamed generated design PNGs to `{sequence}_{stage}_{kind}_{asset_number}_{english_name}.png`, for example `001_STAGE01_NRY_PROPS_001_Blue_fish_crate.png`.
- Reordered generated design PNGs and workbook rows into active stage blocks: `001-054 STAGE01_NRY`, `055-098 STAGE02_HWY`, `099-130 STAGE03_RST`, `131-174 STAGE04_CITY`, `175-217 STAGE05_GNG`, `218-248 COMMON`.
- Generated and refreshed an earlier stage-specific `ROAD` pass with `tools/draw_stage_road_modules.py`; that script covered 41 Noryangjin, Highway, Rest Stop, City, and Gangnam road/path images.
- Road PNGs are MeshyAI input references, so they must be single clean 3D-style road objects on a white background with visible object thickness, shadow, and material texture, but no detached guide strokes, baseline markers, schematic side marks, colored lane strips, red center strips, or decorative carpet/trim lines.
- 2026-05-11 active image cleanup moved duplicated/reference road images out of the root queue and renumbered the remaining generated PNGs. The then-active stage road modules were `046-050`, `086-090`, `114-118`, `154-158`, and `193-197`; their previous versions are preserved under `output/meshy_images/old`.
- The active road modules were redrawn with `tools/redraw_active_stage_roads.py` to better match the stage reference tone: darker Noryangjin wood, roadway lane markings for highway/city/Gangnam, cleaner rest-stop pavement, and no detached black/white outline strips.
- The Noryangjin active road modules `046-050` were redrawn again with heavier depth, darker wet wood, rivet strips, and warmer wet highlights to better match the stage 01 reference road.
- The Noryangjin active road modules `046-050` were redrawn once more using `output/meshy_images/refDedign/048_STAGE01_NRY_GAMEPLAY_033_Noryangjin_coin_line_preset.png` as the material reference: wet wooden pier boards, raised side beams, metal rivet strips, blue puddle accents, and no checkerboard floor read.
- The active COMMON road/gameplay modules `198-199`, `209-214`, `220-221`, and `226-228` were regenerated with `tools/redraw_common_gameplay_modules.py` so shared gameplay modules use one neutral asphalt style instead of mixed flat/unclear road treatments.
- The then-active stage road modules `046-050`, `086-090`, `114-118`, `154-158`, `193-197` and COMMON road/gameplay modules `198-199`, `209-214`, `220-221`, `226-228` were then replaced with `tools/redraw_modular_road_prefab_kit.py`. That direction treated roads as snap-together prefab-kit pieces with flat connection ends, consistent module widths, 90-degree corner pieces, narrowing connectors, and T-junction/split pieces.
- The workbook visual notes and image briefs for the redrawn Noryangjin/COMMON rows were refreshed with `tools/update_redrawn_asset_notes.py` before syncing the active lists.
- The active design workbooks and `meshy_image_prompts_kr.jsonl` were synced with `tools/sync_active_meshy_design_lists.py`; the current root queue has `001-248` active generated PNGs, excluding `refDedign` and `old`.
- Added `output/meshy_images/_analysis/new_assets_182_211_contact_sheet.png` for quick visual inspection.
- Added `output/meshy_images/_analysis/vehicle_assets_contact_sheet.png` for existing vehicle coverage inspection.
- Added `output/meshy_images/_analysis/stage_road_contact_sheet_20260510.png` for checking every regenerated stage road/path module in one image.
- Added `output/meshy_images/_analysis/active_stage_roads_redrawn_20260511.png` for checking the current active redrawn road modules.
- Added `output/meshy_images/_analysis/noryangjin_ref48_redrawn_20260511.png`, `common_improved_20260511.png`, `common_all_current_20260511.png`, and `active_stage_roads_current_20260511.png` for road and COMMON checks.
- Added `output/meshy_images/_analysis/noryangjin_modular_prefab_kit_20260511.png`, `active_stage_roads_modular_prefab_kit_20260511.png`, and `common_modular_prefab_kit_20260511.png` for the current modular road prefab-kit direction.
- Refreshed `output/meshy_images/RnD` to the approved road-only full 3D MeshyAI reference direction: 9 isolated road assets per stage, `road_only_contact_sheet.png`, and `road_only_manifest.jsonl`.
- Promoted the approved RnD road-only assets into the active numbered queue with `tools/promote_rnd_road_only_assets.py --apply`; old active stage road modules were moved under `output/meshy_images/old/stage_roads_replaced_*`, each stage now has 9 active road variants, and the design workbooks plus `meshy_image_prompts_kr.jsonl` were refreshed.
- The active queue now reports generated asset images for every numbered row and no missing sequence numbers.

## Filename Convention
- Stage codes: `STAGE01_NRY`, `STAGE02_HWY`, `STAGE03_RST`, `STAGE04_CITY`, `STAGE05_GNG`, `COMMON`.
- Kind codes include `PROPS`, `OBSTACLE`, `ROAD`, `BACKGROUND`, `BOUNDARY`, `BUILDING`, `GAMEPLAY`, `PICKUP`, `ENEMY`, and `LANDMARK`.
- The filename keeps the stage code once; the asset id prefix is dropped from the filename to avoid duplicates like `STAGE01_NRY_..._NRY-001`.
- Active sequence blocks stay stage-ordered so MeshyAI batches are easy to track: `001-054` Noryangjin, `055-098` Highway, `099-130` Rest Stop, `131-174` City, `175-217` Gangnam, `218-248` Common.
- Road direction: Noryangjin reads as wet fish-market passage first. Asphalt road starts in `STAGE02_HWY`; rest stop uses parking/service-road modules; city uses crosswalk/curb street modules; Gangnam uses glossy boulevard/valet entrance modules.

## Verification
- Run `python tools/rebuild_meshy_design_assets.py` after adding or replacing generated PNGs.
- Run `python tools/rename_meshy_design_images.py --apply` if generated PNGs arrive with the older `{sequence}_{asset_id}_{name}.png` convention.
- Run `python tools/draw_stage_road_modules.py` to redraw stage-specific road/path PNGs from the current Korean workbook.
- Run `python tools/redraw_active_stage_roads.py` after active image pruning/renumbering when only the current root-level stage road module filenames should be replaced.
- Run `python tools/redraw_common_gameplay_modules.py` when the active COMMON road/gameplay modules need to be regenerated in the neutral shared style.
- Run `python tools/redraw_modular_road_prefab_kit.py` only when intentionally returning to the previous procedural modular prefab-kit road direction; this replaces active stage road modules and COMMON road/gameplay pieces while preserving filenames.
- Run `python tools/promote_rnd_road_only_assets.py --apply` when the approved RnD road-only image set should replace the active stage-specific road modules and expand the per-stage road count to 9.
- Do not use `tools/generate_rnd_stage_road_examples.py` for the current RnD direction without replacing its legacy procedural 2D-style output path; the current accepted direction is generated road-only full 3D asset-preview images. The script now requires `--allow-legacy-procedural` before it can clear and replace `RnD`.
- Run `python tools/update_redrawn_asset_notes.py` after redrawing the currently tracked modular road modules so workbook notes keep matching the actual image set.
- Run `python tools/sync_active_meshy_design_lists.py` after moving/removing/renumbering active PNGs to rewrite both workbooks and `meshy_image_prompts_kr.jsonl` from the current root-level image filenames.
- Check the `ImageGenerationQueue` / `이미지생성대기열` sheet for any `Missing image` / `이미지 없음` rows.
