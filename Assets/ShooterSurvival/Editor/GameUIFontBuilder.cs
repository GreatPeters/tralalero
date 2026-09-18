using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class GameUIFontBuilder
{
    public const string Path = "Assets/ShooterSurvival/Resources/UI/JigmoGameUI SDF.asset";
    public static object Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        const string sourcePath = "Assets/ShooterSurvival/Fonts/JigmoGameUI/JigmoGameUI.ttf";
        AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
        var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (source == null) throw new InvalidOperationException("CC0 source font has not imported.");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path);
        if (font != null && (font.material == null || font.atlasTextures.Any(t => t == null)))
        {
            AssetDatabase.DeleteAsset(Path); font = null;
        }
        if (font == null)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            font = TMP_FontAsset.CreateFontAsset(source, 72, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = "JigmoGameUI SDF";
            var material = font.material;
            var atlases = font.atlasTextures;
            AssetDatabase.CreateAsset(font, Path);
            material.name = "JigmoGameUI SDF Material";
            AssetDatabase.AddObjectToAsset(material, font);
            foreach (var texture in atlases) AssetDatabase.AddObjectToAsset(texture, font);
            font.material = material; font.atlasTextures = atlases;
        }
        string text = string.Concat(Enumerable.Range(32,95).Select(c=>(char)c)) +
            "이야기훔친신발두발과꼬리끝에붙었다아무리벗으려해도떨어지지않았다신단항구빠져나갔다저주풀려면인간세상더좋은찾아바쳐야한다고속도로휴게소노량진도전결과생존획득코인진행보상포함광고보고추가받기돌아가기다음장면다시보기건너뛰기체력공격레벨장비피부신발모자강화착용중보유구매개인정보설정준비불러오는새길출구정원매점거리야외식사공간전기차충전물류정비구역주차장";
        string needed = new string(text.Distinct().Where(c => !font.HasCharacter(c)).ToArray());
        if (needed.Length > 0) font.TryAddCharacters(needed, out _);
        string missing = new string(text.Distinct().Where(c => !font.HasCharacter(c)).ToArray());
        if (!string.IsNullOrEmpty(missing)) throw new InvalidOperationException("Missing UI glyphs: " + missing);
        font.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
        font.fallbackFontAssetTable.Clear();
        font.material.SetTexture("_MainTex",font.atlasTextures[0]);
        var data = new SerializedObject(font); data.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(font); EditorUtility.SetDirty(font.material);
        foreach (var texture in font.atlasTextures) EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();
        return new { path=Path, characters=font.characterTable.Count, atlasCount=font.atlasTextures.Length, license="CC0-1.0" };
    }
}
