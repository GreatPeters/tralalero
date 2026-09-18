using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using IndianOceanAssets.ShooterSurvival;

public static partial class HarborGameUIInstaller
{
    public const string DisplayFontPath="Assets/ShooterSurvival/Resources/UI/GmarketHarborDisplay SDF.asset";
    public const string StartOutlinePath="Assets/ShooterSurvival/Resources/UI/HarborMaterials/Coastal_DisplayWhite.mat";

    private static TMP_FontAsset DisplayFont()
    {
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        if(font==null){
            var source=AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("GmarketSansTTFBold t:Font").First()));
            font=TMP_FontAsset.CreateFontAsset(source,96,24,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            font.name="Gmarket Harbor Display SDF";AssetDatabase.CreateAsset(font,DisplayFontPath);AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
        }
        const string characters="ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789,.-+ 노량진수산시장고속도로휴게소좌우움직여게임시작꾸미기강화스토리";
        var missing=new string(characters.Distinct().Where(c=>!font.HasCharacter(c)).ToArray());if(missing.Length>0)font.TryAddCharacters(missing,out _);
        var data=new SerializedObject(font);data.FindProperty("m_ClearDynamicDataOnBuild").boolValue=false;data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(font);foreach(var texture in font.atlasTextures)EditorUtility.SetDirty(texture);AssetDatabase.SaveAssets();return font;
    }

    private static Material DisplayMaterial(TMP_FontAsset font,string name,bool white,float outline=.28f)
    {
        string path=PresetFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(font.material);AssetDatabase.CreateAsset(material,path);}
        material.CopyPropertiesFromMaterial(font.material);material.SetColor("_FaceColor",Color.white);material.SetFloat("_FaceDilate",white?outline+.025f:.015f);
        material.EnableKeyword("OUTLINE_ON");material.SetFloat("_OutlineWidth",white?outline:.015f);material.SetColor("_OutlineColor",white?Ink:Cream);
        material.EnableKeyword("UNDERLAY_ON");material.SetColor("_UnderlayColor",new Color(.015f,.035f,.09f,1));material.SetFloat("_UnderlayOffsetY",white?-.32f:-.13f);
        material.SetFloat("_UnderlayDilate",white?.18f:0);material.SetFloat("_UnderlaySoftness",.035f);
        if(!white)material.DisableKeyword("UNDERLAY_ON");
        EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);return material;
    }

    private static void ReferenceLobby(CanvasScript canvas)
    {
        var font=DisplayFont();var white=DisplayMaterial(font,"Coastal_DisplayWhite",true);var ink=DisplayMaterial(font,"Coastal_DisplayInk",false);
        void Style(TMP_Text text,float size,bool light=false){text.font=font;text.fontSharedMaterial=light?white:ink;text.fontSize=size;text.fontStyle=FontStyles.Normal;text.enableAutoSizing=false;text.color=light?Color.white:Ink;text.extraPadding=true;text.UpdateMeshPadding();}
        var ui=canvas.transform.Find("UI");var center=ui.Find("Main/Center");var title=center.Find("ChapterTitle");
        SetRect((RectTransform)title,.12f,.782f,.88f,.904f);title.GetComponent<Image>().pixelsPerUnitMultiplier=1.15f;
        var header=title.Find("NavyHeader");SetRect((RectTransform)header,.005f,.615f,.995f,.992f);header.GetComponent<Image>().pixelsPerUnitMultiplier=1.25f;
        Move(title,"Chapter",.22f,.69f,.78f,.95f);Style(title.Find("Chapter").GetComponent<TMP_Text>(),45,true);
        title.Find("Chapter").GetComponent<TMP_Text>().fontSharedMaterial=DisplayMaterial(font,"Coastal_DisplayHeader",true,.07f);
        Move(title,"Name",.04f,.12f,.96f,.59f);Style(title.Find("Name").GetComponent<TMP_Text>(),96);
        var name=title.Find("Name").GetComponent<TMP_Text>();name.enableAutoSizing=true;name.fontSizeMin=82;name.fontSizeMax=96;
        title.Find("Anchor").gameObject.SetActive(false);
        var crest=title.Find("ReferenceCrest")??Image(title,"ReferenceCrest",Art("SquareButton"),.435f,.9f,.565f,1.23f,true).transform;
        crest.SetAsFirstSibling();var anchor=title.Find("ReferenceAnchor")??Image(title,"ReferenceAnchor",Art("Anchor"),.466f,.94f,.534f,1.19f,true).transform;anchor.SetAsLastSibling();
        for(int side=0;side<2;side++)for(int i=0;i<10;i++){
            string part="Rope_"+side+"_"+i;var t=title.Find(part)??Solid(title,part,Cream,0,0,1,1).transform;
            float x=(side==0?.026f:.817f)+i*.0155f;SetRect((RectTransform)t,x,.769f,x+.01f,.82f);t.localRotation=Quaternion.Euler(0,0,22);t.GetComponent<Image>().raycastTarget=false;
        }
        var waves=title.Find("ReferenceWaves")??Rect(title,"ReferenceWaves",.40f,.064f,.60f,.125f);
        for(int row=0;row<2;row++)for(int i=0;i<24;i++){
            string part="Wave_"+row+"_"+i;var t=waves.Find(part)??Solid(waves,part,new Color(.015f,.56f,.85f),0,0,1,1).transform;
            float x=i/24f,y=.3f+row*.40f+Mathf.Sin(x*Mathf.PI*6)*.13f;SetRect((RectTransform)t,x,y,x+1.4f/24,y+.105f);t.localRotation=Quaternion.Euler(0,0,Mathf.Cos(x*Mathf.PI*6)*15);t.GetComponent<Image>().raycastTarget=false;
        }
        foreach(Transform wallet in ui.Find("Top/Resource")){
            if(wallet.GetComponent<Image>() is Image panel)panel.pixelsPerUnitMultiplier=1.3f;
            var icon=wallet.Find("Icon");if(icon!=null)SetRect((RectTransform)icon,.035f,.12f,.28f,.88f);
            foreach(var text in wallet.GetComponentsInChildren<TMP_Text>(true)){Style(text,68);text.enableAutoSizing=true;text.fontSizeMin=42;text.fontSizeMax=68;}
        }
        foreach(Transform tile in ui.Find("Main/Bottom")){
            if(tile.GetComponent<Button>()==null)continue;
            var panel=tile.GetComponent<Image>();panel.pixelsPerUnitMultiplier=.95f;
            Move(tile,"Icon",.14f,.31f,.86f,.94f);Move(tile,"Label",.045f,.055f,.955f,.295f);Style(tile.Find("Label").GetComponent<TMP_Text>(),65);
            if(tile.name=="Skin_Button"){
                var selected=tile.Find("ReferenceSelected")??Image(tile,"ReferenceSelected",Art("YellowButton"),.058f,.315f,.942f,.944f).transform;
                selected.SetSiblingIndex(0);selected.GetComponent<Image>().raycastTarget=false;selected.GetComponent<Image>().pixelsPerUnitMultiplier=2;
            }
        }
        var hint=center.Find("StartHint");SetRect((RectTransform)hint,.04f,.315f,.96f,.505f);
        Move(hint,"Title",.205f,.44f,.795f,.98f);var message=hint.Find("Title").GetComponent<TMP_Text>();Style(message,86,true);message.text="좌우로 움직여\n게임 시작";message.lineSpacing=-8;
        foreach(string side in new[]{"Left","Right"}){var arrow=hint.Find(side);arrow.gameObject.SetActive(true);arrow.GetComponent<Image>().sprite=Art(side);arrow.GetComponent<Image>().preserveAspect=true;}
        Move(hint,"Left",.005f,.61f,.195f,.875f);Move(hint,"Right",.805f,.61f,.995f,.875f);
        Move(hint,"Finger",.424f,.035f,.576f,.36f);var finger=hint.Find("Finger").GetComponent<Image>();finger.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+"GestureHand.png");finger.preserveAspect=true;
        var motion=hint.GetComponent<HarborSwipeHint>();motion.finger=finger.rectTransform;motion.amplitude=64;motion.period=1.6f;motion.tiltDegrees=12;
        foreach(var component in canvas.GetComponentsInChildren<Component>(true))if(component is TMP_Text||component is RectTransform||component is Image||component is HarborSwipeHint)EditorUtility.SetDirty(component);
    }

    public static object ApplyReferenceCorrectionsAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        if(!EditorSceneManager.SaveOpenScenes())throw new Exception("Save pending scene changes first");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try{foreach(string sceneName in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity");
            ReferenceLobby(scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<CanvasScript>());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return "Reference lobby corrected in three scenes";
    }
}
