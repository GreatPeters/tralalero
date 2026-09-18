using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class ChapterUpgradeDefinition
{
    public const int MaxLevel = 5;
    public int chapter;
    public string title;
    public int coinCost;
    public float attackPercent;
    public float healthPercent;

    public int CostAtLevel(int currentLevel) => checked(coinCost * (1 << Math.Clamp(currentLevel, 0, MaxLevel - 1)));
}

[CreateAssetMenu(menuName = "Shooter/Chapter Workshop Upgrades")]
public sealed class ChapterUpgradeCatalog : ScriptableObject
{
    public const string ResourcePath = "Upgrades/ChapterWorkshop";
    [HideInInspector] public int pricingVersion;
    public ChapterUpgradeDefinition[] entries = Array.Empty<ChapterUpgradeDefinition>();

    public static void Validate(IEnumerable<ChapterUpgradeDefinition> rows)
    {
        var seen = new HashSet<int>();
        foreach (var row in rows)
            if (row == null || row.chapter < 1 || !seen.Add(row.chapter) || row.coinCost < 0 || row.coinCost > int.MaxValue / 16 ||
                float.IsNaN(row.attackPercent) || float.IsInfinity(row.attackPercent) || row.attackPercent < 0 ||
                float.IsNaN(row.healthPercent) || float.IsInfinity(row.healthPercent) || row.healthPercent < 0)
                throw new ArgumentException("Chapter upgrade entries require unique positive chapters and finite nonnegative prices/effects.");
    }
}

public interface IChapterUpgradeStore
{
    int UnlockedChapter { get; }
    int GetLevel(int chapter);
    void SetLevel(int chapter, int level);
    void Save();
}

public interface IChapterUpgradeWallet
{
    int Coins { get; }
    bool Spend(int amount);
}

public enum ChapterUpgradePurchaseResult { Purchased, Locked, Owned, InsufficientFunds, UnknownChapter, Busy }

/// <summary>Five persistent ranks per unlocked chapter, with synchronous re-entry protection.</summary>
public sealed class ChapterUpgradePurchases
{
    private readonly Dictionary<int, ChapterUpgradeDefinition> definitions;
    private readonly IChapterUpgradeStore store;
    private readonly IChapterUpgradeWallet wallet;
    private bool purchasing;

    public ChapterUpgradePurchases(IEnumerable<ChapterUpgradeDefinition> entries, IChapterUpgradeStore store, IChapterUpgradeWallet wallet)
    {
        var rows = entries.ToArray(); ChapterUpgradeCatalog.Validate(rows);
        definitions = rows.ToDictionary(row => row.chapter); this.store = store; this.wallet = wallet;
    }

    public ChapterUpgradePurchaseResult Buy(int chapter)
    {
        if (purchasing) return ChapterUpgradePurchaseResult.Busy;
        if (!definitions.TryGetValue(chapter, out var row)) return ChapterUpgradePurchaseResult.UnknownChapter;
        int level = Math.Clamp(store.GetLevel(chapter), 0, ChapterUpgradeDefinition.MaxLevel);
        if (level >= ChapterUpgradeDefinition.MaxLevel) return ChapterUpgradePurchaseResult.Owned;
        if (chapter > store.UnlockedChapter) return ChapterUpgradePurchaseResult.Locked;
        int cost = row.CostAtLevel(level);
        if (wallet.Coins < cost) return ChapterUpgradePurchaseResult.InsufficientFunds;
        purchasing = true;
        try
        {
            // MoneyScript saves during the debit, so stage ownership in that same save.
            store.SetLevel(chapter, level + 1);
            if (!wallet.Spend(cost))
            {
                store.SetLevel(chapter, level); store.Save();
                return ChapterUpgradePurchaseResult.InsufficientFunds;
            }
            store.Save();
            return ChapterUpgradePurchaseResult.Purchased;
        }
        finally { purchasing = false; }
    }

    public void GetMultipliers(out float attack, out float health)
    {
        attack = health = 1f;
        foreach (var row in definitions.Values)
        {
            int level = Math.Clamp(store.GetLevel(row.chapter), 0, ChapterUpgradeDefinition.MaxLevel);
            attack += row.attackPercent * level / 100f; health += row.healthPercent * level / 100f;
        }
    }
}
