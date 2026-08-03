using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival.Analytics
{
    public readonly struct UpgradeAnalyticsSnapshot
    {
        private const int MaxAnalyticsStringLength = 100;

        private static readonly UpgradeStatManager.UpgradeType[] UpgradeTypes =
        {
            UpgradeStatManager.UpgradeType.ATT,
            UpgradeStatManager.UpgradeType.HP,
            UpgradeStatManager.UpgradeType.ATT_SPEED,
            UpgradeStatManager.UpgradeType.PROJECTILE_SPEED,
            UpgradeStatManager.UpgradeType.BOSS_DAMAGE,
            UpgradeStatManager.UpgradeType.COIN_BONUS,
            UpgradeStatManager.UpgradeType.HP_REGEN,
            UpgradeStatManager.UpgradeType.TUNGTUNGTUNG,
            UpgradeStatManager.UpgradeType.BOOMBAR
        };

        private static readonly string[] UpgradeCodes =
        {
            "att", "hp", "as", "ps", "bd", "cb", "hr", "tt", "bb"
        };

        public UpgradeAnalyticsSnapshot(
            string levels,
            string flatValues,
            string percentValues)
        {
            Levels = Limit(levels);
            FlatValues = Limit(flatValues);
            PercentValues = Limit(percentValues);
        }

        public string Levels { get; }
        public string FlatValues { get; }
        public string PercentValues { get; }

        public static UpgradeAnalyticsSnapshot Capture()
        {
            return CaptureCore(true);
        }

        public static UpgradeAnalyticsSnapshot CaptureFromSavedValues()
        {
            return CaptureCore(false);
        }

        private static UpgradeAnalyticsSnapshot CaptureCore(
            bool preferLiveManager)
        {
            var levels = new StringBuilder();
            var flatValues = new StringBuilder();
            var percentValues = new StringBuilder();

            for (int i = 0; i < UpgradeTypes.Length; i++)
            {
                if (i > 0)
                {
                    levels.Append(',');
                    flatValues.Append(',');
                    percentValues.Append(',');
                }

                UpgradeStatManager.UpgradeType type = UpgradeTypes[i];
                string code = UpgradeCodes[i];
                int level = Mathf.Max(
                    0,
                    PlayerPrefs.GetInt($"upgrade_lv_{i + 1}", 0));

                float flat = ReadFlatValue(type, preferLiveManager);
                float percent = ReadPercentValue(type, preferLiveManager);

                levels.Append(code).Append(':').Append(level);
                flatValues.Append(code).Append(':').Append(FormatNumber(flat));
                percentValues.Append(code).Append(':').Append(FormatNumber(percent));
            }

            return new UpgradeAnalyticsSnapshot(
                levels.ToString(),
                flatValues.ToString(),
                percentValues.ToString());
        }

        public static string Limit(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= MaxAnalyticsStringLength
                ? value
                : value.Substring(0, MaxAnalyticsStringLength);
        }

        private static float ReadFlatValue(
            UpgradeStatManager.UpgradeType type,
            bool preferLiveManager)
        {
            if (preferLiveManager && UpgradeStatManager.S != null)
                return UpgradeStatManager.S.GetFlatStat(type);

            return ReadSavedValue(type, global::ValueType.Value);
        }

        private static float ReadPercentValue(
            UpgradeStatManager.UpgradeType type,
            bool preferLiveManager)
        {
            if (preferLiveManager && UpgradeStatManager.S != null)
                return UpgradeStatManager.S.GetPercentStat(type);

            return ReadSavedValue(type, global::ValueType.Percent);
        }

        private static float ReadSavedValue(
            UpgradeStatManager.UpgradeType type,
            global::ValueType expectedType)
        {
            int savedType = PlayerPrefs.GetInt(
                $"upgrade_stat_type_{type}",
                (int)global::ValueType.Value);
            if (savedType != (int)expectedType)
                return 0f;

            return PlayerPrefs.GetFloat($"upgrade_stat_{type}", 0f);
        }

        private static string FormatNumber(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return "0";

            float absolute = Mathf.Abs(value);
            string format =
                absolute >= 100000f
                    ? "0.###E+0"
                    : "0.###";
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
