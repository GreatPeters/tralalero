using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class ChapterUIThemeTests
{
    [TestCase("Noryangjin_MapTool_Mode_SR18",1,"LobbyPlaque","노량진 수산시장")]
    [TestCase("HighWay",2,"HighwayPlaque","고속도로")]
    [TestCase("RestStop",3,"RestStopPlaque","휴게소")]
    public void SavedLobbyUsesItsOwnChapterSymbol(string sceneName,int chapter,string spriteName,string label)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").transform;
            Assert.That(canvas.GetComponent<ChapterProgression>().chapter,Is.EqualTo(chapter));
            var plaque=canvas.Find("UI/Main/Center/ChapterTitle");var sprite=plaque.GetComponent<Image>().sprite;
            Assert.That(sprite.name,Is.EqualTo(spriteName));Assert.That(plaque.Find("Name").GetComponent<TMP_Text>().text,Is.EqualTo(label));
            Assert.That(HarborGameUIInstaller.ChapterPlaqueAssetPath(chapter),Does.EndWith("/"+spriteName+".png"),"Rebuild path must use the same thematic symbol");
            Assert.That(plaque.GetComponent<AspectRatioFitter>().aspectRatio,Is.EqualTo(sprite.rect.width/sprite.rect.height).Within(.001f));
            Assert.That(plaque.Find("ReferenceAnchor").gameObject.activeSelf,Is.False,"No old anchor overlay above themed artwork");
            var hint=canvas.Find("UI/Main/Center/StartHint");Assert.That(hint.GetComponent<HarborSwipeHint>().finger,Is.Not.Null);
            foreach(string name in new[]{"Skin_Button","Upgrade_Button","Story_Button"})Assert.That(canvas.Find("UI/Main/Bottom/"+name).GetComponent<Button>().onClick.GetPersistentEventCount(),Is.GreaterThan(0));
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
