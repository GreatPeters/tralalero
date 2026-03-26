using System;
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;
using UnityEngine;

public static class EnvironmentVariableTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "\uD658\uACBD \uBCC0\uC218";
    private const string HeaderVariableName = "\uBCC0\uC218\uBA85";
    private const string HeaderVariableType = "\uBCC0\uC218 \uD0C0\uC785";
    private const string HeaderValue1 = "\uAC121";
    private const string HeaderValue2 = "\uAC122";
    private const string HeaderValue3 = "\uAC123";

    public struct Float3
    {
        public float value1;
        public float value2;
        public float value3;
    }

    private static Dictionary<string, Float3> _float3Map;

    public static bool TryGetFloat3(string variableName, out Float3 value)
    {
        EnsureInit();
        return _float3Map.TryGetValue((variableName ?? string.Empty).Trim(), out value);
    }

    public static bool TryGetFloat(string variableName, out float value)
    {
        value = 0f;

        if (!TryGetFloat3(variableName, out var raw))
            return false;

        value = raw.value1;
        return true;
    }

    public static void Reload()
    {
        _float3Map = null;
        EnsureInit();
    }

    private static void EnsureInit()
    {
        if (_float3Map != null)
            return;

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var path = Path.Combine(Application.streamingAssetsPath, FileName);
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        MoveToSheetByName(reader, SheetName);

        int colCount = reader.FieldCount;
        if (!TryMoveToHeaderRow(reader, colCount, out var headerMap))
            throw new Exception($"[{nameof(EnvironmentVariableTables)}] Header row not found in sheet '{SheetName}'.");

        int idxVariableName = ExcelUtil.GetIdx(headerMap, HeaderVariableName);
        int idxVariableType = ExcelUtil.GetIdx(headerMap, HeaderVariableType);
        int idxValue1 = ExcelUtil.GetIdx(headerMap, HeaderValue1);
        int idxValue2 = ExcelUtil.GetIdx(headerMap, HeaderValue2);
        int idxValue3 = ExcelUtil.GetIdx(headerMap, HeaderValue3);

        _float3Map = new Dictionary<string, Float3>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            if (IsAllEmpty(reader, colCount))
                continue;

            string name = (reader.GetValue(idxVariableName)?.ToString() ?? string.Empty).Trim();
            string type = (reader.GetValue(idxVariableType)?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name) || !string.Equals(type, "float", StringComparison.OrdinalIgnoreCase))
                continue;

            _float3Map[name] = new Float3
            {
                value1 = ExcelUtil.ToFloat(reader.GetValue(idxValue1)),
                value2 = ExcelUtil.ToFloat(reader.GetValue(idxValue2)),
                value3 = ExcelUtil.ToFloat(reader.GetValue(idxValue3))
            };
        }

        Debug.Log($"[EnvironmentVariableTables] Ready. rows={_float3Map.Count}");
    }

    private static void MoveToSheetByName(IExcelDataReader reader, string sheetName)
    {
        do
        {
            if (string.Equals(reader.Name?.Trim(), sheetName?.Trim(), StringComparison.OrdinalIgnoreCase))
                return;
        }
        while (reader.NextResult());

        throw new Exception($"[{nameof(EnvironmentVariableTables)}] Sheet '{sheetName}' not found.");
    }

    private static bool TryMoveToHeaderRow(IExcelDataReader reader, int colCount, out Dictionary<string, int> headerMap)
    {
        headerMap = null;

        while (reader.Read())
        {
            if (IsAllEmpty(reader, colCount))
                continue;

            var values = new object[colCount];
            for (int i = 0; i < colCount; i++)
                values[i] = reader.GetValue(i);

            for (int i = 0; i < colCount; i++)
            {
                var raw = values[i]?.ToString() ?? string.Empty;
                if (ExcelUtil.NormalizeKey(raw) != ExcelUtil.NormalizeKey(HeaderVariableName))
                    continue;

                headerMap = BuildHeaderMap(values);
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, int> BuildHeaderMap(object[] headerRowValues)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerRowValues.Length; i++)
        {
            string header = (headerRowValues[i]?.ToString() ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(header))
                continue;

            if (!map.ContainsKey(header))
                map.Add(header, i);

            var normalized = ExcelUtil.NormalizeKey(header);
            if (!map.ContainsKey(normalized))
                map.Add(normalized, i);
        }

        return map;
    }

    private static bool IsAllEmpty(IExcelDataReader reader, int colCount)
    {
        for (int i = 0; i < colCount; i++)
        {
            if (!string.IsNullOrEmpty(reader.GetValue(i)?.ToString()))
                return false;
        }

        return true;
    }
}
