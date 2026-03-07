using System;

[Serializable]
public struct SkinRow
{
    public int id;
    public string item; // 스킨명
    public PriceType priceType;
    public int price;
    public string note;

    public override string ToString()
        => $"id={id} item={item} price={priceType} {price} note={note}";
}
