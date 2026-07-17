#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MeshyAiAssetRepairer
{
    private const string ModelsRoot = "Assets/ShooterSurvival/Models/MeshyAI";
    private const string TexturesRoot = "Assets/ShooterSurvival/Textures/MeshyAI";
    private const string MaterialsRoot = "Assets/ShooterSurvival/Materials/MeshyAI";
    private const string PrefabsRoot = "Assets/ShooterSurvival/Prefabs/MeshyAI";
    private const string RequestPath = "Temp/MeshyAiAssetRepairRequest.txt";
    private const string ReportPath = "Temp/MeshyAiAssetRepairReport.txt";

    private static readonly string[] TextureKinds =
    {
        "BaseColor",
        "Normal",
        "Metallic",
        "Roughness",
        "Emission"
    };

    [InitializeOnLoadMethod]
    private static void RunRequestedRepair()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestPath))
                return;

            File.Delete(RequestPath);

            try
            {
                RepairAll();
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, "Failed: " + ex);
                Debug.LogException(ex);
            }
        };
    }

    [MenuItem("Tools/MeshyAI/Repair Materials And Prefabs", false, 2300)]
    public static void RepairAll()
    {
        AssetDatabase.StartAssetEditing();

        var report = new RepairReport();

        try
        {
            EnsureFolder(TexturesRoot);
            EnsureFolder(MaterialsRoot);
            EnsureFolder(PrefabsRoot);

            SortedSet<string> fbxPaths = FindRepairTargetFbxPaths();

            foreach (string fbxPath in fbxPaths)
            {
                RepairAsset(fbxPath, report);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        WriteReport(report);

        Debug.Log(
            $"[MeshyAI] Repair complete. models={report.ModelsProcessed}, " +
            $"materials={report.MaterialsCreatedOrUpdated}, prefabs={report.PrefabsCreatedOrUpdated}, " +
            $"copiedTextures={report.TexturesCopied}, missingTextureSets={report.MissingTextureSets}. " +
            $"Report: {ReportPath}");
    }

    private static bool IsRepairTarget(string assetPath)
    {
        if (!assetPath.StartsWith(ModelsRoot + "/", StringComparison.Ordinal))
            return false;

        if (!assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            return false;

        string relative = assetPath.Substring(ModelsRoot.Length + 1);
        string[] parts = relative.Split('/');

        foreach (string part in parts)
        {
            if (part.StartsWith("_", StringComparison.Ordinal))
                return false;
        }

        return parts.Length >= 3;
    }

    private static SortedSet<string> FindRepairTargetFbxPaths()
    {
        var fbxPaths = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsRoot }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (IsRepairTarget(assetPath))
                fbxPaths.Add(assetPath);
        }

        if (Directory.Exists(ModelsRoot))
        {
            foreach (string filePath in Directory.GetFiles(ModelsRoot, "*.fbx", SearchOption.AllDirectories))
            {
                string assetPath = filePath.Replace('\\', '/');
                if (!IsRepairTarget(assetPath))
                    continue;

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                fbxPaths.Add(assetPath);
            }
        }

        return fbxPaths;
    }

    private static void RepairAsset(string fbxPath, RepairReport report)
    {
        string assetDirectory = Path.GetDirectoryName(fbxPath).Replace('\\', '/');
        string folderName = Path.GetFileName(assetDirectory);
        string stageName = GetStageName(fbxPath);

        string textureFolder = $"{TexturesRoot}/{stageName}/{folderName}";
        string materialFolder = $"{MaterialsRoot}/{stageName}/{folderName}";
        string prefabFolder = $"{PrefabsRoot}/{stageName}/{folderName}";

        EnsureFolder(textureFolder);
        EnsureFolder(materialFolder);
        EnsureFolder(prefabFolder);

        var textures = CopyAndLoadTextures(assetDirectory, textureFolder, folderName, report);
        Material material = CreateOrUpdateMaterial(materialFolder, folderName, textures, report);
        CreateOrUpdatePrefab(fbxPath, prefabFolder, folderName, material, report);

        report.ModelsProcessed++;
    }

    private static string GetStageName(string fbxPath)
    {
        string relative = fbxPath.Substring(ModelsRoot.Length + 1);
        int slash = relative.IndexOf('/');
        return slash < 0 ? relative : relative.Substring(0, slash);
    }

    private static Dictionary<string, Texture2D> CopyAndLoadTextures(
        string sourceFolder,
        string destinationFolder,
        string assetName,
        RepairReport report)
    {
        var textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        foreach (string kind in TextureKinds)
        {
            string sourcePath = $"{sourceFolder}/{assetName}_{kind}.png";
            if (!File.Exists(sourcePath))
            {
                if (kind != "Emission")
                    report.MissingTextureSets++;

                continue;
            }

            string destinationPath = $"{destinationFolder}/{assetName}_{kind}.png";
            if (!File.Exists(destinationPath))
            {
                if (AssetDatabase.CopyAsset(sourcePath, destinationPath))
                    report.TexturesCopied++;
                else
                    Debug.LogWarning($"[MeshyAI] Could not copy texture: {sourcePath} -> {destinationPath}");
            }

            ConfigureTextureImporter(destinationPath, kind);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(destinationPath);
            if (texture != null)
                textures[kind] = texture;
        }

        return textures;
    }

    private static void ConfigureTextureImporter(string texturePath, string kind)
    {
        if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
            return;

        bool changed = false;
        TextureImporterType textureType = kind == "Normal"
            ? TextureImporterType.NormalMap
            : TextureImporterType.Default;
        bool sRgb = kind == "BaseColor" || kind == "Emission";

        if (importer.textureType != textureType)
        {
            importer.textureType = textureType;
            changed = true;
        }

        if (importer.sRGBTexture != sRgb)
        {
            importer.sRGBTexture = sRgb;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial(
        string materialFolder,
        string assetName,
        IReadOnlyDictionary<string, Texture2D> textures,
        RepairReport report)
    {
        string materialPath = $"{materialFolder}/{assetName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        SetTexture(material, "_BaseMap", "_MainTex", textures, "BaseColor");
        SetTexture(material, "_BumpMap", null, textures, "Normal");
        SetTexture(material, "_MetallicGlossMap", null, textures, "Metallic");
        SetTexture(material, "_EmissionMap", null, textures, "Emission");

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", textures.ContainsKey("Metallic") ? 1f : 0f);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", textures.ContainsKey("Roughness") ? 0.35f : 0.5f);

        if (textures.ContainsKey("Normal"))
            material.EnableKeyword("_NORMALMAP");
        else
            material.DisableKeyword("_NORMALMAP");

        if (textures.ContainsKey("Metallic"))
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
        else
            material.DisableKeyword("_METALLICSPECGLOSSMAP");

        if (textures.ContainsKey("Emission"))
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.white);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(material);
        report.MaterialsCreatedOrUpdated++;
        return material;
    }

    private static void SetTexture(
        Material material,
        string primaryProperty,
        string fallbackProperty,
        IReadOnlyDictionary<string, Texture2D> textures,
        string textureKind)
    {
        if (!textures.TryGetValue(textureKind, out Texture2D texture))
            return;

        if (material.HasProperty(primaryProperty))
        {
            material.SetTexture(primaryProperty, texture);
            return;
        }

        if (!string.IsNullOrEmpty(fallbackProperty) && material.HasProperty(fallbackProperty))
            material.SetTexture(fallbackProperty, texture);
    }

    private static void CreateOrUpdatePrefab(
        string fbxPath,
        string prefabFolder,
        string assetName,
        Material material,
        RepairReport report)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (source == null)
        {
            Debug.LogWarning($"[MeshyAI] Could not load FBX as GameObject: {fbxPath}");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(source);

        instance.name = assetName;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            var assigned = renderer.sharedMaterials;
            for (int i = 0; i < assigned.Length; i++)
                assigned[i] = material;

            renderer.sharedMaterials = assigned;
        }

        string prefabPath = $"{prefabFolder}/{assetName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool success);
        UnityEngine.Object.DestroyImmediate(instance);

        if (!success)
        {
            Debug.LogWarning($"[MeshyAI] Could not save prefab: {prefabPath}");
            return;
        }

        report.PrefabsCreatedOrUpdated++;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        string folder = Path.GetFileName(assetPath);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void WriteReport(RepairReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, report.ToText());
    }

    private sealed class RepairReport
    {
        public int ModelsProcessed;
        public int TexturesCopied;
        public int MaterialsCreatedOrUpdated;
        public int PrefabsCreatedOrUpdated;
        public int MissingTextureSets;

        public string ToText()
        {
            return
                $"ModelsProcessed: {ModelsProcessed}\n" +
                $"TexturesCopied: {TexturesCopied}\n" +
                $"MaterialsCreatedOrUpdated: {MaterialsCreatedOrUpdated}\n" +
                $"PrefabsCreatedOrUpdated: {PrefabsCreatedOrUpdated}\n" +
                $"MissingTextureSets: {MissingTextureSets}\n";
        }
    }
}
#endif
