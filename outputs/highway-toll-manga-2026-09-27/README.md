# Korean toll plaza, manga rush frame and ten bonus concepts

Actual changes: three curved blue canopy bays, aluminium fascia/gantry braces, three green lane arrows, Korean LED labels, scanners/readers/cameras, roadside islands and flexible delineators. Short blue Hi-pass paint accompanies the existing green/red route guides. Supports stay outside the playable lanes and in the median, not on the adjacent carriageway. The two existing physical pickup choices remain.

Rush: 96 black tapered perimeter rays with thin light companions, animated pulse/length, clear aiming centre and a reserved upper HUD strip. The upper edge fades instead of ending as a hard row of line caps. Existing open-route activation and reset logic is unchanged. The value is stored in Data.xlsx and the runtime archive is current.

Police provenance: existing Meshy V02 white sedan, with Unity-authored Korean blue/yellow livery, Korean lettering and red/blue lightbar from the previous revision. No new police model was generated here. Meshy spending in this task: 0.

Bonus concepts: ten separate built-in image_gen calls, then one repair of damaged sky pixels in concept05. Selected05 is bonus-05-v2.png. All are visual proposals, not imported into Unity. Prompts and source paths are in concepts/prompts.json and concepts/manifest.json. No numbered concept has been selected or implemented.

## Verification

- Both C# project builds pass; the existing SplineSpeed CS0649 warning remains in the runtime build.
- 25 existing NUnit cases pass by direct invocation: HighwayCombatRevisionTests10 and GameDataWorkbookTests15. The asynchronous runner stalled and was cancelled; this is not an asynchronous runner success. See existing-tests-direct.json/txt.
- 14 directed runtime assertions pass after the final toll rebuild: both physical choices, only one grant, no popup, rush1.5x inside segment, reset at1170, input pass-through, HUD/centre exclusion, finite complete mesh and disable cleanup. See contracts-final.json and contracts.txt.
- Native screenshots toll-game/toll-close and rush-game/rush-off use deliberately positioned/frozen visual fixtures. They are actual Unity renders, not concept composites and not evidence of a fresh complete playthrough. Prior combat/balance validation remains in the previous revision.
- Workbook preservation verifier passes18unrelated parts and296non-owned rows. Final existing workbook assertions verify runtime archive consistency.
- Compared with this task's own before workbook, only2cells changed: speedLineCount22→96 and its environment-sheet formula cache. No combat/balance setting changed.
- validate-agent-harness.ps1 passes. Six protected RestStop/Noryangjin scene hashes are unchanged.
- PlayerPrefs restored byte-identically against this task's fresh snapshot; coin37113/jewel0, original start scene restored, time scale1, clean HighWay Edit Mode. See restored.json.
- Gallery: ten numbered items; PNG modal, zoom and next navigation checked in Chrome;520CSSpixel responsive layout has no horizontal overflow; temporary viewport override reset.

## Visual review corrections

The first native pass exposed two issues that structural checks missed. ScreenSpaceOverlay drew ink over a camera-space HUD despite a negative sorting order; the effect now reserves the upper HUD area geometrically. Repeated EditorUtility.CopySerialized calls left existing mesh render buffers visually stale: CPU normals/bounds reflected the new arrow/ribbon data, while the screenshot showed old black arrows and long paint. Explicit mesh channel setters and UploadMeshData(false) fixed the native result. Canopy parts are combined by material into3renderers.

Initial renders are retained in iteration-1. A first procedural arrow used reverse winding; corrected winding alone did not resolve the stale buffers. The capture tool also now stores its selected filename in SessionState because run_script compiles a fresh assembly per invocation. A reflection probe was corrected to select the VertexHelper overload explicitly. Tests dirtied the active scene in memory; the saved owned scene was reloaded before the next Play session, without saving test state.

## Reproduction

- `unity command --project-path . run_script --file tools/refine-highway-combat-presentation.cs --entry RefineHighwayCombatPresentation.Toll`
- `unity command --project-path . run_script --file tools/probe-highway-toll-manga.cs --entry ProbeHighwayTollManga.ExistingTestsDirect` (clean Edit Mode)
- Use a fresh output directory in the preference-preserving playtest tool before another visual cohort; never overwrite its original registry snapshot.
- `tools/build-highway-toll-manga-report.py` publishes the gallery/report. Pillow is available in tmp/highway-report-python; set PYTHONPATH to that absolute path for this local Python installation.

Gallery: http://127.0.0.1:8776/highway/toll-manga/bonus/

Native revision: http://127.0.0.1:8776/highway/toll-manga/

No commit. No other chapter scenes changed. Physical phone frame rate and human comfort with the animated effect remain unmeasured.
