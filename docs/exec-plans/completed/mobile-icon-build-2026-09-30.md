# Android icon and Samsung device build — 2026-09-30

The user requested a generated app icon and a build on their connected phone, then clarified that the local merge target is `master`.

## Delivered build

- `Builds/Android/TralaleroShooter-20260930-AppIcon.apk`
- Unity 6000.2.6f1; Android ARM64/IL2CPP; non-development APK with the existing test-ad configuration.
- Package `com.mzkoreagames.tralaleroshooter`; version 1.0.0/code 1; minimum API 24, target API 36.
- Build succeeded in 887.953 seconds, 0 errors and 59 warnings; 1,526,175,575 bytes.
- APK SHA-256: `743797b36cbb8dcc690b06e11e16c4b2a5c0acaebf9b76c0698eff00076f6e88`.
- APK signature verified. Local Android debug certificate SHA-256: `12a5f3a074bcde4d59f33507ec3e5d7e4bc6fc8af3d234ac16e3bb8bd4c1e187`.
- Build scenes explicitly include the current `Noryangjin_MapTool_Mode_SR18_Revamp`, `HighWay`, `RestStop`, and original `Noryangjin_MapTool_Mode_SR18`. Existing EditorBuildSettings were not changed.
- Temporary signing and build settings were restored. `ProjectSettings.asset` exactly matched its post-icon/pre-build SHA-256.

## Device verification

- Installed successfully on the single connected Samsung SM-S901N with `adb install -r`.
- The installed `base.apk` SHA-256 exactly matches the build above.
- First install and last update time: 2026-10-01 00:18:44 on the phone.
- Activity launch returned `Status: ok`, `LaunchState: COLD`, total time 613 ms. The application process was present afterward.
- At verification the phone was locked (`showing=true`). User unlock was requested. Activity launch is verified; visible game rendering and gameplay are not yet verified.
- No uninstall, data-clear, persistent debug-trust grant, security-setting change or store publication occurred. The separate pre-existing `com.mzkoreagames.killbb` app was left untouched.

## Icon and reproduction

- New asset: `Assets/ShooterSurvival/UI/AppIcon20260930/TralaleroShooter-AppIcon.png`.
- Generated with built-in image generation using an existing game UI montage as reference. Prompt: one polished 3D cartoon gray-blue shark with white belly, determined eyes and a mischievous grin, wearing unbranded cyan sneakers, lunging forward and firing swirling turquoise water in a sunny harbor; full-bleed square, large readable face and shoe, no text, logos or border, safe for launcher masks.
- Original PNG SHA-256: `be18c4a570a75e222b527f812ced219aa9ff5147dd712d6582b4d504d69d124c`.
- Default icon and all 18 Android Adaptive/Round/Legacy slots applied through Unity. Both mandatory adaptive layers use the same opaque artwork. Generated native Android launcher PNG was visually verified.
- Library icon: `libfile_abadb36c04d48191bcc5fd0b12fbd28b`.
- Reproduction scripts: `tools/apply-mobile-icon-20260930.cs`, `tools/build-mobile-icon-20260930.cs`.

## Cache recovery and checks

The first build failed because the migrated Bee cache referenced the former C: project path. While the Editor was idle, `Library/Bee` was renamed to `Library/Bee.before-mobile-build-20260930`, preserving it; the retry used a fresh cache and `BuildOptions.CleanBuildCache`. No source directory was deleted. Both the previously saved scene and newly detected unsaved scene were copied into the ignored evidence folder before the current scene was saved.

- `tools/validate-agent-harness.ps1`: passed.
- Current feature regression classes: GradeSeparation 19, RevampMechanics 13, FeedbackV3 10, InteriorV2 2, CameraOcclusion 9, DebugReview 14, TestStartStage 8, TestSpeed 8: **83 passed**. Three classes were rerun individually because domain reload omitted their individual entries from the broad runner's detailed result list.
- Broader `Noryangjin` test-name run: runner summary **405 total, 379 passed, 26 failed**. Failures include old scene-registration requirements, map-tool expectations, historical scene hashes, presentation expectations and NullReference exceptions. These were recorded, not repaired as part of the icon/build request. The detailed callback list contains fewer entries than the summary after domain reload; do not treat it as a full failure inventory or claim all checks pass.
- Unity returned to clean Edit Mode in the original Revamp scene.

Evidence: `tmp/mobile-icon-build-20260930/` contains the failed and successful build summaries and recovery snapshots. Device/test receipts are retained in the 2026-09-30 Codex task workspace. Settings backups are local recovery artifacts, not published deliverables.

## Local merge scope

855 game source, integrated asset, test, production-tool and documentation files were selected from the existing working tree. The icon/settings and this release record form a separate commit. Untracked generation/review output (829 files, approximately 770.5 MiB at inspection) and generated build state remain outside the commits and are preserved locally. No remote push or backup installation/upload was performed.
