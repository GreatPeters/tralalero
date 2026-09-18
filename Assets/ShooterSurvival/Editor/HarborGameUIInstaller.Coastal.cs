using System;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class HarborGameUIInstaller
{
    private const string CoastalPath="Assets/ShooterSurvival/UI/CoastalEnamel/";
    [Serializable] private sealed class CoastalSpriteList { public CoastalSpriteRow[] sprites=System.Array.Empty<CoastalSpriteRow>(); }
    [Serializable] private sealed class CoastalSpriteRow { public string name=""; public float[] border=new float[4]; }

    private static Sprite CoastalArt(string name)
    {
        string mapped=name switch {
            "ParchmentPanel"=>"IvoryPanel", "WoodSign"=>"NavyPanel", "GoldButton"=>"YellowButton",
            "WoodButton"=>"NavyButton", "BrassPlaque"=>"SquareButton", "WalletPill"=>"Wallet",
            "WindowFrame"=>"Frame", "MerchantBackdrop"=>"Merchant", "AttackIcon"=>"Attack",
            "StoryTileLegacy"=>"Story", "GestureHand"=>"Swipe", "GestureLeft"=>"Left",
            "GestureRight"=>"Right", "SkinTabFin"=>"skin_original", _=>name };
        return AssetDatabase.LoadAssetAtPath<Sprite>(CoastalPath+mapped+".png");
    }

    public static object ImportCoastalAssets()
    {
        AssetDatabase.Refresh();
        string json=File.ReadAllText("map-concepts/coastal-enamel-ui-2026-09-16/sprite-manifest.json");
        var rows=JsonUtility.FromJson<CoastalSpriteList>("{\"sprites\":"+json+"}").sprites;
        foreach(var row in rows)
        {
            string path=CoastalPath+row.name+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spriteBorder=new Vector4(row.border[0],row.border[1],row.border[2],row.border[3]);
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        return new{sprites=rows.Length};
    }

    public static object ApplyCoastalAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Clean Edit Mode required.");
        if(CoastalArt("IvoryPanel")==null)throw new InvalidOperationException("Import Coastal production sprites first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        PrepareSharedAssets();
        var chapters=AssetDatabase.LoadAssetAtPath<ChapterUpgradeCatalog>("Assets/ShooterSurvival/Resources/Upgrades/ChapterWorkshop.asset");
        foreach(var row in chapters.entries){row.attackPercent=5;row.healthPercent=5;}
        EditorUtility.SetDirty(chapters);AssetDatabase.SaveAssetIfDirty(chapters);
        var names=new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"};
        try
        {
            foreach(string name in names)
            {
                var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
                ApplyOpenScene();EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Failed to save "+name);
            }
        }
        finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return new{scenes=names,theme="Coastal Enamel",chapterPercentPerRank=5};
    }

    private static void ApplyCoastalLayouts(CanvasScript canvas,PlayerScript player)
    {
        CoastalLobby(canvas);
        CoastalUpgrades(canvas);
        CoastalShop(canvas);
        CoastalSettings(canvas);
        CoastalHud(canvas,player);
        CoastalVideos(canvas);
        CoastalTutorials(canvas);
        CoastalRemaining(canvas);
        RefineCoastalPresentation(canvas);
        var ink=TextPreset("Coastal_Ink",false);var white=TextPreset("Coastal_White",true);
        foreach(var label in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.font!=Font)continue;
            label.fontSharedMaterial=label.color.grayscale>.6f?white:ink;
            label.extraPadding=true;label.UpdateMeshPadding();
        }
        foreach(var image in canvas.GetComponentsInChildren<Image>(true))
        {
            if(image.sprite!=null && UnityEditor.AssetDatabase.GetAssetPath(image.sprite).StartsWith(CoastalPath))
            {
                if(image.sprite.border!=Vector4.zero)image.type=UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier=image.sprite.name=="RowPanel"||image.sprite.name=="SelectionFrame"?1:2.8f;
            }
            // Catch legacy decorative surfaces on less-frequent screens without
            // touching actual video, model textures, health fills or gameplay art.
            string name=image.sprite!=null?image.sprite.name:"";
            if(new[]{"ParchmentPanel","WoodSign","GoldButton","WoodButton","WalletPill","NavigationTile","WindowFrame","BrassPlaque"}.Contains(name))
                SetCoastalImage(image,Art(name));
        }
        ReferenceLobby(canvas);
        if(AssetDatabase.LoadAssetAtPath<Sprite>(FaithfulPath+"StoryChrome.png")!=null)FaithfulPresentation(canvas);
    }

    private static void SetCoastalImage(Image image,Sprite sprite)
    {
        if(image==null||sprite==null)return;
        image.sprite=sprite;image.color=Color.white;image.type=sprite.border==Vector4.zero?UnityEngine.UI.Image.Type.Simple:UnityEngine.UI.Image.Type.Sliced;
        image.pixelsPerUnitMultiplier=1.5f;RemoveOutline(image.gameObject);
    }

    private static void CoastalButton(Button button,bool primary=false,bool dark=false)
    {
        if(button==null)return;
        if(button.targetGraphic is Image image)SetCoastalImage(image,Art(primary?"YellowButton":dark?"NavyButton":"IvoryPanel"));
        foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.color=dark?Cream:Ink;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.selectedColor=Color.white;
        colors.pressedColor=new Color(.78f,.85f,.94f);colors.disabledColor=new Color(.64f,.69f,.74f);button.colors=colors;
    }

    private static void CoastalClose(Button button)
    {
        ClearChildren(button.transform);SetCoastalImage(button.GetComponent<Image>(),Art("SquareButton"));
        Text(button.transform,"CloseLabel","X",48,Cream,.12f,.1f,.88f,.9f);
    }

    private static void CoastalLobby(CanvasScript canvas)
    {
        var center=canvas.transform.Find("UI/Main/Center");var plaque=center.Find("ChapterTitle");
        SetRect((RectTransform)plaque,.10f,.765f,.90f,.897f);SetCoastalImage(plaque.GetComponent<Image>(),Art("IvoryPanel"));
        Image(plaque,"NavyHeader",Art("NavyPanel"),0,.62f,1,1).transform.SetAsFirstSibling();
        Move(plaque,"Chapter",.14f,.675f,.86f,.90f);Move(plaque,"Name",.04f,.15f,.96f,.56f);
        plaque.Find("Name").GetComponent<TMP_Text>().color=Ink;
        plaque.Find("Name").GetComponent<TMP_Text>().fontSize=66;
        plaque.Find("Chapter").GetComponent<TMP_Text>().fontSize=40;
        Image(plaque,"Anchor",Art("Anchor"),.04f,.69f,.12f,.89f,true);
        var hint=center.Find("StartHint");Move(hint,"Title",.02f,.44f,.98f,1);
        hint.Find("Title").GetComponent<TMP_Text>().text="좌우로 움직여\n게임 시작";
        hint.Find("Title").GetComponent<TMP_Text>().fontSize=83;
        hint.Find("Left").gameObject.SetActive(false);hint.Find("Right").gameObject.SetActive(false);
        Move(hint,"Finger",.27f,0,.73f,.40f);
        var nav=canvas.transform.Find("UI/Main/Bottom");
        foreach(var label in nav.GetComponentsInChildren<TMP_Text>(true)){label.color=Ink;SetRect(label.rectTransform,.05f,.055f,.95f,.20f);label.fontSize=40;}
        foreach(var button in nav.GetComponentsInChildren<Button>(true))Move(button.transform,"Icon",.17f,.30f,.83f,.91f);
        nav.Find("Skin_Button/Icon").GetComponent<Image>().sprite=Art("Sneaker");
        foreach(var image in canvas.transform.Find("UI/Top/Resource").GetComponentsInChildren<Image>(true))if(image.name=="Coin"||image.name=="Jewel")SetCoastalImage(image,Art("Wallet"));
        foreach(Transform wallet in canvas.transform.Find("UI/Top/Resource"))
        {
            var icon=wallet.Find("Icon");if(icon!=null)SetRect((RectTransform)icon,.055f,.17f,.26f,.83f);
            foreach(var text in wallet.GetComponentsInChildren<TMP_Text>(true))SetRect(text.rectTransform,.28f,.12f,.93f,.88f);
        }
    }
}
