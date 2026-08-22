using System;

public enum BonusValueType { Value, Percent, Ratio }

[Serializable]
public struct BonusRow
{
    public int id;
    public string rarity;
    public int level;
    public string alias;
    public string displayName;
    public string stat;
    public float min;
    public float max;
    public BonusValueType valueType;
    public string note;

    public override string ToString()
        => $"id={id} rarity={rarity} alias={alias} stat={stat} min={min} max={max} type={valueType} note={note}";
}
