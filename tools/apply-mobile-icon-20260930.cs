using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class ApplyMobileIcon20260930
{
    public static object Main(string source)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Idle editor required.");
        const string folder = "Assets/ShooterSurvival/UI/AppIcon20260930";
        const string path = folder + "/TralaleroShooter-AppIcon.png";
        if (File.Exists(path)) throw new InvalidOperationException("Preserve existing icon artifact.");
        Directory.CreateDirectory("tmp/mobile-icon-build-20260930");
        File.Copy("ProjectSettings/ProjectSettings.asset", "tmp/mobile-icon-build-20260930/ProjectSettings.before-icon.asset", false);
        Directory.CreateDirectory(folder);
        File.Copy(source, path, false);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = false;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null || texture.width != texture.height || texture.width < 1024)
            throw new InvalidOperationException("Square icon >=1024px required.");
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] {texture}, IconKind.Any);
        int slots = 0;
        foreach (var kind in PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.Android))
        {
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (var icon in icons)
            {
                icon.SetTextures(Enumerable.Repeat(texture, icon.minLayerCount).ToArray());
                slots++;
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }
        AssetDatabase.SaveAssets();
        return new {path, width=texture.width, height=texture.height, slots};
    }
}
