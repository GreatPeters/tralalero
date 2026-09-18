var result = new System.Collections.Generic.List<object>();
foreach (var name in new[] { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" })
{
    var path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
    if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    try
    {
        var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.GameManager>(true)).Single();
        var data = new UnityEditor.SerializedObject(manager);
        var spawn = data.FindProperty("playerSpawnPoint").objectReferenceValue as UnityEngine.Transform;
        result.Add(new { name, player = manager.playerScript != null ? manager.playerScript.transform.position.ToString() : null,
            spawn = spawn != null ? spawn.position.ToString() : null, spawnName = spawn != null ? spawn.name : null,
            enemyParent = manager.EnemyParent != null ? manager.EnemyParent.name : null });
    }
    finally { if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
}
return result;
