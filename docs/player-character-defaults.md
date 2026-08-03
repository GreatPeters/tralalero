# Player Character Defaults

## Authoring

Select the player root that owns `PlayerScript`. All authored base values are grouped under `Character Defaults (Excel / Inspector)`:

- maximum health and attack damage;
- forward movement speed;
- fire rate and projectile count;
- absolute missile speed and missile duration.

`Use Excel Character Defaults` is enabled by default. When enabled, a valid value in `Assets/ShooterSurvival/GameData/Editor/Data.xlsx` overrides the matching Inspector value. A missing or invalid Excel value falls back to the Inspector. Disable the toggle to use the Inspector values for every character default.

Open the workbook from the Noryangjin map tool's `편의` tab. After editing,
click `런타임 데이터 갱신`; player builds use the generated protected archive
instead of shipping the raw workbook.

## Excel Mapping

The `환경 변수` sheet uses the following keys:

| Excel key | Value mapping | Current workbook |
| --- | --- | --- |
| `playerDefaultHp` | 값1 = maximum health | `100` |
| `playerDefaultAtt` | 값1 = attack damage | `50` |
| `playerSpeed` | 값1 = absolute forward movement speed (unit/s) | `8` |
| `playerDefaultFireRate` | 값1 = shots per second | Not present; Inspector fallback |
| `playerDefaultMissileCount` | 값1 = projectiles per shot | Not present; Inspector fallback |
| `missileSpeed` | 값1 = absolute missile speed (unit/s) | Current workbook uses legacy typo `misspleSpeed = 16` |
| `missileDuration` | 값1 = missile lifetime in gameplay seconds | `1` |

The Inspector fallbacks are missile speed `16`, missile duration `1`, fire rate `1`, and projectile count `1`.

Missile speed and player speed use the same absolute unit but are independent inputs. With `playerSpeed = 8` and `missileSpeed = 16`, the missile moves twice as fast only because `16` is twice `8`; changing `playerSpeed` later does not change the missile. The no-collision travel distance is `missileSpeed * missileDuration`, so the current defaults travel `16 * 1 = 16` units.

## Runtime Ownership

- `PlayerScript` resolves the defaults once and supplies player attack, fire rate, and projectile count to child `WeaponScript` components.
- Weapons that do not belong to a `PlayerScript`, including companion/help weapons, keep their own `WeaponSO` damage/fire-rate values and a projectile count of one.
- `BulletScript` receives one resolved absolute missile speed and one duration before movement begins. Every pooled rental resets its elapsed duration in `SetDirection`.
- The legacy persistent upgrade key `PROJECTILE_SPEED` is retained for save compatibility, but its gameplay effect and displayed copy now increase missile duration. Temporary distance wall buffs also add duration instead of keeping a second range system.
- Forward movement stays at the resolved `playerSpeed` 값1 for the entire run.

## Verification

- `PlayerCharacterDefaultsTests` covers Excel precedence, Inspector-only values, independent player/missile speeds, player weapon inheritance, and non-player weapon fallback.
- `MissileDurationTests` covers duration upgrades, run-bonus reset behavior, pooled elapsed-time reset, and upgrade copy.
- `NoryangjinTurnSpotTests.Awake_UsesInspectorForwardMoveSpeedWhenExcelDefaultsAreDisabled` covers the explicit Inspector override path.
