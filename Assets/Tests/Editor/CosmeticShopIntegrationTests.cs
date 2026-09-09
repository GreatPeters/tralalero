#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CosmeticShopIntegrationTests
{
    [Test]
    public void EveryWorkbookCosmeticHasAUsableVisualAndPreview()
    {
        var catalog = Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.splitSharkMesh.subMeshCount, Is.EqualTo(2));
        Assert.That(catalog.splitSharkMesh.boneWeights.Length, Is.EqualTo(catalog.splitSharkMesh.vertexCount));
        Assert.That(CosmeticTables.Rows.Count, Is.EqualTo(12));
        foreach (var item in CosmeticTables.Rows)
        {
            var visual = catalog.Find(item.visualKey);
            Assert.That(visual, Is.Not.Null, item.id);
            Assert.That(visual.icon, Is.Not.Null, item.id);
            if (item.slot != CosmeticSlot.Hat) Assert.That(visual.material, Is.Not.Null, item.id);
            else if (!item.isDefault)
            {
                Assert.That(visual.accessory, Is.Not.Null, item.id);
                Assert.That(visual.accessory.GetComponentsInChildren<Collider>(true), Is.Empty, "Cosmetics must not alter collision");
            }
        }
    }

    [TestCase("Noryangjin_MapTool_Mode")]
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    public void ShopIsReachableAndUsesIndependentPreviewArea(string name)
    {
        string path = "Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas").transform;
            var shop = canvas.GetComponentInChildren<CosmeticShopUI>(true);
            Assert.That(shop, Is.Not.Null);
            var entry = canvas.Find("UI/Main/Bottom/Skin_Button").GetComponent<Button>();
            Assert.That(Enumerable.Range(0, entry.onClick.GetPersistentEventCount()).Any(i => entry.onClick.GetPersistentTarget(i) == shop.gameObject), Is.True);
            Assert.That(shop.preview.display.transform.parent.name, Is.EqualTo("PreviewArea"));
            Assert.That(shop.GetComponentsInChildren<TMP_Text>(true).All(t => t.font != null && t.font.HasCharacter('상')), Is.True, "All shop labels must support Korean");
            var customizer = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerCosmeticCustomizer>(true)).Single();
            Assert.That(customizer.modelRoot, Is.Not.Null);
            Assert.That(customizer.visuals, Is.SameAs(shop.visuals));
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
#endif
