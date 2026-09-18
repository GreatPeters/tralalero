using System;

/// <summary>Only the equipped item in each slot contributes, using stable modifier keys.</summary>
public static class EquipmentRunEffects
{
    public static void Apply(CosmeticInventory inventory, UpgradeStatManager stats)
    {
        if (stats == null) return;
        foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))
        {
            string key = "equipment_" + slot;
            var item = inventory?.Equipped(slot);
            if (item != null && item.effectValue > 0f && Enum.TryParse(item.effectStat, out UpgradeStatManager.UpgradeType type))
                stats.SetRuntimeModifier(key, type, item.effectValue, item.effectValueType);
            else stats.ClearRuntimeModifier(key);
        }
    }

    public static string Describe(CosmeticItem item)
    {
        if (item == null || item.effectValue <= 0f) return "기본 장비";
        string label = item.effectStat switch
        {
            "ATT" => "공격력", "HP" => "최대 체력", "ATT_SPEED" => "공격 속도",
            "PROJECTILE_SPEED" => "미사일 지속 시간", "BOSS_DAMAGE" => "보스 피해",
            "COIN_BONUS" => "코인 획득", "HP_REGEN" => "초당 체력 회복", _ => item.effectStat
        };
        return $"{label} +{item.effectValue:0.##}{(item.effectValueType == ValueType.Percent ? "%" : "")}";
    }
}
