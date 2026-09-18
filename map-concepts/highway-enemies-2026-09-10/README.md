# Highway enemy FBXs — TRELLIS 2

Three characters from the approved highway concept were generated as individual references with ImageGen, reconstructed by the user's installed local TRELLIS2 automation engine, baked/exported with Blender, and imported into Unity with URP materials and normal maps.

| Character | Unity FBX | Unity vertices | Triangles |
|---|---|---:|---:|
| Cone mechanic | `Assets/ShooterSurvival/Models/Highway/ConeMechanic/Highway_ConeMechanic.fbx` |25464|23276|
| Traffic patrol | `Assets/ShooterSurvival/Models/Highway/TrafficPatrol/Highway_TrafficPatrol.fbx` |24719|23264|
| Tollgate chief | `Assets/ShooterSurvival/Models/Highway/TollgateChief/Highway_TollgateChief.fbx` |25098|23476|

Each directory also contains BaseColor/Normal/Roughness/Metallic textures and a `_Visual.prefab`. These are unrigged visual meshes. No skeleton, humanoid avatar, attack animation or highway encounter behavior is claimed.

The user allowed higher mesh budgets. Unity counts include UV/normal split vertices; Blender's fresh FBX import reports13746/13513/13462 vertices. Existing Noryangjin enemy meshes are roughly2k vertices. A separate4000-triangle LOD experiment passed numeric checks but broke silhouettes visibly; those variants were rejected and moved out of Assets to `tmp/rejected-mobile-lods-20260910/`. Do not substitute them for the reviewed FBXs.

## Verification and provenance

- Native engine: `C:/Users/ljh/Desktop/AI 프로그램/Trellis 자동화/앱/engine.py`, invoked by `tools/generate-highway-enemies.py` without altering the app's saved settings.
-1536 reconstruction,2K textures,25k target triangles, models unloaded between supported stages, tiled decoder, existing60-second inter-image cooldown. The existing rembg preprocessing node removes reference backgrounds. No paid external3D API.
- Input references: `input/01-mechanic.png`, `input/02-patrol.png`, `input/03-chief.png`; additional clickable copies under `tmp/image-previews/highway-enemies-2026-09-10/`.
- Source folders under `outputs/highway-enemies-2026-09-10/`: `01-mechanic_4a97f10f4c`, `02-patrol_cd22a7a411`, `03-chief_49f79d0151`. Each retains `trellis_source.glb`, `model.blend`, `model.glb`, `model.fbx`, textures, submitted prompt and engine logs.
- All three engine `validation.json` reports pass fresh FBX and GLB imports: finite coordinates, UVs, zero loose vertices and zero degenerate faces. Manifold printing topology is not asserted.
- Each high-detail GLB was independently rendered with `blender-asset-validation/scripts/render_evidence.py`. All three `evidence/contact_sheet.png` files were opened and reviewed from front/back/sides/top/perspective. The individual silhouettes, clothes and props remain readable.
- Unity import uses `tools/import-highway-enemies.cs`. URP/Lit BaseMap and tangent normal map are explicitly assigned; material scalar roughness/metallic approximations are retained alongside the original separate maps.

## Existing highway roads

The project already has road FBXs. `Assets/ithappy/Megacity/Meshes/Landscape/Roads/road_001.fbx` has90 vertices and is the source of `Assets/ithappy/Megacity/Traffic/Prefabs/Roads/Road_001.prefab`. The same Roads mesh directory contains the other stock road modules. The24 road prefabs and one intersection remain available; no new road FBX or speculative highway map was needed for this request.

## Image brief

Use the corresponding character from the prior three-character highway sheet, preserving outfit, proportions and identity. Generate one unobstructed full-body figure in a relaxed separated-arm stance, with its prop outside the torso/leg silhouette: white-helmet orange-vest cone mechanic; blue-capped reflective-vest patrol with red baton; heavyset orange-vest toll chief with striped barrier club. No road, pedestal, text, panels or extra characters. Built-in ImageGen produced the references; TRELLIS handled3D reconstruction.
