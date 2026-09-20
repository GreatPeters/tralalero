# Merge the current work and update the connected phone

User request: merge to the main branch and build/install on the currently connected phone.

Status: in progress.

- The repository's default branch is `master`; remote HEAD and local base are `bb9348dfb`.
- Integrate the current Bonus talisman, combat/route and final seagull work, with required source assets, tests, tools and durable records. Preserve local capture frames, generated caches and imported scratch images outside the commit.
- The authorized connected phone is Samsung SM-S901N. Existing package: `com.mzkoreagames.tralaleroshooter`, version 1.0.0/code 1. Preserve installed data through an in-place update, with no uninstall/data clear.
- Use the maintained official-Pipeline `tools/build-mobile-playtest.cs` path: ARM64/IL2CPP APK, existing test ads, prior compatible test signing, and settings restoration.
- Verify build report, package/signature, installed APK, launch and startup logs. Keep build/install evidence in `tmp/release-2026-09-20/`.

Source verification already completed across the implementation: focused native/Edit Mode tests, runtime/editor builds, real gameplay probes and final size/impact recordings. A pre-existing road-count assertion expects 230 while both its pre-change snapshot and current scene contain 236; that unrelated assertion is recorded in the seagull-size evidence.
