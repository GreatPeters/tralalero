using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CombatFeedbackBackup
{
    public static object Main()
    {
        string folder = "tmp/combat-feedback-2026-09-14/before";
        if (Directory.Exists(folder)) throw new InvalidOperationException("Preserve fresh backup");
        Directory.CreateDirectory(folder);
        var scene = SceneManager.GetActiveScene();
        File.Copy(scene.path, folder + "/SR18-disk.unity");
        EditorSceneManager.SaveScene(scene, folder + "/SR18-live.unity", true);
        // Preserve the already-authored live scene before switching for verification.
        EditorSceneManager.SaveScene(scene);
        File.Copy("Assets/ShooterSurvival/GameData/Editor/Data.xlsx", folder + "/Data.xlsx");
        File.Copy("Assets/ShooterSurvival/Resources/GameData/Data.bytes", folder + "/Data.bytes");
        foreach (var dir in new[] { "Assets/ShooterSurvival/Scripts", "Assets/ShooterSurvival/Editor", "Assets/Tests/Editor" })
        foreach (var path in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            string target = folder + "/" + path;
            Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(path, target);
        }
        return new { folder, scene = scene.path, saved = !scene.isDirty };
    }
}
