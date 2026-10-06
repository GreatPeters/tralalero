using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Visual Scripting registers ReferenceCollector.Initialize's scene-unload callback on
// every Play entry. With domain reload disabled, previous identical delegates survive.
// Keep one callback using the public event removal API; do not alter other subscribers.
[InitializeOnLoad]
internal static class VisualScriptingPlayModeCleanup
{
    private static readonly FieldInfo SceneUnloaded = typeof(SceneManager).GetField(
        "sceneUnloaded", BindingFlags.Static | BindingFlags.NonPublic);
    private static bool warned;

    static VisualScriptingPlayModeCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode ||
            !EditorSettings.enterPlayModeOptionsEnabled ||
            (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0)
            return;

        // Unity exposes event add/remove but no subscriber inventory. If its private
        // backing field changes, leave subscriptions intact and make the limit visible.
        if (SceneUnloaded == null)
        {
            if (!warned)
                Debug.LogWarning("Visual Scripting Play cleanup could not inspect sceneUnloaded; subscriptions were left unchanged.");
            warned = true;
            return;
        }

        var callbacks = SceneUnloaded.GetValue(null) as System.Delegate;
        if (callbacks == null) return;
        var seen = new HashSet<UnityAction<Scene>>();
        foreach (var subscription in callbacks.GetInvocationList())
        {
            if (subscription is not UnityAction<Scene> callback) continue;
            var method = callback.Method;
            if (method.DeclaringType?.DeclaringType?.FullName != "Unity.VisualScripting.ReferenceCollector" ||
                !method.Name.StartsWith("<Initialize>", System.StringComparison.Ordinal))
                continue;
            if (!seen.Add(callback)) SceneManager.sceneUnloaded -= callback;
        }
    }
}
