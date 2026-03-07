using System;
using System.Collections.Generic;

public static class TableCache
{
    // key = file::sheet::type
    private static readonly Dictionary<string, object> _cache = new();

    public static List<T> Load<T>(string fileName, string sheetName, ITableParser<T> parser, ExcelSheetLoader.DebugOptions dbg = null)
    {
        var key = $"{fileName}::{sheetName}::{typeof(T).FullName}";
        if (_cache.TryGetValue(key, out var boxed))
            return (List<T>)boxed;

        var list = ExcelSheetLoader.LoadBySheetName(fileName, sheetName, parser, dbg);
        _cache[key] = list;
        return list;
    }

    public static void Clear(string fileName = null, string sheetName = null)
    {
        if (fileName == null) { _cache.Clear(); return; }

        var prefix = sheetName == null
            ? $"{fileName}::"
            : $"{fileName}::{sheetName}::";

        var remove = new List<string>();
        foreach (var k in _cache.Keys)
            if (k.StartsWith(prefix, StringComparison.Ordinal))
                remove.Add(k);

        foreach (var k in remove)
            _cache.Remove(k);
    }
}
