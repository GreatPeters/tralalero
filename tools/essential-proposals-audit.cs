using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EssentialProposalsAudit
{
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    public static object Main(string output)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Clean Edit Mode required");
        Directory.CreateDirectory(output);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var results = new List<object>();
        try
        {
            foreach (var name in new[] { "Noryangjin_MapTool_Mode_SR18_Revamp", "HighWay", "RestStop", "Jamsil", "ShoeTower" })
            {
                var path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
                if (!File.Exists(path)) throw new FileNotFoundException(path);
                var scene = EditorSceneManager.OpenScene(path);
                var roots = scene.GetRootGameObjects();
                var renderers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToArray();
                var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
                var transforms = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                var row = new {
                    scene = path, rendererCount = renderers.Length,
                    shaders = materials.GroupBy(m => m.shader.name).Select(g => new { shader = g.Key, count = g.Count() }).ToArray(),
                    ownedToon = materials.Where(m => AssetDatabase.GetAssetPath(m).Contains("Toon")).Select(m => new {
                        path = AssetDatabase.GetAssetPath(m), shader = m.shader.name,
                        textureImpact = m.HasProperty("_TextureImpact") ? m.GetFloat("_TextureImpact") : -1,
                        shade = m.HasProperty("_ColorDim") ? new[] { m.GetColor("_ColorDim").r, m.GetColor("_ColorDim").g, m.GetColor("_ColorDim").b } : null,
                        outline = m.HasProperty("_OutlineWidth") ? m.GetFloat("_OutlineWidth") : 0
                    }).ToArray(),
                    toonRoots = transforms.Where(t => t.name.IndexOf("toon", StringComparison.OrdinalIgnoreCase) >= 0).Select(t => t.name).ToArray(),
                    cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Select(c => new { c.name, c.fieldOfView, c.orthographic, position = c.transform.position.ToString("R") }).ToArray(),
                    settings = roots.SelectMany(r => r.GetComponentsInChildren<HarborSettingsPanel>(true)).Count(),
                    activeLit = renderers.Where(r => r.enabled && r.gameObject.activeInHierarchy && r.sharedMaterials.Any(m => m != null && m.shader.name == "Universal Render Pipeline/Lit")).Select(r => new {
                        name = r.name, path = string.Join("/", r.GetComponentsInParent<Transform>(true).Reverse().Select(t => t.name)),
                        materials = r.sharedMaterials.Where(m => m != null).Select(m => new {
                            m.name, path = AssetDatabase.GetAssetPath(m), surface = m.HasProperty("_Surface") ? m.GetFloat("_Surface") : 0,
                            emission = m.IsKeywordEnabled("_EMISSION"), texture = m.HasProperty("_BaseMap") ? AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")) : ""
                        }).ToArray()
                    }).ToArray(),
                    colliders = roots.Sum(r => r.GetComponentsInChildren<Collider>(true).Length),
                    animators = roots.Sum(r => r.GetComponentsInChildren<Animator>(true).Length)
                };
                results.Add(row);
                File.WriteAllText(Path.Combine(output, name + ".json"), Json(row));
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        File.WriteAllText(Path.Combine(output, "summary.json"), Json(results));
        return new { scenes = results.Count, output, saved = false };
    }
}
