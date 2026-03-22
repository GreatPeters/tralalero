using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ExcelDataReader;

public interface ITableParser<T>
{
    // headerMap??湲곕컲?쇰줈 ???뚯떛
    // (?녿뒗 而щ읆? ?뚯꽌媛 Optional 泥섎━)
    T ParseRow(IExcelDataReader reader, Dictionary<string, int> headerMap);
    bool IsValidRow(T row); // 鍮????섎せ?????ㅽ궢??
}

public static class ExcelSheetLoader
{
    public class DebugOptions
    {
        public bool logSheetNames = true;
        public bool logHeaderRow = true;
        public bool logFirstDataRows = true;
        public int firstDataRowsCount = 5;
    }

    public static List<T> LoadBySheetName<T>(
        string fileName,
        string sheetName,
        ITableParser<T> parser,
        DebugOptions dbg = null)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var path = Path.Combine(Application.streamingAssetsPath, fileName);

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        if (dbg != null && dbg.logSheetNames)
        {
            var names = GetSheetNames(path);
            Debug.Log($"[ExcelSheetLoader] Sheets in {fileName}: " + string.Join(", ", names));
        }

        MoveToSheetByName(reader, sheetName);

        int colCount = reader.FieldCount;

        if (!TryMoveToHeaderRow(reader, colCount, out var headerRow))
            throw new Exception($"?묒? ?쒗듃 '{sheetName}'?먯꽌 ?ㅻ뜑 ?됱쓣 李얠? 紐삵뻽?댁슂. (?ㅻ뜑??'?앸퀎 ?쒕쾲' ?꾩슂)");

        var headerMap = BuildHeaderMap(headerRow);

        if (dbg != null && dbg.logHeaderRow)
            Debug.Log($"[ExcelSheetLoader] ({sheetName}) Header: " + RowToDebugString(headerRow));

        var list = new List<T>();
        int logged = 0;

        while (reader.Read())
        {
            if (IsAllEmpty(reader, colCount)) continue;

            var row = parser.ParseRow(reader, headerMap);
            if (!parser.IsValidRow(row)) continue;

            list.Add(row);

            if (dbg != null && dbg.logFirstDataRows && logged < dbg.firstDataRowsCount)
            {
                logged++;
                Debug.Log($"[ExcelSheetLoader] ({sheetName}) Row#{logged}: {row}");
            }
        }

        Debug.Log($"[ExcelSheetLoader] Loaded {list.Count} rows from '{fileName}' / sheet '{sheetName}'. ({typeof(T).Name})");
        return list;
    }

    // ---- helpers ----
    public static List<string> GetSheetNames(string fullPath)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var names = new List<string>();
        do { names.Add(reader.Name); } while (reader.NextResult());
        return names;
    }

    static void MoveToSheetByName(IExcelDataReader reader, string sheetName)
    {
        var names = new List<string>();

        do
        {
            names.Add(reader.Name);

            if (string.Equals(reader.Name?.Trim(), sheetName?.Trim(), StringComparison.OrdinalIgnoreCase))
                return;

        } while (reader.NextResult());

        throw new Exception($"?쒗듃 ?대쫫 '{sheetName}'??李얠? 紐삵뻽?댁슂. ?ㅼ젣 ?쒗듃: {string.Join(", ", names)}");
    }

    static bool TryMoveToHeaderRow(IExcelDataReader reader, int colCount, out object[] headerRowValues)
    {
        headerRowValues = null;

        while (reader.Read())
        {
            if (IsAllEmpty(reader, colCount)) continue;

            var values = new object[colCount];
            for (int c = 0; c < colCount; c++)
                values[c] = reader.GetValue(c);

            for (int c = 0; c < colCount; c++)
            {
                var s = (values[c]?.ToString() ?? "");
                var normalized = ExcelUtil.NormalizeKey(s);
                if (normalized == ExcelUtil.NormalizeKey("\uC21C\uBC88")
                    || normalized == ExcelUtil.NormalizeKey("\uC2DD\uBCC4\uC21C\uBC88"))
                {
                    headerRowValues = values;
                    return true;
                }
            }
        }

        return false;
    }

    static Dictionary<string, int> BuildHeaderMap(object[] headerRowValues)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int c = 0; c < headerRowValues.Length; c++)
        {
            var raw = headerRowValues[c]?.ToString() ?? "";
            var header = raw.Trim();
            if (string.IsNullOrEmpty(header)) continue;

            if (!map.ContainsKey(header)) map.Add(header, c);

            var norm = ExcelUtil.NormalizeKey(header);
            if (!map.ContainsKey(norm)) map.Add(norm, c);
        }

        return map;
    }

    static bool IsAllEmpty(IExcelDataReader reader, int colCount)
    {
        for (int c = 0; c < colCount; c++)
            if (!string.IsNullOrEmpty(reader.GetValue(c)?.ToString()))
                return false;
        return true;
    }

    static string RowToDebugString(object[] values)
    {
        var parts = new List<string>();
        for (int i = 0; i < values.Length; i++)
            parts.Add(values[i]?.ToString() ?? "");
        return string.Join(" | ", parts);
    }
}

