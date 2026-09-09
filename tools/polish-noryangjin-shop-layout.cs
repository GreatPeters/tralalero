using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class NoryangjinShopPolish
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset");
        foreach(string name in new[]{"Noryangjin_MapTool_Mode","Noryangjin_MapTool_Mode_SR18"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").transform;
            var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);
            foreach(var t in shop.GetComponentsInChildren<TMP_Text>(true))t.font=font;
            Place((RectTransform)shop.transform.Find("Title"),.08f,.84f,.92f,.88f);
            Place((RectTransform)shop.transform.Find("Subtitle"),.08f,.805f,.92f,.835f);
            var area=new GameObject("PreviewArea",typeof(RectTransform)).GetComponent<RectTransform>();area.SetParent(shop.transform,false);Place(area,.05f,.54f,.95f,.80f);
            var preview=shop.preview.display.rectTransform;preview.SetParent(area,false);Place(preview,0,0,1,1);
            var entry=canvas.Find("UI/Main/Bottom/Skin_Button").GetComponentInChildren<TMP_Text>(true);entry.font=font;
            var upgrade=canvas.GetComponentsInChildren<UpgradeUI>(true).First().transform.parent.parent;
            typeof(UpgradeShopReferenceSetup).GetMethod("ApplyReferenceArtwork",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{upgrade});
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        return new{scenes=2,font=font.name};
    }
    static void Place(RectTransform r,float x,float y,float xx,float yy){r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;}
}
