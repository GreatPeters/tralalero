using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ShopFidelityTests
{
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    public void ShopScenesPreserveTransactionsAndUseFinalPresentation(string name)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas");
            var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);
            Assert.That(shop.referencePresentation&&shop.preview.referencePresentation,Is.True);
            Assert.That(shop.preview.catalog.previewModel,Is.Not.Null,"Preview must render the actual model");
            Assert.That(shop.itemTemplate.GetComponent<Image>().sprite.name,Is.EqualTo("ShopCard"));
            Assert.That(shop.itemTemplate.transform.Find("PriceBand"),Is.Not.Null);
            Assert.That(shop.itemTemplate.transform.Find("EquippedCheck"),Is.Not.Null);
            var fitted=shop.fittedCardIcons.Single(s=>s.name=="ShopIcon_skin_armor");
            Assert.That(fitted.rect.width/fitted.rect.height,Is.GreaterThan(1.2f),"Armor icon excludes the large transparent bottom margin");
            Assert.That(shop.itemsRoot.GetComponent<GridLayoutGroup>().constraintCount,Is.EqualTo(2));
            foreach(var text in shop.GetComponentsInChildren<TMP_Text>(true)){
                Assert.That(AssetDatabase.GetAssetPath(text.font),Is.EqualTo(HarborGameUIInstaller.RoundedFontPath),text.name);
                Assert.That(text.fontSharedMaterial.IsKeywordEnabled("OUTLINE_ON"),Is.True,text.name);
            }
            var upgrade=canvas.transform.Find("UI/Upgrade2");
            var cards=upgrade.GetComponentsInChildren<UpgradeUI>(true);
            Assert.That(cards.Select(c=>c.UpgradeId),Is.EquivalentTo(Enumerable.Range(1,10)),"Retain every existing upgrade");
            foreach(var card in cards){
                var data=new SerializedObject(card);
                foreach(string field in new[]{"nameText","levelText","currentValueText","nextValueText","priceText","buyButton"})Assert.That(data.FindProperty(field).objectReferenceValue,Is.Not.Null,field);
                Assert.That(data.FindProperty("currentValueText").objectReferenceValue,Is.EqualTo(card.transform.Find("Up/EffectLine/CurrentValue").GetComponent<TMP_Text>()));
                Assert.That(AssetDatabase.GetAssetPath(data.FindProperty("iconOverride").objectReferenceValue),Does.StartWith(HarborGameUIInstaller.FaithfulPath+"Shop"));
            }
            foreach(var card in upgrade.GetComponentsInChildren<ChapterUpgradeCardUI>(true)){
                Assert.That(card.referencePresentation,Is.True);
                Assert.That(card.emptyPip,Is.Not.Null);Assert.That(card.filledPip,Is.Not.SameAs(card.emptyPip));
                Assert.That(card.rankPips.Length,Is.EqualTo(ChapterUpgradeDefinition.MaxLevel));
                Assert.That(card.lockedText,Is.Not.Null);Assert.That(card.lockedShade,Is.Not.Null);
            }
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
