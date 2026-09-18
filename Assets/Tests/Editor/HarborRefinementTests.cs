#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public sealed class HarborRefinementTests
{
    [Test] public void AudioFocusToleratesPartiallyDestroyedChannels()
    {
        var root=new GameObject("Focus lifecycle test");root.SetActive(false);
        try{
            var audio=root.AddComponent<GameAudioService>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(GameAudioService).GetField("music",flags).SetValue(audio,root.AddComponent<AudioSource>());
            typeof(GameAudioService).GetField("ready",flags).SetValue(audio,true);
            var focus=typeof(GameAudioService).GetMethod("OnApplicationFocus",flags);
            Assert.DoesNotThrow(()=>focus.Invoke(audio,new object[]{false}));
            Assert.DoesNotThrow(()=>focus.Invoke(audio,new object[]{true}));
        }finally{Object.DestroyImmediate(root);}
    }
    [TestCase(0,0)] [TestCase(1,0)] [TestCase(2,1)] [TestCase(3,2)]
    public void PreviousSceneUsesThePrecedingBoundary(int page,int expected)=>Assert.That(OpeningStoryUI.PreviousPageIndex(page),Is.EqualTo(expected));

    [Test] public void PairedEnemiesHaveTenToTwentyPercentDifferentHealth()
    {
        var rows=EncounterPlacementTables.Rows.Where(r=>r.kind=="적 배치").ToArray();
        var byId=rows.ToDictionary(r=>r.scene+"/"+r.id);
        var right=rows.Where(r=>r.id.EndsWith("_Right")).ToArray();Assert.That(right.Length,Is.EqualTo(75));
        foreach(var row in right){var left=byId[row.scene+"/"+row.id.Substring(0,row.id.Length-6)];Assert.That(row.health/left.health,Is.InRange(1.10f,1.20f),row.id);}
        Assert.That(EncounterPlacementTables.Rows.Single(r=>r.id=="SR18_L_G01_T013_Bucket").enabled,Is.False);
    }
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    public void RefinedScreensAndBonusCuesAreSaved(string name)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");
            var display=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HarborGameUIInstaller.DisplayFontPath);
            var rounded=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HarborGameUIInstaller.RoundedFontPath);
            foreach(var text in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMP_Text>(true)))Assert.That(text.font==HarborRefinementFontBuilder.Font||text.font==display||text.font==rounded,Is.True,text.name+" retains an unsupported font");
            var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);Assert.That(story.previousButton,Is.Not.Null);
            Assert.That(story.previousButton.onClick.GetPersistentMethodName(0),Is.EqualTo("Previous"));
            Assert.That(AssetDatabase.GetAssetPath(story.captionText.font),Is.EqualTo(HarborGameUIInstaller.RoundedFontPath));
            var tutorial=canvas.GetComponent<CoastalTutorialUI>();var rect=tutorial.panel.transform.Find("Panel").GetComponent<RectTransform>();
            Assert.That(rect.anchorMax.y-rect.anchorMin.y,Is.LessThanOrEqualTo(.07f));Assert.That(rect.anchorMin.y,Is.GreaterThan(.75f));
            var hud=canvas.GetComponentInChildren<PlayerStatusHud>(true).transform.Find("HealthCard").GetComponent<RectTransform>();Assert.That(hud.anchorMax.y-hud.anchorMin.y,Is.LessThan(.09f));
            foreach(var altar in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AuthoredBonusWall>(true))){
                Assert.That(altar.GetComponents<BonusRewardCue>().Length,Is.EqualTo(1));
                Assert.That(altar.GetComponent<BonusRewardCue>().pickupBurst,Is.Not.Null);
                Assert.That(altar.transform.Find("ChoiceAltarVisual/PickupPedestal").localScale.y,Is.EqualTo(.28f).Within(.001f));
            }
            if(name=="Noryangjin_MapTool_Mode_SR18"){
                var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
                Assert.That(map.Find("Roads").Cast<Transform>().Count(t=>t.name.StartsWith("HarborOpeningPier_")),Is.EqualTo(5));
                Assert.That(map.Find("Props/HarborDeparture_20260916").GetComponentInChildren<HarborMerchantGreeting>(),Is.Not.Null);
                var actor=map.Find("Enemies/SR18_L_E01_T005_Enemy_OldMan").GetComponent<EnemyEventController>();
                Assert.DoesNotThrow(()=>actor.GetComponent<EnemyCornerClearance>().ValidateMovement(actor.transform.position,actor.TargetPoint.position));
                Assert.That(actor.GetComponentInChildren<Animator>().runtimeAnimatorController.name,Does.EndWith("_Calm"));
            }
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    [Test] public void FontPresetsUseTheCurrentAtlasWithoutExtraWeight()
    {
        var font=HarborRefinementFontBuilder.Font;Assert.That(font.HasCharacter('진'),Is.True);
        Assert.That(GameUIFont.Load()==font,Is.True,"Runtime-created damage labels use the same family");
        foreach(string name in new[]{"Coastal_Ink","Coastal_White"}){
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Resources/UI/HarborMaterials/"+name+".mat");
            Assert.That(material.GetTexture("_MainTex")==font.atlasTextures[0],Is.True,"Compare Unity native texture identity across import/reload");Assert.That(material.GetFloat("_FaceDilate"),Is.Zero);
            Assert.That(material.GetFloat("_GradientScale"),Is.EqualTo(font.material.GetFloat("_GradientScale")));
            if(name=="Coastal_White")Assert.That(material.IsKeywordEnabled("OUTLINE_ON"),Is.True,"Mobile SDF requires the outline shader variant");
        }
        var bonus=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Resources/HarborRefinement/BonusReadable.mat");Assert.That(bonus.IsKeywordEnabled("OUTLINE_ON"),Is.True);
    }
}
#endif
