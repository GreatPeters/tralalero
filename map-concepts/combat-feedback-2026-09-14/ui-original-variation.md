# C — Harbor Workshop variation

## Current revision after user review

- Latest proposal: `tmp/image-previews/ui-original-variation-2026-09-14/ui-concept-c-v3-gameplay-start-upgrades.png`.
- User corrections: U75/U76 in `docs/design/USER_STATED_REQUIREMENTS.md`. This remains a generated visual proposal; no runtime scene, UI code, or workbook was changed.
- Start reference located at `Assets/JH/Image/Lobby/Main/img_main_all.jpg`; the user-supplied nested `img/_main/_all.jpg` path does not exist. Inspected the actual reference before generation.
- Start now looks up the playable dock from behind the shark, with the original “좌우로 움직여 / 게임시작” instruction and a unified horizontal finger gesture.
- Workshop current attack and health share one full-width summary and are vertically stacked. Upgrade parts occupy separate full-width rows; scrolling exposes additional upgrades.
- Read-only XLSX XML inspection confirmed real maxima: ATT/HP 60; ATT_SPEED/PROJECTILE_SPEED/BOSS_DAMAGE/COIN_BONUS 30; HP_REGEN/TUNGTUNGTUNG/BOOMBAR 20; LATERAL_SPEED 10. `UpgradeTables.MaxLevel(id)` already derives its maximum from workbook rows, and `UpgradeUI` already has numeric current/max labels.
- Visible generated denominators match the corresponding table categories. Selected/current levels, currency balances, prices and example stat totals are illustrative; this board is not an exact saved-player snapshot or a balance specification.
- The first revision, `ui-concept-c-v2-gameplay-start-numeric-levels.png`, still placed summary stats horizontally and showed the partially visible coin upgrade with a wrong 20 cap. A targeted built-in image edit corrected both; visual review of v3 confirms stacked summary, numeric 60/60/30/30/10/30 caps and no upgrade level dots.
- Both exact built-in generation prompts: [ui-gameplay-start-revision-prompts.md](ui-gameplay-start-revision-prompts.md). Source images and earlier proposals remain preserved.

## Earlier proposal, superseded for start and upgrade layout

User follow-up on2026-09-14 asks for one variation of the UI from before the redesign. This proposal uses the pre-Ocean-Pop brown workshop, confirmed from the original workshop art and saved equipment screenshot. It is a visual proposal; no game scene or UI source was edited in this follow-up.

- Reference art: `tmp/image-previews/noryangjin-release-2026-09-10/workshop-art.png`.
- Original equipment screen: `map-concepts/sr18-presentation-progression-2026-09-10/equipment-shoes-final.png`.
- Final board: `tmp/image-previews/ui-original-variation-2026-09-14/ui-concept-c-harbor-workshop.png`.
- Three screens: harbor lobby, shoe workshop upgrades, equipment shop.
- Preserved: dark timber, brass borders, parchment cards, amber lamps, goggle-wearing cobbler and shoe-part illustrations.
- Variation: larger pictorial navigation, clear stat chips, two-column upgrade/equipment cards, whole-footwear thumbnails, separated prices and purchase action.
- Generated with the built-in image tool, using both local reference PNGs. Full first prompt is retained in `tmp/ui-original-variation-2026-09-14/prompt.txt`.
- Final edit prompt: preserve the board; match the selected red-shoe price with the bottom action (200 gems), correct the action label, and add a matching sneaker on the tail tip without creating a third leg.
- Prices and stats are mockup examples. The selection/action price now agrees. Source generation PNGs and the first candidate remain preserved.

One relevant prior UI session was extracted with the ce-sessions scripts, excluding the current session and auto-review sessions from synthesis. Its palette decision agrees with the versioned transition record. Existing source screenshots, rather than session prose, supplied the visual reference.
