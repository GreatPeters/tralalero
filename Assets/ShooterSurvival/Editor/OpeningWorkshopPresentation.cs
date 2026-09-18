#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Reuses the upgrade workshop's existing art; videos keep a dedicated full-frame area.
public static class OpeningWorkshopPresentation
{
    private static readonly Color Ink = new(.16f,.085f,.045f), MutedInk = new(.33f,.24f,.17f), Gold = new(.94f,.77f,.42f);
    private static Sprite paper, bar, background;
    private static TMP_FontAsset font;

    public static void ApplyOpening(GameObject canvas, TMP_FontAsset fontOverride = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        RefinedGameUI.BuildOpening(canvas);
        var opening = canvas.GetComponentInChildren<OpeningStoryUI>(true);
        Setup(fontOverride != null ? fontOverride : opening.captionText.font);
        var root = (RectTransform)opening.transform;
        var backgroundImage = root.Find("Background").GetComponent<Image>();
        backgroundImage.sprite = background; backgroundImage.color = new Color(.48f,.44f,.39f,1);
        var shade = root.Find("CaptionShade"); if (shade != null) UnityEngine.Object.DestroyImmediate(shade.gameObject);
        var header = Image(root,"WorkshopHeader",.055f,.933f,.945f,.982f,bar,Color.white).rectTransform;
        var mediaSlot = Rect(root,"MediaSlot",.055f,.25f,.945f,.915f);
        var frame = Image(mediaSlot,"Frame",0,0,1,1,paper,Color.white).rectTransform;
        var frameFit = frame.gameObject.AddComponent<AspectRatioFitter>();
        frameFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; frameFit.aspectRatio = 9f/16f;
        var viewport = Image(frame,"Viewport",.022f,.014f,.978f,.986f,null,new Color(.045f,.035f,.027f)).rectTransform;
        MoveArtwork(opening.artwork,viewport); MoveArtwork(opening.movieDisplay,viewport);
        var caption = Image(root,"StoryCard",.055f,.118f,.945f,.235f,bar,Color.white).rectTransform;
        opening.pageFade = caption.gameObject.AddComponent<CanvasGroup>();
        MoveText(opening.chapterText,header,.055f,.14f,.62f,.89f,27,FontWeight.Bold,Ink);
        MoveText(opening.titleText,caption,.055f,.58f,.945f,.91f,43,FontWeight.Bold,Ink);
        MoveText(opening.captionText,caption,.055f,.10f,.945f,.57f,31,FontWeight.Bold,Ink);
        var next = opening.nextText.GetComponentInParent<Button>(true);
        StyleButton(next,root,.395f,.035f,.945f,.101f,Gold,Ink,35);
        var replay = root.Find("Pages/Replay").GetComponent<Button>();
        StyleButton(replay,root,.055f,.035f,.35f,.101f,Color.white,Ink,31);
        replay.GetComponentInChildren<TMP_Text>(true).text = "다시 보기";
        var skip = root.Find("Skip").GetComponent<Button>();
        StyleButton(skip,header,.74f,.14f,.96f,.89f,new Color(1,1,1,0),MutedInk,25);
        skip.GetComponentInChildren<TMP_Text>(true).text = "건너뛰기";
        var oldPages=root.Find("Pages");if(oldPages!=null)UnityEngine.Object.DestroyImmediate(oldPages.gameObject);
        opening.pageContent=caption.gameObject;
        var track=Image(root,"StoryProgressTrack",.055f,.921f,.945f,.926f,null,new Color(.20f,.13f,.075f));
        var fill=Image(track.transform,"Fill",0,0,1,1,bar,Gold);fill.type=UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;
        opening.pageProgress=fill;
        header.SetAsLastSibling();
        EditorUtility.SetDirty(opening);
    }

    public static ChapterTransitionUI BuildTransition(GameObject canvas, TMP_FontAsset fontOverride = null)
    {
        var opening=canvas.GetComponentInChildren<OpeningStoryUI>(true);
        Setup(fontOverride != null ? fontOverride : opening.captionText.font);
        var previous=canvas.transform.Find("ChapterTransition");
        if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var root=Rect(canvas.transform,"ChapterTransition",0,0,1,1);
        var layer=root.gameObject.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=250;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var bg=Image(root,"Background",0,0,1,1,background,new Color(.48f,.44f,.39f));bg.raycastTarget=true;
        var transition=root.gameObject.AddComponent<ChapterTransitionUI>();
        var header=Image(root,"Header",.055f,.933f,.945f,.982f,bar,Color.white).rectTransform;
        Text(header,"Label","새로운 길",.06f,.14f,.65f,.9f,27,FontWeight.Bold,Ink);
        var mediaSlot=Rect(root,"MediaSlot",.055f,.25f,.945f,.915f);
        var frame=Image(mediaSlot,"Frame",0,0,1,1,paper,Color.white).rectTransform;
        var fit=frame.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=9f/16f;
        var viewport=Image(frame,"Viewport",.022f,.014f,.978f,.986f,null,new Color(.045f,.035f,.027f)).rectTransform;
        transition.display=Rect(viewport,"Movie",0,0,1,1).gameObject.AddComponent<RawImage>();transition.display.raycastTarget=false;
        var movieFit=transition.display.gameObject.AddComponent<AspectRatioFitter>();movieFit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;movieFit.aspectRatio=9f/16f;
        var card=Image(root,"Caption",.055f,.118f,.945f,.235f,bar,Color.white).rectTransform;
        transition.title=Text(card,"Title","",.055f,.58f,.945f,.91f,43,FontWeight.Bold,Ink);
        transition.caption=Text(card,"Caption","",.055f,.10f,.945f,.57f,31,FontWeight.Bold,Ink);
        transition.status=Text(root,"Status","",.08f,.045f,.70f,.097f,28,FontWeight.Regular,new Color(.96f,.85f,.65f));
        var skip=Image(root,"Skip",.72f,.035f,.945f,.101f,bar,Gold).gameObject.AddComponent<Button>();skip.targetGraphic=skip.GetComponent<Image>();
        Text(skip.transform,"Text","건너뛰기",.05f,.1f,.95f,.9f,29,FontWeight.Bold,Ink,TextAlignmentOptions.Center);
        UnityEventTools.AddPersistentListener(skip.onClick,transition.Skip);transition.skipButton=skip;
        transition.player=root.gameObject.AddComponent<VideoPlayer>();transition.player.playOnAwake=false;
        transition.player.renderMode=VideoRenderMode.RenderTexture;transition.player.audioOutputMode=VideoAudioOutputMode.None;
        root.gameObject.SetActive(false);EditorUtility.SetDirty(transition);return transition;
    }

    private static void Setup(TMP_FontAsset selected)
    {
        font=selected;if(font==null)throw new InvalidOperationException("A verified TMP font asset is required");
        paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/카드.png");
        bar=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/메뉴이름.png");
        background=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/Workshop_Background.png");
        if(paper==null||bar==null||background==null)throw new InvalidOperationException("Upgrade workshop art missing");
    }
    private static RectTransform Rect(Transform parent,string name,float x,float y,float xx,float yy)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);SetRect(rect,x,y,xx,yy);return rect;
    }
    private static void SetRect(RectTransform rect,float x,float y,float xx,float yy)
    {
        rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(xx,yy);rect.pivot=Vector2.one*.5f;rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
    private static Image Image(Transform parent,string name,float x,float y,float xx,float yy,Sprite sprite,Color color)
    {
        var image=Rect(parent,name,x,y,xx,yy).gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;
        if(sprite!=null&&sprite.border.sqrMagnitude>0)image.type=UnityEngine.UI.Image.Type.Sliced;return image;
    }
    private static TMP_Text Text(Transform parent,string name,string value,float x,float y,float xx,float yy,float size,FontWeight weight,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.Left)
    {
        var text=Rect(parent,name,x,y,xx,yy).gameObject.AddComponent<TextMeshProUGUI>();text.text=value;StyleText(text,size,weight,color);text.alignment=alignment;return text;
    }
    private static void StyleText(TMP_Text text,float size,FontWeight weight,Color color)
    {
        text.font=font;text.fontSize=size;text.fontWeight=weight;text.fontStyle=weight>=FontWeight.SemiBold?FontStyles.Bold:FontStyles.Normal;text.color=color;text.enableAutoSizing=false;
        text.characterSpacing=0;text.lineSpacing=3;text.outlineWidth=0;text.raycastTarget=false;text.alignment=TextAlignmentOptions.Left;
    }
    private static void MoveText(TMP_Text text,Transform parent,float x,float y,float xx,float yy,float size,FontWeight weight,Color color)
    {
        text.transform.SetParent(parent,false);SetRect(text.rectTransform,x,y,xx,yy);StyleText(text,size,weight,color);
    }
    private static void MoveArtwork(RawImage image,Transform parent)
    {
        image.transform.SetParent(parent,false);SetRect(image.rectTransform,0,0,1,1);
        var fit=image.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=9f/16f;
    }
    private static void StyleButton(Button button,Transform parent,float x,float y,float xx,float yy,Color color,Color ink,float size)
    {
        button.transform.SetParent(parent,false);SetRect((RectTransform)button.transform,x,y,xx,yy);
        var image=button.GetComponent<Image>();image.sprite=bar;image.color=color;image.raycastTarget=true;
        var label=button.GetComponentInChildren<TMP_Text>(true);StyleText(label,size,FontWeight.Bold,ink);label.alignment=TextAlignmentOptions.Center;
        SetRect(label.rectTransform,.055f,.10f,.945f,.9f);
    }
}
#endif
