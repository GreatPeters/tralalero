using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

public static partial class HarborGameUIInstaller
{
    public const string RoundedFontPath="Assets/ShooterSurvival/Resources/UI/CoastalRoundedJua SDF.asset";
    public const string FaithfulPath="Assets/ShooterSurvival/UI/CoastalFaithful/";
    [Serializable] private class FaithfulSlice { public string name="";public int x=0,y=0,width=0,height=0; }
    [Serializable] private class FaithfulSlices { public FaithfulSlice[] items=Array.Empty<FaithfulSlice>(); }
    private static TMP_FontAsset RoundedFont()
    {
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RoundedFontPath);
        if(font==null){
            font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/ShooterSurvival/Fonts/CoastalJua/Jua-Regular.ttf"),96,24,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            font.name="Coastal Rounded SDF";AssetDatabase.CreateAsset(font,RoundedFontPath);AssetDatabase.AddObjectToAsset(font.material,font);foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
        }
        string source="ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 /.,+-노량진수산시장고속도로휴게소좌우움직여게임시작꾸미기강화스토리이전장면건너뛰기이야기";
        foreach(string file in Directory.GetFiles("Assets/ShooterSurvival/Scripts","*StoryUI.cs",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/ShooterSurvival/Scripts","*ChapterTransitionUI.cs",SearchOption.AllDirectories)))source+=File.ReadAllText(file);
        var chars=source.Where(c=>c>='가'&&c<='힣'||c>=32&&c<=126).Distinct();var needed=new string(chars.Where(c=>!font.HasCharacter(c)).ToArray());if(needed.Length>0)font.TryAddCharacters(needed,out _);
        var data=new SerializedObject(font);data.FindProperty("m_ClearDynamicDataOnBuild").boolValue=false;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(font);foreach(var atlas in font.atlasTextures)EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssets();return font;
    }
    private static Material RoundedMaterial(TMP_FontAsset font,string name,float outline,bool light)
    {
        string path=PresetFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(font.material);AssetDatabase.CreateAsset(material,path);}
        material.CopyPropertiesFromMaterial(font.material);material.SetColor("_FaceColor",Color.white);material.SetFloat("_OutlineWidth",outline);material.SetFloat("_FaceDilate",light?outline+.075f:.075f);
        material.SetColor("_OutlineColor",light?(name=="Coastal_RoundedPrompt"?Ink:new Color(.01f,.025f,.07f,1)):Cream);material.EnableKeyword("OUTLINE_ON");
        if(light){material.EnableKeyword("UNDERLAY_ON");material.SetColor("_UnderlayColor",new Color(.01f,.02f,.05f,.9f));material.SetFloat("_UnderlayOffsetY",-.2f);material.SetFloat("_UnderlayDilate",.04f);material.SetFloat("_UnderlaySoftness",.06f);}else material.DisableKeyword("UNDERLAY_ON");
        EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);return material;
    }
    private static void ImportFaithfulArt()
    {
        ImportChapterPlaques();
        foreach(string name in new[]{"LobbyPlaque","StoryChrome","NavigationStrip"}){
            string path=FaithfulPath+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
            importer.spriteBorder=name=="StoryChrome"?new Vector4(35,344,35,182):Vector4.zero;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(FaithfulPath+"StoryChrome.png");
        Sprite Part(string name,int x,int top,int width,int height){string path=FaithfulPath+name+".asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;sprite=Sprite.Create(texture,new Rect(x,texture.height-top-height,width,height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(27,27,27,27));sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;}
        Part("TabYellow",14,1511,200,100);Part("TabIvory",224,1511,200,100);Part("PreviousPlate",10,1640,396,151);Part("NextPlate",412,1640,431,151);Part("TransitionHeader",0,0,853,181);Part("MovieBorder",3,173,847,1330);AssetDatabase.SaveAssets();
        var navigation=AssetDatabase.LoadAssetAtPath<Texture2D>(FaithfulPath+"NavigationStrip.png");
        foreach(var row in JsonUtility.FromJson<FaithfulSlices>(File.ReadAllText("map-concepts/harbor-faithful-art-2026-09-17/sources/navigation-slices.json")).items){
            string path=FaithfulPath+row.name+".asset";if(AssetDatabase.LoadAssetAtPath<Sprite>(path)!=null)continue;
            var sprite=Sprite.Create(navigation,new Rect(row.x,navigation.height-row.y-row.height,row.width,row.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=row.name;AssetDatabase.CreateAsset(sprite,path);
        }
        AssetDatabase.SaveAssets();
    }
    private static void FaithfulPresentation(CanvasScript canvas)
    {
        var font=RoundedFont();var ink=RoundedMaterial(font,"Coastal_RoundedInk",.014f,false);var white=RoundedMaterial(font,"Coastal_RoundedWhite",.13f,true);var prompt=RoundedMaterial(font,"Coastal_RoundedPrompt",.24f,true);
        void Style(TMP_Text text,float size,Material material,Color color){text.font=font;text.fontSharedMaterial=material;text.fontStyle=FontStyles.Normal;text.fontSize=size;text.enableAutoSizing=false;text.color=color;text.extraPadding=true;text.UpdateMeshPadding();}
        var ui=canvas.transform.Find("UI");var title=ui.Find("Main/Center/ChapterTitle");
        foreach(Transform child in title)if(child.name!="Name"&&child.name!="Chapter")child.gameObject.SetActive(false);
        ApplyChapterPlaque(canvas);
        var titleRect=(RectTransform)title;titleRect.anchorMin=new Vector2(.13f,.91f);titleRect.anchorMax=new Vector2(.87f,.91f);titleRect.pivot=new Vector2(.5f,1);titleRect.offsetMin=Vector2.zero;titleRect.offsetMax=Vector2.zero;
        Move(title,"Chapter",.30f,.58f,.70f,.76f);var chapter=title.Find("Chapter").GetComponent<TMP_Text>();chapter.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(PresetFolder+"/Coastal_DisplayHeader.mat");chapter.fontSize=43;chapter.color=Cream;
        Move(title,"Name",.075f,.195f,.925f,.51f);Style(title.Find("Name").GetComponent<TMP_Text>(),82,ink,Ink);
        foreach(Transform wallet in ui.Find("Top/Resource"))if(wallet.name=="Coin"||wallet.name=="Jewel")foreach(var text in wallet.GetComponentsInChildren<TMP_Text>(true)){Style(text,67,ink,Ink);text.enableAutoSizing=true;text.fontSizeMin=42;text.fontSizeMax=67;}
        foreach(var text in ui.Find("Main/Bottom").GetComponentsInChildren<TMP_Text>(true))Style(text,61,ink,Ink);
        var bottom=ui.Find("Main/Bottom");var bottomRect=(RectTransform)bottom;bottomRect.anchorMin=new Vector2(.025f,.033f);bottomRect.anchorMax=new Vector2(.975f,.033f);bottomRect.pivot=new Vector2(.5f,0);bottomRect.offsetMin=Vector2.zero;bottomRect.offsetMax=Vector2.zero;
        var bottomFit=GetOrAdd<AspectRatioFitter>(bottom.gameObject);var navFirst=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"SkinTile.asset");bottomFit.aspectMode=AspectRatioFitter.AspectMode.WidthControlsHeight;bottomFit.aspectRatio=navFirst.rect.width/navFirst.rect.height/.326f;
        string[] buttons={"Skin_Button","Upgrade_Button","Story_Button"},sprites={"SkinTile","UpgradeTile","StoryTile"};
        for(int i=0;i<3;i++){
            var button=bottom.Find(buttons[i]);var surface=button.GetComponent<Image>();surface.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+sprites[i]+".asset");surface.type=UnityEngine.UI.Image.Type.Simple;surface.preserveAspect=false;surface.color=Color.white;
            button.Find("Icon").gameObject.SetActive(false);if(button.Find("ReferenceSelected") is Transform selected)selected.gameObject.SetActive(false);
            Move(button,"Label",.1f,.07f,.90f,.27f);button.Find("Label").SetAsLastSibling();
        }
        var message=ui.Find("Main/Center/StartHint/Title").GetComponent<TMP_Text>();Style(message,84,prompt,Color.white);message.lineSpacing=-5;
        foreach(var story in canvas.GetComponentsInChildren<OpeningStoryUI>(true)){
            var content=story.transform.Find("HarborContent");
            foreach(string name in new[]{"HeaderWood","HeaderRightCap","MovieFrame","CaptionDivider"})if(content.Find(name)!=null)content.Find(name).gameObject.SetActive(false);
            var backing=content.Find("WoodBacking").GetComponent<Image>();backing.color=new Color(.015f,.055f,.16f);backing.transform.SetAsFirstSibling();
            var chrome=content.Find("FaithfulChrome")?.GetComponent<Image>()??Image(content,"FaithfulChrome",AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"StoryChrome.png"),0,0,1,1);
            chrome.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"StoryChrome.png");chrome.type=UnityEngine.UI.Image.Type.Sliced;chrome.color=Color.white;chrome.raycastTarget=false;chrome.transform.SetSiblingIndex(1);
            var layout=GetOrAdd<CoastalStoryLayout>(content.gameObject);layout.chrome=chrome;layout.counter=story.chapterText.rectTransform;layout.skip=(RectTransform)content.Find("SkipButton");layout.movieSlot=(RectTransform)content.Find("MovieSlot");layout.caption=story.captionText.rectTransform;layout.title=story.titleText.rectTransform;layout.shade=(RectTransform)content.Find("CaptionShade");layout.previous=(RectTransform)content.Find("ReplayButton");layout.next=(RectTransform)content.Find("NextButton");layout.markers=story.sceneIndicators.Select(x=>x.rectTransform).ToArray();
            layout.movieDisplay=story.movieDisplay.rectTransform;GetOrAdd<RectMask2D>(layout.movieSlot.gameObject);
            Style(story.chapterText,65,ink,Ink);story.chapterText.alignment=TextAlignmentOptions.Center;Style(story.titleText,50,white,new Color(1,.83f,.30f));Style(story.captionText,60,white,Color.white);story.captionText.lineSpacing=2;
            story.captionText.enableAutoSizing=true;story.captionText.fontSizeMin=48;story.captionText.fontSizeMax=60;
            foreach(var button in story.GetComponentsInChildren<Button>(true)){
                foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))Style(text,button.transform==layout.skip?58:67,ink,Ink);
                var colors=button.colors;colors.normalColor=Color.white;colors.disabledColor=new Color(.83f,.85f,.87f,1);colors.pressedColor=new Color(.84f,.89f,.97f);button.colors=colors;
            }
            layout.skip.GetComponent<Image>().color=Color.clear;layout.skip.GetComponent<Button>().transition=Selectable.Transition.None;
            layout.previous.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"PreviousPlate.asset");layout.next.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"NextPlate.asset");
            layout.previous.GetComponent<Image>().type=UnityEngine.UI.Image.Type.Sliced;layout.next.GetComponent<Image>().type=UnityEngine.UI.Image.Type.Sliced;
            story.activeSceneSprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"TabYellow.asset");story.inactiveSceneSprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"TabIvory.asset");
            for(int i=0;i<story.sceneIndicators.Length;i++){
                var marker=story.sceneIndicators[i];marker.sprite=i==0?story.activeSceneSprite:story.inactiveSceneSprite;marker.type=UnityEngine.UI.Image.Type.Sliced;marker.color=Color.white;
                foreach(var label in marker.GetComponentsInChildren<TMP_Text>(true))Style(label,70,ink,Ink);
            }
            story.titleText.transform.SetAsLastSibling();story.captionText.transform.SetAsLastSibling();layout.Refresh();
        }
        foreach(var transition in canvas.GetComponentsInChildren<ChapterTransitionUI>(true)){
            var content=transition.transform.Find("HarborContent");if(content==null)continue;
            var header=content.Find("HeaderWood").GetComponent<Image>();header.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"TransitionHeader.asset");header.type=UnityEngine.UI.Image.Type.Simple;header.color=Color.white;SetRect(header.rectTransform,0,.902f,1,1);
            Move(content,"StoryCounter",.045f,.917f,.535f,.963f);Move(content,"SkipButton",.659f,.916f,.953f,.966f);
            var skip=content.Find("SkipButton").GetComponent<Button>();skip.transition=Selectable.Transition.None;skip.GetComponent<Image>().color=Color.clear;
            var border=content.Find("MovieFrame").GetComponent<Image>();border.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"MovieBorder.asset");border.type=UnityEngine.UI.Image.Type.Sliced;border.pixelsPerUnitMultiplier=.8f;SetRect(border.rectTransform,.004f,.112f,.996f,.907f);Move(content,"MovieSlot",.022f,.124f,.978f,.896f);
            foreach(var text in content.GetComponentsInChildren<TMP_Text>(true))Style(text,48,white,Cream);
            Style(content.Find("StoryCounter").GetComponent<TMP_Text>(),58,ink,Ink);foreach(var label in skip.GetComponentsInChildren<TMP_Text>(true))Style(label,58,ink,Ink);
            content.Find("StoryCounter").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            if(transition.title!=null)Style(transition.title,52,white,new Color(1,.83f,.3f));
            if(transition.caption!=null){Style(transition.caption,56,white,Color.white);transition.caption.enableAutoSizing=true;transition.caption.fontSizeMin=45;transition.caption.fontSizeMax=56;}
            if(content.Find("TransitionStatus") is Transform status){SetRect((RectTransform)status,.05f,.025f,.95f,.102f);Style(status.GetComponent<TMP_Text>(),52,white,Cream);}
            foreach(var child in content.Cast<Transform>().Where(t=>t.name.StartsWith("StorySegment")))child.gameObject.SetActive(false);
        }
        if(AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"ShopSteelToe.asset")!=null)FaithfulShops(canvas);
        FaithfulSettings(canvas);
        AlignPresentationText(canvas);
        foreach(var component in canvas.GetComponentsInChildren<Component>(true))if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
    }
    public static object ApplyFaithfulArtAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");if(!EditorSceneManager.SaveOpenScenes())throw new Exception("Save pending scene changes first");
        ImportFaithfulArt();var setup=EditorSceneManager.GetSceneManagerSetup();
        try{foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");FaithfulPresentation(scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<CanvasScript>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}return "Faithful lobby and opening story art installed in all three chapters";
    }
}
