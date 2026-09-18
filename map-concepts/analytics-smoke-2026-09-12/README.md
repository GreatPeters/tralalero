# Actual Android analytics smoke test

The latest source was built as a development x86_64 Android APK and installed only in the task-owned read-only Google Play API36 emulator. APK: `Builds/Android/AnalyticsSmoke-20260912.apk` (518,974,669 bytes). The build succeeded with0 errors in849.09 seconds; original ARM64/custom-keystore settings were restored by the build script's finally block.

One actual game round was started with a normal touch swipe. No synthetic event was inserted into Firebase, BigQuery or Tra. The app produced one `game_round_start` and one `game_round_end` with the same round ID. End values:38,133ms, death, chapter1, stage3 of16,0 coins,13.3333% chapter progress and all three upgrade snapshots.

The SDK logged successful HTTP204 uploads after both events. Firebase Console DebugView then displayed the two events; the expanded end-event fields showed38133ms and the matching round ID. `result.json` records the correlation identifier for checking the subsequent daily export. No new BigQuery/Tra row is claimed yet: export is daily, and the sheet intentionally reads through UTC yesterday.

Firebase debug mode was enabled for this test and disabled at cleanup. Current [Firebase guidance](https://firebase.google.com/docs/analytics/debugview), updated2026-09-10, says debug events are included in daily exports by default; a configured developer-traffic filter can exclude them. Do not carry forward the older blanket assumption that all debug events are automatically excluded.

## Limits and recovery

The emulator had MSAA/FlatKit capability errors and black3D world rendering. Its UI and gameplay/physics ran and generated the observed death event; this validates event instrumentation and receipt, not Android visual/performance quality. The initial SwiftShader configuration was slow, and host/Vulkan did not stay available. ANGLE with Vulkan disabled permitted the controlled logging test. No production graphics settings were changed to hide these failures.

All test emulators used read-only AVD mode. The task-owned emulator and logcat readers were stopped after upload/DebugView verification; the user's original AVD data and Unity gameplay preferences were not overwritten. Raw SDK logs, screenshots and build/settings records are in `tmp/analytics-flow-20260912`. Use the per-run correlation fields when checking the daily export rather than treating the older July31 row in Tra as this new test.
