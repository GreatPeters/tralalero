#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

public sealed class MobileUiOptimizerTests
{
    [Test]
    public void ClassificationAndExclusions_AreConservativeAndDeterministic()
    {
        Assert.That(
            MobileUiOptimizerWindow.ClassifyPath("Assets/UI/Upgrade/icon.png"),
            Is.EqualTo(MobileUiAtlasGroup.Upgrade));
        Assert.That(
            MobileUiOptimizerWindow.ClassifyPath("Assets/UI/JH/Lobby/header.png"),
            Is.EqualTo(MobileUiAtlasGroup.LobbySettingMenu));
        Assert.That(
            MobileUiOptimizerWindow.ClassifyPath("Assets/UI/HUD/health.png"),
            Is.EqualTo(MobileUiAtlasGroup.HudCommon));

        Assert.That(
            MobileUiOptimizerWindow.IsExcludedPath(
                "Assets/UI/References/Editor/layout.png"),
            Is.True);
        Assert.That(
            MobileUiOptimizerWindow.IsExcludedPath("Assets/UI/Stage_Background.png"),
            Is.True);
        Assert.That(
            MobileUiOptimizerWindow.IsExcludedPath("Assets/UI/setting_all.png"),
            Is.True);
        Assert.That(
            MobileUiOptimizerWindow.IsExcludedPath("Assets/UI/HUD/health.png"),
            Is.False);
    }

    [Test]
    public void CandidateCollection_ContainsOnlyEligibleProductionSprites()
    {
        MobileUiOptimizerWindow.CandidateCollection collection =
            MobileUiOptimizerWindow.CollectCandidates();

        Assert.That(collection.Paths, Is.Not.Empty);
        Assert.That(collection.Paths, Is.Ordered.Using<string>(StringComparer.Ordinal));
        Assert.That(collection.Paths, Has.All.Not.Contains("/References/Editor/"));
        Assert.That(
            collection.Paths.Any(MobileUiOptimizerWindow.IsExcludedPath),
            Is.False);

        foreach (string path in collection.Paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            Assert.That(width, Is.LessThanOrEqualTo(1024), path);
            Assert.That(height, Is.LessThanOrEqualTo(1024), path);
        }
    }

    [Test]
    public void Apply_CreatesConfiguredAtlasesWithoutTouchingSceneOrRendering()
    {
        string sceneHashBefore = HashFile(MobileUiOptimizerWindow.TargetScenePath);
        Dictionary<string, int> msaaBefore = LoadPipelineMsaa();

        MobileUiOptimizationResult result = MobileUiOptimizerWindow.Apply();

        Assert.That(result.atlasPages, Is.InRange(1, 3));
        Assert.That(result.spritesPacked, Is.GreaterThan(0));
        Assert.That(result.newMissingSpriteRefs, Is.Zero);
        Assert.That(result.visualContractPassed, Is.EqualTo(1));
        Assert.That(
            EditorSettings.spritePackerMode,
            Is.Not.EqualTo(SpritePackerMode.Disabled));
        StringAssert.Contains(
            "m_SpritePackerMode: 5",
            File.ReadAllText("ProjectSettings/EditorSettings.asset"));
        Assert.That(
            HashFile(MobileUiOptimizerWindow.TargetScenePath),
            Is.EqualTo(sceneHashBefore));
        Assert.That(LoadPipelineMsaa(), Is.EqualTo(msaaBefore));

        var allPaths = new List<string>();
        foreach (MobileUiAtlasGroup group in Enum.GetValues(typeof(MobileUiAtlasGroup)))
        {
            string atlasPath = MobileUiOptimizerWindow.GetAtlasPath(group);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            var importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
            Assert.That(atlas, Is.Not.Null, atlasPath);
            Assert.That(importer, Is.Not.Null, atlasPath);
            Assert.That(importer.includeInBuild, Is.True, atlasPath);
            Assert.That(importer.packingSettings.padding, Is.EqualTo(4), atlasPath);
            Assert.That(importer.packingSettings.enableRotation, Is.False, atlasPath);
            Assert.That(importer.packingSettings.enableTightPacking, Is.False, atlasPath);
            Assert.That(importer.textureSettings.generateMipMaps, Is.False, atlasPath);
            Assert.That(importer.textureSettings.readable, Is.False, atlasPath);
            Assert.That(importer.textureSettings.sRGB, Is.True, atlasPath);
            Assert.That(
                importer.textureSettings.filterMode,
                Is.EqualTo(FilterMode.Bilinear),
                atlasPath);

            TextureImporterPlatformSettings android =
                importer.GetPlatformSettings("Android");
            Assert.That(android.overridden, Is.True, atlasPath);
            Assert.That(android.maxTextureSize, Is.EqualTo(2048), atlasPath);
            Assert.That(
                android.textureCompression,
                Is.EqualTo(TextureImporterCompression.Compressed),
                atlasPath);
            Assert.That(android.compressionQuality, Is.EqualTo(50), atlasPath);

            allPaths.AddRange(MobileUiOptimizerWindow.GetAtlasPackablePaths(group));
        }

        Assert.That(allPaths, Is.Not.Empty);
        Assert.That(allPaths.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(allPaths.Count));
    }

    [Test]
    public void Apply_SecondInvocationIsIdempotent()
    {
        MobileUiOptimizerWindow.Apply();
        MobileUiOptimizationResult second = MobileUiOptimizerWindow.Apply();

        Assert.That(second.changed, Is.False);
        Assert.That(second.idempotent, Is.EqualTo(1));
        Assert.That(second.visualContractPassed, Is.EqualTo(1));
    }

    private static Dictionary<string, int> LoadPipelineMsaa()
    {
        var values = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UniversalRenderPipelineAsset asset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset != null)
                values[path] = asset.msaaSampleCount;
        }

        return values;
    }

    private static string HashFile(string path)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty);
    }
}
#endif
