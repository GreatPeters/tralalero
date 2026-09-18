#if UNITY_EDITOR
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ChapterEquipmentRevisionTests
{
    [Test]
    public void PaidShoesHaveDistinctModelsAndCompleteSkinnedBodies()
    {
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        var rows=CosmeticTables.Rows.Where(r=>r.slot==CosmeticSlot.Shoes&&!r.isDefault).ToArray();
        Assert.That(rows.Length,Is.EqualTo(7));Assert.That(rows.Select(r=>catalog.Find(r.visualKey).accessory).Distinct().Count(),Is.EqualTo(7));
        foreach(var row in rows)
        {
            var visual=catalog.Find(row.visualKey);Assert.That(visual.fittedBodyMesh,Is.Not.Null,row.id);Assert.That(visual.fittedShoeMesh,Is.Not.Null,row.id);
            foreach(var mesh in new[]{visual.fittedBodyMesh,visual.fittedShoeMesh})
            {
                Assert.That(mesh.bindposes.Length,Is.EqualTo(catalog.sourceSharkMesh.bindposes.Length));Assert.That(mesh.boneWeights.Length,Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.boneWeights.All(w=>Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.001f),Is.True,row.id);
                Assert.That(mesh.triangles.All(i=>i>=0&&i<mesh.vertexCount),Is.True,row.id);
            }
            Assert.That(visual.accessory.GetComponentsInChildren<Collider>(true),Is.Empty,row.id);
        }
    }
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    public void ChapterHasOneTwoOneCompositionAndUsableBindings(string name)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var enemies=map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true);var pairs=map.Find("Bonuses").GetComponentsInChildren<BonusWallChoicePair>(true);var gates=map.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>(true);
            var rows=EncounterPlacementTables.Rows.Where(r=>r.scene==name).ToArray();Assert.That(enemies.Length,Is.EqualTo(50));Assert.That(pairs.Length,Is.EqualTo(25));Assert.That(pairs.All(p=>p.IsConfigured),Is.True);
            Assert.That(rows.Count(r=>r.kind=="적 배치"&&r.enabled),Is.EqualTo(50));Assert.That(rows.Count(r=>r.kind=="보너스 배치"&&r.enabled),Is.EqualTo(25));Assert.That(rows.Count(r=>r.kind=="기믹 배치"&&r.enabled),Is.EqualTo(25));
            foreach(var enemy in enemies)Assert.That(gates.Count(g=>g.Targets.Length==1&&g.Targets[0]==enemy),Is.EqualTo(1),enemy.name);
            Assert.That(rows.Where(r=>r.kind=="적 배치").All(r=>!r.dropBonusAltar&&r.coinReward>0),Is.True);
            foreach(var row in rows.Where(r=>r.kind=="기믹 배치"))
            {
                var placement=map.Find("Props").Find(row.id);Assert.That(placement,Is.Not.Null,row.id);
                var parts=placement.GetComponentsInChildren<ObstacleStats>(true).Where(s=>s.gameObject.activeSelf&&s.enabled).ToArray();Assert.That(parts.Length,Is.GreaterThan(0));Assert.That(parts.All(p=>p.obstaclePattern==row.pattern),Is.True,row.id);
                foreach(var dolphin in parts.Where(p=>p.obstaclePattern==ObstaclePattern.Dolphin)){Assert.That(dolphin.pointA,Is.Not.Null);Assert.That(dolphin.pointB,Is.Not.Null);Assert.That(dolphin.pointA.gameObject.scene,Is.EqualTo(scene));}
            }
            if(name.StartsWith("Noryangjin"))Assert.That(rows.Where(r=>r.kind=="기믹 배치").Select(r=>r.pattern).Distinct(),Is.EquivalentTo(new[]{ObstaclePattern.Hole,ObstaclePattern.Oil,ObstaclePattern.Ship,ObstaclePattern.Light,ObstaclePattern.Oldman,ObstaclePattern.Dolphin,ObstaclePattern.Bucket,ObstaclePattern.Seagull}));
            var shop=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponentInChildren<CosmeticShopUI>(true);Assert.That(shop.itemsRoot.GetComponent<GridLayoutGroup>().constraintCount,Is.EqualTo(2));Assert.That(shop.preview.display.GetComponent<CosmeticPreviewDrag>(),Is.Not.Null);
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    [Test]
    public void IncompleteReplacementFallsBackWithoutRemovingFeet()
    {
        var original=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var catalog=Object.Instantiate(original);catalog.footMounts=System.Array.Empty<CosmeticVisualCatalog.FootMount>();var model=Object.Instantiate(original.previewModel);
        try{CosmeticAppearance.Apply(model.transform,catalog,"skin_original","shoes_gold","hat_none");Assert.That(model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="char1").sharedMesh,Is.SameAs(original.splitSharkMesh));}
        finally{Object.DestroyImmediate(model);Object.DestroyImmediate(catalog);}
    }
}
#endif
