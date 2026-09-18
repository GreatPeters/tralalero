using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MobileBuildSizeAudit
{
    [Serializable] private sealed class AssetRow
    {
        public string path, type;
        public long bytes;
    }
    [Serializable] private sealed class TextureReference
    {
        public string material, shader, property, texture;
        public long packedTextureBytes;
    }
    [Serializable] private sealed class FontRow
    {
        public string path, family, population, source;
        public int characters;
        public string[] atlases, fallbacks;
    }
    [Serializable] private sealed class Report
    {
        public string output;
        public long apkBytes;
        public AssetRow[] assets;
        public TextureReference[] unusedTextureReferences;
        public FontRow[] fonts;
    }

    public static object Run(string output = "map-concepts/mobile-presentation-2026-09-14/baseline/build-content-v2.json")
    {
        if (File.Exists(output)) throw new InvalidOperationException("Preserve the baseline.");
        var build = BuildReport.GetLatestReport();
        if (build == null) throw new InvalidOperationException("An actual build report is required.");
        var assets = build.packedAssets.SelectMany(p => p.contents)
            .GroupBy(a => a.sourceAssetPath ?? "")
            .Select(g => new AssetRow
            {
                path = g.Key,
                type = string.IsNullOrEmpty(g.Key) ? "Builtin" : AssetDatabase.GetMainAssetTypeAtPath(g.Key)?.Name ?? "Unknown",
                bytes = g.Sum(a => (long)a.packedSize)
            }).OrderByDescending(a => a.bytes).ToArray();
        var sizes = assets.ToDictionary(a => a.path, a => a.bytes);
        var stale = new List<TextureReference>();
        var fonts = new List<FontRow>();
        foreach (var row in assets)
        {
            if (row.type == nameof(Material))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(row.path);
                if (material == null || material.shader == null) continue;
                var saved = new SerializedObject(material).FindProperty("m_SavedProperties.m_TexEnvs");
                if (saved == null) continue;
                for (int i = 0; i < saved.arraySize; i++)
                {
                    var entry = saved.GetArrayElementAtIndex(i);
                    string name = entry.FindPropertyRelative("first").stringValue;
                    var texture = entry.FindPropertyRelative("second").FindPropertyRelative("m_Texture").objectReferenceValue;
                    if (texture == null || material.HasProperty(name)) continue;
                    string path = AssetDatabase.GetAssetPath(texture);
                    stale.Add(new TextureReference { material = row.path, shader = material.shader.name,
                        property = name, texture = path, packedTextureBytes = sizes.TryGetValue(path, out long bytes) ? bytes : 0 });
                }
            }
            if (row.type == nameof(TMP_FontAsset))
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(row.path);
                if (font == null) continue;
                fonts.Add(new FontRow
                {
                    path = row.path, family = font.faceInfo.familyName, population = font.atlasPopulationMode.ToString(),
                    source = AssetDatabase.GetAssetPath(font.sourceFontFile), characters = font.characterTable.Count,
                    atlases = font.atlasTextures.Where(t => t != null).Select(t => t.width + "x" + t.height + " " + t.format).ToArray(),
                    fallbacks = font.fallbackFontAssetTable.Where(f => f != null).Select(AssetDatabase.GetAssetPath).ToArray()
                });
            }
        }
        var report = new Report
        {
            output = build.summary.outputPath,
            apkBytes = new FileInfo(build.summary.outputPath).Length,
            assets = assets, unusedTextureReferences = stale.ToArray(), fonts = fonts.ToArray()
        };
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        // JsonUtility omitted arrays of nested types from this ephemeral assembly.
        // Select the canonical assembly because a localization package embeds a second JsonConvert.
        var jsonType = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
            .GetType("Newtonsoft.Json.JsonConvert");
        string json = (string)jsonType.GetMethod("SerializeObject", new[] { typeof(object) })
            .Invoke(null, new object[] { report });
        File.WriteAllText(output, json);
        return new { output, assets = assets.Length, unusedTextureReferences = stale.Count,
            staleUniqueTextures = stale.Select(s => s.texture).Distinct().Count(), fonts = fonts.Count };
    }
}
