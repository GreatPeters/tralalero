# Install the optimized game on the user's phone

User request: "내 모바일에서 플레이해보게 해줄 수 있어? 지금 연결된 거?"

Completed2026-09-14: verified APK installed in place on the user's Samsung phone and actual gameplay rendering confirmed.

## Current state

- Windows Plug and Play detects a Samsung Android ADB Interface with problem code0and WINUSB driver2.21.4.0.
- Both Unity SDK ADB34.0.5 and installed Android SDK ADB36.0.0 report an empty device list.
- Restarting ADB, testing its supported LIBUSB backend and checking mDNS services did not reveal a device. The server was returned to the native backend. No drivers, phone settings or installed apps were modified.
- After the user physically reconnected the cable, ADB reported an authorized device: Samsung SM-S901N, Android16, ARM64,47GBavailable storage. The earlier connection blocker is resolved.
- USB tracing narrowed this further: native ADB found the Samsung USB endpoint but failed to read its serial number with Win32 error31. This is before ADB authorization and is distinct from the PnP problem code, which was0.
- A device-specific pnputil restart was rejected by Windows with Access is denied; no device was restarted. The user was asked to physically unplug/replug into another port and unlock the phone. This was an OS privilege failure, not an automatic approval-review rejection.
- The previous versionCode1/versionName1.0.0APK was pulled and its signature verified. The new APK has the same Android debug certificate. In-place `adb install -r` succeeded; firstInstallTime remained2026-07-31and lastUpdateTime became2026-09-14 00:13:21. No uninstall or data-clear command was used.
- After the build, ADB briefly reported unauthorized. The phone's approval restored device status and the installation then succeeded.

## Build

- Builder: `tools/build-mobile-playtest.cs`, executed through official Unity Pipeline in the original project.
- Intended output: `Builds/Android/TralaleroShooter-Optimized-20260913.apk`.
- Explicit entry scenes: SR18, HighWay, RestStop.
- ARM64/IL2CPP, non-development APK, existing test-ad configuration, temporary Android debug signing. Original signing credentials were not present, so no production signature is claimed.
- Architecture/signing/APK export/development settings are restored in finally.
- Source hashes and pre-build PlayerSettings backup: `tmp/mobile-device-playtest-2026-09-13/`.
- The first delayCall remained queued while Pipeline was responsive. Its single identified callback was transferred to a one-shot Editor update and started successfully. The maintained builder now uses that update pattern. See `../../solutions/workflow-issues/schedule-unity-builds-through-one-shot-update-2026-09-13.md`.

## Completed checks

- Build succeeded with0errors/58warnings in2615.28seconds. Actual APK size699039439bytes. The cold shader and native compilation dominated elapsed time.
- Package `com.mzkoreagames.tralaleroshooter`, minimumAPI24,targetAPI36,ARM64,activity `com.unity3d.player.UnityPlayerActivity`. APK Signature Scheme v2verified; signerSHA256 `12a5f3a074bcde4d59f33507ec3e5d7e4bc6fc8af3d234ac16e3bb8bd4c1e187` matches the installed predecessor.
- APK SHA256 `75609afc636f6571e1090f022a77956265c1b791e87a44a0e956bc4e22bf3535` matches the installed phone's base.apk exactly.
- Activity launch returnedStatus:ok,LaunchState:COLD,TotalTime455ms. This measures activity launch, not complete game/movie initialization.
- The running process was26079; the game was the top resumed activity at capture. Startup logs show Firebase ready and a run started through PlayerScript.TryStartGameFromHorizontalInput. No fatal startup error appeared in the captured log.
- [Actual phone gameplay capture](../../../tmp/image-previews/mobile-device-playtest-2026-09-14/launch-001.png) was inspected. The player, enemies, Korean HUD and world rendering are visible. The legacy CombatHarness keyboard overlay is also visible; release-build suppression of that overlay remains a quality gap. A later attempt to use its existing toggle was stopped by a foreground guard and sent no input.
- PlayerSettings live values were restored, but both SaveAssetIfDirty and the generic serialized-object writer left the temporary signing flag on disk. A guarded before-image restore proved that this flag was the only file difference before copying. Final PlayerSettings bytes exactly match the pre-build snapshot, SHA256 `738d8b35273476b3aae9055391370da5c7bf9045a52b25767d3fd599ee366797`. The maintained builder now performs this guarded restoration; its final version passes Pipeline dry-run compilation.
- Original SR18 scene remains clean in Edit Mode. No further play controls were sent to the user's phone after the foreground guard.

Artifacts are in `tmp/mobile-device-playtest-2026-09-13/`: build result, source hashes, both certificate reports, APK metadata/hash, installed predecessor APK and device-startup.log. The APK is [available locally](../../../Builds/Android/TralaleroShooter-Optimized-20260913.apk).

This completes installation and execution verification. Sustained phone frame-time/thermal profiling and complete chapter playthroughs were not performed.
