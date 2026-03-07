using System;
using System.Collections.Generic;
using UnityEngine;

public static class BonusTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "보너스";

    private static Dictionary<string, BonusRow> _map;

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

        foreach (var r in rows)
        {
            var key = MakeKey(r.rarity, r.stat);
            _map[key] = r;
        }

        Debug.Log($"[BonusTables] Ready. rows={rows.Count}");
    }

    public static bool TryGet(string rarity, string stat, out BonusRow row)
    {
        EnsureInit();
        return _map.TryGetValue(MakeKey(rarity, stat), out row);
    }

    public static void Reload()
    {
        _map = null;
        TableCache.Clear(FileName, SheetName);
        EnsureInit();
    }

    private static string MakeKey(string rarity, string stat)
    {
        var r = (rarity ?? "").Trim().ToUpperInvariant();
        var s = (stat ?? "").Trim().ToUpperInvariant();
        return $"{r}:{s}";
    }
}
