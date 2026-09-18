using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class CaptureNewStylizedSurfaces
{
    const string Output = "tmp/image-previews/new-content-stylized-2026-09-17";
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Preserve dirty scenes before captures.");
        Directory.CreateDirectory(Output);
        var reports = new System.Collections.Generic.List<object>();
        try
        {
            foreach (string chapter in new[] { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + chapter + ".unity");
                var cameras = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).ToArray();
                var source = cameras.First(c => c.name == "MapTool_Camera");
                var go = new GameObject("Surface review camera") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    var camera = go.AddComponent<Camera>(); camera.CopyFrom(source); camera.enabled = false;
                    go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                    camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                    Shot(camera, chapter + "-game.png", 900, 1600);
                    var merchant = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HarborMerchantGreeting>(true)).FirstOrDefault();
                    if (merchant != null)
                    {
                        var center = merchant.transform.position + Vector3.up * 1.9f;
                        camera.transform.position = center + merchant.transform.forward * 9 + merchant.transform.right * 1.5f + Vector3.up * 1.4f;
                        camera.transform.LookAt(center); camera.fieldOfView = 36;
                        Shot(camera, "Merchant-close.png", 1200, 1200);
                    }
                    var materials = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                        .SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
                    reports.Add(new {chapter,styled=materials.Count(m=>m.shader.name==GeneratedStylizedSurface.ShaderName),
                        ownedNonStylized=materials.Where(m=>AssetDatabase.GetAssetPath(m).StartsWith("Assets/ShooterSurvival/Models/") && !m.shader.name.StartsWith("FlatKit/"))
                        .Select(m=>new {path=AssetDatabase.GetAssetPath(m),shader=m.shader.name}).ToArray()});
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return new { output=Output, reports };
    }

    static void Shot(Camera camera, string name, int width, int height)
    {
        string path = Output + "/" + name;
        if (File.Exists(path)) throw new IOException("Keep existing evidence: " + path);
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            rt.Create(); camera.targetTexture = rt; camera.aspect = (float)width/height; camera.Render();
            RenderTexture.active = rt; texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(texture); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
