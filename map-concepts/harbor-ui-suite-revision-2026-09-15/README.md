# Harbor UI suite revision

## User request

2026-09-15, after approving the new lobby concept:

> 어 그래 아주 좋아. 이 방향으로 업그레이드, 스킨상점, 튜토리얼 등도 모두 수정해서 시안을 보여줄 수 있어?

This authorizes a coordinated visual proposal. These files are static image concepts, not installed Unity screens, separated production sprites, or new character/movie assets.

## Selected concepts

| Image | Screens and states |
|---|---|
| [01 Upgrades](../../tmp/image-previews/harbor-ui-suite-revision-2026-09-15/01-upgrades-v1.png) | Regular upgrades; chapter upgrades with unlocked/locked states and five ranks |
| [02 Cosmetic shop](../../tmp/image-previews/harbor-ui-suite-revision-2026-09-15/02-cosmetic-shop-v1.png) | Skin purchase; owned shoes to equip; equipped hat |
| [03 Tutorials](../../tmp/image-previews/harbor-ui-suite-revision-2026-09-15/03-tutorials-v1.png) | Movement; enemy alignment; weapon barrel; positive/negative effects |
| [04 Story](../../tmp/image-previews/harbor-ui-suite-revision-2026-09-15/04-story-v1.png) | Portrait story movie with four scene indicators, skip/replay/next |
| [05 Common panels](../../tmp/image-previews/harbor-ui-suite-revision-2026-09-15/05-common-panels-v3.png) | In-run settings; defeat/results; insufficient gems |

Five final images illustrate thirteen screens/states. The approved lobby and combat references remain the style anchors: cream faces, walnut headers, brass trim, turquoise actions. Prices, totals, cosmetic names/art and scenery are illustrative. Actual skin meshes, enemy assets, hat geometry and movie frames must be reused during production; generated previews do not replace them. The insufficient-funds dialog and tutorial close/focus treatments are proposed presentation changes.

## Grounding and verification

- Read the installed `HarborGameUIInstaller.Upgrades.cs`, `CosmeticShopUI.cs`, `FTUE_script.cs` and previous production/video/polish documentation.
- Retained one-column regular upgrades, global current stats, category-specific numeric caps, and chapter unlock-based five-rank progression.
- Retained two-column cosmetic cards and distinct purchase/equip/equipped actions; owned actions have no currency price.
- Tutorial concepts cover the four existing hint topics and do not imply a new sequential Next-button wizard.
- Story keeps the selected cinematic overlay layout and exactly four separate scene indicators. Its film art is a generated placeholder.
- Visually inspected every generated image. Common-panel v1 incorrectly mixed lobby navigation with in-run controls and changed shop tabs/grid. V2 corrected those and preserved blue gem identity. V3 reconciled the background wallet with the popup (80 available, 200 required, 120 missing) and emptied both defeat health bars. Earlier candidates remain for traceability.
- Selected PNGs were copied into the workspace; dimensions and SHA-256 hashes are recorded in `verification.json` in the preview directory. No runtime tests apply to these static proposals.

## Generation provenance

Built-in image generation tool, one call per concept image, followed by two targeted edits to the common-panel board. Exact prompts: [initial five prompts](../../tmp/image-previews/harbor-ui-suite-revision-2026-09-15/prompts.md), [background correction](../../tmp/image-previews/harbor-ui-suite-revision-2026-09-15/05-common-panels-v2-prompt.txt), [state-value correction](../../tmp/image-previews/harbor-ui-suite-revision-2026-09-15/05-common-panels-v3-prompt.txt).

Related learning was consolidated into [preserve play intent and data limits](../../docs/solutions/design-patterns/preserve-play-intent-and-data-limits-in-ui-concepts-2026-09-14.md) using ce-compound headless, executed sequentially per repository tool mapping. Existing discoverability guidance is sufficient; no instruction-file edit or broader refresh was needed.
