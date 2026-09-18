using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class HarborGameUIInstaller
{
    private const string PresetFolder = "Assets/ShooterSurvival/Resources/UI/HarborMaterials";

    public static void PolishOpenScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var scene=SceneManager.GetActiveScene();
        var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<CanvasScript>();
        var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerScript>(true)).Single();
        BuildHarborSettings(canvas);
        BuildReadableHud(canvas,player);
        BuildHarborDefeat(canvas);
        PolishShopHeader(canvas);
        var upgrades=canvas.transform.Find("UI/Upgrade2");
        var back=upgrades.Find("Back")?.GetComponent<Button>();
        if(back!=null)MakeUpperClose(back);
        var chapterCatalog=AssetDatabase.LoadAssetAtPath<ChapterUpgradeCatalog>("Assets/ShooterSurvival/Resources/Upgrades/ChapterWorkshop.asset");
        int[] prices={1500,5000,12000};
        if(chapterCatalog.pricingVersion<1)
        {
            foreach(var row in chapterCatalog.entries)if(row.chapter>=1&&row.chapter<=3)row.coinCost=prices[row.chapter-1];
            chapterCatalog.pricingVersion=1;
        }
        EditorUtility.SetDirty(chapterCatalog);AssetDatabase.SaveAssetIfDirty(chapterCatalog);
        foreach(var label in upgrades.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.name=="Explanation")label.text="해금한 챕터마다 최대 5회 영구 강화";
            if(label.name=="Effects"){label.fontSize=34;label.enableAutoSizing=true;label.fontSizeMin=30;label.fontSizeMax=34;}
        }
        var camera=player.GetComponentInChildren<Camera>(true) ?? Object.FindFirstObjectByType<StableGameplayCamera>()?.GetComponent<Camera>();
        if(camera!=null&&camera.GetComponent<StableGameplayCamera>()==null)
        {
            var follow=camera.gameObject.AddComponent<StableGameplayCamera>();follow.Configure(player.transform);
            camera.transform.SetParent(null,true);
            EditorUtility.SetDirty(follow);
        }
        ApplyTextPresets(canvas.transform);
        foreach(var button in canvas.GetComponentsInChildren<Button>(true))GetOrAdd<GameUIButtonSound>(button.gameObject);
        foreach(var component in canvas.GetComponentsInChildren<Component>(true))
        {
            if(component==null)continue;EditorUtility.SetDirty(component);
            if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }

    private static Material TextPreset(string name, bool light)
    {
        if(!AssetDatabase.IsValidFolder(PresetFolder))AssetDatabase.CreateFolder("Assets/ShooterSurvival/Resources/UI","HarborMaterials");
        string path=PresetFolder+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Font.material){name=name};AssetDatabase.CreateAsset(material,path);}
        material.CopyPropertiesFromMaterial(Font.material);
        material.SetColor("_FaceColor",Color.white);
        material.SetFloat("_FaceDilate",0f);
        material.SetTexture("_MainTex",Font.atlasTextures[0]);
        material.SetColor("_OutlineColor",new Color(.015f,.04f,.12f,1));
        material.SetFloat("_OutlineWidth",light?.065f:0f);
        if(light)material.EnableKeyword("OUTLINE_ON");else material.DisableKeyword("OUTLINE_ON");
        if(light)
        {
            material.EnableKeyword("UNDERLAY_ON");material.SetColor("_UnderlayColor",new Color(0,0,0,.65f));
            material.SetFloat("_UnderlayOffsetY",-.10f);material.SetFloat("_UnderlayDilate",0f);material.SetFloat("_UnderlaySoftness",.08f);
        }
        else material.DisableKeyword("UNDERLAY_ON");
        EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);return material;
    }

    private static void ApplyTextPresets(Transform root)
    {
        var cream=TextPreset("Harbor_Cream_Outline",true);var ink=TextPreset("Harbor_Ink_Readable",false);
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(text.font!=Font)continue;
            bool light=text.color.grayscale>.52f;
            text.fontSharedMaterial=light?cream:ink;
            if(light)text.color=Cream;
            if(text.fontSize<32)text.fontSize=32;
            if(text.enableAutoSizing){text.fontSizeMin=Mathf.Max(28,text.fontSizeMin);text.fontSizeMax=Mathf.Max(text.fontSize,text.fontSizeMax);}
            text.extraPadding=true;text.UpdateMeshPadding();RemoveOutline(text.gameObject);
        }
    }

    private static Image Solid(Transform parent,string name,Color color,float x,float y,float xx,float yy)
    {var image=Image(parent,name,null,x,y,xx,yy);image.color=color;return image;}

    private static RectTransform HarborModal(Transform root,string title,float bottom,float top)
    {
        ClearChildren(root);SetRect((RectTransform)root,0,0,1,1);Layer(root.gameObject,160);
        var dim=GetOrAdd<Image>(root.gameObject);dim.sprite=null;dim.color=new Color(0,0,0,.72f);dim.raycastTarget=true;
        var safe=GetOrAdd<MobileSafeArea>(root.gameObject);safe.enabled=true;
        var panel=Image(root,"Panel",Art("ParchmentPanel"),.065f,bottom,.935f,top);panel.type=UnityEngine.UI.Image.Type.Tiled;
        Image(panel.transform,"Header",Art("WoodSign"),0,.875f,1,1);
        Text(panel.transform,"Title",title,52,Cream,.07f,.887f,.78f,.985f,TextAlignmentOptions.Left);
        return panel.rectTransform;
    }

    private static void MakeUpperClose(Button button)
    {
        SetRect((RectTransform)button.transform,.852f,.939f,.967f,.992f);
        ClearChildren(button.transform);StyleButton(button,false);
        Text(button.transform,"CloseLabel","닫기",32,Cream,.05f,.08f,.95f,.92f);
    }

    private static Sprite FlatFillSprite()
    {
        const string path=PresetFolder+"/FlatFill.asset";
        var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if(sprite!=null)return sprite;
        TextPreset("Harbor_Ink_Readable",false);
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name="FlatFill"};
        texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();
        AssetDatabase.CreateAsset(texture,path);
        sprite=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),100);sprite.name="FlatFill";
        AssetDatabase.AddObjectToAsset(sprite,texture);AssetDatabase.SaveAssets();return sprite;
    }

    private static void BuildHarborSettings(CanvasScript canvas)
    {
        var root=canvas.transform.Find("SettingsMenu");root.gameObject.SetActive(false);
        var oldPrivacy=root.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.Ads.RewardedPrivacyOptionsButton>(true).FirstOrDefault();
        GameObject privacy=oldPrivacy!=null?oldPrivacy.gameObject:null;
        if(privacy!=null)privacy.transform.SetParent(canvas.transform,false);
        var panel=HarborModal(root,"설정",.135f,.865f);
        var settings=GetOrAdd<HarborSettingsPanel>(root.gameObject);settings.owner=canvas;
        var close=Button(panel,"Close","닫기 X",.785f,.905f,.967f,.970f,false,30);
        UnityEventTools.AddPersistentListener(close.onClick,settings.Close);
        (Button button,TMP_Text label,Image track,RectTransform knob) SwitchRow(string name,string title,float y)
        {
            var row=Image(panel,name,Art("WalletPill"),.06f,y,.94f,y+.115f);
            Text(row.transform,"Label",title,45,Ink,.055f,.15f,.47f,.85f,TextAlignmentOptions.Left);
            var housing=Image(row.transform,"SwitchFrame",Art("GoldButton"),.625f,.16f,.95f,.84f);
            var track=Image(housing.transform,"Switch",MobileUIArt.Rounded,.065f,.11f,.935f,.89f);track.color=new Color(.22f,.43f,.20f);
            track.raycastTarget=true;var button=track.gameObject.AddComponent<Button>();button.targetGraphic=track;
            var knob=Image(track.transform,"Knob",AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),.60f,.12f,.96f,.88f,true);knob.color=new Color(1,.81f,.4f);
            var label=Text(row.transform,"State","ON",36,Ink,.46f,.15f,.62f,.85f);
            return(button,label,track,knob.rectTransform);
        }
        var sound=SwitchRow("Sound","사운드",.72f);var vibration=SwitchRow("Vibration","진동",.585f);
        settings.soundButton=sound.button;settings.soundState=sound.label;settings.soundTrack=sound.track;settings.soundKnob=sound.knob;
        settings.vibrationButton=vibration.button;settings.vibrationState=vibration.label;settings.vibrationTrack=vibration.track;settings.vibrationKnob=vibration.knob;
        Slider SliderRow(string name,string title,float y,float min,float max)
        {
            Text(panel,name+"Label",title,37,Ink,.09f,y+.067f,.90f,y+.115f,TextAlignmentOptions.Left);
            var rect=Rect(panel,name,.09f,y,.91f,y+.065f);var slider=rect.gameObject.AddComponent<Slider>();
            Solid(rect,"Track",new Color(.26f,.16f,.08f),.01f,.32f,.99f,.65f);
            var fillArea=Rect(rect,"FillArea",.025f,.35f,.975f,.62f);
            slider.fillRect=Solid(fillArea,"Fill",new Color(.92f,.57f,.13f),0,0,1,1).rectTransform;
            var handleArea=Rect(rect,"HandleArea",.02f,0,.98f,1);
            var handle=Image(handleArea,"Handle",Art("GoldButton"),0,.1f,0,.9f);handle.rectTransform.sizeDelta=new Vector2(56,0);
            slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=min;slider.maxValue=max;
            return slider;
        }
        settings.volume=SliderRow("Volume","음량",.43f,0,1);
        settings.sensitivity=SliderRow("Sensitivity","좌우 조작 감도",.292f,.3f,2.5f);
        canvas.volumeSlider=settings.volume;canvas.sensitivitySlider=settings.sensitivity;
        var policy=Button(panel,"PrivacyPolicy","개인정보 처리방침",.07f,.211f,.53f,.273f,false,30);
        var terms=Button(panel,"Terms","이용약관",.55f,.211f,.93f,.273f,false,32);
        settings.privacyButton=policy;settings.termsButton=terms;
        policy.interactable=!string.IsNullOrWhiteSpace(settings.privacyUrl);terms.interactable=!string.IsNullOrWhiteSpace(settings.termsUrl);
        UnityEventTools.AddPersistentListener(policy.onClick,settings.OpenPrivacy);UnityEventTools.AddPersistentListener(terms.onClick,settings.OpenTerms);
        if(privacy!=null)
        {
            privacy.transform.SetParent(panel,false);SetRect((RectTransform)privacy.transform,.07f,.133f,.93f,.193f);
            StyleButton(privacy.GetComponent<Button>(),false);
            foreach(var label in privacy.GetComponentsInChildren<TMP_Text>(true)){label.fontSize=32;label.color=Cream;}
        }
        var actions=Rect(panel,"RunActions",.07f,.025f,.93f,.117f);settings.runActions=actions.gameObject;
        var resume=Button(actions,"Resume","계속하기",0,0,.485f,1,true,43);UnityEventTools.AddPersistentListener(resume.onClick,settings.Close);
        var retry=Button(actions,"Retry","다시 도전",.515f,0,1,1,false,42);UnityEventTools.AddPersistentListener(retry.onClick,canvas.LoadGame);
        actions.gameObject.SetActive(false);
        if(canvas.pauseMenuUI!=root.gameObject)canvas.pauseMenuUI.SetActive(false);
        var legacy=canvas.transform.Find("UI/Setting");legacy.gameObject.SetActive(false);
        var entry=canvas.transform.Find("UI/Top/Setting/Image").GetComponent<Button>();
        entry.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(entry.onClick,canvas.SettingsMenu);
        canvas.settingsMenuUI=root.gameObject;
        var pause=canvas.pauseButton.transform;ClearChildren(pause);
        SetRect((RectTransform)pause,.85f,.825f,.971f,.888f);
        var graphic=GetOrAdd<Image>(pause.gameObject);graphic.sprite=Art("WoodButton");graphic.type=UnityEngine.UI.Image.Type.Sliced;graphic.color=Color.white;
        var button=GetOrAdd<Button>(pause.gameObject);button.targetGraphic=graphic;button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,canvas.PauseGame);
        Image(pause,"Gear",Art("Settings"),.14f,.14f,.86f,.86f,true);
        root.gameObject.SetActive(false);EditorUtility.SetDirty(settings);
    }

    private static void BuildReadableHud(CanvasScript canvas,PlayerScript player)
    {
        var hud=canvas.GetComponentInChildren<PlayerStatusHud>(true);ClearChildren(hud.transform);
        SetRect((RectTransform)hud.transform,0,0,1,1);
        var health=Image(hud.transform,"HealthCard",Art("ParchmentPanel"),.025f,.905f,.685f,.978f);
        Text(health.transform,"Label","체력",44,Ink,.055f,.48f,.31f,.92f,TextAlignmentOptions.Left);
        var value=Text(health.transform,"Value","500 / 500",55,Ink,.31f,.48f,.94f,.93f,TextAlignmentOptions.Right);
        value.enableAutoSizing=true;value.fontSizeMin=42;value.fontSizeMax=55;
        Solid(health.transform,"HealthTrack",new Color(.12f,.12f,.075f),.057f,.16f,.943f,.39f);
        var fill=Solid(health.transform,"HealthFill",new Color(.24f,.78f,.10f),.067f,.185f,.933f,.365f);
        fill.sprite=FlatFillSprite();
        var attack=Image(hud.transform,"AttackCard",Art("ParchmentPanel"),.705f,.905f,.975f,.978f);
        Text(attack.transform,"Label","공격력",43,Ink,.08f,.54f,.92f,.92f);
        var att=Text(attack.transform,"Value","68",60,Ink,.08f,.09f,.92f,.58f);
        hud.Configure(value,fill,att);canvas.ConfigurePlayerStatusHud(hud,player);
        canvas.scoreParent.SetActive(false);player.SetPlayerChildCanvasVisible(false);
        hud.gameObject.SetActive(false);
    }

    private static void BuildHarborDefeat(CanvasScript canvas)
    {
        var root=canvas.gameOverUI.transform;root.gameObject.SetActive(false);
        var defeat=root.GetComponent<DefeatPresentation>();if(defeat==null)return;
        var panel=HarborModal(root,"다시 도전할까요?",.22f,.79f);
        var close=Button(panel,"Close","닫기 X",.785f,.905f,.967f,.97f,false,30);UnityEventTools.AddPersistentListener(close.onClick,defeat.ReturnToAltar);
        defeat.closeButton=close;
        Image(panel,"CoinEmblem",Art("Coin"),.40f,.68f,.60f,.83f,true);
        defeat.resultText=Text(panel,"Result","생존 0초\n획득 코인 0",50,Ink,.07f,.445f,.93f,.685f);
        defeat.rewardedButton=Button(panel,"Rewarded","광고 보고 추가 코인",.07f,.266f,.93f,.384f,false,37);
        defeat.rewardedText=defeat.rewardedButton.GetComponentInChildren<TMP_Text>();UnityEventTools.AddPersistentListener(defeat.rewardedButton.onClick,defeat.WatchRewardedVideo);
        defeat.adStatusText=Text(panel,"AdStatus","",30,Ink,.08f,.20f,.92f,.256f);
        defeat.continueButton=Button(panel,"Retry","다시 도전",.07f,.058f,.93f,.179f,true,46);UnityEventTools.AddPersistentListener(defeat.continueButton.onClick,defeat.ReturnToAltar);
        if(canvas.youWinUI!=null&&canvas.youWinUI.transform.Find("HarborClose")==null)
        {
            var victoryClose=Button(canvas.youWinUI.transform,"HarborClose","닫기 X",.80f,.91f,.97f,.97f,false,32);
            UnityEventTools.AddPersistentListener(victoryClose.onClick,canvas.LoadGame);
        }
        EditorUtility.SetDirty(defeat);
    }

    private static void PolishShopHeader(CanvasScript canvas)
    {
        var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);var root=shop.transform;
        var back=root.Find("Back").GetComponent<Button>();MakeUpperClose(back);
        Move(root,"HarborTitleSign",.17f,.934f,.81f,.995f);Move(root,"Title",.195f,.94f,.785f,.987f);
        Move(root,"CoinWallet",.09f,.882f,.47f,.927f);Move(root,"GemWallet",.53f,.882f,.91f,.927f);
        Move(root,"CoinIcon",.115f,.886f,.175f,.923f);Move(root,"JewelIcon",.555f,.886f,.615f,.923f);
        SetRect(shop.coinBalance.rectTransform,.18f,.885f,.445f,.924f);SetRect(shop.jewelBalance.rectTransform,.62f,.885f,.885f,.924f);
        foreach(var text in new[]{shop.coinBalance,shop.jewelBalance}){text.alignment=TextAlignmentOptions.Center;text.fontSize=40;text.fontSizeMax=40;text.fontSizeMin=32;}
        SetRect(shop.selectionName.rectTransform,.08f,.835f,.92f,.875f);
        Move(root,"PreviewArea",.075f,.545f,.925f,.835f);
    }
}
