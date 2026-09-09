using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-session preference only: never stored in a scene, prefab or player build.
[InitializeOnLoad]
internal static class NoryangjinMapToolTestSpeed
{
    private const string SelectionKey = "NoryangjinMapTool.TestTimeScale";
    private const string AppliedKey = "NoryangjinMapTool.TestTimeScaleApplied";
    internal static readonly string[] Labels = { "1배 (기본)", "3배 (테스트)" };

    static NoryangjinMapToolTestSpeed()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ApplySelectedSpeed;
    }

    internal static float SelectedTimeScale => SessionState.GetFloat(SelectionKey, 1f) == 3f ? 3f : 1f;

    internal static void SelectTimeScale(float value)
    {
        if (value != 1f && value != 3f)
            throw new ArgumentOutOfRangeException(nameof(value), "Choose 1x or 3x test speed.");
        SessionState.SetFloat(SelectionKey, value);
        ApplySelectedSpeed();
    }

    internal static bool CanApply(bool playing, string scenePath) =>
        playing && NoryangjinMapToolWindow.IsMapToolScenePath(scenePath);

    private static void ApplySelectedSpeed()
    {
        if (!CanApply(EditorApplication.isPlaying, SceneManager.GetActiveScene().path))
            return;
        Time.timeScale = SelectedTimeScale;
        SessionState.SetBool(AppliedKey, true);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            ApplySelectedSpeed();
        else if ((state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode) &&
                 SessionState.GetBool(AppliedKey, false))
        {
            Time.timeScale = 1f;
            SessionState.SetBool(AppliedKey, false);
        }
    }
}
