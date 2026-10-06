using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Essential proposal 14 (2026-10-06): "broken / stained human textures" (photo 3).
// Cause found: NoryangjinClaudeFeedbackBuilder.OutlineCharacters cached outline materials by texture
// *name*. Every Meshy/TRELLIS atlas is called "texture_0", so all characters it touched shared
// Outline_texture_0.mat, which holds the N13 male merchant atlas. Other meshes (N14 female body,
// Enemy_Woman) sampled that atlas through their own fragmented UVs and showed the camouflage patches.
// This repair gives each affected skinned renderer an outline material with the atlas from its own
// model folder. Geometry, bones, animators and colliders are untouched.
public static class CharacterTextureMismatchRepair
{
    public const string OutputDir = "Assets/ShooterSurvival/Models/Generated/CharacterTextureRepair";
    public static readonly string[] ChapterScenes = { "Noryangjin_MapTool_Mode_SR18_Revamp", "HighWay", "RestStop", "Jamsil", "ShoeTower" };

    public sealed class Finding
    {
        public string path, mesh, material, wrongTexture, ownTexture;
        public override string ToString() => $"{path} | mesh={mesh} | mat={material} | has={wrongTexture} | own={ownTexture}";
    }

    // Atlas textures are per-model files such as texture_0.png next to the mesh.
    public static bool IsPerModelAtlas(string textureName) => textureName != null && textureName.StartsWith("texture_", StringComparison.Ordinal);

    public static string OwnAtlasPath(string meshPath, string textureFileName)
    {
        if (string.IsNullOrEmpty(meshPath) || string.IsNullOrEmpty(textureFileName)) return null;
        string candidate = (Path.GetDirectoryName(meshPath) + "/" + textureFileName).Replace('\\', '/');
        return File.Exists(candidate) ? candidate : null;
    }

    public static List<Finding> Scan(Scene scene)
    {
        var findings = new List<Finding>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh; if (mesh == null) continue;
                string meshPath = AssetDatabase.GetAssetPath(mesh);
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || !material.HasProperty("_BaseMap")) continue;
                    var texture = material.GetTexture("_BaseMap");
                    if (texture == null || !IsPerModelAtlas(texture.name)) continue;
                    string texturePath = AssetDatabase.GetAssetPath(texture);
                    string own = OwnAtlasPath(meshPath, Path.GetFileName(texturePath));
                    if (own == null || own == texturePath) continue;
                    findings.Add(new Finding { path = AnimationUtility.CalculateTransformPath(renderer.transform, null), mesh = meshPath, material = AssetDatabase.GetAssetPath(material), wrongTexture = texturePath, ownTexture = own });
                }
            }
        return findings;
    }

    public static string ScanScene(string name)
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
            var findings = Scan(scene);
            return name + ": " + findings.Count + "\n" + string.Join("\n", findings);
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    public static string RepairScene(string name)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Clean Edit Mode required");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
            int fixedCount = Repair(scene);
            if (fixedCount > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); }
            return $"{name}: repaired {fixedCount} material slots; remaining {Scan(scene).Count}";
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    public static int Repair(Scene scene)
    {
        Directory.CreateDirectory(OutputDir);
        int repaired = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh; if (mesh == null) continue;
                string meshPath = AssetDatabase.GetAssetPath(mesh);
                var materials = renderer.sharedMaterials; bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.HasProperty("_BaseMap")) continue;
                    var texture = material.GetTexture("_BaseMap");
                    if (texture == null || !IsPerModelAtlas(texture.name)) continue;
                    string texturePath = AssetDatabase.GetAssetPath(texture);
                    string own = OwnAtlasPath(meshPath, Path.GetFileName(texturePath));
                    if (own == null || own == texturePath) continue;
                    materials[i] = MaterialFor(material, AssetDatabase.LoadAssetAtPath<Texture2D>(own), own);
                    changed = true; repaired++;
                }
                if (!changed) continue;
                Undo.RecordObject(renderer, "Repair character atlas");
                renderer.sharedMaterials = materials;
                if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
            }
        return repaired;
    }

    // One material per (source material, atlas file) pair, keyed by the atlas GUID — never by name.
    private static Material MaterialFor(Material source, Texture2D atlas, string atlasPath)
    {
        string guid = AssetDatabase.AssetPathToGUID(atlasPath);
        string folder = Path.GetFileName(Path.GetDirectoryName(atlasPath));
        string path = $"{OutputDir}/{source.name}__{folder}_{guid.Substring(0, 8)}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(source) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetTexture("_BaseMap", atlas);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", atlas);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
}
