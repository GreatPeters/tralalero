---
title: Unblock layered Unity Firebase Android player-build failures
date: 2026-07-31
category: docs/solutions/build-errors
module: Unity Android Firebase build pipeline
problem_type: build_error
component: development_workflow
symptoms:
  - "Android player compilation fails because a runtime script imports UnityEditor or declares CustomEditor"
  - "A URP asset reports that it is not at the package's last supported serialized version"
  - "The development APK build stops because the configured custom keystore password is unavailable"
  - "AAPT fails with resource style/BaseUnityGameActivityTheme not found"
  - "Unity automation times out while a Gradle Error dialog blocks the editor update loop"
root_cause: config_error
resolution_type: config_change
severity: medium
related_components: [firebase-analytics, android-manifest, gradle, urp, unity-editor]
tags: [unity, android, firebase, gradle, manifest, gameactivity, keystore, debugview]
---

# Unblock layered Unity Firebase Android player-build failures

## Problem

The first real-device Firebase Analytics build exposed several independent
Unity Android build blockers in sequence. Fixing only the first visible error
did not produce an APK because editor-only code, serialized render-pipeline
state, signing configuration, and the custom Android manifest all participate
in the same player-build pipeline.

## Symptoms

- A third-party runtime script failed player compilation after importing
  `UnityEditor` and declaring a `CustomEditor` without an editor guard.
- FlatKit's example URP asset serialized version was newer than the installed
  URP package's `UniversalRenderPipelineAsset.k_LastVersion`.
- The project selected a custom release keystore but the automated development
  build did not have its password.
- Gradle reached `:launcher:processDebugResources` and AAPT reported:

  ```text
  resource style/BaseUnityGameActivityTheme not found
  ```

- After that failure, Unity's main window still appeared responsive, but a
  visible `Gradle Error: Resource Not Found` dialog prevented editor update
  callbacks, asset refresh, and queued automation commands from completing.

## What Didn't Work

- Keeping both `UnityPlayerActivity` and `UnityPlayerGameActivity` in the custom
  manifest relied on an assumption that Unity would remove the inactive entry.
  Unity disabled the inactive GameActivity but retained its missing theme
  reference, so resource linking still failed.
- Re-sending MCP or local HTTP menu commands while the Gradle error dialog was
  open only produced timeouts. The commands needed Unity's blocked update loop.
- Editing the validator and immediately rebuilding used the already-loaded
  editor assembly. A forced asset refresh and script compilation were required
  before the validator recognized the corrected manifest.
- Temporarily changing `PlayerSettings.Android.useCustomKeystore` inside a
  failed build was not enough to guarantee that the serialized project setting
  returned to its original value. It had to be restored explicitly and saved.

## Solution

Guard editor-only APIs in third-party runtime scripts without changing their
runtime behavior:

```csharp
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[CustomEditor(typeof(PrefabSwitch))]
public class PrefabSwitchEditor : Editor
{
    // Existing inspector code.
}
#endif
```

Match the serialized URP asset version to the installed package's supported
version. For the package installed during this build, `k_LastVersion` was `12`,
so both `k_AssetVersion` and `k_AssetPreviousVersion` were corrected from `13`
to `12`. Preserve unrelated renderer-data edits when making this repair.

Use the active Android application entry in the custom manifest. With
`androidApplicationEntry: 1`, this project uses `UnityPlayerActivity`, so the
custom manifest contains that launcher activity and does not declare the
inactive `UnityPlayerGameActivity`:

```xml
<activity
    android:name="com.unity3d.player.UnityPlayerActivity"
    android:theme="@style/UnityThemeSelector">
    <intent-filter>
        <action android:name="android.intent.action.MAIN" />
        <category android:name="android.intent.category.LAUNCHER" />
    </intent-filter>
    <meta-data
        android:name="unityplayer.UnityActivity"
        android:value="true" />
</activity>
```

Keep the manifest validator aligned with the selected entry instead of
requiring both activity classes. Force a synchronous asset refresh, request
script compilation, and verify the loaded validator result before rebuilding.

For a local development APK, either provide the intended keystore credentials
or temporarily use Android's debug signing. Restore the project preference
afterward and persist it:

```csharp
bool previous = PlayerSettings.Android.useCustomKeystore;
try
{
    PlayerSettings.Android.useCustomKeystore = false;
    BuildPipeline.BuildPlayer(options);
}
finally
{
    PlayerSettings.Android.useCustomKeystore = previous;
    AssetDatabase.SaveAssets();
}
```

If automation stops making progress after a Gradle error, inspect all visible
Unity-owned top-level windows rather than trusting `Process.Responding`. Close
the specific error dialog through its normal **OK** action, wait for compilation
and domain reload, and only then issue a fresh build command.

The successful APK was installed on an `SM-S901N`, launched with Firebase
debug mode enabled, and produced a `game_round_end` event carrying `_dbg=1`.
The Firebase service returned HTTP `204`, and Firebase Console DebugView showed
five recent events.

## Why This Works

Unity player compilation excludes editor assemblies, so every `UnityEditor`
reference reachable from a runtime script must be conditionally compiled out.
URP assets are versioned serialized objects and must not claim a schema newer
than the installed package understands. Android resource linking validates
every manifest reference, including disabled activities, so retaining an
inactive GameActivity still requires its theme resource.

Closing the modal restores Unity's update loop. Refreshing and recompiling then
loads the corrected validator assembly, while explicit keystore restoration
prevents a one-off debug build from silently changing the release-signing
configuration.

## Prevention

- Add an Android development-build smoke test after changing Firebase packages,
  custom Gradle templates, manifest entries, URP packages, or signing settings.
- Make the manifest validator check the activity selected by project settings,
  not every activity type Unity supports.
- Keep editor inspectors in an `Editor` assembly or wrap all editor-only imports
  and declarations in `UNITY_EDITOR`.
- Record and restore temporary `PlayerSettings` mutations, call
  `AssetDatabase.SaveAssets()`, and verify the serialized value afterward.
- Treat visible Unity modal dialogs as a separate health signal from process
  responsiveness and connector health.
- Verify Analytics with all three signals: `_dbg=1` on the event, a successful
  upload response, and the event count in Firebase Console DebugView.

## Related Issues

- [Avoid Synchronous EDM Resolution From Unity Editor Command Queues](../workflow-issues/avoid-synchronous-edm-resolution-from-unity-editor-command-queues-2026-07-30.md)
- [Firebase Analytics and BigQuery setup](../../firebase-analytics-bigquery.md)
