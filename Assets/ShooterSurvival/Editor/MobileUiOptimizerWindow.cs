#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public enum MobileUiAtlasGroup
{
    HudCommon,
    LobbySettingMenu,
    Upgrade
}

[Serializable]
public sealed class MobileUiOptimizationResult
{
    public int newMissingSpriteRefs;
    public int atlasPages;
    public int idempotent;
    public int visualContractPassed;
    public float estimatedMemoryMb;
    public int spritesPacked;
    public bool changed;
    public int excludedCount;
}

public sealed class MobileUiOptimizerWindow : EditorWindow
{
    public const string TargetScenePath =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";
    public const string AtlasFolderPath = "Assets/ShooterSurvival/UI/Atlases";

    private const string ReportPath = "Library/MobileUiOptimizer/latest-report.json";
    private const string MenuPath =
        "Tools/Shooter Survival/Optimization/Mobile UI Optimizer";
    private const string ButtonLabel = "UI 아틀라스 최적화 및 검증";
    private const int AtlasMaxSize = 2048;

    private static readonly Regex SpriteGuidRegex = new Regex(
        @"m_Sprite:\s*\{[^\r\n}]*guid:\s*([0-9a-fA-F]{32})",
        RegexOptions.Compiled);

    private static readonly AtlasDefinition[] AtlasDefinitions =
    {
        new AtlasDefinition(MobileUiAtlasGroup.HudCommon, "HUD_Common"),
        new AtlasDefinition(MobileUiAtlasGroup.LobbySettingMenu, "Lobby_Setting_Menu"),
        new AtlasDefinition(MobileUiAtlasGroup.Upgrade, "Upgrade")
    };

    private MobileUiOptimizationResult lastResult;

    [MenuItem(MenuPath, false, 2300)]
    public static void OpenWindow()
    {
        MobileUiOptimizerWindow window = GetWindow<MobileUiOptimizerWindow>();
        window.titleContent = new GUIContent("Mobile UI Optimizer");
        window.minSize = new Vector2(420f, 170f);
        window.Show();
    }

    [MenuItem(
        "Tools/Shooter Survival/Optimization/Run Mobile UI Optimization",
        false,
        2301)]
    public static void RunFromMenu()
    {
        MobileUiOptimizationResult result = Apply();
        Debug.Log(
            $"[MobileUiOptimizer] packed={result.spritesPacked}, " +
            $"atlases={result.atlasPages}, changed={result.changed}, " +
            $"idempotent={result.idempotent}, visual={result.visualContractPassed}");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Mobile UI Optimizer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "프로덕션 씬에서 사용하는 UI Sprite를 수집해 Android용 V2 Atlas를 갱신하고 검증합니다.",
            MessageType.Info);
        EditorGUILayout.Space(8f);

        if (GUILayout.Button(ButtonLabel, GUILayout.Height(42f)))
            lastResult = Apply();

        if (lastResult == null)
            return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(
            $"Sprites {lastResult.spritesPacked} / Atlases {lastResult.atlasPages} / " +
            $"Changed {lastResult.changed}");
    }

    public static MobileUiOptimizationResult Apply()
    {
        EnsureTargetSceneExists();

        string sceneHashBefore = ComputeFileHash(TargetScenePath);
        int missingBefore = CountMissingSpriteReferences(TargetScenePath);
        CandidateCollection collection = CollectCandidates();
        Dictionary<MobileUiAtlasGroup, List<string>> groupedPaths = GroupPaths(collection.Paths);

        bool changed = EnsureSpritePackerMode();

        EnsureAtlasFolderExists();
        foreach (AtlasDefinition definition in AtlasDefinitions)
        {
            if (ApplyAtlas(definition, groupedPaths[definition.Group]))
                changed = true;
        }

        if (changed)
            PackAndroidAtlases(groupedPaths);

        int missingAfter = CountMissingSpriteReferences(TargetScenePath);
        string sceneHashAfter = ComputeFileHash(TargetScenePath);
        bool secondAssessmentNeedsChanges = NeedsChanges(groupedPaths);
        int nonEmptyAtlasCount = groupedPaths.Count(pair => pair.Value.Count > 0);

        var result = new MobileUiOptimizationResult
        {
            newMissingSpriteRefs = Math.Max(0, missingAfter - missingBefore),
            atlasPages = nonEmptyAtlasCount,
            idempotent = secondAssessmentNeedsChanges ? 0 : 1,
            visualContractPassed =
                sceneHashBefore == sceneHashAfter && AllOwnedAtlasesValid(groupedPaths) ? 1 : 0,
            estimatedMemoryMb = nonEmptyAtlasCount * 16f,
            spritesPacked = collection.Paths.Count,
            changed = changed,
            excludedCount = collection.ExcludedCount
        };

        WriteReport(result);
        return result;
    }

    public static CandidateCollection CollectCandidates()
    {
        EnsureTargetSceneExists();

        string[] dependencies = AssetDatabase.GetDependencies(TargetScenePath, true);
        var candidates = new List<string>();
        int excludedCount = 0;

        foreach (string dependency in dependencies.OrderBy(path => path, StringComparer.Ordinal))
        {
            var importer = AssetImporter.GetAtPath(dependency) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
                continue;

            if (ShouldExclude(dependency, importer))
            {
                excludedCount++;
                continue;
            }

            if (LoadPackable(dependency) == null)
                continue;

            candidates.Add(dependency.Replace('\\', '/'));
        }

        candidates.Sort(StringComparer.Ordinal);
        return new CandidateCollection(candidates, excludedCount);
    }

    public static bool IsExcludedPath(string assetPath)
    {
        string normalized = assetPath.Replace('\\', '/').ToLowerInvariant();
        return normalized.Contains("/references/editor/") ||
               normalized.Contains("/background") ||
               normalized.Contains("_bg") ||
               normalized.Contains("background") ||
               normalized.Contains("setting_all");
    }

    public static MobileUiAtlasGroup ClassifyPath(string assetPath)
    {
        string normalized = assetPath.Replace('\\', '/').ToLowerInvariant();
        if (normalized.Contains("/upgrade/"))
            return MobileUiAtlasGroup.Upgrade;

        bool isJhAsset = normalized.Contains("/jh/") ||
                         normalized.Contains("/jh_") ||
                         normalized.Contains("/jh-");
        bool isLobbyFamily = normalized.Contains("lobby") ||
                             normalized.Contains("setting") ||
                             normalized.Contains("menu") ||
                             normalized.Contains("header") ||
                             normalized.Contains("common");
        return isJhAsset && isLobbyFamily
            ? MobileUiAtlasGroup.LobbySettingMenu
            : MobileUiAtlasGroup.HudCommon;
    }

    public static string GetAtlasPath(MobileUiAtlasGroup group)
    {
        AtlasDefinition definition = AtlasDefinitions.First(item => item.Group == group);
        return definition.Path;
    }

    public static IReadOnlyList<string> GetAtlasPackablePaths(MobileUiAtlasGroup group)
    {
        return GetCurrentPackablePaths(GetAtlasPath(group));
    }

    private static bool ApplyAtlas(AtlasDefinition definition, IReadOnlyList<string> paths)
    {
        string atlasPath = definition.Path;
        bool created = false;
        SpriteAtlasAsset atlasAsset = SpriteAtlasAsset.Load(atlasPath);
        if (atlasAsset == null)
        {
            atlasAsset = new SpriteAtlasAsset();
            SpriteAtlasAsset.Save(atlasAsset, atlasPath);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);
            created = true;
        }

        bool packablesChanged = !PathSetsEqual(GetCurrentPackablePaths(atlasPath), paths);
        if (packablesChanged)
        {
            UnityEngine.Object[] currentPackables = GetCurrentPackables(atlasPath);
            if (currentPackables.Length > 0)
                atlasAsset.Remove(currentPackables);

            UnityEngine.Object[] desiredPackables = paths
                .Select(LoadPackable)
                .Where(packable => packable != null)
                .ToArray();
            if (desiredPackables.Length > 0)
                atlasAsset.Add(desiredPackables);

            SpriteAtlasAsset.Save(atlasAsset, atlasPath);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);
        }

        var importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            throw new InvalidOperationException($"SpriteAtlasImporter not found: {atlasPath}");

        bool settingsChanged = ConfigureImporter(importer);
        if (settingsChanged)
            importer.SaveAndReimport();

        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas != null && (created || packablesChanged || settingsChanged))
            AssetDatabase.SaveAssetIfDirty(atlas);

        return created || packablesChanged || settingsChanged;
    }

    private static bool ConfigureImporter(SpriteAtlasImporter importer)
    {
        bool changed = false;

        if (!importer.includeInBuild)
        {
            importer.includeInBuild = true;
            changed = true;
        }

        SpriteAtlasPackingSettings packing = importer.packingSettings;
        if (packing.padding != 4 || packing.enableRotation || packing.enableTightPacking)
        {
            packing.padding = 4;
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            importer.packingSettings = packing;
            changed = true;
        }

        SpriteAtlasTextureSettings texture = importer.textureSettings;
        if (texture.generateMipMaps || texture.readable || !texture.sRGB ||
            texture.filterMode != FilterMode.Bilinear)
        {
            texture.generateMipMaps = false;
            texture.readable = false;
            texture.sRGB = true;
            texture.filterMode = FilterMode.Bilinear;
            importer.textureSettings = texture;
            changed = true;
        }

        TextureImporterPlatformSettings defaultSettings =
            importer.GetPlatformSettings("DefaultTexturePlatform");
        if (defaultSettings.maxTextureSize != AtlasMaxSize)
        {
            defaultSettings.name = "DefaultTexturePlatform";
            defaultSettings.maxTextureSize = AtlasMaxSize;
            importer.SetPlatformSettings(defaultSettings);
            changed = true;
        }

        TextureImporterPlatformSettings android = importer.GetPlatformSettings("Android");
        if (!IsDesiredAndroidSettings(android))
        {
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = AtlasMaxSize;
            android.format = TextureImporterFormat.Automatic;
            android.textureCompression = TextureImporterCompression.Compressed;
            android.compressionQuality = 50;
            importer.SetPlatformSettings(android);
            changed = true;
        }

        return changed;
    }

    private static bool NeedsChanges(Dictionary<MobileUiAtlasGroup, List<string>> groupedPaths)
    {
        if (!IsSpritePackerModeConfigured())
            return true;

        foreach (AtlasDefinition definition in AtlasDefinitions)
        {
            if (SpriteAtlasAsset.Load(definition.Path) == null)
                return true;
            if (!PathSetsEqual(
                    GetCurrentPackablePaths(definition.Path),
                    groupedPaths[definition.Group]))
                return true;

            var importer = AssetImporter.GetAtPath(definition.Path) as SpriteAtlasImporter;
            if (importer == null || ImporterNeedsConfiguration(importer))
                return true;
        }

        return false;
    }

    private static bool ImporterNeedsConfiguration(SpriteAtlasImporter importer)
    {
        SpriteAtlasPackingSettings packing = importer.packingSettings;
        SpriteAtlasTextureSettings texture = importer.textureSettings;
        TextureImporterPlatformSettings defaults =
            importer.GetPlatformSettings("DefaultTexturePlatform");
        TextureImporterPlatformSettings android = importer.GetPlatformSettings("Android");

        return !importer.includeInBuild ||
               packing.padding != 4 ||
               packing.enableRotation ||
               packing.enableTightPacking ||
               texture.generateMipMaps ||
               texture.readable ||
               !texture.sRGB ||
               texture.filterMode != FilterMode.Bilinear ||
               defaults.maxTextureSize != AtlasMaxSize ||
               !IsDesiredAndroidSettings(android);
    }

    private static bool EnsureSpritePackerMode()
    {
        if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            return false;

        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
        return true;
    }

    private static bool IsSpritePackerModeConfigured()
    {
        return EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2;
    }

    private static bool IsDesiredAndroidSettings(TextureImporterPlatformSettings settings)
    {
        return settings.overridden &&
               settings.maxTextureSize == AtlasMaxSize &&
               settings.format == TextureImporterFormat.Automatic &&
               settings.textureCompression == TextureImporterCompression.Compressed &&
               settings.compressionQuality == 50;
    }

    private static bool ShouldExclude(string path, TextureImporter importer)
    {
        if (IsExcludedPath(path))
            return true;

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        return width > 1024 || height > 1024;
    }

    private static Dictionary<MobileUiAtlasGroup, List<string>> GroupPaths(
        IEnumerable<string> paths)
    {
        var grouped = AtlasDefinitions.ToDictionary(
            definition => definition.Group,
            _ => new List<string>());
        foreach (string path in paths)
            grouped[ClassifyPath(path)].Add(path);
        foreach (List<string> group in grouped.Values)
            group.Sort(StringComparer.Ordinal);
        return grouped;
    }

    private static UnityEngine.Object LoadPackable(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
            return texture;

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static UnityEngine.Object[] GetCurrentPackables(string atlasPath)
    {
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        return atlas == null ? Array.Empty<UnityEngine.Object>() : atlas.GetPackables();
    }

    private static List<string> GetCurrentPackablePaths(string atlasPath)
    {
        return GetCurrentPackables(atlasPath)
            .Select(AssetDatabase.GetAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    private static bool PathSetsEqual(
        IEnumerable<string> currentPaths,
        IEnumerable<string> desiredPaths)
    {
        return currentPaths.SequenceEqual(desiredPaths, StringComparer.Ordinal);
    }

    private static void PackAndroidAtlases(
        Dictionary<MobileUiAtlasGroup, List<string>> groupedPaths)
    {
        SpriteAtlas[] atlases = AtlasDefinitions
            .Where(definition => groupedPaths[definition.Group].Count > 0)
            .Select(definition =>
                AssetDatabase.LoadAssetAtPath<SpriteAtlas>(definition.Path))
            .Where(atlas => atlas != null)
            .ToArray();
        if (atlases.Length > 0)
            SpriteAtlasUtility.PackAtlases(atlases, BuildTarget.Android);
    }

    private static bool AllOwnedAtlasesValid(
        Dictionary<MobileUiAtlasGroup, List<string>> groupedPaths)
    {
        foreach (AtlasDefinition definition in AtlasDefinitions)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(definition.Path);
            var importer = AssetImporter.GetAtPath(definition.Path) as SpriteAtlasImporter;
            if (atlas == null || importer == null || !importer.includeInBuild)
                return false;
        }

        return true;
    }

    private static int CountMissingSpriteReferences(string scenePath)
    {
        string sceneText = File.ReadAllText(scenePath);
        int missing = 0;
        foreach (Match match in SpriteGuidRegex.Matches(sceneText))
        {
            string guid = match.Groups[1].Value;
            if (guid == "00000000000000000000000000000000")
                continue;
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                missing++;
        }

        return missing;
    }

    private static string ComputeFileHash(string assetPath)
    {
        using SHA256 sha = SHA256.Create();
        byte[] bytes = File.ReadAllBytes(assetPath);
        return BitConverter.ToString(sha.ComputeHash(bytes))
            .Replace("-", string.Empty);
    }

    private static void EnsureTargetSceneExists()
    {
        if (!File.Exists(TargetScenePath))
            throw new FileNotFoundException("Production scene was not found.", TargetScenePath);
    }

    private static void EnsureAtlasFolderExists()
    {
        string current = "Assets";
        foreach (string segment in AtlasFolderPath.Split('/').Skip(1))
        {
            string next = current + "/" + segment;
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segment);
            current = next;
        }
    }

    private static void WriteReport(MobileUiOptimizationResult result)
    {
        string directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(
            ReportPath,
            JsonUtility.ToJson(result, true) + Environment.NewLine,
            new UTF8Encoding(false));
    }

    public sealed class CandidateCollection
    {
        public CandidateCollection(List<string> paths, int excludedCount)
        {
            Paths = paths;
            ExcludedCount = excludedCount;
        }

        public List<string> Paths { get; }
        public int ExcludedCount { get; }
    }

    private readonly struct AtlasDefinition
    {
        public AtlasDefinition(MobileUiAtlasGroup group, string name)
        {
            Group = group;
            Path = $"{AtlasFolderPath}/{name}.spriteatlasv2";
        }

        public MobileUiAtlasGroup Group { get; }
        public string Path { get; }
    }
}
#endif
