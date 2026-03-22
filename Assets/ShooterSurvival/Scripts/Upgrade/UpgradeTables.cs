using System.Collections.Generic;
using UnityEngine;

public static class UpgradeTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "\uC5C5\uADF8\uB808\uC774\uB4DC";

    private static Dictionary<int, Dictionary<int, UpgradeRow>> _map;

    private static void EnsureInit()
    {
        if (_map != null) return;

        var rows = TableCache.Load(FileName, SheetName, new UpgradeRowParser(),
            dbg: new ExcelSheetLoader.DebugOptions
            {
                logSheetNames = false,
                logHeaderRow = true,
                logFirstDataRows = true,
                firstDataRowsCount = 3
            });

        _map = new Dictionary<int, Dictionary<int, UpgradeRow>>();

        foreach (var r in rows)
        {
            if (!_map.TryGetValue(r.id, out var lvMap))
                _map[r.id] = lvMap = new Dictionary<int, UpgradeRow>();

            lvMap[r.level] = r;
        }

        Debug.Log($"[UpgradeTables] Ready. rows={rows.Count}");
    }

    public static UpgradeRow Get(int id, int level)
    {
        EnsureInit();
        return _map[id][level];
    }

    public static bool TryGet(int id, int level, out UpgradeRow row)
    {
        EnsureInit();
        row = default;

        if (!_map.TryGetValue(id, out var lvMap)) return false;
        return lvMap.TryGetValue(level, out row);
    }

    public static void Reload()
    {
        _map = null;
        TableCache.Clear(FileName, SheetName);
        EnsureInit();
    }
}
