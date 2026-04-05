using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeStatManager : MonoBehaviour
{
    public static UpgradeStatManager S;

    public enum UpgradeType
    {
        ATT,
        HP,
        ATT_SPEED,
        PROJECTILE_SPEED,
        BOSS_DAMAGE,
        COIN_BONUS,
        HP_REGEN,
        TUNGTUNGTUNG,
        BOOMBAR
    }

    private struct RuntimeStatModifier
    {
        public UpgradeType type;
        public float amount;
        public ValueType valueType;
    }

    private readonly Dictionary<UpgradeType, float> stats = new Dictionary<UpgradeType, float>();
    private readonly Dictionary<UpgradeType, ValueType> statValueTypes = new Dictionary<UpgradeType, ValueType>();
    private readonly Dictionary<string, RuntimeStatModifier> runtimeModifiers = new Dictionary<string, RuntimeStatModifier>();

    const string SAVE_KEY = "upgrade_stat_";
    const string SAVE_KEY_TYPE = "upgrade_stat_type_";

    public event Action StatsChanged;

    void Awake()
    {
        if (S != null && S != this)
        {
            Destroy(gameObject);
            return;
        }
        S = this;
    }

    public void ApplyUpgrade(UpgradeType type, float amount, ValueType valueType)
    {
        if (!stats.ContainsKey(type))
            stats[type] = Load(type);

        stats[type] = amount;
        Save(type, stats[type]);
        SaveValueType(type, valueType);
        RaiseStatsChanged();
    }

    public void SetRuntimeModifier(string sourceKey, UpgradeType type, float amount, ValueType valueType)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return;

        runtimeModifiers[sourceKey] = new RuntimeStatModifier
        {
            type = type,
            amount = amount,
            valueType = valueType
        };

        RaiseStatsChanged();
    }

    public void ClearRuntimeModifier(string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return;

        if (!runtimeModifiers.Remove(sourceKey))
            return;

        RaiseStatsChanged();
    }

    public float GetStat(UpgradeType type)
    {
        var savedValueType = GetSavedValueType(type);
        return GetSavedStat(type) + GetRuntimeStat(type, savedValueType);
    }

    public float GetFlatStat(UpgradeType type)
    {
        float total = 0f;
        if (GetSavedValueType(type) == ValueType.Value)
            total += GetSavedStat(type);
        total += GetRuntimeStat(type, ValueType.Value);
        return total;
    }

    public float GetPercentStat(UpgradeType type)
    {
        float total = 0f;
        if (GetSavedValueType(type) == ValueType.Percent)
            total += GetSavedStat(type);
        total += GetRuntimeStat(type, ValueType.Percent);
        return total;
    }

    public ValueType GetValueType(UpgradeType type)
    {
        if (GetPercentStat(type) != 0f && GetFlatStat(type) == 0f)
            return ValueType.Percent;
        return ValueType.Value;
    }

    public float GetAppliedValue(UpgradeType type, float percentBaseValue)
    {
        return GetFlatStat(type) + (percentBaseValue * (GetPercentStat(type) / 100f));
    }

    public float ApplyToBase(UpgradeType type, float baseValue)
    {
        return baseValue * (1f + GetPercentStat(type) / 100f) + GetFlatStat(type);
    }

    float GetSavedStat(UpgradeType type)
    {
        if (!stats.ContainsKey(type))
            stats[type] = Load(type);

        return stats[type];
    }

    ValueType GetSavedValueType(UpgradeType type)
    {
        if (!statValueTypes.ContainsKey(type))
            statValueTypes[type] = LoadValueType(type);

        return statValueTypes[type];
    }

    float GetRuntimeStat(UpgradeType type, ValueType valueType)
    {
        float total = 0f;
        foreach (var modifier in runtimeModifiers.Values)
        {
            if (modifier.type != type || modifier.valueType != valueType)
                continue;

            total += modifier.amount;
        }

        return total;
    }

    void RaiseStatsChanged()
    {
        StatsChanged?.Invoke();
    }

    void Save(UpgradeType type, float value)
    {
        PlayerPrefs.SetFloat(SAVE_KEY + type, value);
        PlayerPrefs.Save();
    }

    float Load(UpgradeType type)
    {
        return PlayerPrefs.GetFloat(SAVE_KEY + type, 0f);
    }

    void SaveValueType(UpgradeType type, ValueType valueType)
    {
        PlayerPrefs.SetInt(SAVE_KEY_TYPE + type, (int)valueType);
        PlayerPrefs.Save();
        statValueTypes[type] = valueType;
    }

    ValueType LoadValueType(UpgradeType type)
    {
        int v = PlayerPrefs.GetInt(SAVE_KEY_TYPE + type, (int)ValueType.Value);
        return (ValueType)v;
    }
}
