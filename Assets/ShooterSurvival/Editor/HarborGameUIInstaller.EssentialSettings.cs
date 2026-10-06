using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Essential proposal 3/4 (2026-10-06): remove the lateral sensitivity row and use the freed
// height to separate the bottom buttons. Touch rects are the visual rects, so widening the gaps
// also removes adjacent hit areas. Re-running is idempotent.
public static partial class HarborGameUIInstaller
{
    public static readonly string[] EssentialSettingsScenes = { "Noryangjin_MapTool_Mode_SR18_Revamp", "HighWay", "RestStop", "Jamsil", "ShoeTower" };

    public static object ApplyEssentialSettingsAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Clean Edit Mode required");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        int updated = 0;
        try
        {
            foreach (string name in EssentialSettingsScenes)
            {
                var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
                var settings = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HarborSettingsPanel>(true)).Single();
                EssentialSettings(settings);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                updated++;
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return new { updated, gameplayChanged = false };
    }

    // One scene per call keeps each Pipeline request under its main-thread time limit.
    public static object ApplyEssentialSettings(string name)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Clean Edit Mode required");
        if (!EssentialSettingsScenes.Contains(name)) throw new ArgumentException("Unknown chapter scene " + name);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
            EssentialSettings(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HarborSettingsPanel>(true)).Single());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return name + " saved";
    }

    private static void EssentialSettings(HarborSettingsPanel settings)
    {
        var panel = settings.transform.Find("Panel");
        foreach (string name in new[] { "Sensitivity", "SensitivityLabel", "SensitivityIcon" })
        {
            var row = panel.Find(name);
            if (row != null) row.gameObject.SetActive(false);
        }
        // .525-.560 is the runtime best-record line (HarborSettingsPanel.BestRecordRect).
        // Legal rows: 0.018 panel-height gaps instead of 0.007.
        Move(panel, "PrivacyPolicy", .065f, .455f, .935f, .502f);
        Move(panel, "Terms", .065f, .390f, .935f, .437f);
        Move(panel, "AdPrivacyOptions", .065f, .325f, .935f, .372f);
        Move(panel, "AccountStatus", .070f, .258f, .930f, .316f);
        Move(panel, "GooglePlayAccount", .065f, .168f, .555f, .243f);
        Move(panel, "DeleteGameAccount", .615f, .168f, .935f, .243f);
        Move(panel, "RunActions", .060f, .030f, .940f, .118f);
        foreach (var button in settings.runActions.GetComponentsInChildren<Button>(true))
        {
            var rect = (RectTransform)button.transform;
            if (button.name == "Resume") SetRect(rect, 0f, 0f, .46f, 1f);
            else if (button.name == "Retry") SetRect(rect, .54f, 0f, 1f, 1f);
        }
        foreach (var component in settings.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }
}
