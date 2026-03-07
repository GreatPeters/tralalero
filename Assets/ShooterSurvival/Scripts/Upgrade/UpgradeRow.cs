using System;

public enum ValueType { Value, Percent }
public enum PriceType { Coin, Jewel }

[Serializable]
public struct UpgradeRow
{
    public int id;
    public int level;

    // 엑셀 '식별 Enum' (ATT, HP, COIN_BONUS...)
    public UpgradeStatManager.UpgradeType type;

    // 엑셀 '항목' (공격력, 체력...) - UI 표시용
    public string item;

    public float amount;
    public ValueType valueType;
    public PriceType priceType;
    public int price;
    public string note;

    public override string ToString()
        => $"id={id} lv={level} type={type} item={item} amount={amount} {valueType} price={priceType} {price} note={note}";
}
