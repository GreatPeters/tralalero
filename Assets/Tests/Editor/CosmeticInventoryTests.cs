using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class CosmeticInventoryTests
{
    private sealed class Store : ICosmeticStorage
    {
        public readonly HashSet<string> Owned = new(); public readonly Dictionary<CosmeticSlot,string> Equipped = new();
        public bool IsOwned(string id) => Owned.Contains(id);
        public void SetOwned(string id, bool owned) { if(owned) Owned.Add(id); else Owned.Remove(id); }
        public string GetEquipped(CosmeticSlot slot) => Equipped.TryGetValue(slot,out var id)?id:"";
        public void SetEquipped(CosmeticSlot slot,string id)=>Equipped[slot]=id;
        public void Save() { }
    }
    private sealed class Wallet : ICosmeticWallet
    {
        public int Coins=300, Calls; public bool Refuse; public System.Action DuringSpend;
        public int Balance(PriceType type)=>Coins;
        public bool Spend(PriceType type,int amount){Calls++;DuringSpend?.Invoke();if(Refuse||amount>Coins)return false;Coins-=amount;return true;}
    }
    private static CosmeticItem[] Catalog() => new[] {
        new CosmeticItem{id="skin_base",name="Base",slot=CosmeticSlot.Skin,visualKey="base",isDefault=true},
        new CosmeticItem{id="shoes_base",name="Base shoes",slot=CosmeticSlot.Shoes,visualKey="shoes",isDefault=true},
        new CosmeticItem{id="hat_none",name="None",slot=CosmeticSlot.Hat,visualKey="none",isDefault=true},
        new CosmeticItem{id="skin_pink",name="Pink",slot=CosmeticSlot.Skin,visualKey="pink",price=100},
        new CosmeticItem{id="hat_cap",name="Cap",slot=CosmeticSlot.Hat,visualKey="cap",price=80}};
    [Test] public void BuyEquipAndReload_KeepIndependentSlotsAndChargeOnce()
    {
        var store=new Store();var wallet=new Wallet();var inventory=new CosmeticInventory(Catalog(),store,wallet);
        Assert.That(inventory.PurchaseOrEquip("skin_pink"),Is.EqualTo(CosmeticPurchaseResult.Purchased));
        Assert.That(inventory.PurchaseOrEquip("skin_pink"),Is.EqualTo(CosmeticPurchaseResult.Equipped));
        inventory.PurchaseOrEquip("hat_cap");
        var loaded=new CosmeticInventory(Catalog(),store,wallet);
        Assert.That(wallet.Coins,Is.EqualTo(120));Assert.That(wallet.Calls,Is.EqualTo(2));
        Assert.That(loaded.Equipped(CosmeticSlot.Skin).id,Is.EqualTo("skin_pink"));
        Assert.That(loaded.Equipped(CosmeticSlot.Hat).id,Is.EqualTo("hat_cap"));
        Assert.That(loaded.Equipped(CosmeticSlot.Shoes).id,Is.EqualTo("shoes_base"));
    }
    [Test] public void InsufficientOrFailedPayment_DoesNotGrantOrEquip()
    {
        var store=new Store();var wallet=new Wallet{Coins=20};var inventory=new CosmeticInventory(Catalog(),store,wallet);
        Assert.That(inventory.PurchaseOrEquip("skin_pink"),Is.EqualTo(CosmeticPurchaseResult.InsufficientFunds));
        wallet.Coins=300;wallet.Refuse=true;
        inventory.PurchaseOrEquip("skin_pink");
        Assert.That(inventory.Owns("skin_pink"),Is.False);
        Assert.That(inventory.Equipped(CosmeticSlot.Skin).id,Is.EqualTo("skin_base"));
        Assert.That(wallet.Coins,Is.EqualTo(300));
    }
    [Test] public void PaymentCallback_CannotReenterPurchase()
    {
        var wallet=new Wallet();var inventory=new CosmeticInventory(Catalog(),new Store(),wallet);
        wallet.DuringSpend=()=>Assert.That(inventory.PurchaseOrEquip("hat_cap"),Is.EqualTo(CosmeticPurchaseResult.Busy));
        inventory.PurchaseOrEquip("skin_pink");Assert.That(wallet.Coins,Is.EqualTo(200));
    }
    [Test] public void InvalidSavedSelection_FallsBackToFreeSlotDefault()
    {
        var store=new Store();store.Equipped[CosmeticSlot.Skin]="hat_cap";
        var inventory=new CosmeticInventory(Catalog(),store,new Wallet());
        Assert.That(inventory.Equipped(CosmeticSlot.Skin).id,Is.EqualTo("skin_base"));
        Assert.That(inventory.PurchaseOrEquip("missing"),Is.EqualTo(CosmeticPurchaseResult.UnknownItem));
    }
    [Test] public void NegativePriceAndDuplicateDefaults_AreRejected()
    {
        var rows=Catalog();rows.Last().price=-1;Assert.Throws<System.IO.InvalidDataException>(()=>CosmeticTables.Validate(rows));
        rows=Catalog();rows.Last().isDefault=true;rows.Last().price=0;Assert.Throws<System.IO.InvalidDataException>(()=>CosmeticTables.Validate(rows));
    }
}
