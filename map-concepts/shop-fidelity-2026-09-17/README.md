# Shop reference fidelity — 2026-09-17

## Result

Cosmetic, permanent-upgrade and chapter-upgrade shops are authored in SR18, HighWay and RestStop with a final shop-specific pass. The supplied reference images are `tmp/image-previews/harbor-ui-all-2026-09-15/1/04-skin-shop.png` and `1/03-upgrades.png`.

The discrepancy was broader than missing TMP outline: shops were omitted from the newer Faithful pass; old small font/rectangles remained; generic 9-slice multipliers reduced borders; runtime theme tint recolored the complete card; price bands/equipped markers/rank states were incomplete.

## Applied

- Jua SDF shop type, geometric centering and dedicated white outline material. Existing OFL font source/license retained.
- Reference-guided transparent card, header and gold-button artwork with native Unity 9-slice/sprite assets. Ten consistent cartoon upgrade icons, preserving all ten upgrade IDs and gameplay data.
- Large cosmetic model region, compact wearing/preview plaque, two columns/four visible cards, larger 2D icons, effect and price bands, independent selected and equipped markers, subdued equipped footer.
- Native sub-sprites normalize skin-icon alpha bounds. The armor source was 385x436 with artwork only through row270; removing that bottom margin fixes its visibly smaller card illustration without repainting it or changing the gameplay catalog.
- Actual model and equipment still render in the preview. Camera orbit/reset, disposal/reopen and two-frame demand rendering remain. A thicker lit navy platform with metal rivets replaces the nearly flat disc.
- Regular rows use name, level and one inline effect progression, retaining the serialized current/next text bindings. Five rows are visible with all remaining upgrades reachable by scrolling.
- Chapter rows use five visible hollow/filled rank circles, legible incremental stats, catalog prices and an explicit gray locked state. Existing five-rank +5% attack/+5% health balance is unchanged.
- Original buying, locking, currency, close and tab callbacks remain connected. `ApplyFaithfulShopsAll` is repeatable and removes duplicate new decorations left by early iteration.

## Assets and generation record

Built-in imagegen, reference-guided generation; no external API fallback. Assets are project-local under `Assets/ShooterSurvival/UI/CoastalFaithful/`.

| Production PNG | Prompt |
| --- | --- |
| `UpgradeIcons.png` | [Ten-icon atlas](sources/icon-prompt.md) |
| `ShopCard.png` | [Blank item frame](sources/card-prompt.md) |
| `ShopHeader.png` | [Decorated blank header](sources/header-prompt.md) |
| `ShopGoldButton.png` | [Blank gold button](sources/gold-prompt.md) |

The header/button sprites trim transparent margins through native Sprite.Create; the PNGs retain their original alpha. UI rings are deterministic native textures, not generated images.

## Verification

- Native Editor tests: **38 passed, 0 failed**. `ShopFidelityTests` 3; `CosmeticShopIntegrationTests` 4; `ChapterWorkshopTests` 10; `CoastalUIIntegrationTests` 9; `HarborFaithfulArtworkTests` 5; `UITextAlignmentTests` 7. Individual completed nonzero results are under `verification/`.
- **15 live transaction/state checks passed**: insufficient gems, one exact cosmetic charge, actual equip, equipped/focused state, preserved tint, no double charge, preview reopen, table-price regular purchase, catalog-price chapter purchase, rank fill, locked rejection and maximum-rank rejection. `verification/interactions.txt`.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`: runtime/editor projects compile, zero errors; one unrelated existing SplineSpeed warning. Harness validation passes.
- **36,362 non-Canvas scene YAML documents remain byte-identical** to the task baseline across the three scenes. `verification/world-preservation.json`.
- Fresh snapshot: 66 inventory/wallet/progression records + 3 setting/tutorial records + 6 chapter-rank/legacy-owned records. Restore verifies values and original absence for all 75 keys; 33 originally present keys are listed in `verification/restored-prefs.txt`. Coin191/jewel0 restored.
- Final native 1080x2340 captures are in `tmp/image-previews/harbor-ui-all-2026-09-15/9-shop-fidelity-2026-09-17/final-2/`, separated by scene. Each set includes skin, alternate skin preview, shoes, hats, regular top/bottom and chapter rows. Earlier candidate/final folders preserve iteration evidence. Extra tall-hat and skin-list-bottom captures verify framing and icon size at the end of the catalog.

## Reproduce

1. In clean Edit Mode: `unity command --project-path . run_script --file tools/apply-faithful-shops.cs --entry ApplyFaithfulShops.Main --timeout_ms 120000`.
2. Run each test suite separately using `run_tests --mode editor --filter <suite> --async_tests true`; poll `test_status`. Pipe-combined filters returned zero tests and are not valid verification.
3. Start Play Mode and wait for scene initialization, then run `eval_file --file tools/capture-faithful-shops.cs`. Require its per-scene COMPLETE marker before leaving Play Mode.
4. Live purchase checks use `tools/verify-faithful-shops.cs` and mutate test preferences. They require a fresh task snapshot including chapter-rank keys. Restore only this task's snapshot in Edit Mode with `tools/restore-shop-fidelity-prefs.cs`.
5. `python tools/verify-shop-scene-preservation.py` verifies the world against this task's baseline.

## Practical limits

The reference character is an illustration, while the center preview uses the existing actual game mesh/materials; their mesh silhouette and surface detail are not identical. Reference prices/levels are examples and were not substituted for actual data. Verification is in the Unity Editor at the supplied portrait ratio; no new APK/device acceptance is claimed.

## Learning

[Runtime shop fidelity and authoring order](../../docs/solutions/ui-bugs/keep-shop-reference-styling-through-runtime-refresh-2026-09-17.md), written with ce-compound headless and repo-mandated sequential execution. Existing docs/solutions discovery in AGENTS.md already covers the new lesson.
