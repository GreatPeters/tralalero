using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class StartupPrefs
{
    [Serializable] public sealed class Entry { public string key, kind, text; public bool exists; public float number; }
    [Serializable] public sealed class Snapshot { public List<Entry> entries = new List<Entry>(); }
    private const string FilePath = "map-concepts/startup-performance-2026-09-12/prefs-before.tsv";
    public static object Begin()
    {
        
        if (File.Exists(FilePath)) throw new InvalidOperationException("UI preference snapshot already exists");
        var result = new Snapshot();
        void Add(string key, string kind) => result.entries.Add(new Entry { key = key, kind = kind, exists = PlayerPrefs.HasKey(key),
            text = kind == "string" ? PlayerPrefs.GetString(key) : "", number = kind == "float" ? PlayerPrefs.GetFloat(key) : kind == "int" ? PlayerPrefs.GetInt(key) : 0 });
        Add("coin", "int"); Add("jewel", "int");
        for (int i = 1; i <= 9; i++) Add("upgrade_lv_" + i, "int");
        foreach (var type in Enum.GetNames(typeof(UpgradeStatManager.UpgradeType))) { Add("upgrade_stat_" + type, "float"); Add("upgrade_stat_type_" + type, "int"); }
        foreach (var row in CosmeticTables.Rows) Add(CosmeticService.OwnedKey(row.id), "int");
        foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot))) Add(CosmeticService.EquippedKey(slot), "string");
        string[] lines = result.entries.Select(e => string.Join("\t", e.key, e.kind, e.exists.ToString(), e.number.ToString("R", CultureInfo.InvariantCulture), Convert.ToBase64String(Encoding.UTF8.GetBytes(e.text)))).ToArray();
        File.WriteAllLines(FilePath, lines); return new { saved = result.entries.Count, file = FilePath };
    }
    public static object Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before restoring test purchases");
        var snapshot = new Snapshot();
        foreach(string line in File.ReadAllLines(FilePath)) {var parts=line.Split('\t');snapshot.entries.Add(new Entry {key=parts[0],kind=parts[1],exists=bool.Parse(parts[2]),number=float.Parse(parts[3],CultureInfo.InvariantCulture),text=Encoding.UTF8.GetString(Convert.FromBase64String(parts[4]))});}
        if (snapshot.entries.Count < 20) throw new InvalidOperationException("Incomplete preference snapshot; do not report restoration");
        foreach (var row in CosmeticTables.Rows)
            if (!snapshot.entries.Any(e => e.key == CosmeticService.OwnedKey(row.id)))
                PlayerPrefs.DeleteKey(CosmeticService.OwnedKey(row.id));
        foreach (var e in snapshot.entries)
        {
            if (!e.exists) PlayerPrefs.DeleteKey(e.key);
            else if (e.kind == "int") PlayerPrefs.SetInt(e.key, (int)e.number);
            else if (e.kind == "float") PlayerPrefs.SetFloat(e.key, e.number);
            else PlayerPrefs.SetString(e.key, e.text);
        }
        PlayerPrefs.Save(); CosmeticService.ReloadCatalog();
        return new { restored = snapshot.entries.Count, coin = PlayerPrefs.GetInt("coin"), jewel = PlayerPrefs.GetInt("jewel") };
    }
}

