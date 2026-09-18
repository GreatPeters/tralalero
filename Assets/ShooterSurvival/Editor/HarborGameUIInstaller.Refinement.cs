using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static partial class HarborGameUIInstaller
{
    private static void RefineCoastalPresentation(CanvasScript canvas)
    {
        // The concept uses compact, regular strokes; remove brush-like glyphs
        // and synthetic extra weight from every screen, including video prefabs.
        foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font=Font;text.fontStyle=FontStyles.Normal;
            text.characterSpacing=0;text.wordSpacing=0;
            text.enableAutoSizing=false;
        }
        var ui=canvas.transform.Find("UI");var plaque=ui.Find("Main/Center/ChapterTitle");
        plaque.Find("Name").GetComponent<TMP_Text>().fontSize=59;
        plaque.Find("Chapter").GetComponent<TMP_Text>().fontSize=34;
        var hint=ui.Find("Main/Center/StartHint/Title").GetComponent<TMP_Text>();hint.fontSize=70;
        foreach(var text in ui.Find("Main/Bottom").GetComponentsInChildren<TMP_Text>(true))text.fontSize=36;
        foreach(var text in ui.Find("Top/Resource").GetComponentsInChildren<TMP_Text>(true))FitNumber(text,26,42);

        var hud=canvas.GetComponentInChildren<PlayerStatusHud>(true).transform;var card=hud.Find("HealthCard");
        Move(hud,"HealthCard",.03f,.895f,.715f,.978f);
        foreach(var text in card.GetComponentsInChildren<TMP_Text>(true))text.fontSize=text.name=="Value"?40:text.name=="Attack"?34:31;
        FitNumber(card.Find("Value").GetComponent<TMP_Text>(),24,40);
        FitNumber(card.Find("Attack").GetComponent<TMP_Text>(),24,34);
        Move(card,"Heart",.045f,.61f,.125f,.87f);Move(card,"HealthLabel",.15f,.60f,.345f,.90f);
        Move(card,"Value",.35f,.59f,.945f,.92f);Move(card,"HealthTrack",.055f,.365f,.945f,.535f);Move(card,"HealthFill",.06f,.38f,.94f,.52f);
        card.Find("HealthFill").GetComponent<Image>().color=new Color(.10f,.66f,.48f);
        card.Find("HealthTrack").GetComponent<Image>().color=new Color(.055f,.14f,.21f);
        var worldBar=canvas.GetComponentInChildren<PlayerWorldHealthBar>(true);
        if(worldBar!=null){worldBar.fill.color=new Color(.10f,.66f,.48f);var barRect=(RectTransform)worldBar.transform;barRect.sizeDelta=new Vector2(barRect.sizeDelta.x,14);foreach(var image in worldBar.GetComponentsInChildren<Image>(true))if(image!=worldBar.fill)image.color=new Color(.035f,.105f,.19f);}
        SetRect(canvas.pauseButton.GetComponent<RectTransform>(),.825f,.919f,.965f,.977f);

        var settings=canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>();var panel=settings.transform.Find("Panel");
        Move(settings.transform,"Panel",.085f,.17f,.915f,.84f);
        Move(panel,"Header",0,.89f,1,1);Move(panel,"Title",.065f,.91f,.77f,.978f);
        panel.Find("Title").GetComponent<TMP_Text>().fontSize=44;Move(panel,"Close",.845f,.915f,.962f,.976f);
        void Toggle(string name,float y)
        {
            var row=panel.Find(name);SetRect((RectTransform)row,.065f,y,.935f,y+.09f);
            row.GetComponent<Image>().color=Color.clear;
            Move(row,"Label",.005f,.12f,.57f,.88f);row.Find("Label").GetComponent<TMP_Text>().fontSize=36;
            Move(row,"SwitchFrame",.70f,.15f,.97f,.85f);
            var frame=row.Find("SwitchFrame").GetComponent<Image>();frame.sprite=MobileUIArt.Rounded;frame.type=UnityEngine.UI.Image.Type.Sliced;frame.color=Ink;
            var state=row.Find("State");state.SetParent(frame.transform,false);SetRect((RectTransform)state,.06f,.1f,.59f,.9f);state.GetComponent<TMP_Text>().fontSize=27;
            Solid(row,"Separator",new Color(.35f,.46f,.57f,.30f),0,0,1,.009f);
        }
        Toggle("Sound",.778f);Toggle("Vibration",.675f);
        Move(panel,"VolumeLabel",.07f,.593f,.93f,.642f);Move(panel,"Volume",.07f,.548f,.93f,.59f);
        Move(panel,"SensitivityLabel",.07f,.466f,.93f,.515f);Move(panel,"Sensitivity",.07f,.421f,.93f,.463f);
        foreach(string name in new[]{"VolumeLabel","SensitivityLabel"})panel.Find(name).GetComponent<TMP_Text>().fontSize=32;
        foreach(var slider in new[]{settings.volume,settings.sensitivity})
        {
            var rect=slider.handleRect;rect.sizeDelta=new Vector2(42,0);
            Move(slider.transform,"Track",.01f,.44f,.99f,.58f);Move(slider.transform,"FillArea",.025f,.455f,.975f,.565f);
        }
        Move(panel,"PrivacyPolicy",.065f,.303f,.935f,.359f);Move(panel,"Terms",.065f,.237f,.935f,.293f);
        var ads=panel.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.Ads.RewardedPrivacyOptionsButton>(true);
        if(ads!=null)SetRect(ads.GetComponent<RectTransform>(),.065f,.171f,.935f,.227f);
        foreach(var button in new[]{settings.privacyButton,settings.termsButton,ads!=null?ads.GetComponent<Button>():null})if(button!=null)
        {
            SetCoastalImage(button.GetComponent<Image>(),Art("RowPanel"));
            foreach(var text in button.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=29;text.alignment=TextAlignmentOptions.Left;SetRect(text.rectTransform,.05f,.08f,.93f,.92f);}
        }
        Move(panel,"RunActions",.065f,.037f,.935f,.119f);
        foreach(var text in settings.runActions.GetComponentsInChildren<TMP_Text>(true))text.fontSize=36;

        var tutorial=canvas.GetComponent<CoastalTutorialUI>();var toast=tutorial.panel.transform.Find("Panel");
        SetRect((RectTransform)toast,.07f,.812f,.93f,.877f);SetCoastalImage(toast.GetComponent<Image>(),Art("NavyPanel"));
        toast.Find("Header").gameObject.SetActive(false);
        SetRect(tutorial.title.rectTransform,.14f,.57f,.87f,.88f);tutorial.title.fontSize=24;tutorial.title.color=new Color(.99f,.83f,.28f);
        SetRect(tutorial.body.rectTransform,.14f,.12f,.88f,.57f);tutorial.body.fontSize=29;tutorial.body.color=Cream;
        Move(toast,"Icon",.023f,.23f,.115f,.79f);Move(toast,"Close",.9f,.3f,.975f,.72f);
        toast.Find("Close").GetComponentInChildren<TMP_Text>().fontSize=25;
        foreach(var text in ui.Find("Upgrade2").GetComponentsInChildren<TMP_Text>(true))
        {
            if(text.name=="Title")text.fontSize=43;
            else if(text.name=="Name")text.fontSize=32;
            else if(text.name=="Level"||text.name=="Effect")text.fontSize=28;
        }
        foreach(var story in canvas.GetComponentsInChildren<OpeningStoryUI>(true))
        {
            story.chapterText.fontSize=36;story.titleText.fontSize=42;story.captionText.fontSize=34;
            foreach(var button in story.GetComponentsInChildren<Button>(true))foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.fontSize=37;
        }
    }
    private static void FitNumber(TMP_Text text,float min,float max)
    {
        text.fontSize=max;text.fontSizeMin=min;text.fontSizeMax=max;text.enableAutoSizing=true;
    }
}
