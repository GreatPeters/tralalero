using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ICosmeticStorage
{
    bool IsOwned(string id);
    void SetOwned(string id, bool owned);
    string GetEquipped(CosmeticSlot slot);
    void SetEquipped(CosmeticSlot slot, string id);
    void Save();
}
public interface ICosmeticWallet
{
    int Balance(PriceType currency);
    bool Spend(PriceType currency, int amount);
}
public enum CosmeticPurchaseResult { Purchased, Equipped, InsufficientFunds, UnknownItem, Busy }

public sealed class CosmeticInventory
{
    private readonly Dictionary<string, CosmeticItem> items;
    private readonly ICosmeticStorage storage;
    private readonly ICosmeticWallet wallet;
    private bool purchasing;
    public event Action Changed;
    public IEnumerable<CosmeticItem> Items => items.Values;
    public CosmeticInventory(IEnumerable<CosmeticItem> catalog, ICosmeticStorage storage, ICosmeticWallet wallet)
    {
        var rows = catalog.ToArray(); CosmeticTables.Validate(rows);
        items = rows.ToDictionary(r => r.id, StringComparer.Ordinal); this.storage = storage; this.wallet = wallet;
        bool changed = false;
        foreach (var row in rows.Where(r => r.isDefault))
            if (!storage.IsOwned(row.id)) { storage.SetOwned(row.id, true); changed = true; }
        if (changed) storage.Save();
    }
    public bool Owns(string id) => items.ContainsKey(id) && storage.IsOwned(id);
    public CosmeticItem Find(string id) => id != null && items.TryGetValue(id, out var row) ? row : null;
    public CosmeticItem Equipped(CosmeticSlot slot)
    {
        var row = Find(storage.GetEquipped(slot));
        return row != null && row.slot == slot && Owns(row.id) ? row : items.Values.Single(r => r.slot == slot && r.isDefault);
    }
    public CosmeticPurchaseResult PurchaseOrEquip(string id)
    {
        if (purchasing) return CosmeticPurchaseResult.Busy;
        var row = Find(id); if (row == null) return CosmeticPurchaseResult.UnknownItem;
        bool owned = Owns(id);
        if (!owned && wallet.Balance(row.currency) < row.price) return CosmeticPurchaseResult.InsufficientFunds;
        purchasing = true;
        try
        {
            if (!owned)
            {
                // Stage ownership before MoneyScript saves the debit; both keys persist together.
                storage.SetOwned(id, true);
                if (!wallet.Spend(row.currency, row.price)) { storage.SetOwned(id, false); return CosmeticPurchaseResult.InsufficientFunds; }
            }
            storage.SetEquipped(row.slot, id); storage.Save(); Changed?.Invoke();
            return owned ? CosmeticPurchaseResult.Equipped : CosmeticPurchaseResult.Purchased;
        }
        finally { purchasing = false; }
    }
}

public static class CosmeticService
{
    private static CosmeticInventory current;
    private static IReadOnlyList<CosmeticItem> catalog;
    public static event Action Changed;
    public static string OwnedKey(string id) => "cosmetic_owned_" + id;
    public static string EquippedKey(CosmeticSlot slot) => "cosmetic_equipped_" + slot;
    public static CosmeticInventory Current
    {
        get
        {
            var rows = CosmeticTables.Rows;
            if (rows.Count == 0) return null;
            if (current == null || !ReferenceEquals(catalog, rows))
            {
                catalog = rows; current = new CosmeticInventory(rows, new PrefsStorage(), new GameWallet());
                current.Changed += () => {
                    if (!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)
                        EquipmentRunEffects.Apply(current, UpgradeStatManager.S);
                    Changed?.Invoke();
                };
            }
            return current;
        }
    }
    public static void ReloadCatalog() { CosmeticTables.Reload(); current = null; Changed?.Invoke(); }
    private sealed class PrefsStorage : ICosmeticStorage
    {
        public bool IsOwned(string id) => PlayerPrefs.GetInt(OwnedKey(id), 0) == 1;
        public void SetOwned(string id, bool owned) { if (owned) PlayerPrefs.SetInt(OwnedKey(id), 1); else PlayerPrefs.DeleteKey(OwnedKey(id)); }
        public string GetEquipped(CosmeticSlot slot) => PlayerPrefs.GetString(EquippedKey(slot), "");
        public void SetEquipped(CosmeticSlot slot, string id) => PlayerPrefs.SetString(EquippedKey(slot), id);
        public void Save() => PlayerPrefs.Save();
    }
    private sealed class GameWallet : ICosmeticWallet
    {
        public int Balance(PriceType type) => MoneyScript.S == null ? 0 : type == PriceType.Coin ? MoneyScript.S.Coin : MoneyScript.S.Jewel;
        public bool Spend(PriceType type, int amount) => MoneyScript.S != null && (type == PriceType.Coin ? MoneyScript.S.SpendCoin(amount) : MoneyScript.S.SpendJewel(amount));
    }
}
