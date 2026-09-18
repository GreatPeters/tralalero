using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class HarborRefinementFontBuilder
{
    public const string AssetPath="Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset";
    public static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
    public static object Build()
    {
        string path=AssetDatabase.FindAssets("GmarketSansTTFBold t:Font").Select(AssetDatabase.GUIDToAssetPath).First();
        var source=AssetDatabase.LoadAssetAtPath<Font>(path);var font=Font;
        if(font==null)
        {
            font=TMP_FontAsset.CreateFontAsset(source,72,9,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            font.name="Gmarket Harbor SDF";var material=font.material;var textures=font.atlasTextures;
            AssetDatabase.CreateAsset(font,AssetPath);AssetDatabase.AddObjectToAsset(material,font);
            foreach(var texture in textures)AssetDatabase.AddObjectToAsset(texture,font);
            font.material=material;font.atlasTextures=textures;
        }
        string labels=string.Concat(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(t=>t.text));
        labels+=string.Concat(Directory.GetFiles("Assets/ShooterSurvival/Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));
        var needed=new string(labels.Where(c=>c>=32&&c<=126||c>='가'&&c<='힣'||"→←·%+−".Contains(c)).Distinct().ToArray());
        var additions=new string(needed.Where(c=>!font.HasCharacter(c)).ToArray());
        if(additions.Length>0)font.TryAddCharacters(additions,out _);
        string missing=new string(needed.Where(c=>!font.HasCharacter(c)).ToArray());if(!string.IsNullOrEmpty(missing))throw new InvalidOperationException("Missing glyphs: "+missing);
        font.material.SetFloat("_FaceDilate",0);font.material.SetFloat("_OutlineWidth",0);font.material.DisableKeyword("UNDERLAY_ON");
        var data=new SerializedObject(font);data.FindProperty("m_ClearDynamicDataOnBuild").boolValue=false;data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(font);EditorUtility.SetDirty(font.material);foreach(var atlas in font.atlasTextures)EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssets();
        return new{path=AssetPath,source=path,characters=font.characterTable.Count,atlasCount=font.atlasTextures.Length};
    }
}
