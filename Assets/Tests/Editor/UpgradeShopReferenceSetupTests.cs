#if UNITY_EDITOR
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UpgradeShopReferenceSetupTests
{
    private const string MapScenePath =
        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";

    [Test]
    public void UpgradeUI_PriceCurrencyFollowsDisplayedRowPriceType()
    {
        var root = new GameObject("Upgrade Currency Test");
        var currencyObject = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        var coinTexture = new Texture2D(1, 1);
        var jewelTexture = new Texture2D(1, 1);
        Sprite coin = null;
        Sprite jewel = null;
        FieldInfo tableField = typeof(UpgradeTables).GetField(
            "_map",
            BindingFlags.Static | BindingFlags.NonPublic);
        object originalTable = tableField?.GetValue(null);
        try
        {
            currencyObject.transform.SetParent(root.transform, false);
            UpgradeUI upgrade = root.AddComponent<UpgradeUI>();
            Image currency = currencyObject.GetComponent<Image>();
            coin = Sprite.Create(coinTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            jewel = Sprite.Create(jewelTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

            var serialized = new SerializedObject(upgrade);
            serialized.FindProperty("priceCurrencyImage").objectReferenceValue = currency;
            serialized.FindProperty("coinPriceSprite").objectReferenceValue = coin;
            serialized.FindProperty("jewelPriceSprite").objectReferenceValue = jewel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(tableField, Is.Not.Null);
            tableField.SetValue(
                null,
                new Dictionary<int, Dictionary<int, UpgradeRow>>
                {
                    [1] = new Dictionary<int, UpgradeRow>
                    {
                        [0] = new UpgradeRow
                        {
                            id = 1,
                            level = 0,
                            priceType = PriceType.Coin
                        },
                        [1] = new UpgradeRow
                        {
                            id = 1,
                            level = 1,
                            priceType = PriceType.Jewel
                        }
                    }
                });

            MethodInfo refresh = typeof(UpgradeUI).GetMethod(
                "Refresh",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo levelField = typeof(UpgradeUI).GetField(
                "level",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(refresh, Is.Not.Null);
            Assert.That(levelField, Is.Not.Null);
            refresh.Invoke(upgrade, null);
            Assert.That(currency.sprite, Is.SameAs(jewel));

            tableField.SetValue(
                null,
                new Dictionary<int, Dictionary<int, UpgradeRow>>
                {
                    [1] = new Dictionary<int, UpgradeRow>
                    {
                        [1] = new UpgradeRow
                        {
                            id = 1,
                            level = 1,
                            priceType = PriceType.Coin
                        }
                    }
                });
            levelField.SetValue(upgrade, 1);
            refresh.Invoke(upgrade, null);
            Assert.That(currency.sprite, Is.SameAs(coin));
        }
        finally
        {
            if (tableField != null)
                tableField.SetValue(null, originalTable);
            Object.DestroyImmediate(coin);
            Object.DestroyImmediate(jewel);
            Object.DestroyImmediate(coinTexture);
            Object.DestroyImmediate(jewelTexture);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void UpgradeWorkshop_UsesReferenceBackgroundAndFunctionalThreeByThreeCards()
    {
        Scene scene = SceneManager.GetSceneByPath(MapScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
            scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Additive);

        try
        {
            Transform upgradeRoot =
                UpgradeShopReferenceSetup.ResolveReachableUpgradeRoot(scene);
            Assert.That(upgradeRoot, Is.Not.Null);
            Assert.That(upgradeRoot.name, Is.EqualTo("Upgrade2"));
            Assert.That(upgradeRoot.GetComponent<GridLayoutGroup>(), Is.Null);
            Transform topNavigation = upgradeRoot.parent.Find("Top");
            Assert.That(topNavigation, Is.Not.Null);
            Assert.That(
                upgradeRoot.GetSiblingIndex(),
                Is.LessThan(topNavigation.GetSiblingIndex()),
                "Global Back navigation must render above the opaque workshop.");

            Image background = upgradeRoot.GetComponent<Image>();
            Assert.That(background, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(background.sprite),
                Is.EqualTo(UpgradeShopReferenceSetup.BackgroundPath));
            Assert.That(upgradeRoot.Find(UpgradeShopReferenceSetup.OverlayName), Is.Not.Null);

            Button openButton = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .Single(button => button.name == "Upgrade_Button");
            Assert.That(
                HasPersistentSetActiveCall(
                    openButton,
                    upgradeRoot.gameObject,
                    expectedValue: true),
                Is.True,
                "The normal main-menu upgrade button must open the rebuilt panel.");

            Transform backRoot = upgradeRoot.root.Find("UI/Top/Back");
            Assert.That(backRoot, Is.Not.Null);
            Button backButton = backRoot?.GetComponentInChildren<Button>(true);
            Assert.That(
                HasPersistentSetActiveCall(
                    openButton,
                    backRoot.gameObject,
                    expectedValue: true),
                Is.True,
                "Opening the workshop must make the global Back control visible.");
            Assert.That(backButton, Is.Not.Null);
            Assert.That(
                HasPersistentSetActiveCall(
                    backButton,
                    upgradeRoot.gameObject,
                    expectedValue: false),
                Is.True,
                "The visible global back button must close the rebuilt panel.");

            Transform container = upgradeRoot.Find("GameObject");
            Assert.That(container, Is.Not.Null);
            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(3));

            UpgradeUI[] cards = container.GetComponentsInChildren<UpgradeUI>(true)
                .OrderBy(card => card.transform.GetSiblingIndex())
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(9));
            for (int index = 0; index < cards.Length; index++)
            {
                UpgradeUI card = cards[index];
                Assert.That(card.name, Is.EqualTo($"UpgradeCard_{index + 1:00}"));
                Assert.That(card.GetComponent<Button>(), Is.Not.Null);
                Assert.That(card.transform.Find("CurrentValue"), Is.Not.Null);
                Assert.That(card.transform.Find("NextValue"), Is.Not.Null);
                Assert.That(card.transform.Find("Description"), Is.Not.Null);
                Assert.That(card.transform.Find(UpgradeShopReferenceSetup.PriceBarName), Is.Not.Null);

                var serialized = new SerializedObject(card);
                Assert.That(
                    serialized.FindProperty("layoutMode").enumValueIndex,
                    Is.EqualTo(2));
                Assert.That(
                    serialized.FindProperty("buyButton").objectReferenceValue,
                    Is.SameAs(card.GetComponent<Button>()));
                Assert.That(
                    serialized.FindProperty("priceCurrencyImage").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serialized.FindProperty("coinPriceSprite").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serialized.FindProperty("jewelPriceSprite").objectReferenceValue,
                    Is.Not.Null);
            }

            TextMeshProUGUI title = upgradeRoot.Find("Button (1)")
                ?.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.That(title, Is.Not.Null);
            Assert.That(title.text, Is.EqualTo("그지 신발 개조소"));
            Assert.That(upgradeRoot.Find(UpgradeShopReferenceSetup.FooterName), Is.Not.Null);
        }
        finally
        {
            if (openedForTest)
                EditorSceneManager.CloseScene(scene, removeScene: true);
        }
    }

    private static bool HasPersistentSetActiveCall(
        Button button,
        GameObject target,
        bool expectedValue)
    {
        var serialized = new SerializedObject(button);
        SerializedProperty calls = serialized.FindProperty(
            "m_OnClick.m_PersistentCalls.m_Calls");
        for (int index = 0; index < calls.arraySize; index++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(index);
            if (call.FindPropertyRelative("m_Target").objectReferenceValue != target ||
                call.FindPropertyRelative("m_MethodName").stringValue != "SetActive" ||
                call.FindPropertyRelative("m_CallState").enumValueIndex == 0 ||
                call.FindPropertyRelative("m_Mode").enumValueIndex != 6)
            {
                continue;
            }

            return call.FindPropertyRelative("m_Arguments")
                .FindPropertyRelative("m_BoolArgument")
                .boolValue == expectedValue;
        }

        return false;
    }
}
#endif
