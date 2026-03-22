using System;
using System.Collections.Generic;
using UnityEngine;

public static class MonsterTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "\uBAAC\uC2A4\uD130";

    private static Dictionary<string, MonsterRow> _map;
    private static List<MonsterRow> _allRows;

    private static void EnsureInit()
    {
        if (_map != null) return;

        var rows = TableCache.Load(FileName, SheetName, new MonsterRowParser(),
            dbg: new ExcelSheetLoader.DebugOptions
            {
                logSheetNames = false,
                logHeaderRow = true,
                logFirstDataRows = true,
                firstDataRowsCount = 3
            });

        _map = new Dictionary<string, MonsterRow>(StringComparer.OrdinalIgnoreCase);
        _allRows = rows;

        foreach (var r in rows)
            _map[MakeKey(r.chapter, r.stage, r.tier)] = r;

        Debug.Log($"[MonsterTables] Ready. rows={rows.Count}");
    }

    public static bool TryGet(int chapter, int stage, EnemyTier tier, out MonsterRow row)
    {
        EnsureInit();
        return _map.TryGetValue(MakeKey(chapter, stage, tier), out row);
    }

    public static List<MonsterRow> GetAll()
    {
        EnsureInit();
        return _allRows;
    }

    public static void Reload()
    {
        _map = null;
        _allRows = null;
        TableCache.Clear(FileName, SheetName);
        EnsureInit();
    }

    private static string MakeKey(int chapter, int stage, EnemyTier tier)
        => $"{chapter}:{stage}:{tier}";
}

