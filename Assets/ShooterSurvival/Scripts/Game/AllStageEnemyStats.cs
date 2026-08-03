using System.Collections.Generic;
using UnityEngine;

public enum EnemyTier { Normal, Elite, Boss }
public enum EnemyCombatType { Melee, Ranged }

public readonly struct ForwardEnemyArchetypeDefinition
{
    public ForwardEnemyArchetypeDefinition(
        string identity,
        string prefabPath,
        string koreanLabel,
        EnemyTier tier)
    {
        Identity = identity;
        PrefabPath = prefabPath;
        KoreanLabel = koreanLabel;
        Tier = tier;
    }

    public string Identity { get; }
    public string PrefabPath { get; }
    public string KoreanLabel { get; }
    public EnemyTier Tier { get; }
}

public static class ForwardEnemyArchetypeCatalog
{
    private const string PrefabRoot = "Assets/JH/Model/Prefab";

    public static readonly ForwardEnemyArchetypeDefinition[] Definitions =
    {
        new("Enemy_YllowMan", PrefabRoot + "/Enemy_YllowMan.prefab", "옐로우맨", EnemyTier.Normal),
        new("Enemy_Guard", PrefabRoot + "/Enemy_Guard.prefab", "가드", EnemyTier.Normal),
        new("Enemy_OldMan", PrefabRoot + "/Enemy_OldMan.prefab", "노인", EnemyTier.Normal),
        new("Enemy_FatMan", PrefabRoot + "/Enemy_FatMan.prefab", "뚱보", EnemyTier.Elite),
        new("Enemy_Woman", PrefabRoot + "/Enemy_Woman.prefab", "여성 보스", EnemyTier.Boss)
    };
}

public static class ForwardEnemyTierResolver
{
    public static bool TryResolve(string identity, out EnemyTier tier)
    {
        if (!string.IsNullOrEmpty(identity))
        {
            foreach (ForwardEnemyArchetypeDefinition definition in
                     ForwardEnemyArchetypeCatalog.Definitions)
            {
                if (identity.IndexOf(
                        definition.Identity,
                        System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                tier = definition.Tier;
                return true;
            }
        }

        tier = EnemyTier.Normal;
        return false;
    }

    public static EnemyTier ResolveOrFallback(string identity, EnemyTier fallback)
        => TryResolve(identity, out EnemyTier tier) ? tier : fallback;
}


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


