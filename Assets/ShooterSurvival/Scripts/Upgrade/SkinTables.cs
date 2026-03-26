using System.Collections.Generic;
using UnityEngine;

public static class SkinTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "\uC2A4\uD0A8";

    private static Dictionary<int, SkinRow> _map;
    private static Dictionary<string, SkinRow> _itemMap;

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
        _itemMap = new Dictionary<string, SkinRow>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            _map[r.id] = r;
            if (!string.IsNullOrWhiteSpace(r.item))
                _itemMap[r.item.Trim()] = r;
        }

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

    public static bool TryGetByItem(string item, out SkinRow row)
    {
        EnsureInit();

        if (string.IsNullOrWhiteSpace(item))
        {
            row = default;
            return false;
        }

        return _itemMap.TryGetValue(item.Trim(), out row);
    }

    public static void Reload()
    {
        _map = null;
        _itemMap = null;
        TableCache.Clear(FileName, SheetName);
        EnsureInit();
    }
}
