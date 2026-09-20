---
title: Schedule live Unity builds through a one-shot Editor update
date: 2026-09-13
category: workflow-issues
module: Unity Android build automation
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - "A Pipeline build request returns scheduled but no running marker appears."
  - "The Editor services Pipeline calls while its delayCall queue remains pending."
tags: [unity, android, pipeline, build, callbacks, adb]
---

# Schedule live Unity builds through a one-shot Editor update

## Context

The optimized phone-play APK builder registered an EditorApplication.delayCall and returned successfully through Pipeline run_script. Several subsequent successful status/eval calls showed an idle Editor, no build in progress and no running marker. Inspecting the delegate list confirmed the build callback was still pending alongside unrelated scene callbacks. Auto-tick was already enabled, and focusing the Editor did not start it.

## Guidance

Use a one-shot EditorApplication.update callback for this live build path. Remove the callback before entering BuildPipeline.BuildPlayer, write a running marker, then perform the build with settings restoration in finally. Write a separate completion report containing the actual BuildSummary outcome and APK file size. The maintained implementation is [build-mobile-playtest.cs](../../../tools/build-mobile-playtest.cs).

The recovery transferred only the identified existing MobilePlaytestBuild callback out of delayCall and into one-shot update. It did not clear unrelated callbacks or schedule a second build. The running marker and actual Android compiler/shader logs then appeared.

## Why This Matters

Transport success and delegate registration do not prove execution. A marker separates a pending callback from a running build; a result plus a restoration marker separates a running build from a completed one. Heavy builds must execute outside the bounded Pipeline request, and a repeated update callback must unsubscribe before calling them.

## When to Apply

Use this recovery when the precise pending-callback condition has been observed. The finding does not mean every delayCall is broken or that a slow active build should be retried. Never launch another build while the current one is running.

Windows reporting a Samsung ADB interface also does not establish an authorized ADB transport. Check adb devices and its device/unauthorized/offline state before claiming installation. An empty list means installation is still unavailable even when Plug and Play shows the phone.

In this case both installed ADB34.0.5and36.0.0reported an empty list. The documented [ADB_LIBUSB override](https://android.googlesource.com/platform/packages/modules/adb/%2Bshow/refs/heads/main/docs/user/adb.1.md) also made no difference, and the native backend was restored. No driver or phone authorization bypass was attempted.

USB-only tracing later showed native ADB discovering the Samsung endpoint but failing to retrieve its serial number with Win32 error31, before authorization. The PnP problem code was0: these are different error namespaces. A targeted pnputil restart was denied by Windows; physical reconnection was requested instead. Do not describe an empty device list as proven missing phone authorization.

The user's physical cable reconnection restored an already-authorized SM-S901N/Android16transport immediately. This establishes the successful recovery, not whether the cable, USB port or phone stack originally caused the failure.

## Preserve signing state on disk as well as in memory

The build's finally restored useCustomKeystore=truein the API and SerializedObject, while ProjectSettings.asset still contained the temporary0value. Saving the dirty object was initially rejected by automatic review because the stale disk diff was interpreted as the proposed state. A read-only comparison proved backup/API/serialized=trueand disk=false; guarded restoration calls were subsequently accepted.

SaveAssetIfDirty and the generic Unity serialized-file writer did not update this special setting on disk in this run. The successful recovery first proved that the entire file differed from its pre-build snapshot only at the temporary signing flag, then restored the exact before-image. Both file hashes matched afterward, with the live setting already true. The builder retains that guard so unrelated project-setting edits cannot be overwritten. Do not report restoration from a finally marker alone.

The resulting699039439-byte APK verified under the same certificate as the predecessor, installed with adb install -r, retained the original first-install date, and matched the phone's installed APK hash. Native gameplay rendering was observed on2026-09-14. Full device frame-rate certification is a separate task.

## Phone launch is not rendering verification

The September20 update built revision `dedade520`, matched the predecessor's certificate, installed with `adb install -r`, retained first-install time and matched the installed APK hash. PlayerSettings were restored byte-for-byte. The cold activity launch reported success, but the device was on its always-on/lock screen and the activity then became hidden. Check device visibility separately; do not report game rendering from an `am start` success or live PID alone. A wake action does not authorize bypassing the lock screen. Ask the owner to unlock when visual verification is needed, while accurately separating completed installation from pending rendering checks. See [the release record](../../../map-concepts/mobile-release-2026-09-20/README.md).

## Examples

- Pending: builder output says scheduled, no running.txt, isBuildingPlayer=false, matching callback remains in delayCall.
- Running: running.txt exists and Android build logs advance.
- Complete: result.json reports Succeeded, APK exists with the expected package/ABI, signature verification passes and settings-restored.txt exists.

## Related

- [Official Unity CLI/Pipeline control](../tooling-decisions/adopt-official-unity-cli-pipeline-as-codex-editor-control-path-2026-08-23.md).
- [Bounded combat presentation and trustworthy probes](../performance-issues/prewarm-bounded-combat-presentation-pools-2026-09-13.md).
