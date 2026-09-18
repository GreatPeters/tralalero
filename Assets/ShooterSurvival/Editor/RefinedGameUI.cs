using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public static class RefinedGameUI
{
    private static readonly Color Navy = new(.025f,.065f,.10f), Panel = new(.065f,.13f,.18f),
        White = new(.93f,.96f,.96f), Muted = new(.59f,.70f,.76f), Gold = new(.87f,.69f,.38f), Cyan = new(.36f,.83f,.95f);
    private static TMP_FontAsset body, bold;
    private static Sprite round, fade, jewel, coin;

    public static object BuildOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!NoryangjinMapToolWindow.IsMapToolScenePath(scene.path)) throw new InvalidOperationException("Map tool scene required.");
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas");
        BuildShop(canvas); BuildOpening(canvas);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return new { scene = scene.name, shop = "two-column equipment grid", story = "full-screen animation" };
    }
    private static void Setup()
    {
        bold = body = GameUIFont.Load();
        if (body == null) throw new InvalidOperationException("Create the CC0 Game UI font before rebuilding presentation.");
        jewel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/Image/Lobby/Upgrade/img_nav_diamond.png");
        coin = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/Image/Lobby/Upgrade/img_nav_coin.png");
        round = ShapeSprite("RoundedPanel", false); fade = ShapeSprite("BottomFade", true);
    }
    public static void BuildShop(GameObject canvas)
    {
        Setup();
        var shop = canvas.GetComponentInChildren<CosmeticShopUI>(true);
        var root = shop.transform;
        foreach (Transform child in root.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        var background = root.GetComponent<Image>(); background.sprite = null; background.color = Navy;
        var layer = root.GetComponent<Canvas>();
        if (layer == null) layer = root.gameObject.AddComponent<Canvas>();
        layer.overrideSorting = true; layer.sortingOrder = 110;
        if (root.GetComponent<GraphicRaycaster>() == null) root.gameObject.AddComponent<GraphicRaycaster>();
        if (root.GetComponent<CanvasGroup>() == null) root.gameObject.AddComponent<CanvasGroup>();
        var back = Button(root,"Back","뒤로",.04f,.943f,.17f,.981f,Panel,White,34);
        UnityEventTools.AddPersistentListener(back.onClick,shop.Close);
        Text(root,"Title","나만의 상어",56,White,.21f,.937f,.96f,.983f,true,TextAlignmentOptions.Left);
        Text(root,"Subtitle","모습도, 능력도 새롭게",31,Muted,.055f,.900f,.57f,.936f,false,TextAlignmentOptions.Left);
        var coins = Image(root,"CoinIcon",.60f,.904f,.65f,.929f,Color.white); coins.sprite=coin;coins.preserveAspect=true;
        shop.coinBalance=Text(root,"CoinBalance","0",34,Gold,.656f,.9f,.79f,.935f,false,TextAlignmentOptions.Left);
        var gems=Image(root,"JewelIcon",.8f,.904f,.852f,.929f,Color.white);gems.sprite=jewel;gems.preserveAspect=true;
        shop.jewelBalance=Text(root,"JewelBalance","0",34,Cyan,.86f,.9f,.98f,.935f,false,TextAlignmentOptions.Left);
        Image(root,"HeaderLine",.055f,.888f,.945f,.889f,new Color(.23f,.37f,.43f));
        shop.selectionName=Text(root,"SelectionName","",51,White,.055f,.817f,.945f,.873f,true);
        var previewArea=Rect(root,"PreviewArea",.09f,.553f,.91f,.825f);
        var display=Rect(previewArea,"Preview",0,0,1,1).gameObject.AddComponent<RawImage>();display.raycastTarget=true;
        var aspect=display.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=4f/3f;
        shop.preview=root.GetComponent<CosmeticPreview>()??root.gameObject.AddComponent<CosmeticPreview>();shop.preview.catalog=shop.visuals;shop.preview.display=display;
        display.gameObject.AddComponent<CosmeticPreviewDrag>().preview=shop.preview;
        Text(root,"RotateHint","드래그해서 둘러보기",26,Muted,.25f,.557f,.75f,.581f,false);
        var reset=Button(root,"ResetPreview","정면",.82f,.566f,.945f,.596f,Panel,Muted,25);
        UnityEventTools.AddPersistentListener(reset.onClick,shop.preview.ResetView);
        shop.skinTab=Button(root,"SkinTab","피부",.055f,.501f,.344f,.549f,Panel,White,41);
        shop.shoesTab=Button(root,"ShoesTab","신발",.355f,.501f,.644f,.549f,Panel,White,41);
        shop.hatTab=Button(root,"HatTab","모자",.655f,.501f,.945f,.549f,Panel,White,41);
        var viewport=Rect(root,"EquipmentViewport",.055f,.177f,.945f,.484f);viewport.gameObject.AddComponent<RectMask2D>();
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;scroll.scrollSensitivity=42;
        var track=Image(root,"EquipmentScrollbar",.955f,.18f,.969f,.481f,new Color(.07f,.15f,.20f),true);
        track.raycastTarget=true;
        var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
        var thumb=Image(track.transform,"Handle",0,0,1,1,Muted,true);
        thumb.raycastTarget=true;
        scrollbar.handleRect=thumb.rectTransform;scrollbar.targetGraphic=thumb;
        scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        var content=Rect(viewport,"Items",0,1,1,1);content.pivot=new Vector2(.5f,1);scroll.content=content;
        var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.spacing=new Vector2(18,18);grid.padding=new RectOffset(0,0,4,8);
        var layout=content.gameObject.AddComponent<EquipmentCardGrid>();layout.grid=grid;layout.scroll=scroll;layout.cardHeight=280;
        var template=Image(content,"ItemTemplate",0,0,1,1,new Color(.94f,.94f,.90f),true);template.raycastTarget=true;
        var choose=template.gameObject.AddComponent<Button>();choose.targetGraphic=template;
        Image(template.transform,"Selected",.06f,.023f,.94f,.04f,new Color(.08f,.48f,.51f),true);
        var icon=Image(template.transform,"Icon",.06f,.32f,.94f,.93f,Color.white);icon.preserveAspect=true;
        Text(template.transform,"Name","장비",36,Navy,.07f,.205f,.93f,.34f,true,TextAlignmentOptions.Left);
        var effect=Text(template.transform,"Effect","",29,new Color(.18f,.28f,.31f),.07f,.065f,.93f,.205f,false,TextAlignmentOptions.Left);effect.fontStyle=FontStyles.Bold;
        Text(template.transform,"State","",30,new Color(.05f,.35f,.39f),.60f,.86f,.93f,.97f,true,TextAlignmentOptions.Right);
        var currency=Image(template.transform,"CurrencyIcon",.53f,.868f,.64f,.967f,Color.white);currency.sprite=jewel;currency.preserveAspect=true;
        template.gameObject.SetActive(false);shop.itemTemplate=template.gameObject;shop.itemsRoot=content;shop.jewelIcon=jewel;shop.lightCards=true;
        Image(root,"Footer",0,0,1,.173f,Navy);
        shop.detailText=Text(root,"Detail","",35,Muted,.065f,.108f,.935f,.172f,false);
        shop.actionButton=Button(root,"PurchaseEquip","착용하기",.065f,.036f,.935f,.100f,Gold,Navy,45);
        shop.actionText=shop.actionButton.GetComponentInChildren<TMP_Text>();
        shop.actionCurrencyIcon=Image(shop.actionButton.transform,"Currency",.10f,.24f,.16f,.76f,Color.white);shop.actionCurrencyIcon.sprite=jewel;shop.actionCurrencyIcon.preserveAspect=true;
        shop.statusText=Text(root,"Status","",29,Muted,.065f,.004f,.935f,.03f,false);
        var entry=canvas.transform.Find("UI/Main/Bottom/Skin_Button").GetComponent<Button>();
        entry.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(entry.onClick,shop.Open);EditorUtility.SetDirty(entry);
        EditorUtility.SetDirty(shop);root.gameObject.SetActive(false);
    }
    public static void BuildOpening(GameObject canvas)
    {
        Setup();
        var old=canvas.transform.Find("OpeningStory");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=Rect(canvas.transform,"OpeningStory",0,0,1,1);
        var layer=root.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=200;root.gameObject.AddComponent<GraphicRaycaster>();
        Image(root,"Background",0,0,1,1,Navy).raycastTarget=true;
        var opening=root.gameObject.AddComponent<OpeningStoryUI>();
        opening.comic=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/JH/UI/Opening/Curse_Opening.png");
        opening.pages=Enumerable.Range(1,4).Select(i=>AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/JH/UI/Opening/Animated/Story_{i:D2}.png")).ToArray();
        opening.artwork=FullScreenArtwork(root,"Artwork");
        opening.movieDisplay=FullScreenArtwork(root,"MovieSurface");opening.movieDisplay.gameObject.SetActive(false);
        opening.movieDisplay.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
        var shade=Image(root,"CaptionShade",0,0,1,.39f,Color.white);shade.sprite=fade;
        var pages=Rect(root,"Pages",0,0,1,1);opening.pageContent=pages.gameObject;opening.pageFade=pages.gameObject.AddComponent<CanvasGroup>();
        opening.chapterText=Text(pages,"Chapter","",32,Gold,.06f,.932f,.72f,.974f,true,TextAlignmentOptions.Left);
        opening.titleText=Text(pages,"Title","",58,White,.065f,.199f,.935f,.26f,true,TextAlignmentOptions.Left);
        opening.captionText=Text(pages,"Caption","",42,new Color(.81f,.87f,.90f),.065f,.11f,.935f,.198f,false,TextAlignmentOptions.Left);
        var next=Button(pages,"Next","다음 장면",.62f,.034f,.935f,.091f,Gold,Navy,38);opening.nextText=next.GetComponentInChildren<TMP_Text>();UnityEventTools.AddPersistentListener(next.onClick,opening.Next);
        var replay=Button(pages,"Replay","다시 재생",.065f,.034f,.365f,.091f,Panel,White,35);UnityEventTools.AddPersistentListener(replay.onClick,opening.ReplayMovie);
        var skip=Button(root,"Skip","건너뛰기",.73f,.932f,.945f,.974f,new Color(.025f,.065f,.10f,.82f),White,31);UnityEventTools.AddPersistentListener(skip.onClick,opening.Skip);
        opening.video=root.gameObject.AddComponent<VideoPlayer>();opening.video.playOnAwake=false;opening.video.renderMode=VideoRenderMode.RenderTexture;opening.video.audioOutputMode=VideoAudioOutputMode.None;
        opening.movie=AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4");
        opening.autoPlayMovie=true;EditorUtility.SetDirty(opening);
        var entry=canvas.transform.Find("UI/Main/Bottom/Story_Button");
        if(entry!=null)
        {
            var button=entry.GetComponent<Button>();if(button==null)button=entry.gameObject.AddComponent<Button>();
            button.targetGraphic=entry.GetComponent<Image>();button.targetGraphic.raycastTarget=true;
            button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,opening.Open);EditorUtility.SetDirty(button);
        }
        if(canvas.scene.name=="HighWay" || canvas.scene.name=="RestStop")root.gameObject.SetActive(false);
    }
    private static RawImage FullScreenArtwork(Transform parent,string name)
    {
        var raw=Rect(parent,name,0,0,1,1).gameObject.AddComponent<RawImage>();raw.raycastTarget=false;
        var fit=raw.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=9f/16f;return raw;
    }
    private static RectTransform Rect(Transform parent,string name,float x,float y,float xx,float yy)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    private static Image Image(Transform parent,string name,float x,float y,float xx,float yy,Color color,bool rounded=false)
    {var r=Rect(parent,name,x,y,xx,yy);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;if(rounded){image.sprite=round;image.type=UnityEngine.UI.Image.Type.Sliced;}return image;}
    private static TMP_Text Text(Transform parent,string name,string value,float size,Color color,float x,float y,float xx,float yy,bool strong,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
    {var t=Rect(parent,name,x,y,xx,yy).gameObject.AddComponent<TextMeshProUGUI>();t.font=strong?bold:body;t.fontStyle=strong?FontStyles.Bold:FontStyles.Normal;t.text=value;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.85f;t.fontSizeMax=size;t.color=color;t.alignment=alignment;t.raycastTarget=false;return t;}
    private static Button Button(Transform parent,string name,string label,float x,float y,float xx,float yy,Color fill,Color ink,float size)
    {var i=Image(parent,name,x,y,xx,yy,fill,true);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;var c=b.colors;c.highlightedColor=new Color(1.1f,1.1f,1.1f);c.pressedColor=new Color(.8f,.8f,.8f);c.disabledColor=new Color(.4f,.5f,.55f,.75f);b.colors=c;Text(i.transform,"Label",label,size,ink,.05f,.1f,.95f,.9f,true);return b;}
    private static Sprite ShapeSprite(string name,bool gradient)
    {
        string folder="Assets/JH/UI/Refined",path=folder+"/"+name+".png";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing!=null)return existing;
        Directory.CreateDirectory(folder);int size=64;var t=new Texture2D(size,size,TextureFormat.RGBA32,false);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float a=gradient?Mathf.Pow(1-y/(float)(size-1),.6f):Mathf.Clamp01(18f-Vector2.Distance(new Vector2(x,y),new Vector2(Mathf.Clamp(x,18,45),Mathf.Clamp(y,18,45))));
            t.SetPixel(x,y,gradient?new Color(Navy.r,Navy.g,Navy.b,a):new Color(1,1,1,a));
        }
        t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=gradient?Vector4.zero:new Vector4(20,20,20,20);importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
