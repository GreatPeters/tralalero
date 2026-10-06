using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using IndianOceanAssets.ShooterSurvival;

public static class EssentialProposalsApply
{
    const string Folder = "Assets/ShooterSurvival/Materials/Generated/EssentialToon20261006";
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static string Hash(string text) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", ""); }
    static string Ref(Object value) => value == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
    static string Fields(Component component)
    {
        var result = new StringBuilder();
        var iterator = new SerializedObject(component).GetIterator();
        while (iterator.NextVisible(true))
        {
            string value = "";
            switch (iterator.propertyType)
            {
                case SerializedPropertyType.ObjectReference: value = Ref(iterator.objectReferenceValue); break;
                case SerializedPropertyType.Integer: value = iterator.longValue.ToString(); break;
                case SerializedPropertyType.Boolean: value = iterator.boolValue.ToString(); break;
                case SerializedPropertyType.Float: value = iterator.doubleValue.ToString("R", CultureInfo.InvariantCulture); break;
                case SerializedPropertyType.String: value = iterator.stringValue; break;
                case SerializedPropertyType.Enum: value = iterator.intValue.ToString(); break;
                case SerializedPropertyType.Vector2: value = iterator.vector2Value.ToString("R"); break;
                case SerializedPropertyType.Vector3: value = iterator.vector3Value.ToString("R"); break;
                case SerializedPropertyType.Vector4: value = iterator.vector4Value.ToString("R"); break;
                case SerializedPropertyType.Quaternion: value = iterator.quaternionValue.ToString("R"); break;
                case SerializedPropertyType.Color: var color = iterator.colorValue; value = string.Join(",", color.r.ToString("R"), color.g.ToString("R"), color.b.ToString("R"), color.a.ToString("R")); break;
            }
            result.Append(iterator.propertyPath).Append('=').Append(value).Append('\n');
        }
        return result.ToString();
    }
    static SortedDictionary<string, string> Protected(Scene scene)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var protectedComponents = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c != null &&
            (c is Collider || c is Rigidbody || c is Camera || c is Animator || c is Light || c is PlayerScript || c is ChapterProgression || c is Chapter45Director || c is Chapter45Target || c is Chapter45Goal || c is Chapter45Lift || c is Chapter45Encounter || c is RestStopHoldout || c is EnemyScript_space || c is ObstacleStats)).ToArray();
        foreach (var component in protectedComponents)
        {
            result[Ref(component)] = Fields(component);
            foreach (var transform in component.GetComponentsInParent<Transform>(true))
                if (!(transform is RectTransform)) result[Ref(transform)] = transform.localPosition.ToString("R") + transform.localRotation.ToString("R") + transform.localScale.ToString("R") + Ref(transform.parent);
        }
        foreach (var skin in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SkinnedMeshRenderer>(true)))
            result[Ref(skin) + ":rig"] = Ref(skin.sharedMesh) + Ref(skin.rootBone) + string.Join(";", skin.bones.Select(b => Ref(b)));
        return result;
    }
    static void Capture(Scene scene, string file)
    {
        var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First(c => c.CompareTag("MainCamera"));
        var target = new RenderTexture(720, 1560, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var texture = new Texture2D(720, 1560, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 720, 1560), 0, 0); texture.Apply();
            File.WriteAllBytes(file, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        }
        finally { camera.targetTexture = previousTarget; RenderTexture.active = previousActive; target.Release(); Object.DestroyImmediate(target); }
    }
    public static object Main(string output, bool apply)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        Directory.CreateDirectory(output);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var rows = new List<object>();
        if (apply) { Directory.CreateDirectory(Folder); AssetDatabase.Refresh(); }
        try
        {
            int chapter = 0;
            foreach (var name in new[] { "Noryangjin_MapTool_Mode_SR18_Revamp", "HighWay", "RestStop", "Jamsil", "ShoeTower" })
            {
                chapter++;
                string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
                string backup = Path.Combine(output, name + ".before.unity");
                if (apply && !File.Exists(backup)) File.Copy(path, backup);
                var scene = EditorSceneManager.OpenScene(path);
                var before = Protected(scene);
                int changed = 0;
                var replacements = new Dictionary<Material, Material>();
                var candidates = new List<object>();
                foreach (var renderer in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)))
                {
                    var array = renderer.sharedMaterials;
                    bool edited = false;
                    for (int i = 0; i < array.Length; i++)
                    {
                        var source = array[i];
                        if (source == null || source.shader.name != "Universal Render Pipeline/Lit" || source.GetFloat("_Surface") != 0 || source.IsKeywordEnabled("_EMISSION")) continue;
                        candidates.Add(new { renderer = renderer.name, material = source.name, original = AssetDatabase.GetAssetPath(source) });
                        if (!apply) continue;
                        if (!replacements.TryGetValue(source, out var material))
                        {
                            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Only persistent source materials may be converted");
                            string materialPath = Folder + "/Ch" + chapter + "_" + guid + ".mat";
                            material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                            if (material == null)
                            {
                                var color = source.GetColor("_BaseColor");
                                var texture = source.GetTexture("_BaseMap");
                                material = new Material(source) { name = "Essential Ch" + chapter + " " + source.name, shader = Shader.Find("FlatKit/Stylized Surface"), enableInstancing = true };
                                material.shaderKeywords = Array.Empty<string>();
                                material.SetColor("_BaseColor", color);
                                material.SetColor("_ColorDim", new Color(color.r * .67f, color.g * .68f, color.b * .70f, color.a));
                                material.SetFloat("_CelPrimaryMode", 1); material.EnableKeyword("_CELPRIMARYMODE_SINGLE");
                                material.SetFloat("_SelfShadingSize", .45f); material.SetFloat("_ShadowEdgeSize", .12f); material.SetFloat("_Flatness", .8f);
                                material.SetFloat("_LightContribution", .5f); material.SetFloat("_LightFalloffSize", 1);
                                material.SetTexture("_BaseMap", texture); material.SetFloat("_TextureImpact", texture == null ? 0 : chapter == 1 ? .82f : .65f);
                                material.SetFloat("_DetailMapImpact", 0); material.SetTexture("_BumpMap", null); material.DisableKeyword("_NORMALMAP");
                                material.SetFloat("_UnityShadowMode", 1); material.EnableKeyword("_UNITYSHADOWMODE_MULTIPLY"); material.SetFloat("_UnityShadowPower", .18f);
                                AssetDatabase.CreateAsset(material, materialPath);
                            }
                            replacements[source] = material;
                        }
                        array[i] = material; edited = true; changed++;
                    }
                    if (edited) { renderer.sharedMaterials = array; EditorUtility.SetDirty(renderer); if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
                }
                if (apply)
                {
                    var now = Protected(scene);
                    if (Json(now) != Json(before)) throw new InvalidOperationException("Protected gameplay changed before save in " + name);
                    AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                    scene = EditorSceneManager.OpenScene(path);
                    if (Json(Protected(scene)) != Json(before)) throw new InvalidOperationException("Protected gameplay changed after reopen in " + name);
                }
                Capture(scene, Path.Combine(output, name + (apply ? "-after.png" : "-before.png")));
                rows.Add(new { scene = name, changedSlots = changed, clonedMaterials = replacements.Count, candidates, protectedCount = before.Count, protectedHash = Hash(Json(before)), protectedUnchanged = true, camerasAndLightsChanged = false });
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        File.WriteAllText(Path.Combine(output, apply ? "applied.json" : "before.json"), Json(rows));
        return new { scenes = rows.Count, apply, output, actualNewModels = 0 };
    }
}
