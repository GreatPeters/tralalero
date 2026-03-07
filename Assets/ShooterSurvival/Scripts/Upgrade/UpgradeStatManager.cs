using UnityEngine;
using System.Collections.Generic;

public class UpgradeStatManager : MonoBehaviour
{
    public static UpgradeStatManager S;

    // 엑셀 '식별 Enum' 컬럼 값과 이름을 동일하게 맞추세요
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

    private readonly Dictionary<UpgradeType, float> stats = new Dictionary<UpgradeType, float>();
    private readonly Dictionary<UpgradeType, ValueType> statValueTypes = new Dictionary<UpgradeType, ValueType>();

    const string SAVE_KEY = "upgrade_stat_";
    const string SAVE_KEY_TYPE = "upgrade_stat_type_";

    void Awake()
    {
        if (S != null && S != this)
        {
            Destroy(gameObject);
            return;
        }
        S = this;
    }

    // amount는 엑셀 수치 그대로 누적 저장
    // percent면 5,10,15...가 그대로 쌓임 (실제 적용할 때 /100f 해서 사용)
    public void ApplyUpgrade(UpgradeType type, float amount, ValueType valueType)
    {
        if (!stats.ContainsKey(type))
            stats[type] = Load(type);

        stats[type] += amount;
        Save(type, stats[type]);
        SaveValueType(type, valueType);
    }

    public float GetStat(UpgradeType type)
    {
        if (!stats.ContainsKey(type))
            stats[type] = Load(type);

        return stats[type];
    }

    public ValueType GetValueType(UpgradeType type)
    {
        if (!statValueTypes.ContainsKey(type))
            statValueTypes[type] = LoadValueType(type);

        return statValueTypes[type];
    }

    public float ApplyToBase(UpgradeType type, float baseValue)
    {
        float value = GetStat(type);
        return GetValueType(type) == ValueType.Percent
            ? baseValue * (1f + value / 100f)
            : baseValue + value;
    }

    void Save(UpgradeType type, float value)
    {
        PlayerPrefs.SetFloat(SAVE_KEY + type.ToString(), value);
        PlayerPrefs.Save();
    }

    float Load(UpgradeType type)
    {
        return PlayerPrefs.GetFloat(SAVE_KEY + type.ToString(), 0f);
    }

    void SaveValueType(UpgradeType type, ValueType valueType)
    {
        PlayerPrefs.SetInt(SAVE_KEY_TYPE + type.ToString(), (int)valueType);
        PlayerPrefs.Save();
        statValueTypes[type] = valueType;
    }

    ValueType LoadValueType(UpgradeType type)
    {
        int v = PlayerPrefs.GetInt(SAVE_KEY_TYPE + type.ToString(), (int)ValueType.Value);
        return (ValueType)v;
    }
}
