using System;

[Serializable]
public struct SkinRow
{
    public int id;
    public string item;
    public PriceType priceType;
    public int price;
    public string bonusType;
    public ValueType bonusValueType;
    public float bonusValue;
    public string note;

    public override string ToString()
        => $"id={id} item={item} price={priceType} {price} bonus={bonusType} {bonusValueType} {bonusValue} note={note}";
}
