using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ChapterUpgradeRosterTests
{
    [Test]
    public void NewChapterCardsKeepIndependentBindingsAndRemainInsideScrollableContent()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var catalog = ScriptableObject.CreateInstance<ChapterUpgradeCatalog>();
        try
        {
            var root = Rect(null, "ChapterUpgrades");
            SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            root.sizeDelta = new Vector2(1000, 900);
            var viewport = Rect(root, "Viewport");
            viewport.sizeDelta = new Vector2(1000, 900);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.viewport = viewport;
            var content = Rect(viewport, "Items");
            content.sizeDelta = new Vector2(1000, 0); scroll.content = content;
            var layout = content.gameObject.AddComponent<EquipmentCardGrid>();
            layout.grid = content.gameObject.AddComponent<GridLayoutGroup>();
            layout.grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.grid.constraintCount = 1;
            layout.grid.spacing = new Vector2(0, 15);
            layout.grid.padding = new RectOffset(2, 2, 4, 14);
            layout.cardHeight = 362; layout.scroll = scroll;
            for (int chapter = 1; chapter <= 3; chapter++) Card(content, chapter);
            catalog.entries = Enumerable.Range(1, 5).Select(chapter => new ChapterUpgradeDefinition
            {
                chapter = chapter, title = "Chapter " + chapter, coinCost = chapter * 100,
                attackPercent = 5, healthPercent = 5
            }).ToArray();

            Assert.That(ChapterUpgradeRoster.Ensure(root, catalog), Is.EqualTo(2));
            Assert.That(ChapterUpgradeRoster.Ensure(root, catalog), Is.Zero, "Reopening the tab must not duplicate cards");
            var cards = root.GetComponentsInChildren<ChapterUpgradeCardUI>(true).OrderBy(card => card.chapter).ToArray();
            Assert.That(cards.Select(card => card.chapter), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(content.childCount, Is.EqualTo(5));
            Assert.That(scroll.content, Is.SameAs(content));
            Assert.That(scroll.vertical, Is.True);
            Assert.That(content.rect.height, Is.EqualTo(5 * 362 + 4 * 15 + 18).Within(.1f));
            Assert.That(content.rect.height, Is.GreaterThan(viewport.rect.height));
            foreach (var card in cards.Skip(3))
            {
                Assert.That(card.transform.parent, Is.SameAs(content));
                Assert.That(card.buyButton.transform.IsChildOf(card.transform), Is.True);
                Assert.That(card.title.transform.IsChildOf(card.transform), Is.True);
                Assert.That(card.buyButton, Is.Not.SameAs(cards[2].buyButton));
                Assert.That(card.title.text, Is.EqualTo("Chapter " + card.chapter));
                Assert.That(card.chapterArtwork.gameObject.activeSelf, Is.False, "Do not label RestStop artwork as a new chapter");
                Assert.That(card.transform.Find("ChapterNumberArtwork").GetComponent<TMP_Text>().text,
                    Is.EqualTo(card.chapter.ToString("00")));
            }
        }
        finally
        {
            Object.DestroyImmediate(catalog);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static ChapterUpgradeCardUI Card(Transform parent, int chapter)
    {
        var root = Rect(parent, "Chapter" + chapter);
        var card = root.gameObject.AddComponent<ChapterUpgradeCardUI>();
        card.chapter = chapter;
        card.title = Label(root, "Title");
        card.chapterLabel = Label(root, "ChapterNumber");
        card.effects = Label(root, "Effects");
        card.actionLabel = Label(root, "Action");
        card.buyButton = Rect(root, "Buy").gameObject.AddComponent<Button>();
        card.lockMarker = Rect(root, "Locked").gameObject;
        card.coinIcon = Rect(root, "Coin").gameObject;
        card.chapterArtwork = Rect(root, "ChapterArt").gameObject.AddComponent<Image>();
        return card;
    }

    private static TMP_Text Label(Transform parent, string name) =>
        Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();

    private static RectTransform Rect(Transform parent, string name)
    {
        var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        if (parent != null) result.SetParent(parent, false);
        return result;
    }
}
