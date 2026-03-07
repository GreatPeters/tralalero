using System;

[Serializable]
public struct MonsterRow
{
    public int id;
    public int chapter;
    public int stage;
    public EnemyTier tier;
    public float damage;
    public float health;
    public string note;

    public override string ToString()
        => $"id={id} chapter={chapter} stage={stage} tier={tier} dmg={damage} hp={health} note={note}";
}
