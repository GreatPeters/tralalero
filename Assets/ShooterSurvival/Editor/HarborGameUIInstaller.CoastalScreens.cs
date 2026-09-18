using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static partial class HarborGameUIInstaller
{
    private static void CoastalSettings(CanvasScript canvas)
    {
        var settings=canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>();var panel=settings.transform.Find("Panel");
        Move(settings.transform,"Panel",.065f,.14f,.935f,.865f);
        CoastalClose(panel.Find("Close").GetComponent<Button>());
        foreach(var image in panel.GetComponentsInChildren<Image>(true))
        {
            if(image.name=="Sound"||image.name=="Vibration")SetCoastalImage(image,Art("RowPanel"));
            if(image.name=="SwitchFrame")SetCoastalImage(image,Art("IvoryPanel"));
            if(image.name=="Knob"){image.color=Cream;}
            if(image.name=="Track")image.color=new Color(.39f,.45f,.54f);
            if(image.name=="Fill")image.color=new Color(.03f,.68f,.9f);
            if(image.name=="Handle"){image.sprite=UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");image.type=UnityEngine.UI.Image.Type.Simple;image.color=Cream;image.preserveAspect=true;}
        }
        foreach(var button in panel.GetComponentsInChildren<Button>(true))
        {
            if(button.name=="Close"||button.name=="Switch")continue;
            CoastalButton(button,button.name=="Resume");
        }
        Move(panel,"PrivacyPolicy",.07f,.212f,.93f,.272f);Move(panel,"Terms",.07f,.143f,.93f,.203f);
        var ads=panel.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.Ads.RewardedPrivacyOptionsButton>(true);
        if(ads!=null)SetRect(ads.GetComponent<RectTransform>(),.07f,.074f,.93f,.134f);
        Move(panel,"RunActions",.07f,.014f,.93f,.095f);
        Move(panel,"PrivacyPolicy",.07f,.30f,.93f,.36f);Move(panel,"Terms",.07f,.23f,.93f,.29f);
        if(ads!=null)SetRect(ads.GetComponent<RectTransform>(),.07f,.16f,.93f,.22f);
        Move(panel,"VolumeLabel",.09f,.56f,.90f,.61f);Move(panel,"Volume",.09f,.50f,.91f,.555f);
        Move(panel,"SensitivityLabel",.09f,.43f,.90f,.48f);Move(panel,"Sensitivity",.09f,.37f,.91f,.425f);
        Move(panel,"Sound",.06f,.745f,.94f,.83f);Move(panel,"Vibration",.06f,.635f,.94f,.72f);
        var actions=panel.Find("RunActions");CoastalButton(actions.Find("Resume").GetComponent<Button>(),true);CoastalButton(actions.Find("Retry").GetComponent<Button>());
    }

    private static void CoastalHud(CanvasScript canvas,PlayerScript player)
    {
        var hud=canvas.GetComponentInChildren<PlayerStatusHud>(true);ClearChildren(hud.transform);
        var panel=Image(hud.transform,"HealthCard",Art("IvoryPanel"),.025f,.858f,.79f,.981f);
        Image(panel.transform,"Heart",Art("Heart"),.035f,.63f,.13f,.9f,true);
        Text(panel.transform,"HealthLabel","체력",43,Ink,.15f,.62f,.35f,.91f,TextAlignmentOptions.Left);
        var health=Text(panel.transform,"Value","500 / 500",54,Ink,.37f,.60f,.955f,.94f,TextAlignmentOptions.Right);
        health.enableAutoSizing=true;health.fontSizeMin=38;health.fontSizeMax=54;
        var track=Image(panel.transform,"HealthTrack",MobileUIArt.Rounded,.05f,.36f,.95f,.55f);track.color=new Color(.06f,.09f,.12f);
        var fill=Image(panel.transform,"HealthFill",FlatFillSprite(),.057f,.38f,.943f,.53f);fill.color=new Color(.14f,.86f,.08f);
        Image(panel.transform,"Sword",Art("Attack"),.04f,.055f,.12f,.28f,true);
        Text(panel.transform,"AttackLabel","공격력",38,Ink,.15f,.055f,.41f,.27f,TextAlignmentOptions.Left);
        var attack=Text(panel.transform,"Attack","68",42,Ink,.42f,.055f,.92f,.27f,TextAlignmentOptions.Left);
        hud.Configure(health,fill,attack);canvas.ConfigurePlayerStatusHud(hud,player);
        var pause=canvas.pauseButton;SetRect(pause.GetComponent<RectTransform>(),.817f,.910f,.973f,.981f);
        SetCoastalImage(pause.GetComponent<Image>(),Art("SquareButton"));
    }

    private static void CoastalVideos(CanvasScript canvas)
    {
        foreach(var root in canvas.GetComponentsInChildren<OpeningStoryUI>(true).Select(x=>x.transform)
            .Concat(canvas.GetComponentsInChildren<ChapterTransitionUI>(true).Select(x=>x.transform)))
        {
            var content=root.Find("HarborContent");bool story=root.GetComponent<OpeningStoryUI>()!=null;
            var backing=content.Find("WoodBacking").GetComponent<Image>();backing.sprite=FlatFillSprite();backing.color=new Color(.02f,.085f,.23f);
            SetCoastalImage(content.Find("HeaderWood").GetComponent<Image>(),Art("NavyPanel"));
            SetCoastalImage(content.Find("HeaderRightCap").GetComponent<Image>(),Art("NavyPanel"));
            Move(content,"HeaderWood",.01f,.927f,.99f,.995f);content.Find("HeaderRightCap").gameObject.SetActive(false);
            Move(content,"StoryCounter",.055f,.942f,.60f,.98f);
            var counter=content.Find("StoryCounter").GetComponent<TMP_Text>();counter.fontSize=45;counter.color=Cream;
            Move(content,"SkipButton",.67f,.938f,.96f,.987f);CoastalButton(content.Find("SkipButton").GetComponent<Button>());
            Move(content,"MovieSlot",.019f,.19f,.981f,.923f);
            var frame=content.Find("MovieFrame").GetComponent<Image>();SetCoastalImage(frame,Art("Frame"));Move(content,"MovieFrame",.009f,.18f,.991f,.929f);
            Move(content,"CaptionShade",.02f,.19f,.98f,.42f);
            content.Find("CaptionDivider").gameObject.SetActive(false);
            Move(content,"SceneTitle",.05f,.29f,.95f,.34f);Move(content,"Caption",.065f,.205f,.935f,.294f);
            content.Find("SceneTitle").GetComponent<TMP_Text>().color=new Color(1,.85f,.12f);
            content.Find("Caption").GetComponent<TMP_Text>().color=Cream;
            content.Find("Caption").GetComponent<TMP_Text>().fontSize=43;
            content.Find("SceneTitle").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            content.Find("Caption").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            var markers=content.Cast<Transform>().Where(t=>t.name.StartsWith("StorySegment")).ToArray();
            for(int i=0;i<markers.Length;i++)
            {
                float width=story?.214f:.32f,left=story?.036f+i*.238f:.34f;
                SetRect((RectTransform)markers[i],left,.122f,left+width,.173f);
                SetCoastalImage(markers[i].GetComponent<Image>(),Art(i==0?"YellowButton":"IvoryPanel"));
                Text(markers[i],"Number",(i+1).ToString(),44,Ink,.05f,.08f,.95f,.92f);
            }
            if(story)
            {
                var movie=root.GetComponent<OpeningStoryUI>();movie.activeSceneSprite=Art("YellowButton");movie.inactiveSceneSprite=Art("IvoryPanel");
                Move(content,"ReplayButton",.03f,.035f,.47f,.105f);Move(content,"NextButton",.51f,.035f,.97f,.105f);
                CoastalButton(content.Find("ReplayButton").GetComponent<Button>());CoastalButton(content.Find("NextButton").GetComponent<Button>(),true);
                var replay=content.Find("ReplayButton");replay.GetComponentInChildren<TMP_Text>().text="이전 장면";
                SetRect(replay.GetComponentInChildren<TMP_Text>().rectTransform,.22f,.12f,.95f,.88f);
                Image(replay,"ReplayIcon",Art("Left"),.06f,.22f,.22f,.78f,true);
                var arrow=content.Find("NextArrow");if(arrow!=null)arrow.gameObject.SetActive(false);
            }
            else {Move(content,"TransitionStatus",.05f,.025f,.95f,.103f);}
        }
    }

    private static CoastalMessagePanel BuildCoastalMessage(CanvasScript canvas)
    {
        var old=canvas.transform.Find("CoastalMessage");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=Rect(canvas.transform,"CoastalMessage",0,0,1,1);Layer(root.gameObject,180);root.gameObject.SetActive(false);
        var dim=root.gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.67f);dim.raycastTarget=true;
        GetOrAdd<MobileSafeArea>(root.gameObject);
        var panel=Image(root,"Panel",Art("IvoryPanel"),.09f,.29f,.91f,.73f);
        Image(panel.transform,"Header",Art("NavyPanel"),0,.80f,1,1);
        var message=root.gameObject.AddComponent<CoastalMessagePanel>();
        message.heading=Text(panel.transform,"Title","안내",44,Cream,.07f,.835f,.78f,.95f,TextAlignmentOptions.Left);
        message.message=Text(panel.transform,"Message","",39,Ink,.09f,.27f,.91f,.72f);
        var close=Button(panel.transform,"Close","X",.80f,.835f,.95f,.961f);CoastalClose(close);UnityEventTools.AddPersistentListener(close.onClick,message.Close);
        var ok=Button(panel.transform,"OK","확인",.09f,.07f,.91f,.22f,true,44);UnityEventTools.AddPersistentListener(ok.onClick,message.Close);
        return message;
    }

    private static void CoastalRemaining(CanvasScript canvas)
    {
        var defeat=canvas.gameOverUI.GetComponent<DefeatPresentation>();
        if(defeat!=null)
        {
            var panel=defeat.transform.Find("Panel");CoastalClose(panel.Find("Close").GetComponent<Button>());
            Move(panel,"CoinEmblem",.36f,.65f,.64f,.83f);CoastalButton(defeat.rewardedButton);CoastalButton(defeat.continueButton,true);
            defeat.resultText.color=Ink;defeat.adStatusText.color=Muted;
        }
        var win=canvas.youWinUI.transform;ClearChildren(win);SetRect((RectTransform)win,0,0,1,1);Layer(win.gameObject,160);
        var backdrop=GetOrAdd<Image>(win.gameObject);backdrop.sprite=null;backdrop.color=new Color(0,0,0,.65f);backdrop.raycastTarget=true;
        var card=Image(win,"VictoryCard",Art("IvoryPanel"),.075f,.25f,.925f,.79f);
        Image(card.transform,"Header",Art("NavyPanel"),0,.82f,1,1);
        Text(card.transform,"Title","챕터 완료!",52,Cream,.08f,.855f,.80f,.958f);
        Image(card.transform,"Coin",Art("Jewel"),.34f,.46f,.66f,.74f,true);
        Text(card.transform,"Message","다음 도전을 준비하세요",39,Ink,.08f,.30f,.92f,.43f);
        var progression=canvas.GetComponent<ChapterProgression>();
        if(progression!=null)progression.clearRewardText=Text(card.transform,"ClearReward","",30,Ink,.08f,.235f,.92f,.30f);
        var back=Button(card.transform,"Continue","돌아가기",.09f,.07f,.91f,.23f,true,46);UnityEventTools.AddPersistentListener(back.onClick,canvas.LoadGame);
        var close=Button(card.transform,"Close","X",.82f,.86f,.95f,.97f);CoastalClose(close);UnityEventTools.AddPersistentListener(close.onClick,canvas.LoadGame);
        win.gameObject.SetActive(false);
        foreach(string path in new[]{"PauseMenu","UI/Setting","UI/Upgrade"})
        {
            var legacy=canvas.transform.Find(path);if(legacy==null)continue;
            foreach(var button in legacy.GetComponentsInChildren<Button>(true))CoastalButton(button);
            foreach(var label in legacy.GetComponentsInChildren<TMP_Text>(true))label.color=Ink;
        }
    }

    private static void CoastalTutorials(CanvasScript canvas)
    {
        var root=canvas.transform.Find("FTUE");if(root==null)root=Rect(canvas.transform,"FTUE",0,0,1,1);
        ClearChildren(root);SetRect((RectTransform)root,0,0,1,1);
        var image=GetOrAdd<Image>(root.gameObject);image.sprite=null;image.color=Color.clear;image.raycastTarget=false;Layer(root.gameObject,120);
        var panel=Image(root,"Panel",Art("IvoryPanel"),.07f,.20f,.93f,.37f);
        Image(panel.transform,"Header",Art("NavyPanel"),0,.71f,1,1);
        var tutorial=GetOrAdd<CoastalTutorialUI>(canvas.gameObject);tutorial.panel=root.gameObject;
        tutorial.title=Text(panel.transform,"Title","조작 안내",36,Cream,.055f,.75f,.86f,.94f,TextAlignmentOptions.Left);
        tutorial.body=Text(panel.transform,"Body","",38,Ink,.26f,.10f,.94f,.68f,TextAlignmentOptions.Left);
        tutorial.icon=Image(panel.transform,"Icon",Art("Swipe"),.035f,.12f,.235f,.67f,true);
        tutorial.icons=new[]{Art("Swipe"),Art("Attack"),Art("RocketCharm"),Art("Heart")};
        var close=Button(panel.transform,"Close","X",.875f,.76f,.97f,.95f);CoastalClose(close);UnityEventTools.AddPersistentListener(close.onClick,tutorial.Close);
        root.gameObject.SetActive(false);
    }
}
