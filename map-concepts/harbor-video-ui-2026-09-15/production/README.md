# A/B video UI asset kit

The selected A design is now connected in the saved game scenes: [live integration](../../harbor-ui-live-2026-09-15/README.md). The standalone A/B prefabs described here remain presentation references; runtime bindings are authored into the scenes by `HarborGameUIInstaller`.

Both September15 video concepts are separated and reconstructed as reusable artwork. The user explicitly requested cutting and practical alternatives where a flat PNG cannot yield perfect hidden layers. Existing game scenes and movies are unchanged.

## Selected A revision

The user selected A and requested B-style discrete indicators instead of the continuous bar. A now has four independent `StorySegment` images, one per current story scene, with only the current scene gold. The installed opening is one movie containing four timed scenes, not four separate movie files. The current preview highlights scene 1. No other A layout entries changed; B's prefab remains byte-identical.

Updated exports are `HarborVideoUI_AB_v2.unitypackage` and `harbor-video-ui-AB-assets-v2.zip`; the v1 downloads remain preserved. The revised image is `tmp/image-previews/harbor-video-ui-2026-09-15/video-ui-a-segments-v2.png`. Existing templates still require playback hookup.

## Files

- `Assets/ShooterSurvival/UI/HarborVideo/`: 25 UI PNGs plus one actual movie-frame preview. Thirteen surfaces have native nine-slice borders. The full wood backing and movie still are intentionally opaque; frames, controls and ornaments have appropriate alpha.
- `HarborVideo_A.prefab` and `HarborVideo_B.prefab` in that folder: independent Image, Button, RawImage, AspectRatioFitter and TextMeshPro components. A has 6 editable text labels; B has 7. Each has 3 named button/label groups.
- `layered/video-ui-A.psd`: 21 independent pixel layers. `layered/video-ui-B.psd`: 25. Art, video placeholder and rasterized label layers are separate. These are not recovered Photoshop text/type layers; edit live text through the Unity TMP components or layout JSON.
- `layout-A.json`, `layout-B.json`: exact reference positions and editable text strings, sizes, colors, button roles and video-slot bounds. Logical reference size is 887×1774.
- `sprite-manifest.json`: reviewed source crops, dimensions, alpha counts, native borders and method per asset. `sources/` retains text-free plates, generated ornament/backing alternatives and the real preview frame extracted at 2 seconds from the installed movie.
- Workspace previews: `tmp/image-previews/harbor-video-ui-2026-09-15/production/`. It contains alpha review, A/B rebuilt PNGs, transparent overlays, native Unity renders and fixed-layout clean reference shells.

## Intended use

Use either prefab as a **presentation template**. It has no playback controller and does not replace the existing opening screen automatically. Buttons have named hit areas and editable child labels; their onClick events intentionally have no scene references.

For the existing `OpeningStoryUI`, map these nodes when integrating the selected design:

| Existing role | Template node |
|---|---|
| Movie display | `MovieSlot/MovieDisplay` RawImage |
| Story counter | `StoryCounter` TMP |
| Scene title | `SceneTitle` TMP |
| Caption | `Caption` TMP |
| Skip action | `SkipButton` → `OpeningStoryUI.Skip()` |
| Replay action | `ReplayButton` → `OpeningStoryUI.ReplayMovie()` |
| Next action/text | `NextButton` → `OpeningStoryUI.Next()`; child `NextLabel` |
| A/B scene progress | `StorySegment1`–`StorySegment4` images; show only the current scene in gold |

The movie slots preserve 9:16 using FitInParent, including narrow inner margins when the illustrated aperture has a different ratio. Replace the placeholder texture with the actual VideoPlayer RenderTexture during integration; do not stretch or crop the movie to match the generated picture. No whole video is duplicated in the kit.

The prefabs reference this project's existing `Assets/ShooterSurvival/Resources/UI/KERISKEDU_B SDF.asset`. The Unity export is intended for this project or one with that dependency available. Imported PNGs remain independently usable elsewhere. The Python source uses `Assets/JH/Font/KERISKEDU_B.ttf` for raster label reviews.

## Fidelity and verification

- The movie UI's text and video were removed using the built-in image tool, restoring blank surfaces; frames/buttons were then cut from those plates. Hidden wood and complete side ornaments are reconstructed alternatives. The pack does not claim pixel-perfect recovery of the original flat PNG's missing layers.
- B's fixed caption card retains its ornamental divider and uses no nine-slice border. A separate divider is also supplied. B's edge-mounted lantern rope intentionally terminates at its mounting edge.
- Actual alpha centers of both video-frame sprites are zero; bright/dark contact sheets show the edges and openings. Overlay cutouts preserve foreground frame corners and A's subtitle gradient.
- PSDs were saved and reopened with the stated layer counts. Runtime TMP labels remain editable and separate from art. All native buttons contain their labels and have working Graphic raycast targets.
- Native renders use a temporary PreviewScene, a matching camera scene-culling mask and an empty/dark-image rejection check. Both movie RectTransforms measured 0.5625 width/height. Initial blank captures from missing preview-scene culling are retained under `evidence/` and are not accepted previews.
- The old long-progress artwork remains available in the sprite kit, but selected A no longer uses it. Native verification confirmed four indicators matching the four `OpeningStoryUI` boundaries, one active indicator and no continuous track/fill objects.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:quiet`: zero warnings/errors after builder field initialization cleanup.
- Original SR18 scene stays clean and active outside Play Mode. No gameplay preferences or existing UI/movie code are changed by this asset pass.

## Reproduce in this repository

1. Use `tmp/harbor-ui-tools/Scripts/python.exe -X utf8 tools/prepare-harbor-video-assets.py` (Pillow, numpy, psd-tools). It reuses `tools/extract-harbor-ui-sprites.py` for chroma-key extraction. Sources are preserved in this directory.
2. Through official Unity Pipeline, call `HarborVideoAssetBuilder.Build()` in Edit Mode after the new Editor script is compiled.
3. Run Pipeline `eval_file` on `tools/render-harbor-video-prefabs.cs` to validate/render the separate temporary preview scenes.
4. `tools/package-harbor-video-assets.py` creates and verifies the zip. Do not overwrite an earlier package when publishing a new version.

The preview-scene correction follows Unity's [Camera scene-culling mask API](https://docs.unity3d.com/cn/2023.2/ScriptReference/Camera-overrideSceneCullingMask.html) and [preview-scene mask behavior](https://docs.unity3d.com/ja/2022.1/ScriptReference/SceneManagement.SceneCullingMasks.DefaultSceneCullingMask.html). Exact image-generation prompts are in [prompts.md](prompts.md).
