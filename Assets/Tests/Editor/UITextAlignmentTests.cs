using System.Linq;using NUnit.Framework;using TMPro;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;

public sealed class UITextAlignmentTests
{
    [TestCase("1")]
    [TestCase("다음 장면")]
    [TestCase("이야기 03 / 04")]
    [TestCase("저주를 풀려면 인간 세상에서\n더 좋은 신발을 찾아 바쳐야 한다.")]
    public void RoundedGlyphsAreCenteredByTheirVisibleGeometry(string value)
    {
        var root=new GameObject("Alignment glyph test",typeof(Canvas));
        try{
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var text=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();text.transform.SetParent(root.transform,false);
            text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HarborGameUIInstaller.RoundedFontPath);Assert.That(text.font,Is.Not.Null);
            text.fontSize=50;text.text=value;text.alignment=TextAlignmentOptions.MidlineGeoAligned;text.rectTransform.sizeDelta=new Vector2(900,200);text.ForceMeshUpdate(true,true);
            var glyphs=text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible).ToArray();Assert.That(glyphs.Length,Is.GreaterThan(0));
            var center=new Vector2((glyphs.Min(c=>c.bottomLeft.x)+glyphs.Max(c=>c.topRight.x))*.5f,(glyphs.Min(c=>c.bottomLeft.y)+glyphs.Max(c=>c.topRight.y))*.5f);
            Assert.That(Vector2.Distance(center,text.rectTransform.rect.center),Is.LessThan(.3f));
        }finally{Object.DestroyImmediate(root);}
    }
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    public void SavedVideoControlsHaveSymmetricLabelsAndCenteredPreviousGroup(string name)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            var story=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OpeningStoryUI>(true)).Single();
            var skip=story.transform.Find("HarborContent/SkipButton").GetComponentInChildren<TMP_Text>(true);
            foreach(var text in new[]{story.nextText,skip}.Concat(story.sceneIndicators.SelectMany(m=>m.GetComponentsInChildren<TMP_Text>(true)))){
                var rect=text.rectTransform;Assert.That((rect.anchorMin+rect.anchorMax)*.5f,Is.EqualTo(new Vector2(.5f,.5f)));Assert.That(text.alignment,Is.EqualTo(TextAlignmentOptions.MidlineGeoAligned));Assert.That(text.margin,Is.EqualTo(Vector4.zero));
            }
            Assert.That(story.chapterText.fontSize,Is.EqualTo(skip.fontSize));Assert.That(story.captionText.alignment,Is.EqualTo(TextAlignmentOptions.MidlineGeoAligned));
            var group=story.previousButton.transform.Find("AlignedContent").GetComponent<RectTransform>();Assert.That(group.anchorMin,Is.EqualTo(Vector2.one*.5f));Assert.That(group.anchorMax,Is.EqualTo(Vector2.one*.5f));Assert.That(group.anchoredPosition,Is.EqualTo(Vector2.zero));
            Assert.That(group.GetComponent<HorizontalLayoutGroup>().childAlignment,Is.EqualTo(TextAnchor.MiddleCenter));Assert.That(group.GetComponent<ContentSizeFitter>().horizontalFit,Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
            Assert.That(story.previousButton.onClick.GetPersistentMethodName(0),Is.EqualTo("Previous"));
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
