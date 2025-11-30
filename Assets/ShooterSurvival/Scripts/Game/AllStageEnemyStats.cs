using System.Collections.Generic;
using UnityEngine;

public enum EnemyTier { Normal, Elite, Boss }
public enum EnemyCombatType { Melee, Ranged }


[CreateAssetMenu(fileName = "AllStageEnemyStats", menuName = "Game/All Stage Enemy Stats")]
public class AllStageEnemyStats : ScriptableObject
{
    public List<StageEnemyStatBlock> stageEnemyStats;

    public EnemyStatEntry GetEnemyStat(int chapter, int stage, EnemyTier enemyTier, EnemyCombatType combatType)
    {
        var stageBlock = stageEnemyStats.Find(s => s.chapter == chapter && s.stage == stage);
        if (stageBlock == null) return null;

        return stageBlock.enemyStats.Find(e => e.enemyClass == enemyTier && e.combatType == combatType);
    }
}

[System.Serializable]
public class StageEnemyStatBlock
{
    public int chapter;
    public int stage;
    public List<EnemyStatEntry> enemyStats;
}

[System.Serializable]
public class EnemyStatEntry
{
    public EnemyTier enemyClass;
    public EnemyCombatType combatType;
    public float health;
    public float damage;
}


