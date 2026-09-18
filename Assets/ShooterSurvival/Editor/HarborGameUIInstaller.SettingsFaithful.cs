using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static partial class HarborGameUIInstaller
{
    public static object ApplyFaithfulSettingsAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        if(setup.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))throw new InvalidOperationException("Save open scenes first");
        try {
            foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}) {
                var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
                FaithfulSettings(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CanvasScript>(true)).Single());
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        } finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return "Settings saved in all three chapters";
    }

    private static void FaithfulSettings(CanvasScript canvas)
    {
        var settings=canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>();var panel=settings.transform.Find("Panel");
        var rect=(RectTransform)panel;SetRect(rect,.055f,.5f,.945f,.5f);rect.pivot=new Vector2(.5f,.5f);
        var fit=GetOrAdd<AspectRatioFitter>(panel.gameObject);fit.aspectMode=AspectRatioFitter.AspectMode.WidthControlsHeight;fit.aspectRatio=.72f;
        ShopSurface(panel.GetComponent<Image>(),"IvoryPanel",.85f);
        Move(panel,"Header",.005f,.873f,.995f,.995f);ShopSurface(panel.Find("Header").GetComponent<Image>(),"NavyPanel",.9f);
        Move(panel,"Title",.25f,.887f,.75f,.975f);ShopType(panel.Find("Title").GetComponent<TMP_Text>(),76,true);
        var crest=ShopImage(panel,"AnchorCrest",Art("Anchor"),.43f,.977f,.57f,1.055f,true);crest.color=Color.white;
        var close=panel.Find("Close").GetComponent<Button>();Move(panel,"Close",.827f,.89f,.964f,.975f);ShopSurface(close.GetComponent<Image>(),"SquareButton",.9f);
        ShopType(close.GetComponentInChildren<TMP_Text>(),66,true);
        void Toggle(string name,float y,string icon) {
            var row=panel.Find(name);SetRect((RectTransform)row,.065f,y,.935f,y+.102f);row.GetComponent<Image>().color=Color.clear;
            var sep=row.Find("Separator");if(sep!=null)sep.gameObject.SetActive(false);
            ShopImage(row,"Icon",SettingsIcon(icon),.018f,.20f,.13f,.8f,true);
            Move(row,"Label",.18f,.1f,.61f,.90f);ShopType(row.Find("Label").GetComponent<TMP_Text>(),57,false,true);
            var frame=row.Find("SwitchFrame");Move(row,"SwitchFrame",.65f,.13f,.99f,.87f);
            var housing=frame.GetComponent<Image>();housing.sprite=MobileUIArt.Rounded;housing.type=UnityEngine.UI.Image.Type.Sliced;housing.pixelsPerUnitMultiplier=.48f;housing.color=Ink;
            Move(frame,"Switch",.024f,.065f,.976f,.935f);
            var track=frame.Find("Switch").GetComponent<Image>();track.sprite=MobileUIArt.Rounded;track.type=UnityEngine.UI.Image.Type.Sliced;track.pixelsPerUnitMultiplier=.48f;
            var label=name=="Sound"?settings.soundState:settings.vibrationState;label.transform.SetParent(frame,false);SetRect(label.rectTransform,.06f,.12f,.60f,.88f);ShopType(label,43);
            var knob=name=="Sound"?settings.soundKnob:settings.vibrationKnob;knob.GetComponent<Image>().sprite=SettingsIcon("Knob");knob.GetComponent<Image>().color=Color.white;
            (name=="Sound"?settings.soundButton:settings.vibrationButton).transition=Selectable.Transition.None;
        }
        Toggle("Sound",.744f,"Sound");Toggle("Vibration",.634f,"Phone");
        var divider=ShopImage(panel,"SettingsDivider",FlatFillSprite(),.065f,.620f,.935f,.622f);divider.color=new Color(.58f,.59f,.55f,.65f);
        void SliderRow(Slider slider,string name,float y,string icon) {
            Move(panel,name+"Label",.22f,y+.077f,.93f,y+.132f);ShopType(panel.Find(name+"Label").GetComponent<TMP_Text>(),51,false,true);
            ShopImage(panel,name+"Icon",SettingsIcon(icon),.078f,y+.06f,.166f,y+.133f,true);
            Move(panel,name,.225f,y,.925f,y+.070f);
            var track=slider.transform.Find("Track").GetComponent<Image>();SetRect(track.rectTransform,0,.33f,1,.60f);track.sprite=MobileUIArt.Rounded;track.type=UnityEngine.UI.Image.Type.Sliced;track.color=new Color(.56f,.58f,.59f);track.pixelsPerUnitMultiplier=1;
            Move(slider.transform,"FillArea",.012f,.35f,.988f,.58f);var fill=slider.fillRect.GetComponent<Image>();fill.sprite=MobileUIArt.Rounded;fill.type=UnityEngine.UI.Image.Type.Sliced;fill.color=new Color(.02f,.65f,.95f);
            Move(slider.transform,"HandleArea",.015f,0,.985f,1);slider.handleRect.anchorMin=new Vector2(slider.handleRect.anchorMin.x,.04f);slider.handleRect.anchorMax=new Vector2(slider.handleRect.anchorMax.x,.96f);slider.handleRect.sizeDelta=new Vector2(80,0);
            var handle=slider.handleRect.GetComponent<Image>();handle.sprite=SettingsIcon("Knob");handle.color=Color.white;handle.preserveAspect=true;
        }
        SliderRow(settings.volume,"Volume",.477f,"Music");SliderRow(settings.sensitivity,"Sensitivity",.327f,"Control");
        var ads=panel.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.Ads.RewardedPrivacyOptionsButton>(true);
        var legal=new[]{settings.privacyButton,settings.termsButton,ads!=null?ads.GetComponent<Button>():null};
        for(int i=0;i<legal.Length;i++)if(legal[i]!=null){
            var button=legal[i];float y=.230f-i*.069f;SetRect(button.GetComponent<RectTransform>(),.065f,y,.935f,y+.060f);
            ShopSurface(button.GetComponent<Image>(),"RowPanel",.65f);
            var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;colors.disabledColor=new Color(.89f,.89f,.87f);colors.pressedColor=new Color(.8f,.9f,1);button.colors=colors;
            var label=button.GetComponentInChildren<TMP_Text>();ShopType(label,40,false,true);SetRect(label.rectTransform,.05f,.08f,.87f,.92f);
            ShopImage(button.transform,"Chevron",Art("Right"),.92f,.24f,.965f,.76f,true);
        }
        Move(panel,"RunActions",.06f,.019f,.94f,.083f);
        foreach(var button in settings.runActions.GetComponentsInChildren<Button>(true)){ShopButton(button,button.name=="Resume");ShopType(button.GetComponentInChildren<TMP_Text>(),49);}
        foreach(var component in settings.GetComponentsInChildren<Component>(true))if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
    }

    private static Sprite SettingsIcon(string kind)
    {
        string path=FaithfulPath+"Settings"+kind+".asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
        const int size=128;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Settings "+kind,filterMode=FilterMode.Bilinear};
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float u=(x+.5f)/size,v=(y+.5f)/size;bool B(float a,float b,float c,float d)=>u>a&&u<c&&v>b&&v<d;
            bool C(float a,float b,float radius)=>Vector2.Distance(new Vector2(u,v),new Vector2(a,b))<radius;
            bool on=false;Color color=Ink;
            switch(kind){
                case "Sound":on=B(.08f,.35f,.29f,.65f)||(u>.26f&&u<.52f&&Mathf.Abs(v-.5f)<u-.1f)||((u>.57f)&&((Mathf.Abs(Vector2.Distance(new Vector2(u,v),new Vector2(.48f,.5f))-.25f)<.034f)||(Mathf.Abs(Vector2.Distance(new Vector2(u,v),new Vector2(.48f,.5f))-.40f)<.034f))&&Mathf.Abs(v-.5f)<.30f);break;
                case "Phone":on=(B(.28f,.08f,.72f,.92f)&&!B(.34f,.23f,.66f,.82f))||C(.5f,.16f,.035f)||((u<.2f||u>.8f)&&Mathf.Abs(v-.5f)<.22f&&Mathf.Abs(u-(u<.5f?.15f:.85f)-.028f*Mathf.Sin(v*40))<.025f);break;
                case "Music":on=C(.23f,.21f,.145f)||C(.72f,.31f,.145f)||B(.30f,.21f,.38f,.83f)||B(.79f,.31f,.87f,.94f)||(u>.3f&&u<.87f&&v>.68f+.2f*u&&v<.83f+.2f*u);break;
                case "Control":on=B(.40f,.08f,.60f,.92f)||B(.08f,.40f,.92f,.60f)||C(.5f,.82f,.13f)||C(.5f,.18f,.13f)||C(.18f,.5f,.13f)||C(.82f,.5f,.13f);break;
                case "Knob":float d=Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.5f));on=d<.48f;color=d>.43f?Ink:d>.37f?new Color(.30f,.61f,.81f):Color.Lerp(new Color(.74f,.85f,.92f),Color.white,v);break;
            }
            color.a=on?1:0;tex.SetPixel(x,y,color);
        }
        tex.Apply();sprite=Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);sprite.name="Settings"+kind;AssetDatabase.CreateAsset(sprite,path);AssetDatabase.AddObjectToAsset(tex,sprite);return sprite;
    }
}
