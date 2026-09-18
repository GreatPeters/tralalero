#if UNITY_EDITOR
using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public static class WorkshopPresentationUI
{
    private static TMP_FontAsset font;
    private static Sprite paper, bar, jewel, backdrop;
    private static readonly Color Ink = new Color(.16f,.075f,.035f);
    private static readonly Color Cream = new Color(.98f,.87f,.66f);
    private static readonly Color Brown = new Color(.16f,.08f,.04f);
    public static object BuildBothScenes()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset");
        paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/카드.png");
        bar=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/Image/Lobby/Upgrade/img_nav_bar.png");
        jewel=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/Image/Lobby/Upgrade/img_nav_diamond.png");
        backdrop=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/Workshop_Background.png");
        foreach(string name in new[]{"Noryangjin_MapTool_Mode","Noryangjin_MapTool_Mode_SR18"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");
            BuildShop(canvas); BuildDefeat(canvas); BuildOpening(canvas);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        return new{scenes=2,shop="Jewel workshop",openingPages=4};
    }
    private static void BuildShop(GameObject canvas) => RefinedGameUI.BuildShop(canvas);
    public static void BuildDefeat(GameObject canvas, TMP_FontAsset fontOverride = null)
    {
        font = fontOverride != null ? fontOverride : GameUIFont.Load();
        paper = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/카드.png");
        bar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/메뉴이름.png");
        var controller=canvas.GetComponent<CanvasScript>();var root=controller.gameOverUI.transform;
        foreach(Transform child in root.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        if(root.TryGetComponent<Image>(out var bg)){bg.sprite=null;bg.color=new Color(.045f,.02f,.015f,.84f);bg.raycastTarget=true;}
        var panel=Image(root,"AltarCard",new Vector2(.07f,.22f),new Vector2(.93f,.76f),Color.white);panel.sprite=paper;panel.type=UnityEngine.UI.Image.Type.Sliced;
        Text(panel.transform,"Eyebrow","이번 도전",26,new Color(.45f,.27f,.12f),.07f,.87f,.93f,.94f);
        Text(panel.transform,"Title","도전 결과",56,Ink,.07f,.72f,.93f,.86f);
        Text(panel.transform,"Story","모은 코인으로 장비를 강화해 보세요.",28,Ink,.08f,.51f,.92f,.59f);
        var presentation=root.GetComponent<DefeatPresentation>()??root.gameObject.AddComponent<DefeatPresentation>();
        presentation.resultText=Text(panel.transform,"Result","",36,Ink,.08f,.59f,.92f,.73f);
        var rewarded=Button(panel.transform,"WatchRewardedVideo","광고 보고 추가 코인 받기",.10f,.29f,.90f,.41f,bar);
        rewarded.GetComponent<Image>().color = new Color(.96f,.80f,.46f);
        UnityEventTools.AddPersistentListener(rewarded.onClick,presentation.WatchRewardedVideo);
        presentation.rewardedButton=rewarded; presentation.rewardedText=rewarded.GetComponentInChildren<TMP_Text>(true);
        presentation.adStatusText=Text(panel.transform,"AdStatus","",23,new Color(.42f,.30f,.20f),.08f,.22f,.92f,.28f);
        var button=Button(panel.transform,"ReturnToAltar","돌아가기",.10f,.07f,.90f,.19f,bar);
        UnityEventTools.AddPersistentListener(button.onClick,presentation.ReturnToAltar);
        presentation.continueButton=button;
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) label.fontStyle = FontStyles.Bold;
        root.gameObject.SetActive(false);EditorUtility.SetDirty(presentation);
    }
    private static void BuildOpening(GameObject canvas) => RefinedGameUI.BuildOpening(canvas);
    public static TMP_Text BuildVictory(GameObject canvas, TMP_FontAsset selectedFont)
    {
        font=selectedFont;
        paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/카드.png");
        bar=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JH/UI/Upgrade/메뉴이름.png");
        var root=canvas.GetComponent<CanvasScript>().youWinUI.transform;
        foreach(Transform child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        if(root.TryGetComponent<Image>(out var bg)){bg.sprite=null;bg.color=new Color(.045f,.02f,.015f,.84f);bg.raycastTarget=true;}
        var panel=Image(root,"VictoryCard",new Vector2(.07f,.23f),new Vector2(.93f,.77f),Color.white);panel.sprite=paper;panel.type=UnityEngine.UI.Image.Type.Sliced;
        Text(panel.transform,"Eyebrow","길의 끝에 도착했습니다",27,Ink,.07f,.85f,.93f,.94f);
        Text(panel.transform,"Title","챕터 완료",56,Ink,.07f,.64f,.93f,.84f);
        var reward=Text(panel.transform,"ChapterClearReward","",37,Ink,.07f,.42f,.93f,.61f);
        var chapter=canvas.GetComponent<ChapterProgression>();
        bool onward=!string.IsNullOrEmpty(chapter.nextScene);
        var replay=Button(panel.transform,"ReplayChapter","다시 도전",.1f,.1f,onward?.48f:.9f,.27f,bar);
        replay.GetComponent<Image>().color=new Color(.96f,.8f,.46f);
        UnityEventTools.AddPersistentListener(replay.onClick,chapter.Replay);
        if(onward){var next=Button(panel.transform,"NextChapter","다음 챕터",.52f,.1f,.9f,.27f,bar);UnityEventTools.AddPersistentListener(next.onClick,chapter.LoadNextChapter);}
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))text.fontStyle=FontStyles.Bold;
        root.gameObject.SetActive(false);
        return reward;
    }
    private static RectTransform Rect(Transform parent,string name,float x,float y,float xx,float yy)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    private static Image Image(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {var r=Rect(parent,name,min.x,min.y,max.x,max.y);var i=r.gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
    private static TMP_Text Text(Transform parent,string name,string value,float size,Color color,float x,float y,float xx,float yy,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
    {var r=Rect(parent,name,x,y,xx,yy);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.color=color;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.8f;t.fontSizeMax=size;t.alignment=alignment;t.raycastTarget=false;return t;}
    private static Button Button(Transform parent,string name,string label,float x,float y,float xx,float yy,Sprite sprite)
    {var i=Image(parent,name,new Vector2(x,y),new Vector2(xx,yy),Color.white);i.sprite=sprite;i.type=UnityEngine.UI.Image.Type.Sliced;i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;Text(i.transform,"Label",label,27,Ink,.035f,.08f,.965f,.92f);return b;}
}
#endif
