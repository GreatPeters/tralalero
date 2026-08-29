# Protected Game-Data Workbook

## What It Owns

`Assets/ShooterSurvival/GameData/Editor/Data.xlsx` is the editable source for:

- enemy stats and chapter growth;
- upgrades;
- skins and bonuses;
- stage patterns;
- environment variables and player character defaults.

Every gameplay table loader opens the workbook through `GameDataWorkbook`.
There is no runtime loader that reads a raw file from `StreamingAssets`.

## Editing Workflow

1. Open `Data.xlsx` from the Noryangjin map tool or the `Tools/Data` menu.
2. Edit and save the workbook, then wait for Excel to finish writing it.
3. Regenerate the protected runtime data from `Tools/Data`.
4. Run the protected-data validation command from the same menu.

When Unity imports a saved `Data.xlsx`, `GameDataWorkbookAssetPostprocessor`
first validates a complete workbook snapshot, then reloads environment,
monster-growth, upgrade, bonus, skin, and pattern caches together. During Play
Mode, loaded `PlayerScript` and `GameManager` instances immediately re-apply
the changed values. The same reload also runs on `EnteredPlayMode` because this
project disables domain and scene reload on Play Mode entry.

## Monster Growth

The authoring sheet is named `몬스터 성장`. Use these headers exactly:

| 챕터 | 티어 | 초기 공격력 | 최종 공격력 | 초기 체력 | 최종 체력 | 계수 |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Normal | manual | manual | manual | manual | manual |
| 1 | Elite | manual | manual | manual | manual | manual |
| 1 | Boss | manual | manual | manual | manual | manual |

Add the same three rows for every chapter. Chapter numbers must be contiguous
from 1, and every chapter must contain exactly one `Normal`, `Elite`, and
`Boss` row. Damage must be finite and non-negative; health and coefficient must
be finite and positive. The coefficient is an authoring input used by workbook
formulas; runtime combat uses the resolved initial/final values in the row.

There is no stage value in monster growth. `ChapterEnemyProgression` determines
progression from the enemies actually placed in the current chapter scene.
Legacy scenes invoke it through `GameManager`; Noryangjin scenes invoke it
through `ChapterEnemyStatController` even though they do not contain a
`GameManager`:

- encounter enemies are ordered along the player route, including configured
  `NoryangjinTurnSpot` corners;
- pooled inventory is excluded from the placed-enemy count;
- with `N` placed enemies, enemy index `i` uses `i / (N - 1)`;
- therefore the first enemy receives progress 0 and the last receives progress
  1, while every enemy between them increases linearly;
- if only one enemy is placed, it receives the initial value;
- all tiers share the same route progress, but each enemy uses its own tier's
  chapter row for damage and health.

For example, 20 enemies create 19 intervals and 30 enemies create 29 intervals.
Changing the number of placed enemies automatically redistributes the values
without editing the workbook. Starting a new chapter selects that chapter's
three rows, so chapter-start values can intentionally drop after wall bonuses
reset.

Forward prefab identity is authoritative. `Enemy_YllowMan`, `Enemy_Guard`, and
`Enemy_OldMan` are Normal; `Enemy_FatMan` is Elite; `Enemy_Woman` is Boss.
Scene-instance tier overrides do not change this mapping.

If `몬스터 성장` is absent, the legacy `몬스터` table remains a compatibility
fallback. If the growth sheet exists but contains a missing chapter/tier,
duplicate row, or malformed value, validation fails instead of silently using
legacy data.

The automatic editor reload does not replace the protected runtime archive
step. Regenerate `Data.bytes` before testing a built application; a player build
also regenerates it through the build preprocessor when needed.

## Bonus Altars

The `보너스` sheet is also the source of truth for the Noryangjin map-tool altar.
Each row supplies its rarity in `식별 Enum`, stat key in `항목`, value semantics in
`수치 타입`, and the random range in `최소`/`최대`. `Normal` and `Unique` map
directly to the same altar grades; the Inspector's `Elite` label maps to the
workbook's existing `Rare` rows.

`이름` becomes the visible compact badge label beneath the large formatted
value. The UI has no separate `별칭` title; `별칭` remains workbook flavor copy
and is still validated with the row. The label and value use independent
auto-sizing so localized strings through `ATK SPEED` and values such as `+999`
or `+11%` remain in their own lane. Adding an unsupported stat does not silently
turn it into a different bonus: the altar excludes that row and reports an
error if a grade has no supported rows.

Ratio values multiply the workbook range by the player's original matching stat;
percent values remain percentage points internally. The bonus-altar UI appends
`%` only when `수치 타입` is `Percent`; `Ratio` and `Value` render plain numbers.
Percent-specific attack/health icons are not used, and `attPercent`/`hpPercent`
reuse the corresponding `att`/`hp` display names; helper and projectile-count values are
rounded to whole numbers. `BonusAltarRulesTests` verifies the current workbook
candidate counts, aliases, names, ranges, and nearby duplicate exclusion.

## Build and Runtime Flow

- The raw workbook stays inside an `Editor` folder. The RSA private key stays
  outside the project and version control.
- On Windows the default local key path is
  `%LOCALAPPDATA%\MZKoreaGames\TralaleroShooter\Secrets\GameDataSigningKey.json`.
  CI or another machine can set `TRALALERO_GAME_DATA_SIGNING_KEY_PATH` to an
  external absolute path.
- `GameDataWorkbookEditor` wraps the workbook with AES-256-CBC and signs it
  with an external RSA-2048/SHA-256 private key.
- The generated player asset is
  `Assets/ShooterSurvival/Resources/GameData/Data.bytes`.
- Before a player build, `GameDataBuildPreprocessor` regenerates the asset when
  needed, validates every required gameplay sheet with its production parser,
  and confirms that the source did not change during generation.
- The previous runtime archive is replaced atomically only after schema,
  signature, decryption, and source-equality checks pass.
- At runtime, the player verifies the RSA signature before decrypting. A
  missing, changed, or forged archive raises `GameDataIntegrityException`.
- The verified workbook bytes are cached once so all gameplay loaders share
  the one-time verification cost.

Do not manually edit `Data.bytes`; regenerate it from the workbook.

## Security Boundary

This design removes the directly editable XLSX from the player and prevents a
normal user from replacing the protected archive without the external signing
key. It also detects corruption. A patched client can still bypass local
checks; server-authoritative stats are required for a strong anti-cheat
boundary.

## Verification

- `GameDataWorkbookTests` covers source placement, archive round-trip,
  modification rejection, malformed-workbook rejection, archive freshness,
  and all gameplay table reloads.
- `PlayerCharacterDefaultsTests` verifies that an existing player can refresh
  workbook defaults without recreating the scene object.
- `MonsterGrowthAndMapToolEnemyTests` verifies chapter/tier validation,
  endpoint interpolation, turn-aware route ordering, actual placed-enemy count,
  fixed prefab tiers, pool exclusion behavior, and enemy placement occupancy.
- `dotnet build Assembly-CSharp.csproj -nologo`
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`
- `powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1`
