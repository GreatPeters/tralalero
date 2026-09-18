using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

public static partial class HarborGameUIInstaller
{
    private static void BuildShop(CanvasScript canvas)
    {
        RefinedGameUI.BuildShop(canvas.gameObject);
        var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);var root=shop.transform;
        Layer(root.gameObject,110);
        var background=root.GetComponent<Image>();background.sprite=Art("ShopBackdrop");background.color=Color.white;background.type=UnityEngine.UI.Image.Type.Simple;background.raycastTarget=true;
        root.Find("Subtitle").gameObject.SetActive(false);root.Find("HeaderLine").gameObject.SetActive(false);
        var sign=Image(root,"HarborTitleSign",Art("WoodSign"),.15f,.936f,.555f,.99f);sign.transform.SetSiblingIndex(0);
        Move(root,"Title",.19f,.94f,.53f,.987f);
        var title=root.Find("Title").GetComponent<TMP_Text>();title.text="밀수 장비점";title.fontSize=41;title.color=Cream;title.alignment=TextAlignmentOptions.Center;
        var back=root.Find("Back").GetComponent<Button>();StyleButton(back,false);Move(root,"Back",.033f,.94f,.14f,.99f);
        back.GetComponentInChildren<TMP_Text>().text="";Image(back.transform,"Arrow",Art("BackArrow"),.2f,.13f,.8f,.87f,true);
        Image(root,"CoinWallet",Art("WalletPill"),.57f,.94f,.765f,.989f).transform.SetSiblingIndex(0);
        Image(root,"GemWallet",Art("WalletPill"),.78f,.94f,.97f,.989f).transform.SetSiblingIndex(0);
        Move(root,"CoinIcon",.583f,.947f,.635f,.982f);Move(root,"JewelIcon",.793f,.947f,.845f,.982f);
        SetRect(shop.coinBalance.rectTransform,.638f,.945f,.753f,.984f);SetRect(shop.jewelBalance.rectTransform,.85f,.945f,.958f,.984f);
        foreach(var value in new[]{shop.coinBalance,shop.jewelBalance}){value.color=Ink;value.fontSize=32;value.enableAutoSizing=true;value.fontSizeMin=20;value.fontSizeMax=32;}
        SetRect(shop.selectionName.rectTransform,.08f,.878f,.92f,.93f);shop.selectionName.color=Cream;shop.selectionName.fontSize=40;
        Move(root,"PreviewArea",.075f,.545f,.925f,.878f);
        shop.preview.platformTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(VideoPath+"WoodBackground.png");
        Move(root,"RotateHint",.13f,.533f,.78f,.558f);root.Find("RotateHint").GetComponent<TMP_Text>().color=Cream;
        Move(root,"ResetPreview",.815f,.534f,.945f,.566f);StyleButton(root.Find("ResetPreview").GetComponent<Button>(),false);
        Button[] tabs={shop.skinTab,shop.shoesTab,shop.hatTab};string[] icons={"SkinTabFin","LateralSneaker","HatTabCap"};
        for(int i=0;i<tabs.Length;i++)
        {
            SetRect((RectTransform)tabs[i].transform,.045f+i*.308f,.456f,.337f+i*.308f,.523f);StyleButton(tabs[i],false);
            var label=tabs[i].GetComponentInChildren<TMP_Text>();SetRect(label.rectTransform,.35f,.1f,.96f,.9f);label.fontSize=37;
            Image(tabs[i].transform,"TabIcon",Art(icons[i]),.07f,.17f,.34f,.84f,true);
        }
        shop.activeTabSprite=Art("GoldButton");shop.inactiveTabSprite=Art("WoodButton");
        Move(root,"EquipmentViewport",.045f,.158f,.95f,.446f);Move(root,"EquipmentScrollbar",.958f,.163f,.973f,.44f);
        var grid=shop.itemsRoot.GetComponent<EquipmentCardGrid>();grid.cardHeight=308;grid.grid.spacing=new Vector2(18,18);grid.grid.padding=new RectOffset(4,4,4,20);
        var template=shop.itemTemplate;template.GetComponent<Image>().sprite=Art("ParchmentPanel");template.GetComponent<Image>().type=UnityEngine.UI.Image.Type.Sliced;template.GetComponent<Image>().color=Color.white;RemoveOutline(template);
        Move(template.transform,"Icon",.10f,.43f,.90f,.94f);
        Move(template.transform,"Name",.075f,.275f,.925f,.425f);Move(template.transform,"Effect",.07f,.155f,.93f,.275f);
        Move(template.transform,"State",.46f,.035f,.91f,.15f);Move(template.transform,"CurrencyIcon",.32f,.046f,.43f,.139f);
        foreach(var text in template.GetComponentsInChildren<TMP_Text>(true)){text.color=Ink;text.alignment=TextAlignmentOptions.Center;}
        template.transform.Find("Name").GetComponent<TMP_Text>().fontSize=33;
        template.transform.Find("Effect").GetComponent<TMP_Text>().fontSize=28;
        var marker=template.transform.Find("Selected").GetComponent<Image>();SetRect(marker.rectTransform,0,0,1,1);marker.sprite=Art("WindowFrame");marker.type=UnityEngine.UI.Image.Type.Sliced;marker.pixelsPerUnitMultiplier=2;marker.color=new Color(1,.8f,.35f);marker.transform.SetAsLastSibling();
        var footer=root.Find("Footer").GetComponent<Image>();footer.sprite=Art("WoodSign");footer.type=UnityEngine.UI.Image.Type.Sliced;footer.color=Color.white;
        SetRect(footer.rectTransform,0,0,1,.149f);footer.pixelsPerUnitMultiplier=2;
        shop.detailText.gameObject.SetActive(false);
        SetRect(shop.statusText.rectTransform,.05f,.11f,.95f,.147f);shop.statusText.color=Cream;shop.statusText.fontSize=28;
        StyleButton(shop.actionButton,true);SetRect((RectTransform)shop.actionButton.transform,.075f,.032f,.925f,.109f);shop.actionText.fontSize=46;
        grid.RefreshLayout();EditorUtility.SetDirty(shop);EditorUtility.SetDirty(shop.preview);
    }

    private static void Move(Transform root,string name,float x,float y,float xx,float yy)
    {var target=root.Find(name) as RectTransform;if(target!=null)SetRect(target,x,y,xx,yy);}

    private static void StyleButton(Button button,bool gold)
    {
        if(button.targetGraphic is TMP_Text link)
        {
            var linkColors=button.colors;linkColors.normalColor=Color.white;linkColors.highlightedColor=new Color(1f,.8f,.35f);linkColors.selectedColor=Color.white;button.colors=linkColors;
            return;
        }
        var image=button.targetGraphic as Image;
        if(image==null)image=GetOrAdd<Image>(button.gameObject);
        button.targetGraphic=image;
        image.pixelsPerUnitMultiplier=1.75f;
        image.sprite=Art(gold?"GoldButton":"WoodButton");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;image.raycastTarget=true;RemoveOutline(image.gameObject);
        foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.color=gold?Ink:Cream;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.selectedColor=Color.white;colors.pressedColor=new Color(.76f,.65f,.49f);colors.disabledColor=new Color(.60f,.57f,.51f);button.colors=colors;
    }

    private static RectTransform MakeVideoContent(Transform root,float fontScale,int order)
    {
        root.gameObject.SetActive(false);
        ClearChildren(root);
        var oldSafe=root.GetComponent<MobileSafeArea>();if(oldSafe!=null)Object.DestroyImmediate(oldSafe);
        SetRect((RectTransform)root,0,0,1,1);Layer(root.gameObject,order);
        var blocking=GetOrAdd<Image>(root.gameObject);blocking.sprite=null;blocking.color=new Color(.075f,.04f,.02f);blocking.raycastTarget=true;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(VideoPath+"HarborVideo_A.prefab");
        var content=(GameObject)PrefabUtility.InstantiatePrefab(source,root.gameObject.scene);content.transform.SetParent(root,false);
        PrefabUtility.UnpackPrefabInstance(content,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        content.name="HarborContent";
        Object.DestroyImmediate(content.GetComponent<GraphicRaycaster>());Object.DestroyImmediate(content.GetComponent<CanvasScaler>());Object.DestroyImmediate(content.GetComponent<Canvas>());
        var rect=(RectTransform)content.transform;SetRect(rect,0,0,1,1);
        // A standalone overlay Canvas stores a camera-sized root scale. Once its
        // Canvas is removed, normalize the nested RectTransform explicitly.
        rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;rect.localPosition=Vector3.zero;
        GetOrAdd<MobileSafeArea>(content);
        foreach(var label in content.GetComponentsInChildren<TMP_Text>(true)){label.fontSize*=fontScale;EditorUtility.SetDirty(label);}
        content.transform.Find("WoodBacking").GetComponent<Image>().raycastTarget=true;
        return rect;
    }

    private static VideoPlayer KeepVideoPlayer(VideoPlayer old,GameObject root)
    {
        if(old!=null&&old.gameObject==root)return old;
        var player=GetOrAdd<VideoPlayer>(root);if(old!=null)EditorUtility.CopySerialized(old,player);return player;
    }

    private static Image[] MakeIndicators(Transform content,int count)
    {
        foreach(var old in content.Cast<Transform>().Where(t=>t.name.StartsWith("StorySegment")).ToArray())Object.DestroyImmediate(old.gameObject);
        float width=Mathf.Min(.12f,.7f/count-.015f),gap=.018f,total=width*count+gap*(count-1),left=(1-total)/2;
        var result=new Image[count];
        for(int i=0;i<count;i++)result[i]=Image(content,"StorySegment"+(i+1),VideoArt(i==0?"ProgressOn":"ProgressOff"),left+i*(width+gap),.104f,left+i*(width+gap)+width,.123f);
        return result;
    }

    private static void BuildVideos(CanvasScript canvas)
    {
        float fontScale=canvas.GetComponent<CanvasScaler>().referenceResolution.x/887f;
        var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);
        story.video=KeepVideoPlayer(story.video,story.gameObject);
        var content=MakeVideoContent(story.transform,fontScale,200);
        story.movieDisplay=content.Find("MovieSlot/MovieDisplay").GetComponent<RawImage>();story.movieDisplay.texture=null;
        var fallback=Rect(story.movieDisplay.transform.parent,"FallbackArtwork",0,0,1,1).gameObject.AddComponent<RawImage>();fallback.raycastTarget=false;fallback.transform.SetAsFirstSibling();
        var fit=fallback.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=9f/16f;
        story.artwork=fallback;story.pageFade=null;story.pageContent=content.gameObject;
        story.chapterText=content.Find("StoryCounter").GetComponent<TMP_Text>();story.titleText=content.Find("SceneTitle").GetComponent<TMP_Text>();story.captionText=content.Find("Caption").GetComponent<TMP_Text>();
        story.nextText=content.Find("NextButton/NextLabel").GetComponent<TMP_Text>();
        story.sceneIndicators=MakeIndicators(content,OpeningStoryUI.MovieSceneCount);story.activeSceneSprite=VideoArt("ProgressOn");story.inactiveSceneSprite=VideoArt("ProgressOff");story.pageProgress=null;
        var skip=content.Find("SkipButton").GetComponent<Button>();skip.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(skip.onClick,story.Skip);
        var replay=content.Find("ReplayButton").GetComponent<Button>();replay.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(replay.onClick,story.Previous);story.previousButton=replay;
        var next=content.Find("NextButton").GetComponent<Button>();next.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(next.onClick,story.Next);
        story.autoPlayMovie=true;story.gameObject.SetActive(true);EditorUtility.SetDirty(story);
        foreach(var transition in canvas.GetComponentsInChildren<ChapterTransitionUI>(true))
        {
            transition.player=KeepVideoPlayer(transition.player,transition.gameObject);
            var view=MakeVideoContent(transition.transform,fontScale,210);
            foreach(string name in new[]{"ReplayButton","NextButton","NextArrow"}){var item=view.Find(name);if(item!=null)Object.DestroyImmediate(item.gameObject);}
            MakeIndicators(view,1);
            view.Find("StoryCounter").GetComponent<TMP_Text>().text="다음 챕터";
            transition.display=view.Find("MovieSlot/MovieDisplay").GetComponent<RawImage>();transition.display.texture=null;
            transition.title=view.Find("SceneTitle").GetComponent<TMP_Text>();transition.caption=view.Find("Caption").GetComponent<TMP_Text>();
            transition.status=Text(view,"TransitionStatus","",34,Cream,.08f,.04f,.92f,.095f);
            transition.skipButton=view.Find("SkipButton").GetComponent<Button>();transition.skipButton.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(transition.skipButton.onClick,transition.Skip);
            transition.gameObject.SetActive(false);EditorUtility.SetDirty(transition);
        }
    }

    private static void StyleOtherSurfaces(CanvasScript canvas)
    {
        if(canvas.pauseButton!=null&&canvas.pauseButton.GetComponent<Button>()!=null)StyleButton(canvas.pauseButton.GetComponent<Button>(),false);
        var roots=new[]{canvas.pauseMenuUI.transform,canvas.settingsMenuUI.transform,canvas.gameOverUI.transform,canvas.youWinUI.transform,canvas.transform.Find("UI/Setting"),canvas.GetComponentInChildren<PlayerStatusHud>(true)?.transform};
        foreach(var root in roots)
        {
            if(root==null)continue;
            var backdrop=root.GetComponent<Image>();if(backdrop!=null&&backdrop.sprite==null)backdrop.color=new Color(.09f,.05f,.025f,.92f);
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.type==UnityEngine.UI.Image.Type.Filled)
                {if(root.GetComponent<PlayerStatusHud>()!=null)image.color=new Color(.25f,.85f,.08f);continue;}
                if(image.sprite==MobileUIArt.Rounded || image.sprite!=null&&image.sprite.name=="RoundedPanel")
                {
                    if(image.transform.Find("Fill")!=null){image.sprite=null;image.color=Color.clear;}
                    else {image.sprite=Art("ParchmentPanel");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;}
                    RemoveOutline(image.gameObject);
                }
                if(image.sprite==Art("ParchmentPanel")&&image.rectTransform.rect.height>image.rectTransform.rect.width)
                    image.type=UnityEngine.UI.Image.Type.Tiled;
            }
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var parent=label.GetComponentInParent<Image>(true);label.color=parent!=null&&parent.sprite==Art("ParchmentPanel")?Ink:Cream;
            }
            foreach(var button in root.GetComponentsInChildren<Button>(true))StyleButton(button,button.name.Contains("Resume")||button.name.Contains("Retry")||button.name.Contains("Next")||button.name.Contains("Watch"));
        }
    }
}
