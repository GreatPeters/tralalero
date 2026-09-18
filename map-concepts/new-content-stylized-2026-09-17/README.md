# New content — Stylized Surface

User request: apply Stylized Surface to the new Merchant and all newly added content. Exact wording is U91 in `docs/design/USER_STATED_REQUIREMENTS.md`.

## Applied

`materials.txt` enumerates 174 project-owned new surface materials: 164 URP Lit, two road-paint Unlit and eight deprecated FlatKit outline materials. This includes Merchant, workshop, stool, Highway/RestStop scenery and enemies, food-hall counters, mascot props, body atlases and equipment. They now use `FlatKit/Stylized Surface`; 171 opaque non-paint surfaces have thin outlines. Glass retains alpha 0.1 and queue 3000; the two road paints retain their colors without silhouette outlines. UI/font/particle/skybox/coin shaders are intentionally specialized.

Implementation choices: primary cel threshold 0.45, edge 0.08, shadow tint 72% of base tint; outline width 1.2, depth offset 0.005, camera-distance impact 0.2. Textures, normal maps, base tint, UVs, transparency, culling and queue remain intact. PBR metallic/roughness source data remains stored, but FlatKit supplies the requested stylized lighting response.

Shared material identities are unchanged, so existing scenes, prefabs and catalog references inherit the result. `GeneratedStylizedSurface.Apply` is used by the related Editor importers/builders and road-authoring scripts. Mobile, PC and the FlatKit example renderer each register the 171 outline materials; no duplicate features were added.

## Verification

- `before.json`: native inventory; `preserved-properties.json`: all 174 compared against the original backup with zero differences in protected source properties.
- `verified.json`: native post-conversion shader/map/keyword audit.
- `tests.json`: three Unity EditMode tests pass: source/transparent-state preservation and idempotence; every manifest material; all shipping outline renderer registrations. `tests-first-pass.json` retains the transparent-tag correction evidence.
- `captures.json`: actual scene renderer inspection. The only non-FlatKit project Models surfaces still used are the two pre-existing weapon/player materials outside this new-content scope.
- Editor build and `tools/validate-agent-harness.ps1` pass. Native frames inspected for visible texture loss, pink shaders, excessive black shading and missing outlines. These are Editor camera renders, not device performance measurements or a full campaign playthrough.

Native PNGs (saved without overwriting previous captures):

- `../../tmp/image-previews/new-content-stylized-2026-09-17/Merchant-close.png`
- `../../tmp/image-previews/new-content-stylized-2026-09-17/Noryangjin_MapTool_Mode_SR18-game.png`
- `../../tmp/image-previews/new-content-stylized-2026-09-17/HighWay-game.png`
- `../../tmp/image-previews/new-content-stylized-2026-09-17/RestStop-game.png`

## Reproduce and recover

```powershell
unity command --project-path . run_script --file tools/apply-new-stylized-materials.cs --entry ApplyNewStylizedMaterials.Main --timeout_ms 120000 --timeout 120 --format json
unity command --project-path . run_script --file tools/apply-new-stylized-materials.cs --entry ApplyNewStylizedMaterials.Verify --format json
unity command --project-path . run_tests --mode editor --filter GeneratedStylizedSurfaceTests --async_tests true --format json
dotnet build Assembly-CSharp-Editor.csproj -nologo
powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1
```

The capture script is `tools/capture-new-stylized-surfaces.cs`; use a new output folder before rerunning, because it preserves existing evidence. It restores the initially open scene setup. The Editor returned to the clean SR18 scene, outside Play Mode. No PlayerPrefs or gameplay state was changed.

Original material/renderer bytes: `tmp/backups/new-content-stylized-20260917-200034/`. Subsequent timestamped backups contain intermediate state and must not replace this original baseline. Live Unity asset APIs performed all conversions. Initial apply revealed shader assignment resetting the glass queue; a regression also exposed reset tags and an unsupported URP-only transparency keyword. The final helper and tests handle these cases; the FlatKit workflow learning was updated through ce-compound headless.
