using System.Collections.Generic;
using UnityEngine;

public static class SkinTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "\uC2A4\uD0A8";

    private static Dictionary<int, SkinRow> _map;

    private static void EnsureInit()
    {
        if (_map != null) return;

        var rows = TableCache.Load(FileName, SheetName, new SkinRowParser(),
            dbg: new ExcelSheetLoader.DebugOptions
            {
                logSheetNames = false,
                logHeaderRow = true,
                logFirstDataRows = true,
                firstDataRowsCount = 3
            });

        _map = new Dictionary<int, SkinRow>();
        foreach (var r in rows)
            _map[r.id] = r;

        Debug.Log($"[SkinTables] Ready. rows={rows.Count}");
    }

    public static SkinRow Get(int id)
    {
        EnsureInit();
        return _map[id];
    }

    public static bool TryGet(int id, out SkinRow row)
    {
        EnsureInit();
        return _map.TryGetValue(id, out row);
    }

    public static void Reload()
    {
        _map = null;
        TableCache.Clear(FileName, SheetName);
        EnsureInit();
    }
}
