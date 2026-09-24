# Bonus display and applied amount agreement

Status: complete in source and protected runtime data. No APK rebuild/install in this follow-up.

The review observed ATT+12/+16/+17 applying+1 with `originalDamage=8`, and HP+14 applying+8 with `originalHealth=60`. Ratio resolution multiplied by the base stat while the separate display resolver multiplied by100. A former test even asserted different applied/display values; a base100test accidentally hid the mismatch.

Changes:
- Convert four production flat ATT/HP rows in `Data.xlsx` / `보너스` from Ratio to Value. Normal ATT12–18, normal HP12–20, unique ATT36–54, unique HP36–54. Percentage rows retain their original ranges and behavior.
- `WallScript.SetStats` assigns `displayBonusValue` from the already-resolved `bonusValue`. Delete the separate display resolver. Future/legacy Ratio rows therefore also show the actual flat increment they will apply.
- Six new regression cases failed before the fix and pass afterward. Correct the old test that accepted the mismatch.

Verification:
-45focused tests pass across bonus amount, altar rules, talisman lifecycle, run health balance and combat feedback.
- Actual physics contact with a moved native wall prefab and seeded values within the real workbook ranges: ATT68→80(+12), HP500→514(+14), ATT80→116(+36), HP514→568(+54). MaxHPincreased by the same amount for both health pickups. The probe does not invoke the trigger or reward method directly.
- Native images/report: `tmp/image-previews/bonus-amount-fix-2026-09-22/143326/`.
- Both C# project builds and harness validation pass. Runtime archive validation passes.66preference records restored;731coins/0jewels restored; SR18 clean in Edit Mode.

Workbook preservation:
- Artifact Tool authors only requested values. A selective ZIP graft installs twelve cells: I/M/N at rows3,7,12,16. Every unrelated worksheet cell and package part, including formula caches, is preserved against the task snapshot.
- Candidate export can use `t="str"` for string values; the installer converts both shared and direct strings to inline strings while preserving original cell styles. Initial conversion rejected direct string values before writing; the corrected installer completed safely.
- Backup, before/after renders and preservation report: `tmp/bonus-amount-fix-2026-09-22/`. Final exported workbook: `outputs/bonus-amount-fix-2026-09-22/Data.xlsx` (identical to installed source).
