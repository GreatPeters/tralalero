using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CosmeticIconRenderer
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        const string folder="Assets/ShooterSurvival/Resources/Cosmetics/Icons";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var catalog=AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>("Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset");
        var go=new GameObject("Cosmetic thumbnail renderer");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;
        int count=0;
        try
        {
            foreach(var row in CosmeticTables.Rows)
            {
                string skin=row.slot==CosmeticSlot.Skin?row.visualKey:"skin_original";
                string shoes=row.slot==CosmeticSlot.Shoes?row.visualKey:"shoes_original";
                string hat=row.slot==CosmeticSlot.Hat?row.visualKey:"hat_none";
                preview.Show(skin,shoes,hat);
                var old=RenderTexture.active;RenderTexture.active=preview.Texture;
                var image=new Texture2D(512,512,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();
                string path=folder+"/"+row.id+".png";File.WriteAllBytes(path,image.EncodeToPNG());
                RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=256;importer.SaveAndReimport();
                catalog.Find(row.visualKey).icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);count++;
            }
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        finally {preview.Dispose();UnityEngine.Object.DestroyImmediate(go);}
        return new{icons=count};
    }
}
