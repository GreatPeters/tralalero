using System;
using UnityEditor;
using UnityEditor.SceneManagement;

// Editor-session override only. Never rewrite scenes, build settings or the workbook.
[InitializeOnLoad]
internal static class NoryangjinMapToolTestStartStage
{
    private const string SelectionKey = "NoryangjinMapTool.TestStartStage";
    private const string DefaultPathKey = SelectionKey + ".DefaultPath";
    private const string AppliedPathKey = SelectionKey + ".AppliedPath";
    private const string ActiveKey = SelectionKey + ".Active";
    internal static readonly string[] Labels = { "데이터", "1", "2", "3", "4", "5" };
    internal static readonly string[] Names = { "기존 시작 설정", "노량진", "고속도로", "휴게소", "잠실", "슈타워" };

    static NoryangjinMapToolTestStartStage()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.quitting += RestoreDefault;
        EditorApplication.delayCall += ApplySelection;
    }

    internal static int SelectedStage => Math.Max(0, Math.Min(5, SessionState.GetInt(SelectionKey, 0)));

    internal static string ScenePathForStage(int stage)
    {
        switch (stage)
        {
            case 0: return string.Empty;
            // Stage 1 plays the Noryangjin revamp safe copy; the original SR18 scene stays untouched.
            case 1: return NoryangjinMapToolWindow.Sr18RevampMapToolScenePath;
            case 2: return NoryangjinMapToolWindow.HighwayMapToolScenePath;
            case 3: return NoryangjinMapToolWindow.RestStopMapToolScenePath;
            case 4: return NoryangjinMapToolWindow.JamsilMapToolScenePath;
            case 5: return NoryangjinMapToolWindow.ShoeTowerMapToolScenePath;
            default: throw new ArgumentOutOfRangeException(nameof(stage), "Choose Data or stage 1–5.");
        }
    }

    internal static void SelectStage(int stage)
    {
        string path = ScenePathForStage(stage);
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Change the start stage after exiting Play Mode.");
        if (stage != 0 && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            throw new InvalidOperationException("Start scene is missing: " + path);
        SessionState.SetInt(SelectionKey, stage);
        ApplySelection();
    }

    private static void ApplySelection()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        int stage = SelectedStage;
        if (stage == 0) { RestoreDefault(); return; }
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePathForStage(stage));
        if (scene == null) { RestoreDefault(); return; }
        string currentPath = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
        if (!SessionState.GetBool(ActiveKey, false) || currentPath != SessionState.GetString(AppliedPathKey, ""))
            SessionState.SetString(DefaultPathKey, currentPath);
        EditorSceneManager.playModeStartScene = scene;
        SessionState.SetString(AppliedPathKey, AssetDatabase.GetAssetPath(scene));
        SessionState.SetBool(ActiveKey, true);
    }

    private static void RestoreDefault()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        // Another editor tool may have deliberately replaced our override.
        if (AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) == SessionState.GetString(AppliedPathKey, ""))
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(DefaultPathKey, ""));
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseString(DefaultPathKey);
        SessionState.EraseString(AppliedPathKey);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) RestoreDefault();
        else if (state == PlayModeStateChange.EnteredEditMode) ApplySelection();
    }
}
