using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival.Account
{
    [Serializable]
    public sealed class ProgressEntry
    {
        public string key;
        public string kind;
        public string value;
    }

    [Serializable]
    public sealed class GameProgressSnapshot
    {
        public const int CurrentVersion = 1;
        public const int MaxBytes = 128 * 1024;
        public int version = CurrentVersion;
        public string playerId;
        public long updatedUtc;
        public ProgressEntry[] entries = Array.Empty<ProgressEntry>();

        public string Fingerprint => Hash(JsonUtility.ToJson(new EntryList { entries = entries.OrderBy(e => e.key, StringComparer.Ordinal).ToArray() }));
        [Serializable] private sealed class EntryList { public ProgressEntry[] entries; }
        public static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant();
        }

        public byte[] Encode() => Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));

        public static bool TryDecode(byte[] bytes, string expectedPlayer, IReadOnlyDictionary<string, string> keys,
            out GameProgressSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = "저장 데이터 형식을 확인할 수 없습니다.";
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxBytes) return false;
            try { snapshot = JsonUtility.FromJson<GameProgressSnapshot>(Encoding.UTF8.GetString(bytes)); }
            catch (Exception) { return false; }
            return Validate(snapshot, expectedPlayer, keys, out error);
        }

        public static bool Validate(GameProgressSnapshot snapshot, string expectedPlayer,
            IReadOnlyDictionary<string, string> keys, out string error)
        {
            error = "저장 데이터 형식을 확인할 수 없습니다.";
            if (snapshot == null || snapshot.version != CurrentVersion || snapshot.entries == null || snapshot.entries.Length > keys.Count) return false;
            if (!string.Equals(snapshot.playerId, expectedPlayer, StringComparison.Ordinal)) { error = "다른 계정의 저장 데이터입니다."; return false; }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in snapshot.entries)
            {
                if (entry == null || entry.key == null || !seen.Add(entry.key) || !keys.TryGetValue(entry.key, out var kind) || kind != entry.kind || entry.value == null) return false;
                if (kind == "int" && (!int.TryParse(entry.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < 0)) return false;
                if (kind == "float" && (!float.TryParse(entry.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) || float.IsNaN(f) || float.IsInfinity(f) || f < 0)) return false;
                if (entry.key.StartsWith(ChapterRunProgress.BestKeyPrefix, StringComparison.Ordinal) && float.Parse(entry.value, CultureInfo.InvariantCulture) > 1f) return false;
                if (kind == "string" && entry.value.Length > 256) return false;
                if ((entry.key == "chapter_unlocked" || entry.key == "chapter_last_played") && int.Parse(entry.value, CultureInfo.InvariantCulture) is < 1 or > 5) return false;
                if (entry.key.StartsWith("chapter_workshop_level_", StringComparison.Ordinal) && int.Parse(entry.value, CultureInfo.InvariantCulture) > 5) return false;
            }
            error = null;
            return true;
        }

        public int Int(string key, int fallback = 0) => int.TryParse(entries.FirstOrDefault(e => e.key == key)?.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : fallback;
        public bool HasProgress => entries.Any(e => e.kind == "int" && e.key != "chapter_unlocked" && e.key != "TutorialDone" && Int(e.key) > 0) || Int("chapter_unlocked", 1) > 1;
        public string Summary => $"챕터 {Int("chapter_unlocked", 1)} / 코인 {Int("coin"):N0} / 보석 {Int("jewel"):N0}";
    }

    public static class GameProgressStore
    {
        public static Dictionary<string, string> Keys()
        {
            var keys = new Dictionary<string, string>(StringComparer.Ordinal) {
                ["coin"] = "int", ["jewel"] = "int", ["chapter_unlocked"] = "int", ["chapter_last_played"] = "int", ["TutorialDone"] = "int", ["ads_last_reward_round"] = "string"
            };
            for (int chapter = 1; chapter <= 5; chapter++)
            {
                keys["chapter_rewarded_" + chapter] = "int";
                keys[ChapterRunProgress.BestKey(chapter)] = "float";
                keys[ChapterUpgradeService.LevelKey(chapter)] = "int";
                keys[ChapterUpgradeService.OwnedKey(chapter)] = "int";
            }
            foreach (int id in UpgradeTables.Ids) keys["upgrade_lv_" + id] = "int";
            foreach (var type in Enum.GetNames(typeof(UpgradeStatManager.UpgradeType)))
            {
                keys["upgrade_stat_" + type] = "float";
                keys["upgrade_stat_type_" + type] = "int";
            }
            foreach (var row in CosmeticTables.Rows) keys[CosmeticService.OwnedKey(row.id)] = "int";
            foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot))) keys[CosmeticService.EquippedKey(slot)] = "string";
            return keys;
        }

        public static GameProgressSnapshot Capture(string playerId)
        {
            var entries = new List<ProgressEntry>();
            foreach (var pair in Keys().OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (!PlayerPrefs.HasKey(pair.Key)) continue;
                string value = pair.Value == "int" ? PlayerPrefs.GetInt(pair.Key).ToString(CultureInfo.InvariantCulture)
                    : pair.Value == "float" ? PlayerPrefs.GetFloat(pair.Key).ToString("R", CultureInfo.InvariantCulture) : PlayerPrefs.GetString(pair.Key);
                entries.Add(new ProgressEntry { key = pair.Key, kind = pair.Value, value = value });
            }
            return new GameProgressSnapshot { playerId = playerId, updatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), entries = entries.ToArray() };
        }

        public static void Apply(GameProgressSnapshot snapshot, string expectedPlayer)
        {
            var keys = Keys();
            if (!GameProgressSnapshot.Validate(snapshot, expectedPlayer, keys, out var error)) throw new InvalidOperationException(error);
            // Validate the entire payload before touching any live preference.
            foreach (var key in keys.Keys) PlayerPrefs.DeleteKey(key);
            foreach (var entry in snapshot.entries)
            {
                if (entry.kind == "int") PlayerPrefs.SetInt(entry.key, int.Parse(entry.value, CultureInfo.InvariantCulture));
                else if (entry.kind == "float") PlayerPrefs.SetFloat(entry.key, float.Parse(entry.value, CultureInfo.InvariantCulture));
                else PlayerPrefs.SetString(entry.key, entry.value);
            }
            PlayerPrefs.Save();
        }
    }
}
