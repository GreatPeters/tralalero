#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class LateralUpgradeTests
{
    private readonly List<GameObject> objects = new();
    private readonly Dictionary<string, (bool exists, float value, bool integer)> preferences = new();
    private UpgradeStatManager previousManager;
    private UpgradeStatManager manager;

    [SetUp]
    public void Setup()
    {
        previousManager = UpgradeStatManager.S;
        foreach (string type in Enum.GetNames(typeof(UpgradeStatManager.UpgradeType)))
        {
            SavePreference("upgrade_stat_" + type, false);
            SavePreference("upgrade_stat_type_" + type, true);
        }
        SavePreference("upgrade_lv_10", true);
        manager = Make("Isolated lateral stats").AddComponent<UpgradeStatManager>();
        UpgradeStatManager.S = manager;
    }

    private GameObject Make(string name)
    {
        var root = new GameObject(name);
        root.SetActive(false);
        objects.Add(root);
        return root;
    }

    private void SavePreference(string key, bool integer) =>
        preferences[key] = (PlayerPrefs.HasKey(key), integer ? PlayerPrefs.GetInt(key) : PlayerPrefs.GetFloat(key), integer);

    [TearDown]
    public void Cleanup()
    {
        UpgradeStatManager.S = previousManager;
        foreach (var root in objects) if (root != null) Object.DestroyImmediate(root);
        objects.Clear();
        foreach (var pair in preferences)
        {
            if (!pair.Value.exists) PlayerPrefs.DeleteKey(pair.Key);
            else if (pair.Value.integer) PlayerPrefs.SetInt(pair.Key, (int)pair.Value.value);
            else PlayerPrefs.SetFloat(pair.Key, pair.Value.value);
        }
        preferences.Clear();
        PlayerPrefs.Save();
    }

    [Test]
    public void NewType_PreservesLegacySavedEnumValues()
    {
        Assert.That((int)UpgradeStatManager.UpgradeType.ATT, Is.Zero);
        Assert.That((int)UpgradeStatManager.UpgradeType.BOOMBAR, Is.EqualTo(8));
        Assert.That((int)UpgradeStatManager.UpgradeType.LATERAL_SPEED, Is.EqualTo(9));
    }

    [TestCase(0f, 1f)]
    [TestCase(5f, 1.05f)]
    [TestCase(50f, 1.5f)]
    [TestCase(100f, 1.5f)]
    [TestCase(-20f, 1f)]
    public void LateralResponse_UsesOneAdditivePercentAndStaysWithinBudget(float percent, float expected)
    {
        manager.ApplyUpgrade(UpgradeStatManager.UpgradeType.LATERAL_SPEED, percent, global::ValueType.Percent);
        var player = Make("Isolated player").AddComponent<PlayerScript>();
        Assert.That(player.LateralSpeedMultiplier, Is.EqualTo(expected).Within(.00001f));
    }

    [Test]
    public void Workbook_HasTenFivePercentLevels_AndNoEleventhPurchase()
    {
        for (int level = 1; level <= 10; level++)
        {
            Assert.That(UpgradeTables.TryGet(10, level, out var row), Is.True, "Missing lateral level " + level);
            Assert.That(row.type, Is.EqualTo(UpgradeStatManager.UpgradeType.LATERAL_SPEED));
            Assert.That(row.amount, Is.EqualTo(level * 5f));
            Assert.That(row.valueType, Is.EqualTo(global::ValueType.Percent));
            Assert.That(row.priceType, Is.EqualTo(PriceType.Coin));
            Assert.That(row.price, Is.GreaterThan(0));
        }
        Assert.That(UpgradeTables.TryGet(10, 11, out _), Is.False);
    }

    [Test]
    public void SavedLevel_RecomputesMovementFromWorkbookOnLobbyReload()
    {
        PlayerPrefs.SetInt("upgrade_lv_10", 7);
        PlayerPrefs.SetFloat("upgrade_stat_LATERAL_SPEED", 999f);
        PlayerPrefs.SetInt("upgrade_stat_type_LATERAL_SPEED", (int)global::ValueType.Percent);
        manager.SyncFromPurchasedLevels();
        var player = Make("Isolated player").AddComponent<PlayerScript>();
        Assert.That(player.LateralSpeedMultiplier, Is.EqualTo(1.35f).Within(.00001f));
    }
}
#endif
