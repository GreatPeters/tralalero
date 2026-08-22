using System;
using System.Collections.Generic;
using UnityEngine;

public static class BonusTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "\uBCF4\uB108\uC2A4";

    private static Dictionary<string, BonusRow> _map;
    private static Dictionary<string, List<BonusRow>> _rowsByRarity;

    private static void EnsureInit()
    {
        if (_map != null) return;

        var rows = TableCache.Load(FileName, SheetName, new BonusRowParser(),
            dbg: new ExcelSheetLoader.DebugOptions
            {
                logSheetNames = false,
                logHeaderRow = true,
                logFirstDataRows = true,
                firstDataRowsCount = 3
            });

        _map = new Dictionary<string, BonusRow>(StringComparer.OrdinalIgnoreCase);
        _rowsByRarity = new Dictionary<string, List<BonusRow>>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in rows)
        {
            var key = MakeKey(r.rarity, r.stat);
            _map[key] = r;

            string rarityKey = NormalizeRarity(r.rarity);
            if (!_rowsByRarity.TryGetValue(rarityKey, out List<BonusRow> rarityRows))
            {
                rarityRows = new List<BonusRow>();
                _rowsByRarity.Add(rarityKey, rarityRows);
            }

            rarityRows.Add(r);
        }

        Debug.Log($"[BonusTables] Ready. rows={rows.Count}");
    }

    public static bool TryGet(string rarity, string stat, out BonusRow row)
    {
        EnsureInit();
        return _map.TryGetValue(MakeKey(rarity, stat), out row);
    }

    public static IReadOnlyList<BonusRow> GetAll(string rarity)
    {
        EnsureInit();
        return _rowsByRarity.TryGetValue(NormalizeRarity(rarity), out List<BonusRow> rows)
            ? rows
            : Array.Empty<BonusRow>();
    }

    public static BonusRow ResolveDisplayRow(BonusRow row)
    {
        string displayStat =
            IndianOceanAssets.ShooterSurvival.BonusAltarRules.ResolveDisplayStatKey(
                row.stat);
        return !string.Equals(
                   displayStat,
                   row.stat,
                   StringComparison.OrdinalIgnoreCase) &&
               TryGet(row.rarity, displayStat, out BonusRow displayRow)
            ? displayRow
            : row;
    }

    public static void Reload()
    {
        _map = null;
        _rowsByRarity = null;
        TableCache.Clear(FileName, SheetName);
        EnsureInit();
    }

    private static string MakeKey(string rarity, string stat)
    {
        var r = NormalizeRarity(rarity);
        var s = (stat ?? "").Trim().ToUpperInvariant();
        return $"{r}:{s}";
    }

    private static string NormalizeRarity(string rarity)
    {
        string normalized = (rarity ?? "").Trim().ToUpperInvariant();
        return normalized == "ELITE" ? "RARE" : normalized;
    }
}
