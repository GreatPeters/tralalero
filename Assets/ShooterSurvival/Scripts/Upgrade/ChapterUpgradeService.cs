using System;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;

public static class ChapterUpgradeService
{
    private static ChapterUpgradeCatalog catalog;
    private static ChapterUpgradePurchases purchases;
    public static event Action Changed;
    public static string OwnedKey(int chapter) => "chapter_workshop_owned_" + chapter;
    public static string LevelKey(int chapter) => "chapter_workshop_level_" + chapter;
    public static int UnlockedChapter => Mathf.Max(1, PlayerPrefs.GetInt("chapter_unlocked", 1));
    public static int Level(int chapter) => Mathf.Clamp(PlayerPrefs.GetInt(LevelKey(chapter), PlayerPrefs.GetInt(OwnedKey(chapter), 0) == 1 ? 1 : 0), 0, ChapterUpgradeDefinition.MaxLevel);
    public static bool Owns(int chapter) => Level(chapter) > 0;
    public static ChapterUpgradeCatalog Catalog => catalog != null ? catalog : catalog = Resources.Load<ChapterUpgradeCatalog>(ChapterUpgradeCatalog.ResourcePath);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { catalog = null; purchases = null; Changed = null; }

    private static ChapterUpgradePurchases Purchases => purchases ??= Catalog == null ? null :
        new ChapterUpgradePurchases(Catalog.entries, new Store(), new Wallet());

    public static ChapterUpgradePurchaseResult Buy(int chapter)
    {
        if (TimeManager.isGameRunning) return ChapterUpgradePurchaseResult.Busy;
        var result = Purchases?.Buy(chapter) ?? ChapterUpgradePurchaseResult.UnknownChapter;
        if (result == ChapterUpgradePurchaseResult.Purchased)
        {
            ApplyToStats(UpgradeStatManager.S);
            GameAudioService.Play(GameSound.Upgrade);
            Changed?.Invoke();
        }
        else GameAudioService.Play(GameSound.Denied);
        return result;
    }

    public static void ApplyToStats(UpgradeStatManager stats)
    {
        if (stats == null) return;
        float attack = 1, health = 1;
        Purchases?.GetMultipliers(out attack, out health);
        stats.SetChapterMultipliers(attack, health);
    }

    private sealed class Store : IChapterUpgradeStore
    {
        public int UnlockedChapter => ChapterUpgradeService.UnlockedChapter;
        public int GetLevel(int chapter) => Level(chapter);
        public void SetLevel(int chapter, int level) => PlayerPrefs.SetInt(LevelKey(chapter), level);
        public void Save() => PlayerPrefs.Save();
    }
    private sealed class Wallet : IChapterUpgradeWallet
    {
        public int Coins => MoneyScript.S != null ? MoneyScript.S.Coin : 0;
        public bool Spend(int amount) => MoneyScript.S != null && MoneyScript.S.SpendCoin(amount);
    }
}
