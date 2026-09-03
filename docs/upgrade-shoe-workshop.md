# Upgrade Shoe Workshop

The Noryangjin permanent-upgrade screen keeps the existing `UpgradeUI`,
`UpgradeStatManager`, `MoneyScript`, PlayerPrefs levels, and back-button
UnityEvents. `UpgradeShopReferenceSetup` changes only presentation and serialized
UI bindings.

## Visual contract

- `Assets/JH/UI/Upgrade/Workshop_Background.png` is the clean, text-free
  shoe-workshop background generated from the approved reference direction.
- The header sign reads `그지 신발 개조소` and retains a short workshop subtitle.
- Nine upgrades remain ordered by sibling index in a fixed 3×3 grid.
- Every parchment card shows its existing icon, current value, `>` separator,
  next value, effect description, correct coin/diamond icon, and price.
- All nine card `Button` components are reused. The main menu's existing global
  back flow remains authoritative.
- The builder resolves the exact panel activated by the main `Upgrade_Button`
  UnityEvent instead of selecting a duplicate hierarchy by name.

## Rebuild

Run `Tools/Shooter Survival/UI/Rebuild Upgrade Shoe Workshop` while the target
Noryangjin scene is open. The operation is idempotent and intentionally does
not replace purchase logic or PlayerPrefs keys.

Agents invoke the same operation with
`unity command eval "UpgradeShopReferenceSetup.ConfigureOpenScene();" --project-path .`.
Run it outside Play Mode with the intended scene active. Resolve unrelated
dirty-scene work first, save after verification, and run
`UpgradeShopReferenceSetupTests` plus a second-run scene-hash comparison.

The current authored target is
`Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity`.
