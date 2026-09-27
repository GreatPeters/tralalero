using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class SnapshotHighwayEditor
{
    public static object Copy()
    {
        var scene=SceneManager.GetActiveScene();
        return EditorSceneManager.SaveScene(scene,"outputs/highway-three-lane-rebuild-2026-09-26/dirty-before-test.unity",true);
    }
}
