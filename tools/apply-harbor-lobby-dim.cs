if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
    throw new InvalidOperationException("Clean Edit Mode required.");
var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var backup = "tmp/backups/harbor-lobby-dim-2026-09-15/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
System.IO.Directory.CreateDirectory(backup);
var results = new System.Collections.Generic.List<string>();
try
{
    foreach (var name in new[] { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" })
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, backup + "/" + name + ".unity", true))
            throw new Exception("Backup failed: " + name);
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
        HarborGameUIInstaller.ApplyLobbyDim(canvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("Save failed: " + name);
        scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path);
        canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
        var dim = canvas.buttons.transform.Find("LobbyDim").GetComponent<UnityEngine.UI.Image>();
        if (dim.transform.GetSiblingIndex() != 0 || dim.raycastTarget || !Mathf.Approximately(dim.color.a, .20f))
            throw new Exception("Saved dim configuration incorrect: " + name);
        results.Add(name);
    }
}
finally { UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original); }
return new { scenes = results, alpha = .20f, backup };
