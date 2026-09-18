using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MobileFontMigration
{
    public static readonly string[] Scenes = { "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity", "Assets/ShooterSurvival/Scenes/Tools/HighWay.unity", "Assets/ShooterSurvival/Scenes/Tools/RestStop.unity" };
    public static void Backup(string path)
    {
        string target = "tmp/backups/mobile-presentation-2026-09-14/assets/" + path;
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        if (!File.Exists(target)) File.Copy(path, target);
        if (File.Exists(path + ".meta") && !File.Exists(target + ".meta")) File.Copy(path + ".meta", target + ".meta");
    }
    public static int ApplyRoot(GameObject root)
    {
        var font = MobileUIArt.Font; int changed = 0;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font == font && text.fontSharedMaterial == font.material) continue;
            text.font = font; text.fontSharedMaterial = font.material; text.fontStyle = FontStyles.Normal;
            EditorUtility.SetDirty(text);
            if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            changed++;
        }
        foreach (var sub in root.GetComponentsInChildren<TMP_SubMeshUI>(true))
            if (sub.fontAsset != null && sub.fontAsset != font) { sub.fontAsset = font; sub.sharedMaterial = font.material; EditorUtility.SetDirty(sub); changed++; }
        foreach (var sub in root.GetComponentsInChildren<TMP_SubMesh>(true))
            if (sub.fontAsset != null && sub.fontAsset != font) { sub.fontAsset = font; sub.sharedMaterial = font.material; EditorUtility.SetDirty(sub); changed++; }
        var legacy = AssetDatabase.LoadAssetAtPath<Font>("Assets/JH/Font/KERISKEDU_B.ttf");
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            if (text.font != legacy) { text.font = legacy; text.fontStyle = FontStyle.Normal; EditorUtility.SetDirty(text); changed++; }
        return changed;
    }
    public static object MigrateDependencies()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var prefabs = AssetDatabase.GetDependencies(Scenes, true).Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            .Concat(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ShooterSurvival/Resources" }).Select(AssetDatabase.GUIDToAssetPath)).Distinct().ToArray();
        var changed = new List<string>(); int labels = 0;
        foreach (string path in prefabs)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source.GetComponentsInChildren<TMP_Text>(true).Length + source.GetComponentsInChildren<Text>(true).Length == 0) continue;
            Backup(path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int count = ApplyRoot(root); if (count == 0) continue;
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new IOException("Font prefab save failed: " + path);
                labels += count; changed.Add(path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        string settingsPath = AssetDatabase.FindAssets("t:TMP_Settings").Select(AssetDatabase.GUIDToAssetPath).Single();
        Backup(settingsPath);
        var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath));
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = MobileUIArt.Font;
        settings.FindProperty("m_fallbackFontAssets").ClearArray();
        settings.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
        // Move unused Resources fonts out of automatic inclusion while retaining their source and GUIDs.
        foreach (string path in new[] {
            "Assets/ShooterSurvival/Resources/UI/JigmoGameUI SDF.asset",
            "Assets/ShooterSurvival/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
            "Assets/ShooterSurvival/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" })
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null) continue;
            Backup(path); const string archive = "Assets/ShooterSurvival/Fonts/LegacyArchive";
            Directory.CreateDirectory(archive); AssetDatabase.Refresh();
            string result = AssetDatabase.MoveAsset(path, archive + "/" + Path.GetFileName(path));
            if (!string.IsNullOrEmpty(result)) throw new IOException(result);
        }
        return new { prefabs = changed.ToArray(), labels, font = MobileUIArt.Font.name };
    }
}
