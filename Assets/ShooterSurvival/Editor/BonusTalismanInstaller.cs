using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BonusTalismanInstaller
{
    public const string Root = "Assets/ShooterSurvival/Resources/BonusTalisman";
    public static readonly string[] SceneNames = { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" };

    [MenuItem("Tools/Bonus/Build and Apply Common Talisman")]
    public static void BuildAndApply() { Debug.Log(ApplyAll()); }

    public static object ApplyAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save current scene edits before authoring.");
        BuildArt();
        int prefabs = 0, placements = 0;
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ShooterSurvival/Prefabs/Walls" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset.GetComponentsInChildren<WallScript>(true).Any(w => w.wallType == WallType.BuffWall)) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponentsInChildren<WallScript>(true).Length == 1 && root.GetComponent<BonusWallLifetimeRoot>() == null)
                    root.AddComponent<BonusWallLifetimeRoot>();
                foreach (var wall in root.GetComponentsInChildren<WallScript>(true))
                    if (wall.wallType == WallType.BuffWall) BonusTalismanPresentation.Refresh(wall);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabs++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string name in SceneNames)
        {
            string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var wall in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WallScript>(true)))
                {
                    if (wall.wallType != WallType.BuffWall) continue;
                    BonusTalismanPresentation.Refresh(wall);
                    EditorUtility.SetDirty(wall.gameObject);
                    placements++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
        return new { prefabs, placements, visual = Root + "/Talisman.prefab" };
    }

    public static void BuildArt() => PolishedTalismanAssets.Build();
}
