# Firebase Analytics and BigQuery Round Logs

## Goal

Add repository-side Firebase Analytics instrumentation for retention, app
engagement, and one-row-per-round gameplay analysis while keeping BigQuery
credentials out of the Unity player.

## Completed

- Pinned Firebase Unity App and Analytics `13.14.0` and External Dependency
  Manager `1.2.186` as local UPM archives configured for Git LFS.
- Resolved the Android dependencies into
  `Assets/GeneratedLocalRepo/Firebase`, patched the Unity 6 main/settings
  Gradle templates, enabled AndroidX/Jetifier, and added build validation for
  every required template and hashed Firebase Unity AAR/POM resolver output.
- Added main-thread Firebase dependency initialization and an Android
  configuration/build guard for `com.mzkoreagames.tralaleroshooter`.
- Added a pure round tracker and `game_round_start` / `game_round_end` contract.
- Connected Tap to Play, death, win, earned coins, active play time, chapter,
  stage-index progress, end position, and all nine upgrade values.
- Noryangjin scenes now derive live route progress from consumed turn
  checkpoints; explicit scene metadata opts into that behavior, while authored
  inactive and map-tool preview spots are excluded.
- Added a persistent 128-event pre-initialization queue, 15-second active-round
  checkpoints, and next-launch `abandoned` recovery.
- Added a persistent runtime collection preference and Android defaults that
  disable pre-initialization collection, Advertising ID collection, and ad
  personalization.
- Added BigQuery Standard SQL for joined round logs, D1/D7/D30 retention,
  automatic app engagement, and custom active round playtime.
- Documented client telemetry trust limits, privacy constraints, operational
  failure modes, and iOS follow-up requirements.
- Moved the game-data RSA private key outside the repository, added project
  guards against reintroducing it under `Assets`, and clarified that the
  player-side AES layer is obfuscation while RSA provides tamper detection.
- Registered Android app `com.mzkoreagames.tralaleroshooter` in Firebase
  project `tralaleroshooter`, installed the matching `google-services.json`,
  and pinned the reviewed Firebase destination.
- Linked Google Analytics daily export to BigQuery in `asia-northeast3` with
  streaming and Advertising ID export disabled.
- Built and installed a development APK on an `SM-S901N`, enabled Firebase
  debug mode, and verified real-device events in Firebase Console DebugView.

## External Follow-up Still Required

Wait for the first finalized daily export, then verify the created
`analytics_<property_id>.events_YYYYMMDD` table and run the repository SQL
against that actual dataset. The first daily export is asynchronous and is not
proven by a successful DebugView upload.

Do not add a service-account key to the Unity project to automate these steps.

## Repository Verification

- `GameplayAnalyticsTests`: focused EditMode coverage for event contracts,
  deterministic occurrence time, idempotency, active time, coin accumulation,
  snapshots, Firebase limits, collection withdrawal/re-enable, queue fidelity,
  and abandoned-run recovery.
- `FirebaseAnalyticsSetupValidatorTests`: focused EditMode coverage for package
  pins and hashes, structured `google-services.json` parsing, exact destination
  pinning, Android privacy configuration, and documentation paths.
- `BigQueryAnalyticsSqlTests`: static contract checks for strict parameter
  extraction, occurrence-time windows, pairing statuses, quarantine output,
  and overflow-safe aggregation.
- Focused Unity EditMode results on 2026-07-30:
  `FirebaseAnalyticsSetupValidatorTests` 41/41,
  `GameDataWorkbookTests` 10/10,
  `GameplayAnalyticsTests` 24/24,
  `NoryangjinTurnSpotTests` 35/35, and
  `BigQueryAnalyticsSqlTests` 2/2.
- Full Unity EditMode result: 382/390 passed, 8 failed. This is the same
  eight-failure map-tool/scene baseline recorded before this work
  (312/320 passed): the suite gained 70 tests and all 70 passed. The existing
  failures cover one authored-scene gameplay fixture, five map-tool visual or
  palette expectations, and two stale protected-scene hash expectations.
- `dotnet build Assembly-CSharp.csproj -nologo`: passed on 2026-07-30 with
  zero errors and five existing deprecation or unused-field warnings in
  unrelated project code.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo`: passed on 2026-07-30
  with zero errors and zero warnings.
- `powershell -ExecutionPolicy Bypass -File tools/validate-agent-harness.ps1`:
  passed on 2026-07-30.
- Both SQL files parsed successfully as BigQuery dialect with `sqlglot`
  (`round_logs.sql`: 18 statements,
  `retention_and_playtime.sql`: 22 statements); a Google Cloud dry run still
  requires the owner's actual exported dataset.
- Authored scene hashes were rechecked on 2026-07-30 and remain unchanged:
  `Noryangjin_MapTool_Mode.unity`
  `4AF773C43D293F62EBBF1A2EDC9D35ED78F0DDCA0BEA7EB6BBD5D3A1AB9E3C6F`;
  `Noryangjin_MapTool_Mode_2.unity`
  `08B18CFC4B941A58D1ABEAC499730637DDFC58D1619DC9952F9085D6C3B22139`.
- External Dependency Manager ultimately completed with the exact five pinned
  Firebase dependencies after the user-authorized editor restart and
  callback-based asynchronous `Resolve(...)`. A prior main-thread `ResolveSync`
  attempt froze the editor; the recovered run left the active scene clean, and
  the post-test hashes matched the pre-test hashes.
- The Android setup validator passed with the registered package,
  `Assets/google-services.json`, and the exact
  `ProjectSettings/FirebaseAnalyticsDestination.json` pin in place.
- Real-device verification on 2026-07-31 used Android package
  `com.mzkoreagames.tralaleroshooter` on an `SM-S901N`. Firebase initialized
  against app ID `1:75162182731:android:d05cb9c9a51716bbed59ce`, uploaded a
  `game_round_end` event carrying `_dbg=1`, received HTTP `204`, and Firebase
  Console DebugView displayed five recent events.
- Post-build cleanup verification on 2026-07-31 passed Unity recompilation,
  `FirebaseAnalyticsSetupValidatorTests` 40/40, the editor C# build with zero
  errors, and `tools/validate-agent-harness.ps1`.

Keep this plan active only until the first exported dataset table is verified.
Repository-side implementation, Firebase destination pinning, BigQuery linkage,
and real-device DebugView verification are complete.
