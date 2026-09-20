# Merge the current work and update the connected phone

User request: merge to the main branch and build/install on the currently connected phone.

Status: requested merge, Android build and in-place phone installation complete. Actual game rendering was not verified because the phone remained locked; the owner was notified.

Merge/publication completed: game content `627bc4e00`, workflow safeguards `dedade520`. `master` fast-forwarded to `dedade520` and pushed to `origin/master`. APK build requested from this exact revision; the maintained one-shot update callback wrote `running.txt` and Android compilation is active. Existing phone APK was backed up and its Android debug certificate SHA-256 verified as `12a5f3a074bcde4d59f33507ec3e5d7e4bc6fc8af3d234ac16e3bb8bd4c1e187`.

- The repository's default branch is `master`; remote HEAD and local base are `bb9348dfb`.
- Integrate the current Bonus talisman, combat/route and final seagull work, with required source assets, tests, tools and durable records. Preserve local capture frames, generated caches and imported scratch images outside the commit.
- The authorized connected phone is Samsung SM-S901N. Existing package: `com.mzkoreagames.tralaleroshooter`, version 1.0.0/code 1. Preserve installed data through an in-place update, with no uninstall/data clear.
- Use the maintained official-Pipeline `tools/build-mobile-playtest.cs` path: ARM64/IL2CPP APK, existing test ads, prior compatible test signing, and settings restoration.
- Verify build report, package/signature, installed APK, launch and startup logs. Keep build/install evidence in `tmp/release-2026-09-20/`.

Source verification already completed across the implementation: focused native/Edit Mode tests, runtime/editor builds, real gameplay probes and final size/impact recordings. A pre-existing road-count assertion expects 230 while both its pre-change snapshot and current scene contain 236; that unrelated assertion is recorded in the seagull-size evidence.

Build succeeded (0 errors, 58 warnings), package/signature verified, `adb install -r` succeeded, and installed APK hash matches the built artifact. PlayerSettings are restored byte-for-byte. `am start` succeeded, but the lock screen hides the game; the user was asked to unlock it for final visual verification. Full record: `map-concepts/mobile-release-2026-09-20/README.md`.
