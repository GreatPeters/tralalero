using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ChapterWorkshopTests
{
    private sealed class Store : IChapterUpgradeStore
    {
        public int UnlockedChapter { get; set; } = 1;
        public readonly Dictionary<int,int> Levels = new();
        public bool Owns(int chapter) => GetLevel(chapter) > 0;
        public int GetLevel(int chapter) => Levels.TryGetValue(chapter,out int level) ? level : 0;
        public void SetLevel(int chapter,int level) => Levels[chapter]=level;
        public void Save() { }
    }
    private sealed class Wallet : IChapterUpgradeWallet
    {
        public int Coins { get; set; } = 10000;
        public int Charges;
        public bool Refuse;
        public Action DuringSpend;
        public bool Spend(int amount)
        {
            DuringSpend?.Invoke();
            if(Refuse || amount>Coins)return false;
            Coins-=amount;Charges++;return true;
        }
    }
    private static ChapterUpgradeDefinition[] Rows() => new[] {
        new ChapterUpgradeDefinition{chapter=1,coinCost=500,attackPercent=100,healthPercent=200},
        new ChapterUpgradeDefinition{chapter=2,coinCost=2000,attackPercent=200,healthPercent=400},
        new ChapterUpgradeDefinition{chapter=3,coinCost=5000,attackPercent=300,healthPercent=600}
    };

    [Test] public void LockedChapter_RejectsPurchaseEvenWithEnoughCoins()
    {
        var store=new Store();var wallet=new Wallet();var purchases=new ChapterUpgradePurchases(Rows(),store,wallet);
        Assert.That(purchases.Buy(2),Is.EqualTo(ChapterUpgradePurchaseResult.Locked));
        Assert.That(wallet.Coins,Is.EqualTo(10000));Assert.That(store.Owns(2),Is.False);
    }
    [Test] public void FiveRanks_ChargeEscalatingPricesAndRejectSixthAfterReload()
    {
        var store=new Store();var wallet=new Wallet{Coins=100000};var purchases=new ChapterUpgradePurchases(Rows(),store,wallet);
        for(int level=0;level<5;level++)
        {
            int before=wallet.Coins;
            Assert.That(purchases.Buy(1),Is.EqualTo(ChapterUpgradePurchaseResult.Purchased));
            Assert.That(before-wallet.Coins,Is.EqualTo(500*(1<<level)));
        }
        var reloaded=new ChapterUpgradePurchases(Rows(),store,wallet);
        Assert.That(reloaded.Buy(1),Is.EqualTo(ChapterUpgradePurchaseResult.Owned));
        reloaded.GetMultipliers(out float attack,out float health);
        Assert.That(wallet.Coins,Is.EqualTo(84500));Assert.That(wallet.Charges,Is.EqualTo(5));
        Assert.That(attack,Is.EqualTo(6));Assert.That(health,Is.EqualTo(11));
    }
    [Test] public void FirstChapter_IsAvailableBeforeAnyClear_AndRanksAreIndependent()
    {
        var store=new Store{UnlockedChapter=1};var wallet=new Wallet{Coins=100000};
        var purchases=new ChapterUpgradePurchases(Rows(),store,wallet);
        Assert.That(purchases.Buy(1),Is.EqualTo(ChapterUpgradePurchaseResult.Purchased));
        Assert.That(purchases.Buy(1),Is.EqualTo(ChapterUpgradePurchaseResult.Purchased));
        Assert.That(purchases.Buy(2),Is.EqualTo(ChapterUpgradePurchaseResult.Locked));
        store.UnlockedChapter=2;
        Assert.That(purchases.Buy(2),Is.EqualTo(ChapterUpgradePurchaseResult.Purchased));
        Assert.That(store.GetLevel(1),Is.EqualTo(2));Assert.That(store.GetLevel(2),Is.EqualTo(1));
    }
    [Test] public void LegacyOwnership_ReadsAsRankOneUntilExplicitLevelExists()
    {
        const int chapter=99;string owned=ChapterUpgradeService.OwnedKey(chapter),level=ChapterUpgradeService.LevelKey(chapter);
        bool hadOwned=UnityEngine.PlayerPrefs.HasKey(owned),hadLevel=UnityEngine.PlayerPrefs.HasKey(level);
        int oldOwned=UnityEngine.PlayerPrefs.GetInt(owned),oldLevel=UnityEngine.PlayerPrefs.GetInt(level);
        try
        {
            UnityEngine.PlayerPrefs.DeleteKey(level);UnityEngine.PlayerPrefs.SetInt(owned,1);
            Assert.That(ChapterUpgradeService.Level(chapter),Is.EqualTo(1));
            UnityEngine.PlayerPrefs.SetInt(level,3);Assert.That(ChapterUpgradeService.Level(chapter),Is.EqualTo(3));
            UnityEngine.PlayerPrefs.SetInt(level,0);Assert.That(ChapterUpgradeService.Level(chapter),Is.Zero);
        }
        finally
        {
            if(hadOwned)UnityEngine.PlayerPrefs.SetInt(owned,oldOwned);else UnityEngine.PlayerPrefs.DeleteKey(owned);
            if(hadLevel)UnityEngine.PlayerPrefs.SetInt(level,oldLevel);else UnityEngine.PlayerPrefs.DeleteKey(level);
        }
    }
    [Test] public void PurchasedChapters_AddBonusesInsteadOfMultiplyingEachOther()
    {
        var store=new Store{UnlockedChapter=3};var wallet=new Wallet();var purchases=new ChapterUpgradePurchases(Rows(),store,wallet);
        purchases.Buy(1);purchases.Buy(2);purchases.Buy(3);purchases.GetMultipliers(out float attack,out float health);
        Assert.That(attack,Is.EqualTo(7));Assert.That(health,Is.EqualTo(13));
        Assert.That(wallet.Coins,Is.EqualTo(2500));
    }
    [Test] public void InsufficientOrRefusedPayment_DoesNotGrantOwnership()
    {
        var store=new Store();var wallet=new Wallet{Coins=499};var purchases=new ChapterUpgradePurchases(Rows(),store,wallet);
        Assert.That(purchases.Buy(1),Is.EqualTo(ChapterUpgradePurchaseResult.InsufficientFunds));
        wallet.Coins=10000;wallet.Refuse=true;
        Assert.That(purchases.Buy(1),Is.EqualTo(ChapterUpgradePurchaseResult.InsufficientFunds));
        Assert.That(store.Owns(1),Is.False);purchases.GetMultipliers(out float attack,out float health);
        Assert.That(attack,Is.EqualTo(1));Assert.That(health,Is.EqualTo(1));Assert.That(wallet.Charges,Is.Zero);
    }
    [Test] public void SynchronousWalletCallback_CannotReenterPurchase()
    {
        var store=new Store{UnlockedChapter=3};var wallet=new Wallet();var purchases=new ChapterUpgradePurchases(Rows(),store,wallet);
        wallet.DuringSpend=()=>{
            Assert.That(store.Owns(1),Is.True,"ownership is staged before the wallet's combined save");
            Assert.That(purchases.Buy(2),Is.EqualTo(ChapterUpgradePurchaseResult.Busy));
        };
        purchases.Buy(1);Assert.That(wallet.Charges,Is.EqualTo(1));Assert.That(store.Owns(2),Is.False);
    }
    [Test] public void RefusedNextRank_RestoresThePreviouslyPurchasedRank()
    {
        var store=new Store();store.SetLevel(1,2);var wallet=new Wallet{Refuse=true};
        var purchases=new ChapterUpgradePurchases(Rows(),store,wallet);
        Assert.That(purchases.Buy(1),Is.EqualTo(ChapterUpgradePurchaseResult.InsufficientFunds));
        Assert.That(store.GetLevel(1),Is.EqualTo(2));Assert.That(wallet.Coins,Is.EqualTo(10000));
    }
    [Test] public void InvalidOrUnknownDefinitions_AreRejected()
    {
        var rows=Rows();rows[1].chapter=1;
        Assert.Throws<ArgumentException>(()=>new ChapterUpgradePurchases(rows,new Store(),new Wallet()));
        rows=Rows();rows[1].healthPercent=float.PositiveInfinity;
        Assert.Throws<ArgumentException>(()=>new ChapterUpgradePurchases(rows,new Store(),new Wallet()));
        rows=Rows();rows[0].coinCost=-1;
        Assert.Throws<ArgumentException>(()=>new ChapterUpgradePurchases(rows,new Store(),new Wallet()));
        Assert.That(new ChapterUpgradePurchases(Rows(),new Store(),new Wallet()).Buy(9),Is.EqualTo(ChapterUpgradePurchaseResult.UnknownChapter));
    }

    [Test] public void ChapterMultiplier_AppliesAfterRegularAndEquipmentBonuses_WithoutChangingOtherStats()
    {
        var types=new[]{UpgradeStatManager.UpgradeType.ATT,UpgradeStatManager.UpgradeType.HP,UpgradeStatManager.UpgradeType.ATT_SPEED};
        var saved=new Dictionary<string,(bool exists,float value)>();
        var savedTypes=new Dictionary<string,(bool exists,int value)>();
        foreach(var type in types)
        {
            string key="upgrade_stat_"+type,kind="upgrade_stat_type_"+type;
            saved[key]=(UnityEngine.PlayerPrefs.HasKey(key),UnityEngine.PlayerPrefs.GetFloat(key));
            savedTypes[kind]=(UnityEngine.PlayerPrefs.HasKey(kind),UnityEngine.PlayerPrefs.GetInt(kind));
        }
        var prior=UpgradeStatManager.S;var go=new UnityEngine.GameObject("Chapter multiplier test");
        try
        {
            var stats=go.AddComponent<UpgradeStatManager>();
            stats.ApplyUpgrade(UpgradeStatManager.UpgradeType.ATT,8,ValueType.Value);
            stats.ApplyUpgrade(UpgradeStatManager.UpgradeType.HP,20,ValueType.Value);
            stats.ApplyUpgrade(UpgradeStatManager.UpgradeType.ATT_SPEED,50,ValueType.Percent);
            stats.SetRuntimeModifier("equipment_Shoes",UpgradeStatManager.UpgradeType.ATT,16,ValueType.Percent);
            stats.SetChapterMultipliers(2,3);stats.SetChapterMultipliers(2,3);
            Assert.That(stats.ApplyToBase(UpgradeStatManager.UpgradeType.ATT,8),Is.EqualTo(34.56f).Within(.001f));
            Assert.That(stats.ApplyToBase(UpgradeStatManager.UpgradeType.HP,60),Is.EqualTo(240));
            Assert.That(stats.ApplyToBase(UpgradeStatManager.UpgradeType.ATT_SPEED,1),Is.EqualTo(1.5f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);UpgradeStatManager.S=prior;
            foreach(var row in saved){if(row.Value.exists)UnityEngine.PlayerPrefs.SetFloat(row.Key,row.Value.value);else UnityEngine.PlayerPrefs.DeleteKey(row.Key);}
            foreach(var row in savedTypes){if(row.Value.exists)UnityEngine.PlayerPrefs.SetInt(row.Key,row.Value.value);else UnityEngine.PlayerPrefs.DeleteKey(row.Key);}
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
