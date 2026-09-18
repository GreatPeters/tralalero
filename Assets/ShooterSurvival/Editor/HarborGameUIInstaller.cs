using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class HarborGameUIInstaller
{
    private const string ArtPath = "Assets/ShooterSurvival/UI/HarborWorkshop/";
    private const string VideoPath = "Assets/ShooterSurvival/UI/HarborVideo/";
    private static readonly Color Ink = new(.035f,.08f,.22f), Cream = new(1f,.975f,.88f), Muted = new(.35f,.40f,.48f);
    private static TMP_FontAsset Font => HarborRefinementFontBuilder.Font ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MobileUIArt.FontPath);
    private static Sprite Art(string name) => CoastalArt(name) ?? AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + name + ".png");
    private static Sprite VideoArt(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(VideoPath + name + ".png");

    public static object ApplyAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Clean Edit Mode required; preserve any dirty scene first.");
        string original = SceneManager.GetActiveScene().path;
        string[] names = { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" };
        foreach (string name in names)
        {
            var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
            string backup = "tmp/backups/harbor-ui-live-2026-09-15/" + name + ".before-ui.unity";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup) && !EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Scene backup failed.");
        }
        PrepareSharedAssets();
        var results = new System.Collections.Generic.List<string>();
        foreach (string name in names)
        {
            var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
            ApplyOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("UI save failed: " + name);
            results.Add(name);
        }
        EditorSceneManager.OpenScene(original);
        return new { scenes = results, chapterUpgrades = 3, videoStyle = "A with actual scene indicators" };
    }

    public static void ApplyOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var scene = SceneManager.GetActiveScene();
        var canvas = scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<CanvasScript>();
        var player = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerScript>(true)).Single();
        BuildUpgrades(canvas, player);
        BuildShop(canvas);
        BuildLobby(canvas);
        BuildVideos(canvas);
        StyleOtherSurfaces(canvas);
        PolishOpenScene();
        ApplyCoastalLayouts(canvas, player);
        foreach (var button in canvas.GetComponentsInChildren<Button>(true)) GetOrAdd<GameUIButtonSound>(button.gameObject);
        foreach (var component in canvas.GetComponentsInChildren<Component>(true))
        {
            if(component==null)continue;
            EditorUtility.SetDirty(component);
            if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }

    private static void PrepareSharedAssets()
    {
        if (Font == null || Art("ParchmentPanel") == null) throw new InvalidOperationException("Import the selected Harbor UI assets first.");
        var theme = AssetDatabase.LoadAssetAtPath<GameUITheme>("Assets/ShooterSurvival/Resources/UI/MobileTheme.asset");
        theme.paletteName = "Coastal Enamel";
        theme.page = new Color(.035f,.09f,.23f); theme.card = Color.white; theme.primary = Color.white;
        theme.ink = Ink; theme.buttonInk = Ink; theme.muted = Muted;
        theme.accent = new Color(.08f,.69f,.88f); theme.line = new Color(.06f,.14f,.30f);
        EditorUtility.SetDirty(theme); AssetDatabase.SaveAssetIfDirty(theme);
        const string folder = "Assets/ShooterSurvival/Resources/Upgrades";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/ShooterSurvival/Resources", "Upgrades");
        const string path = folder + "/ChapterWorkshop.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<ChapterUpgradeCatalog>(path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ChapterUpgradeCatalog>();
            catalog.entries = new[] {
                new ChapterUpgradeDefinition { chapter=1,title="노량진",coinCost=500,attackPercent=100,healthPercent=200 },
                new ChapterUpgradeDefinition { chapter=2,title="고속도로",coinCost=2000,attackPercent=200,healthPercent=400 },
                new ChapterUpgradeDefinition { chapter=3,title="휴게소",coinCost=5000,attackPercent=300,healthPercent=600 }
            };
            AssetDatabase.CreateAsset(catalog,path);
        }
        ChapterUpgradeCatalog.Validate(catalog.entries);
        foreach(string name in new[]{"BossBreaker","HealingInsert","SahurShield","BomberCharm"})ImportIcon(ArtPath+name+".png");
        var visuals=AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>("Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset");
        foreach(var entry in visuals.entries)
        {
            string iconPath=ArtPath+"EquipmentIcons/"+entry.key+".png";
            if(entry.key.StartsWith("skin_")&&File.Exists(CoastalPath+entry.key+".png"))iconPath=CoastalPath+entry.key+".png";
            if(entry.key=="hat_diver"&&File.Exists(ArtPath+"EquipmentIcons/hat_diver_polished.png"))iconPath=ArtPath+"EquipmentIcons/hat_diver_polished.png";
            if(!File.Exists(iconPath))continue;
            ImportIcon(iconPath);entry.icon=AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        }
        EditorUtility.SetDirty(visuals);AssetDatabase.SaveAssetIfDirty(visuals);
        string[] sources = { "Assets/JH/UI/Opening/Animated/Story_04.png", "Assets/JH/UI/ChapterTransitions/Highway_Entry.png", "Assets/JH/UI/ChapterTransitions/RestStop_Entry.png" };
        for (int i=0;i<sources.Length;i++)
        {
            string target=ArtPath+"Chapter"+(i+1)+".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(target)==null && !AssetDatabase.CopyAsset(sources[i],target)) throw new IOException("Chapter thumbnail copy failed.");
            var importer=(TextureImporter)AssetImporter.GetAtPath(target);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;importer.maxTextureSize=512;importer.SaveAndReimport();
        }
    }

    private static void ImportIcon(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=512;
        importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
    }

    private static RectTransform Rect(Transform parent,string name,float x,float y,float xx,float yy)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.gameObject.layer=LayerMask.NameToLayer("UI");SetRect(rect,x,y,xx,yy);return rect;
    }
    private static void SetRect(RectTransform rect,float x,float y,float xx,float yy) => MobileUIArt.SetRect(rect,x,y,xx,yy);
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float xx,float yy,bool preserve=false)
    {
        var image=Rect(parent,name,x,y,xx,yy).gameObject.AddComponent<Image>();
        image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;image.preserveAspect=preserve;
        if(sprite!=null && sprite.border!=Vector4.zero)image.type=UnityEngine.UI.Image.Type.Sliced;
        return image;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string value,float size,Color color,float x,float y,float xx,float yy,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
    {
        var text=Rect(parent,name,x,y,xx,yy).gameObject.AddComponent<TextMeshProUGUI>();
        text.font=Font;text.fontSharedMaterial=Font.material;text.text=value;text.fontSize=size;text.color=color;
        text.alignment=alignment;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;
    }
    private static Button Button(Transform parent,string name,string label,float x,float y,float xx,float yy,bool gold=false,float size=36)
    {
        var image=Image(parent,name,Art(gold?"GoldButton":"WoodButton"),x,y,xx,yy);
        image.pixelsPerUnitMultiplier=1.75f;
        image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;
        colors.pressedColor=new Color(.76f,.65f,.49f);colors.selectedColor=Color.white;colors.disabledColor=new Color(.58f,.56f,.50f);
        button.colors=colors;
        Text(button.transform,"Label",label,size,gold?Ink:Cream,.04f,.05f,.96f,.95f);
        return button;
    }
    private static void ClearChildren(Transform parent)
    { foreach(Transform child in parent.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject); }
    private static T GetOrAdd<T>(GameObject go) where T:Component
    {var component=go.GetComponent<T>();return component!=null?component:go.AddComponent<T>();}
    private static void CopyButtonCalls(Button source,Button target)
    {
        var from=new SerializedObject(source);var to=new SerializedObject(target);
        to.CopyFromSerializedProperty(from.FindProperty("m_OnClick"));to.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Layer(GameObject root,int order)
    {GetOrAdd<MobileUILayer>(root).Configure(order);GetOrAdd<GraphicRaycaster>(root);}
    private static void RemoveOutline(GameObject root)
    {foreach(var outline in root.GetComponents<Outline>())Object.DestroyImmediate(outline);}

    private static void BuildLobby(CanvasScript canvas)
    {
        ApplyLobbyDim(canvas);
        var ui=canvas.transform.Find("UI");var top=ui.Find("Top");
        foreach(var background in ui.Cast<Transform>().Where(t=>t.name=="Background"))
            foreach(var image in background.GetComponentsInChildren<Image>(true)){image.color=Color.clear;image.raycastTarget=false;}
        var resource=top.Find("Resource");SetRect((RectTransform)resource,.035f,.92f,.81f,.977f);
        foreach(var image in resource.GetComponentsInChildren<Image>(true))
        {
            if(image.name=="Coin"||image.name=="Jewel") {image.sprite=Art("WalletPill");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;RemoveOutline(image.gameObject);}
            else if(image.name=="Icon") {image.sprite=Art(image.transform.parent.name=="Coin"?"Coin":"Jewel");image.color=Color.white;image.preserveAspect=true;}
        }
        foreach(var text in resource.GetComponentsInChildren<TMP_Text>(true)){text.color=Ink;text.fontSize=42;text.enableAutoSizing=true;text.fontSizeMin=24;text.fontSizeMax=42;}
        var setting=top.Find("Setting");SetRect((RectTransform)setting,.84f,.917f,.966f,.98f);
        var settingsButton=setting.GetComponentInChildren<Button>(true);
        ClearChildren(settingsButton.transform);
        var settingsImage=GetOrAdd<Image>(settingsButton.gameObject);settingsImage.sprite=Art("BrassPlaque");settingsImage.type=UnityEngine.UI.Image.Type.Sliced;settingsImage.color=Color.white;settingsImage.raycastTarget=true;settingsButton.targetGraphic=settingsImage;RemoveOutline(settingsButton.gameObject);
        Image(settingsButton.transform,"Gear",Art("Settings"),.15f,.15f,.85f,.85f,true);
        var center=ui.Find("Main/Center");ClearChildren(center);
        var title=Image(center,"ChapterTitle",Art("WoodSign"),.16f,.79f,.84f,.893f);
        var chapter=canvas.GetComponent<ChapterProgression>();
        string chapterName=chapter.chapter==1?"노량진 수산시장":chapter.chapter==2?"고속도로":"휴게소";
        Text(title.transform,"Chapter","CHAPTER "+chapter.chapter.ToString("00"),31,Cream,.05f,.58f,.95f,.88f);
        Text(title.transform,"Name",chapterName,49,Cream,.04f,.10f,.96f,.57f);
        var hint=Rect(center,"StartHint",.10f,.32f,.90f,.49f);
        Text(hint,"Title","좌우로 움직여\n게임시작",64,Cream,.04f,.40f,.96f,1);
        Image(hint,"Left",Art("GestureLeft"),.24f,.02f,.40f,.32f,true);
        Image(hint,"Right",Art("GestureRight"),.60f,.02f,.76f,.32f,true);
        var finger=Image(hint,"Finger",Art("GestureHand"),.435f,0,.565f,.36f,true);
        hint.gameObject.AddComponent<HarborSwipeHint>().finger=finger.rectTransform;
        var bottom=ui.Find("Main/Bottom");SetRect((RectTransform)bottom,.025f,.033f,.975f,.165f);
        string[] names={"Skin_Button","Upgrade_Button","Story_Button"}, labels={"꾸미기","강화","스토리"},icons={"LateralSneaker","AttackIcon","StoryTileLegacy"};
        for(int i=0;i<names.Length;i++)
        {
            var button=bottom.Find(names[i]).GetComponent<Button>();ClearChildren(button.transform);
            SetRect((RectTransform)button.transform,i*.337f,0,i*.337f+.326f,1);
            var image=GetOrAdd<Image>(button.gameObject);image.sprite=Art("NavigationTile");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;image.raycastTarget=true;button.targetGraphic=image;RemoveOutline(button.gameObject);
            Image(button.transform,"Icon",Art(icons[i]),.19f,.30f,.81f,.91f,true);
            Text(button.transform,"Label",labels[i],45,Cream,.05f,.055f,.95f,.32f);
        }
        var back=top.Find("Back/Image").GetComponent<Button>();
        var graphic=back.targetGraphic as Image;if(graphic!=null){graphic.sprite=Art("WoodButton");graphic.color=Color.white;RemoveOutline(graphic.gameObject);}
        foreach(var label in back.GetComponentsInChildren<TMP_Text>(true))label.color=Cream;
    }

    public static void ApplyLobbyDim(CanvasScript canvas)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        // The existing lobby root is hidden by the horizontal-start flow.
        // Its first child darkens the scene without tinting or intercepting UI.
        var ui = canvas.buttons.transform;
        var existing = ui.Find("LobbyDim");
        var dim = existing != null
            ? GetOrAdd<Image>(existing.gameObject)
            : Image(ui, "LobbyDim", null, 0, 0, 1, 1);
        SetRect(dim.rectTransform, 0, 0, 1, 1);
        dim.transform.SetAsFirstSibling();
        dim.sprite = null;
        dim.type = UnityEngine.UI.Image.Type.Simple;
        dim.color = new Color(0, 0, 0, .20f);
        dim.raycastTarget = false;
        EditorUtility.SetDirty(dim);
        EditorUtility.SetDirty(dim.rectTransform);
    }
}
