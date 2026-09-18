using System.Linq;using NUnit.Framework;using UnityEngine;using UnityEngine.UI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using TMPro;

public sealed class HarborFaithfulArtworkTests
{
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    public void SavedScreensUseIntegratedArtAndRetainPlaybackBindings(string name)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").transform;var plaque=canvas.Find("UI/Main/Center/ChapterTitle");
            int chapter=canvas.GetComponent<ChapterProgression>().chapter;
            var sprite=plaque.GetComponent<Image>().sprite;
            Assert.That(AssetDatabase.GetAssetPath(sprite),Is.EqualTo(HarborGameUIInstaller.ChapterPlaqueAssetPath(chapter)));
            Assert.That(plaque.GetComponent<AspectRatioFitter>().aspectRatio,Is.EqualTo(sprite.rect.width/sprite.rect.height).Within(.001));
            Assert.That(plaque.Find("ReferenceCrest").gameObject.activeSelf,Is.False,"No floating substitute badge");
            Assert.That(plaque.Cast<Transform>().Where(t=>t.name.StartsWith("Rope_")).All(t=>!t.gameObject.activeSelf),Is.True);
            foreach(string buttonName in new[]{"Skin_Button","Upgrade_Button","Story_Button"}){
                var button=canvas.Find("UI/Main/Bottom/"+buttonName).GetComponent<Button>();Assert.That(button.onClick.GetPersistentEventCount(),Is.GreaterThan(0));
                Assert.That(AssetDatabase.GetAssetPath(button.GetComponent<Image>().sprite),Does.StartWith(HarborGameUIInstaller.FaithfulPath));Assert.That(button.transform.Find("Icon").gameObject.activeSelf,Is.False,"No duplicate icon over integrated artwork");
            }
            var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);var layout=story.GetComponentInChildren<CoastalStoryLayout>(true);
            Assert.That(layout.chrome.sprite.name,Is.EqualTo("StoryChrome"));Assert.That(layout.movieDisplay,Is.SameAs(story.movieDisplay.rectTransform));Assert.That(layout.movieSlot.GetComponent<RectMask2D>(),Is.Not.Null);
            Assert.That(story.movie,Is.Not.Null);Assert.That(story.previousButton.onClick.GetPersistentMethodName(0),Is.EqualTo("Previous"));
            Assert.That(story.activeSceneSprite.name,Is.EqualTo("TabYellow"));Assert.That(story.inactiveSceneSprite.name,Is.EqualTo("TabIvory"));
            Assert.That(story.captionText.fontSizeMax,Is.GreaterThanOrEqualTo(60));Assert.That(story.captionText.fontSizeMin,Is.GreaterThanOrEqualTo(48));
            Assert.That(AssetDatabase.GetAssetPath(story.captionText.font),Is.EqualTo(HarborGameUIInstaller.RoundedFontPath));Assert.That(story.captionText.fontSharedMaterial.IsKeywordEnabled("OUTLINE_ON"),Is.True);
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    [TestCase(2340f)] [TestCase(1920f)]
    public void ChromeRegionsKeepControlsOutsideTheMovie(float height)
    {
        var root=new GameObject("Layout test",typeof(RectTransform));
        try{
            ((RectTransform)root.transform).sizeDelta=new Vector2(1080,height);var layout=root.AddComponent<CoastalStoryLayout>();
            RectTransform Child(string name){var t=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();t.SetParent(root.transform,false);return t;}
            layout.chrome=Child("chrome").GetComponent<Image>();layout.counter=Child("counter");layout.skip=Child("skip");layout.movieSlot=Child("movie");layout.title=Child("title");layout.caption=Child("caption");layout.shade=Child("shade");layout.previous=Child("previous");layout.next=Child("next");layout.markers=Enumerable.Range(0,4).Select(i=>Child("tab"+i)).ToArray();layout.Refresh();
            float Bottom(RectTransform t){var corners=new Vector3[4];t.GetLocalCorners(corners);return t.localPosition.y+corners[0].y;}
            float Top(RectTransform t){var corners=new Vector3[4];t.GetLocalCorners(corners);return t.localPosition.y+corners[1].y;}
            Assert.That(Bottom(layout.counter),Is.GreaterThan(Top(layout.movieSlot)));Assert.That(Top(layout.previous),Is.LessThan(Bottom(layout.markers[0])));Assert.That(Top(layout.markers[0]),Is.LessThan(Bottom(layout.movieSlot)));
            Assert.That(Bottom(layout.caption),Is.GreaterThan(Bottom(layout.movieSlot)));Assert.That(Top(layout.title),Is.LessThan(Top(layout.movieSlot)));
        }finally{Object.DestroyImmediate(root);}
    }
}
