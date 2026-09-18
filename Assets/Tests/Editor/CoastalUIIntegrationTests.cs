#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CoastalUIIntegrationTests
{
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    public void SavedGameplayScreensUseCoastalArtAndRealBindings(string name)
    {
        var path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");
            var upgrade=canvas.transform.Find("UI/Upgrade2");
            Assert.That(upgrade.GetComponentsInChildren<UpgradeUI>(true).Select(c=>c.UpgradeId),Is.EquivalentTo(Enumerable.Range(1,10)));
            var tabs=upgrade.GetComponent<HarborUpgradeTabs>();
            Assert.That(AssetDatabase.GetAssetPath(tabs.activeSprite),Is.EqualTo(HarborGameUIInstaller.FaithfulPath+"ShopGoldButtonTrimmed.asset"));
            foreach(var chapter in upgrade.GetComponentsInChildren<ChapterUpgradeCardUI>(true))
            {
                Assert.That(chapter.rankPips.Length,Is.EqualTo(5));
                Assert.That(chapter.rankPips.All(p=>p!=null),Is.True);
                Assert.That(chapter.priceLabel,Is.Not.Null);Assert.That(chapter.rankLabel,Is.Not.Null);
            }
            var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);
            Assert.That(shop.preview.display,Is.Not.Null);
            Assert.That(shop.preview.display.transform.parent.name,Is.EqualTo("PreviewArea"));
            Assert.That(shop.messagePanel,Is.Not.Null);
            Assert.That(shop.itemsRoot.GetComponent<GridLayoutGroup>().constraintCount,Is.EqualTo(2));
            var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);
            Assert.That(story.movie,Is.Not.Null);Assert.That(story.movieDisplay,Is.Not.Null);
            Assert.That(story.sceneIndicators.Length,Is.EqualTo(OpeningStoryUI.MovieSceneCount));
            Assert.That(story.sceneIndicators.All(p=>p.transform.Find("Number")!=null),Is.True);
            Assert.That(canvas.GetComponent<CoastalTutorialUI>().icons.Length,Is.EqualTo(4));
            var setting=canvas.GetComponentInChildren<HarborSettingsPanel>(true);
            Assert.That(canvas.GetComponent<ChapterProgression>().clearRewardText,Is.Not.Null,"Victory replacement retains the live clear-reward label");
            Assert.That(setting.runActions,Is.Not.Null);
            Assert.That(setting.transform.Find("Panel").GetComponent<Image>().type,Is.EqualTo(Image.Type.Sliced),"Modal panels must not tile fasteners");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }

    [Test] public void AllChapterRanksGrantFivePercentAndKeepFiveRankCap()
    {
        var catalog=Resources.Load<ChapterUpgradeCatalog>(ChapterUpgradeCatalog.ResourcePath);
        Assert.That(catalog.entries.Length,Is.EqualTo(3));
        Assert.That(catalog.entries.All(r=>r.attackPercent==5&&r.healthPercent==5),Is.True);
        Assert.That(ChapterUpgradeDefinition.MaxLevel,Is.EqualTo(5));
    }
    [Test] public void EverySkinUsesASeparatedTwoDimensionalIcon()
    {
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        foreach(var item in CosmeticTables.Rows.Where(r=>r.slot==CosmeticSlot.Skin))
            Assert.That(AssetDatabase.GetAssetPath(catalog.Find(item.visualKey).icon),Is.EqualTo("Assets/ShooterSurvival/UI/CoastalEnamel/"+item.visualKey+".png"));
    }
    [TestCase(0,12,true)]
    [TestCase(0,-1,false)]
    [TestCase(0,25,false)]
    [TestCase(30,2,false)]
    public void TutorialTriggersOnlyForNearbyObjectsAhead(float x,float z,bool expected)
    {
        Assert.That(CoastalTutorialUI.IsAheadAndNear(Vector3.zero,Vector3.forward,new Vector3(x,4,z),24),Is.EqualTo(expected));
    }
}
#endif
