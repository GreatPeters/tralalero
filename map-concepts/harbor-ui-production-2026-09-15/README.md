# Harbor Workshop UI production

The later explicit integration request is complete: [live Harbor UI](../harbor-ui-live-2026-09-15/README.md). This document preserves the earlier asset-production record.

User-selected source: `tmp/image-previews/ui-original-variation-2026-09-14/ui-concept-c-v2-gameplay-start-numeric-levels.png` (`exec-abd1090d-a9f9-4b10-bd1d-837511a2045d.png`). Work began September 14 and continued September 15 KST. Direct requirements are U77–U81.

## Deliverables

- `harbor-ui-asset-kit.zip`: verified 84-file PNG/meta, PSD, preview and script bundle (29,684,602 bytes).
- `HarborWorkshopUI.unitypackage`: native Unity export of the 35 sprite assets and import settings (7,912,899 bytes).
- `tmp/image-previews/harbor-ui-production-2026-09-15/ui-v4-main.png`: revised start, permanent-upgrade tab and owned-equipment equip state. Generated concept; sample balances/effects/prices.
- `tmp/image-previews/harbor-ui-production-2026-09-15/ui-v4-chapter-and-purchase.png`: chapter unlock/lock states, large proposed bonuses and unowned purchase action. Chapter values/prices/one-time rule are illustrative, not installed balance.
- `tmp/image-previews/harbor-ui-production-2026-09-15/start-actual-scene-composite.png`: unchanged actual camera pixels resized 2x, with reconstructed art/text layered above. This is an offline composite, not a capture of a fully replaced runtime UI.
- `tmp/image-previews/harbor-ui-production-2026-09-15/start-ui-overlay.png`: real RGBA overlay without the scene pixels.
- `Assets/ShooterSurvival/UI/HarborWorkshop`: 35 PNG assets, including generated/keyed UI surfaces and illustrations, reused legacy icons, and intentionally opaque shop/merchant art. All imported as Unity sprites; 8 surfaces have 9-slice borders.
- `sprite-manifest.json`: names, reviewed crop rectangles, dimensions, borders and reuse paths. PNGs are native-resolution sources. Some fallback legacy icons differ from the approved illustration style; they are identified in the manifest.
- `layered/harbor-ui-sprites.psd`: 35 named, independently movable sprite review layers fitted to a sheet; use native PNGs for full-resolution production.
- `layered/start-actual-scene.psd`: 27 layers, including the actual scene, independent art, and separate rasterized text. The text is not a Photoshop TypeLayer; strings and the KERISKEDU_B font path remain editable in the packaging script.
- `prompts.md`: exact built-in image-tool prompts. `sources/` preserves generated chroma sources and rejected opaque checkerboards.

## Runtime changes and verified limits

- `PlayerWorldHealthBar` plus `tools/install-player-world-health-bar.cs` installs one green screen-space bar following the player in SR18, HighWay and RestStop. The existing upper status HUD remains intact. It does not re-enable the legacy player child Canvas.
- SR18 actual Play Mode: health 60 → 39, fill 1.0 → 0.65, retry returns 60 / 1.0 and one bar. See `healthbar-verification.json` and `healthbar-lobby-full.png` / `healthbar-damaged.png` in the preview directory. Other two scenes have verified authored bindings; their runtime movement was not separately played in this pass.
- CosmeticShopUI action labels are `구매 <price>`, `장착`, `장착 중` according to ownership/equipped state. Owned/equipped states hide the currency icon. Native checks verified 200-gem unowned, owned/not-equipped and equipped cases; existing effect text reads `공격력 +16%`. The existing purchase-and-auto-equip transaction remains unchanged.
- Runtime compile: `dotnet build Assembly-CSharp.csproj -nologo -v:quiet`, zero errors and one existing SplineSpeed warning. The scene was returned to clean SR18 Edit Mode, and both test sessions restored 66 preference keys (coin 3047, jewel 0).
- At this asset-pass checkpoint, full visual replacement and chapter purchasing were not installed. They were subsequently connected in the live-UI pass linked above. No workbook values were changed.
- Backups: `tmp/backups/harbor-ui-production-2026-09-14/` for scenes and `tmp/backups/harbor-ui-production-2026-09-15/` for runtime source/preferences.

## Reproduce

1. Run `tmp/harbor-ui-tools/Scripts/python.exe -X utf8 tools/extract-harbor-ui-sprites.py` (Pillow and numpy).
2. Run official `unity command eval_file --file tools/import-harbor-ui-art.cs --project-path .` outside Play Mode, or use the connected eval_file tool. Do not hand-edit import metadata.
3. Run `tmp/harbor-ui-tools/Scripts/python.exe -X utf8 tools/package-harbor-ui.py` (psd-tools as well). PSD files are saved and reopened, with layer counts recorded in `layered/verification.json`.

PSD construction follows the [official psd-tools creation API](https://psd-tools.readthedocs.io/en/latest/reference/psd_tools.api.html). The asset pack is a reconstruction plus reusable original sources, not a claim to recover the flat PNG's hidden layers perfectly. Live scene/model areas and dynamic labels belong in Unity, not in the decorative sprite atlas.
