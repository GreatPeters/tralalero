using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CosmeticPresentationBuilder
{
    public static object BuildShops()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        foreach(string name in new[]{"Noryangjin_MapTool_Mode","Noryangjin_MapTool_Mode_SR18","HighWay"})
        {
            string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                RefinedGameUI.BuildShop(scene.GetRootGameObjects().Single(g=>g.name=="Canvas"));
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        return new{scenes=3,columns=2};
    }
    public static object CaptureFitting()
    {
        const string folder="map-concepts/skins-reststop-2026-09-12/fitting-v3";
        Directory.CreateDirectory(folder);
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        var go=new GameObject("Cosmetic fitting capture");var preview=go.AddComponent<CosmeticPreview>();preview.catalog=catalog;
        int count=0;
        try
        {
            foreach(string key in new[]{"shoes_original","shoes_gold","shoes_mint","shoes_relic","shoes_ruby"})
            {
                preview.Show("skin_original",key,"hat_none");preview.ResetView();Save(preview.Texture,folder+"/"+key+"-front.png");
                preview.Rotate(125);Save(preview.Texture,folder+"/"+key+"-rear.png");count++;
            }
            File.WriteAllText(folder+"/mounts.json",JsonUtility.ToJson(catalog,true));
        }
        finally{preview.Dispose();UnityEngine.Object.DestroyImmediate(go);}
        return new{count};
    }
    public static void Save(RenderTexture target,string path)
    {
        var old=RenderTexture.active;Texture2D image=null;
        try
        {
            RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally{RenderTexture.active=old;if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
}
