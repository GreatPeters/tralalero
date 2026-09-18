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
    private static void AlignPresentationText(CanvasScript canvas)
    {
        var rounded=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RoundedFontPath);
        var display=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true)){
            if(text.font!=rounded&&text.font!=display)continue;
            // Face ascender/descender metrics do not equal the visible glyph center.
            // Retain intentionally left/top-aligned text; normalize centered display labels.
            if(text.horizontalAlignment==HorizontalAlignmentOptions.Center||text.horizontalAlignment==HorizontalAlignmentOptions.Geometry){
                if(text.verticalAlignment==VerticalAlignmentOptions.Middle||text.verticalAlignment==VerticalAlignmentOptions.Geometry){
                    text.alignment=TextAlignmentOptions.MidlineGeoAligned;text.margin=Vector4.zero;EditorUtility.SetDirty(text);
                }
            }
        }
        foreach(var story in canvas.GetComponentsInChildren<OpeningStoryUI>(true)){
            var content=story.transform.Find("HarborContent");
            void CenterLabel(TMP_Text text){
                text.alignment=TextAlignmentOptions.MidlineGeoAligned;text.margin=Vector4.zero;text.rectTransform.localScale=Vector3.one;text.rectTransform.pivot=new Vector2(.5f,.5f);
                text.textWrappingMode=TextWrappingModes.NoWrap;
                SetRect(text.rectTransform,.075f,.12f,.925f,.88f);EditorUtility.SetDirty(text);EditorUtility.SetDirty(text.rectTransform);
            }
            var skip=content.Find("SkipButton").GetComponent<Button>();var skipLabel=skip.GetComponentInChildren<TMP_Text>(true);
            CenterLabel(skipLabel);skipLabel.fontSize=62;story.chapterText.fontSize=62;
            CenterLabel(story.nextText);
            foreach(var marker in story.sceneIndicators)foreach(var label in marker.GetComponentsInChildren<TMP_Text>(true))CenterLabel(label);
            var previous=story.previousButton.transform;var previousLabel=previous.GetComponentInChildren<TMP_Text>(true);var icon=previous.Find("ReplayIcon")??previous.Find("AlignedContent/ReplayIcon");
            if(previousLabel==null||icon==null)throw new InvalidOperationException("Previous button needs its label and icon");
            var group=previous.Find("AlignedContent") as RectTransform;if(group==null){group=new GameObject("AlignedContent",typeof(RectTransform)).GetComponent<RectTransform>();group.SetParent(previous,false);}
            group.anchorMin=group.anchorMax=group.pivot=new Vector2(.5f,.5f);group.anchoredPosition=Vector2.zero;
            icon.SetParent(group,false);previousLabel.transform.SetParent(group,false);icon.SetSiblingIndex(0);previousLabel.transform.SetSiblingIndex(1);
            var row=GetOrAdd<HorizontalLayoutGroup>(group.gameObject);row.childAlignment=TextAnchor.MiddleCenter;row.spacing=14;row.padding=new RectOffset();row.childControlWidth=true;row.childControlHeight=true;row.childForceExpandWidth=false;row.childForceExpandHeight=false;
            var fit=GetOrAdd<ContentSizeFitter>(group.gameObject);fit.horizontalFit=ContentSizeFitter.FitMode.PreferredSize;fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var iconLayout=GetOrAdd<LayoutElement>(icon.gameObject);iconLayout.minWidth=iconLayout.preferredWidth=70;iconLayout.minHeight=iconLayout.preferredHeight=70;iconLayout.flexibleWidth=iconLayout.flexibleHeight=0;
            previousLabel.alignment=TextAlignmentOptions.MidlineGeoAligned;previousLabel.margin=Vector4.zero;previousLabel.enableAutoSizing=false;
            previousLabel.textWrappingMode=TextWrappingModes.NoWrap;
            icon.localScale=Vector3.one;previousLabel.rectTransform.localScale=Vector3.one;icon.GetComponent<Image>().raycastTarget=false;
            LayoutRebuilder.ForceRebuildLayoutImmediate(group);
        }
        foreach(var component in canvas.GetComponentsInChildren<Component>(true))if(component is TMP_Text||component is RectTransform||component is LayoutGroup||component is ContentSizeFitter||component is LayoutElement){
            EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }

    public static object ApplyUITextAlignmentAll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        if(!EditorSceneManager.SaveOpenScenes())throw new InvalidOperationException("Preserve pending scene state first");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try{foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"}){
            var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
            AlignPresentationText(scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<CanvasScript>());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return "Display glyph centers and video button content aligned in three scenes";
    }
}
