using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    public static class BonusAltarRules
    {
        public const float DefaultNearbyDistance = 12f;

        public static string DataRarityFor(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Normal => "Normal",
                Rarity.Rare => "Rare",
                Rarity.Unique => "Unique",
                _ => "Normal"
            };
        }

        public static string GradeLabel(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Rare => "엘리트",
                Rarity.Unique => "유니크",
                _ => "노멀"
            };
        }

        public static string[] CreateGradeLabels()
        {
            return new[]
            {
                GradeLabel(Rarity.Normal),
                GradeLabel(Rarity.Rare),
                GradeLabel(Rarity.Unique)
            };
        }

        public static List<BonusRow> BuildCandidates(
            IReadOnlyList<BonusRow> rows,
            Rarity rarity,
            ISet<string> excludedStats)
        {
            var candidates = new List<BonusRow>();
            string dataRarity = DataRarityFor(rarity);

            if (rows == null)
                return candidates;

            for (int i = 0; i < rows.Count; i++)
            {
                BonusRow row = rows[i];
                if (!string.Equals(row.rarity, dataRarity, StringComparison.OrdinalIgnoreCase) ||
                    !TryResolveBuffType(rarity, row.stat, out _))
                {
                    continue;
                }

                if (excludedStats == null || !excludedStats.Contains(row.stat))
                    candidates.Add(row);
            }

            return candidates;
        }

        public static bool TryResolveBuffType(
            Rarity rarity,
            string stat,
            out BuffType buffType)
        {
            string key = (stat ?? string.Empty).Trim();
            if (rarity == Rarity.Normal)
            {
                switch (key)
                {
                    case "att": buffType = BuffType.att_normmal; return true;
                    case "attPercent": buffType = BuffType.attPer_normal; return true;
                    case "attackSpeed": buffType = BuffType.attackSpeed_normal; return true;
                    case "missileDistance": buffType = BuffType.missileDistance_normal; return true;
                    case "hp": buffType = BuffType.hp_normal; return true;
                    case "hpPercent": buffType = BuffType.hpPer_normal; return true;
                }
            }
            else if (rarity == Rarity.Rare)
            {
                switch (key)
                {
                    case "tungtungAdd": buffType = BuffType.tungtung_rare; return true;
                    case "boombarAdd": buffType = BuffType.boombar_rare; return true;
                }
            }
            else if (rarity == Rarity.Unique)
            {
                switch (key)
                {
                    case "missileAdd": buffType = BuffType.missileAdd_unique; return true;
                    case "att": buffType = BuffType.att_unique; return true;
                    case "attPercent": buffType = BuffType.attPer_unique; return true;
                    case "attackSpeed": buffType = BuffType.attackSpeed_unique; return true;
                    case "missileDistance": buffType = BuffType.missileDistance_unique; return true;
                    case "hp": buffType = BuffType.hp_unique; return true;
                    case "hpPercent": buffType = BuffType.hpPer_unique; return true;
                }
            }

            buffType = default;
            return false;
        }

        public static string ResolveAlias(BonusRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.alias))
                return row.alias.Trim();

            return ResolveDisplayName(row);
        }

        public static string ResolveDisplayName(BonusRow row)
        {
            return !string.IsNullOrWhiteSpace(row.displayName)
                ? row.displayName.Trim()
                : (row.stat ?? string.Empty).Trim();
        }

        public static string FormatDisplayValue(
            float value,
            BonusValueType valueType)
        {
            string suffix = valueType == BonusValueType.Percent ? "%" : string.Empty;
            return $"+{Mathf.RoundToInt(value)}{suffix}";
        }

        public static string ResolveDisplayStatKey(string stat)
        {
            return stat switch
            {
                "attPercent" => "att",
                "hpPercent" => "hp",
                _ => stat ?? string.Empty
            };
        }

        public static string ResolveIconResourceName(BuffType type)
        {
            return ResolveDisplayStatKey(ResolveStatKey(type)) switch
            {
                "att" => "WallBonus_Attack",
                "attackSpeed" => "WallBonus_AttackSpeed",
                "missileDistance" => "WallBonus_MissileDuration",
                "hp" => "WallBonus_Health",
                "tungtungAdd" => "WallBonus_Tungtung",
                "boombarAdd" => "WallBonus_Boombar",
                "missileAdd" => "WallBonus_MissileAdd",
                _ => null
            };
        }

        public static string ResolveLocalizationKey(BuffType type)
        {
            string displayStat = ResolveDisplayStatKey(ResolveStatKey(type));
            return displayStat == "attackSpeed" ? "missileSpeed" : displayStat;
        }

        public static float ResolveValue(BonusRow row, float random01, float baseValue)
        {
            float value = InterpolateRange(row, random01);
            if (row.valueType == BonusValueType.Ratio)
                value *= baseValue;

            return ShouldRound(row.stat) ? Mathf.Round(value) : value;
        }

        public static float ResolveDisplayValue(BonusRow row, float random01)
        {
            float value = InterpolateRange(row, random01);
            if (row.valueType == BonusValueType.Ratio)
                value *= 100f;

            return ShouldRound(row.stat) ? Mathf.Round(value) : value;
        }

        public static bool AreNearby(
            Vector3 first,
            Vector3 second,
            float nearbyDistance)
        {
            Vector2 firstPlanar = new(first.x, first.z);
            Vector2 secondPlanar = new(second.x, second.z);
            return Vector2.SqrMagnitude(firstPlanar - secondPlanar) <=
                   nearbyDistance * nearbyDistance;
        }

        private static bool ShouldRound(string stat)
        {
            return stat is "att" or "hp" or "missileAdd" or
                "tungtungAdd" or "boombarAdd";
        }

        private static float InterpolateRange(BonusRow row, float random01)
        {
            float min = Mathf.Min(row.min, row.max);
            float max = Mathf.Max(row.min, row.max);
            return Mathf.Lerp(min, max, Mathf.Clamp01(random01));
        }

        private static string ResolveStatKey(BuffType type)
        {
            return type switch
            {
                BuffType.att_normmal or BuffType.att_unique => "att",
                BuffType.attPer_normal or BuffType.attPer_unique => "attPercent",
                BuffType.attackSpeed_normal or BuffType.attackSpeed_unique or
                    BuffType.FireRateIncrease => "attackSpeed",
                BuffType.missileDistance_normal or BuffType.missileDistance_unique =>
                    "missileDistance",
                BuffType.hp_normal or BuffType.hp_unique or BuffType.HealthBoost => "hp",
                BuffType.hpPer_normal or BuffType.hpPer_unique => "hpPercent",
                BuffType.missileAdd_unique => "missileAdd",
                BuffType.tungtung_rare or BuffType.ExtraHelp => "tungtungAdd",
                BuffType.boombar_rare => "boombarAdd",
                _ => "att"
            };
        }

    }
}
