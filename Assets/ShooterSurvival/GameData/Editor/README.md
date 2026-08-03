# Game Data Excel Source

`Data.xlsx` is the editable source for enemy stats, upgrades, skins, bonuses,
stage patterns, and character defaults.

- Open it from the Noryangjin map tool's `편의` tab or `Tools/Data`.
- After editing, run `런타임 보호 데이터 갱신`.
- Player builds do not include this Editor-only workbook.
- The generated runtime archive lives at
  `Assets/ShooterSurvival/Resources/GameData/Data.bytes`.
- Build preprocessing regenerates and validates that archive before a player
  build.

The private signing key is not a project asset and must never be committed.
Its default local path on Windows is
`%LOCALAPPDATA%\MZKoreaGames\TralaleroShooter\Secrets\GameDataSigningKey.json`.
CI or another developer machine can point to an external absolute path with
`TRALALERO_GAME_DATA_SIGNING_KEY_PATH`. A current signed `Data.bytes` can be
validated and built without the private key; the key is required only when the
workbook changes and the archive must be regenerated. Back up that key in the
team's access-controlled secret manager, never in Git.
