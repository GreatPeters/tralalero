# 보너스 바닥 패드 + 홀로그램 제작 기록 (2026-09-27)

Bonus-wall concept 08, shared by Noryangjin SR18, HighWay and RestStop.

## Assets
- Codex concepts: `concepts/B01_bonus_pad/try1_0.png` (hexagon), `concepts/B02_bonus_pad_cracked/try1_0.png` (negative, cracked). Earlier octagon attempts are in `rejected/`. Codex used 81,076 tokens over 4 runs.
- Meshy image-to-3D: B01 3,287 tris, B02 4,296 tris. 60 credits total (`ledger.json`); the balance was 2,312 before this run.
- Unity import: `unity command run_script --file tools/import-meshy-objects.cs --entry ImportMeshyObjects.BonusPad0927`
- Prefab and materials: `unity command run_script --file tools/build-bonus-pad.cs --entry BuildBonusPad.Main` → `Assets/ShooterSurvival/Resources/BonusPad/`
- HighWay random gates: open HighWay in Edit Mode, then `unity command run_script --file tools/install-highway-chapter2.cs --entry InstallHighwayChapter2.MysteryPads`. Scene backup: `backup/HighWay.unity`.

## Runtime
- `BonusPadVisual`: Meshy base plus procedural edge band, centre ring, light column and contact shadow. Coloured per type: attack red, HP green, attack speed yellow, missile blue, 퉁퉁퉁 orange, 붐바르딜로/help cyan. Negative uses the cracked purple base and is not wired to any scene yet (no NerfWall is placed). Random uses a rainbow cycle.
- `BonusTalismanPresentation.ApplyPad`: hides the paper talisman. The hand-drawn icon floats 1.75 m above the pad and the value sits above it with a dark outline.
- `BonusPadRandomDisplay`: two rainbow pads at ±2.4 m, a "?" and the user's icons cycling like a slot reel.

## Verification
- `dotnet build` of Assembly-CSharp and Assembly-CSharp-Editor passes; `tools/validate-agent-harness.ps1` passes.
- EditMode: BonusPadVisualTests 4/4, BonusTalismanTests 10/10, BonusAltarRules 18/18, BonusDisplayedAmount 6/6, BonusWallChoicePair 6/6, BonusWallCooldown 6/6, HighwayChapter2Rules 16/16, HighwayChapterIntegration 1/1, HighwayEncounterLanes 8/8, HighwayRebuildContract 3/3.
- Play-mode captures: `tmp/image-previews/bonus-pad-2026-09-27/` (`sheet-far.jpg`, `sheet-near.jpg`, `sheet-v2.jpg`). Pads found: HighWay 32 (4 random), SR18 50 (width 2.58 m), RestStop 22. The capture camera keeps the gameplay camera's rotation, so some shots are blocked by market props or traffic; this is not a pad fault.
- PlayerPrefs restored and byte-identical to `outputs/meshy-reststop-2026-09-25/prefs/playerprefs.original.tsv`; playModeStartScene is back to RestStop.
