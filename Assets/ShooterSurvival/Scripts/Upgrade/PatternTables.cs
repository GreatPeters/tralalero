using System.Collections.Generic;
using UnityEngine;

public static class PatternTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "\uD328\uD134";

    private static List<PatternSheetRow> _allRows;

    private static void EnsureInit()
    {
        if (_allRows != null) return;

        _allRows = TableCache.Load(FileName, SheetName, new PatternSheetRowParser(),
            dbg: new ExcelSheetLoader.DebugOptions
            {
                logSheetNames = false,
                logHeaderRow = true,
                logFirstDataRows = true,
                firstDataRowsCount = 3
            });

        Debug.Log($"[PatternTables] Ready. rows={_allRows.Count}");
    }

    public static List<PatternSheetRow> GetAll()
    {
        EnsureInit();
        return _allRows;
    }

    public static void Reload()
    {
        _allRows = null;
        TableCache.Clear(FileName, SheetName);
        EnsureInit();
    }
}
