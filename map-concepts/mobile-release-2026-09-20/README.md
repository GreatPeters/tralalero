# Merge and Android phone update

User requested merging the current work into the main branch and installing a build on the connected phone. The repository's default branch is `master`.

## Source

- Game/content commit: `627bc4e00` (`fix: finish talisman pickups and combat hazard feedback`).
- Workflow safeguards: `dedade520`; `master` fast-forwarded to this revision and pushed to `origin/master` before building.
- Build input revision: `dedade520`. Subsequent changes to this release record are documentation only.
- Required untracked dependencies of all three saved build scenes were checked against the explicit staging list. Local frame sequences, scratch images and generated caches were preserved outside the commits.

## Build and installation

- Official Unity Pipeline ran `tools/build-mobile-playtest.cs`, with evidence folder `tmp/release-2026-09-20/build-v1` and output `Builds/Android/TralaleroShooter-20260920.apk`.
- Build succeeded in 892.76 seconds, 0 errors and 58 warnings. APK size: 686,722,337 bytes. ARM64/IL2CPP, non-development APK, existing test-ad configuration.
- Package: `com.mzkoreagames.tralaleroshooter`; version 1.0.0/code 1; minimum API 24, target API 36; activity `com.unity3d.player.UnityPlayerActivity`.
- APK SHA-256: `3e5c3fb91640c853bbec52c8b8d87193bdaafcbd44fc22616ec9e912c00ebbdd`.
- Signature Scheme v2 verified. Certificate SHA-256 `12a5f3a074bcde4d59f33507ec3e5d7e4bc6fc8af3d234ac16e3bb8bd4c1e187` matches the installed predecessor, which was pulled and retained before replacement. This is the compatible Android debug certificate for local phone testing, not a production-store release.
- `adb install -r` succeeded on the authorized Samsung SM-S901N. No uninstall or data-clear command was used. First-install time remained 2026-07-31 21:36:41; last-update time became 2026-09-20 21:26:43.
- The phone's installed `base.apk` SHA-256 exactly matches the new build.
- `am start -W` returned `Status: ok`, cold activity launch 764ms. This measures Android activity launch, not completed Unity rendering.
- Original PlayerSettings bytes matched their pre-build SHA-256 after restoration; live custom signing was restored to true. Unity returned to clean SR18 Edit Mode.

## Execution verification

The process started, but the phone was showing its lock screen/always-on display. Read-only logs showed the activity becoming hidden; a wake action confirmed `showing=true`. The user was asked to unlock the phone for actual game-screen verification. No lock bypass was attempted. Installation and binary identity are verified; rendering remains pending that step.

Local evidence: `tmp/release-2026-09-20/` contains build summary, predecessor APK, signature/package/hash records, install output, package timestamps and startup logs. The initial device capture is under `tmp/image-previews/mobile-release-2026-09-20/`.
