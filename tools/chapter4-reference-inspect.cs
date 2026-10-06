using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Read-only native inventory. Never saves a scene or asset.
public static class Chapter4ReferenceInspect
{
    public static object People()
    {
        return new[] { "A01_dad", "A02_mom", "A05_student", "A06_grandma" }.Select(id => {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/" + id + ".prefab");
            var a = p.GetComponentInChildren<Animator>(true);
            return new { id, components = p.GetComponentsInChildren<Component>(true).Select(c=>c.GetType().Name).Distinct().ToArray(),
                controller = a == null ? "" : AssetDatabase.GetAssetPath(a.runtimeAnimatorController),
                clips = a == null || a.runtimeAnimatorController == null ? new string[0] : a.runtimeAnimatorController.animationClips.Select(c=>c.name).Distinct().ToArray(),
                meshes = p.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>new {r.name, vertices=r.sharedMesh.vertexCount, materials=r.sharedMaterials.Select(m=>m.name).ToArray()}).ToArray() };
        }).ToArray();
    }
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Idle editor required.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Preserve dirty scenes.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            return new[] { "Jamsil", "ShoeTower" }.Select(name => {
                var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
                var d = Object.FindFirstObjectByType<Chapter45Director>();
                return new { scene = name, routeLength=d.route.Length,routeSegments=d.route.segments.Length,
                    roots = scene.GetRootGameObjects().Select(o => o.name).ToArray(),
                    cameraFov = Camera.main == null ? 0 : Camera.main.fieldOfView,
                    cameraPosition = Camera.main == null ? "none" : Camera.main.transform.position.ToString(),
                    sceneryGroups = d.GetComponentsInChildren<Chapter45SceneryGroup>(true).Length };
            }).ToArray();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
