using System;
using System.Collections.Generic;
using UnityEngine;
using ExcelDataReader;

public static class ExcelReader
{
    public class DebugOptions
    {
        public bool logSheetNames = true;
        public bool logHeaderRow = true;
        public bool logFirstDataRows = true;
        public int firstDataRowsCount = 5;
    }

    // 시트 이름으로 읽기
    public static List<UpgradeRow> LoadBySheetName(
        string fileName,
        string sheetName,
        DebugOptions dbg = null)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = GameDataWorkbook.OpenRead(fileName);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        if (dbg != null && dbg.logSheetNames)
        {
            var names = GetSheetNames(fileName);
            Debug.Log($"[ExcelReader] Sheets in {fileName}: " + string.Join(", ", names));
        }

        MoveToSheetByName(reader, sheetName);

        int colCount = reader.FieldCount;

        if (!TryMoveToHeaderRow(reader, colCount, out var headerRowValues))
            throw new Exception($"엑셀 시트 '{sheetName}'에서 헤더 행을 찾지 못했어요. (헤더에 '식별 순번' 필요)");

        var headerToIndex = BuildHeaderMap(headerRowValues);

        if (dbg != null && dbg.logHeaderRow)
            Debug.Log($"[ExcelReader] ({sheetName}) Header: " + RowToDebugString(headerRowValues));

        int idxId        = GetIdx(headerToIndex, "식별 순번");
        int idxEnum      = GetIdxOptional(headerToIndex, "식별 Enum");   // 추가
        int idxLv        = GetIdx(headerToIndex, "레벨");
        int idxItem      = GetIdx(headerToIndex, "항목");
        int idxAmount    = GetIdx(headerToIndex, "수치");
        int idxValType   = GetIdx(headerToIndex, "수치 타입");
        int idxPriceType = GetIdx(headerToIndex, "가격 타입");
        int idxPrice     = GetIdx(headerToIndex, "가격 수치");
        int idxNote      = GetIdxOptional(headerToIndex, "기타 설명");

        var list = new List<UpgradeRow>();
        int logged = 0;

        while (reader.Read())
        {
            if (IsAllEmpty(reader, colCount)) continue;

            var rawId = reader.GetValue(idxId);
            var rawLv = reader.GetValue(idxLv);
            if (rawId == null || rawLv == null) continue;

            int id = ToInt(rawId);
            int lv = ToInt(rawLv);

            string enumKey = idxEnum >= 0 ? (reader.GetValue(idxEnum)?.ToString() ?? "").Trim() : "";
            var type = ParseTypeFallback(enumKey, id);

            var row = new UpgradeRow
            {
                id = id,
                level = lv,
                type = type,
                item = (reader.GetValue(idxItem)?.ToString() ?? "").Trim(),
                amount = ToFloat(reader.GetValue(idxAmount)),
                valueType = ToValueType(reader.GetValue(idxValType)),
                priceType = ToPriceType(reader.GetValue(idxPriceType)),
                price = ToInt(reader.GetValue(idxPrice)),
                note = idxNote >= 0 ? (reader.GetValue(idxNote)?.ToString() ?? "").Trim() : ""
            };

            if (string.IsNullOrEmpty(row.item)) continue;

            list.Add(row);

            if (dbg != null && dbg.logFirstDataRows && logged < dbg.firstDataRowsCount)
            {
                logged++;
                Debug.Log($"[ExcelReader] ({sheetName}) Row#{logged}: id={row.id}, lv={row.level}, type={row.type}, item={row.item}, amount={row.amount} {row.valueType}, price={row.priceType} {row.price}, note={row.note}");
            }
        }

        Debug.Log($"[ExcelReader] Loaded {list.Count} rows from sheet '{sheetName}'.");
        return list;
    }

    // ---- Utilities ----
    public static List<string> GetSheetNames(string fileName)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = GameDataWorkbook.OpenRead(fileName);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var names = new List<string>();
        do { names.Add(reader.Name); } while (reader.NextResult());
        return names;
    }

    static void MoveToSheetByName(IExcelDataReader reader, string sheetName)
    {
        int guard = 0;
        do
        {
            if (string.Equals(reader.Name, sheetName, StringComparison.OrdinalIgnoreCase))
                return;

            guard++;
            if (guard > 200)
                throw new Exception($"시트 이동 실패: '{sheetName}'");
        }
        while (reader.NextResult());

        throw new Exception($"엑셀에 '{sheetName}' 시트가 없어요.");
    }

    static bool TryMoveToHeaderRow(IExcelDataReader reader, int colCount, out List<string> headerRowValues)
    {
        headerRowValues = null;

        int guard = 0;
        while (reader.Read())
        {
            guard++;
            if (guard > 2000) return false;

            var values = new List<string>(colCount);
            for (int i = 0; i < colCount; i++)
                values.Add((reader.GetValue(i)?.ToString() ?? "").Trim());

            // 헤더 행 조건: "식별 순번"이 있어야 함
            if (values.Exists(v => string.Equals(v, "식별 순번", StringComparison.OrdinalIgnoreCase)))
            {
                headerRowValues = values;
                return true;
            }
        }

        return false;
    }

    static Dictionary<string, int> BuildHeaderMap(List<string> headerRowValues)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerRowValues.Count; i++)
        {
            var key = headerRowValues[i];
            if (string.IsNullOrEmpty(key)) continue;
            if (!map.ContainsKey(key))
                map.Add(key, i);
        }
        return map;
    }

    static int GetIdx(Dictionary<string, int> headerToIndex, string name)
    {
        if (!headerToIndex.TryGetValue(name, out int idx))
            throw new Exception($"엑셀 헤더에 '{name}' 컬럼이 없어요.");
        return idx;
    }

    static int GetIdxOptional(Dictionary<string, int> headerToIndex, string name)
    {
        return headerToIndex.TryGetValue(name, out int idx) ? idx : -1;
    }

    static bool IsAllEmpty(IExcelDataReader reader, int colCount)
    {
        for (int i = 0; i < colCount; i++)
        {
            if (reader.GetValue(i) != null) return false;
        }
        return true;
    }

    static string RowToDebugString(List<string> row)
    {
        return string.Join(" | ", row);
    }

    static int ToInt(object v)
    {
        if (v == null) return 0;
        if (v is double d) return (int)d;
        int.TryParse(v.ToString(), out int r);
        return r;
    }

    static float ToFloat(object v)
    {
        if (v == null) return 0f;
        if (v is double d) return (float)d;
        float.TryParse(v.ToString(), out float r);
        return r;
    }

    static ValueType ToValueType(object v)
    {
        var s = (v?.ToString() ?? "").Trim();
        if (string.Equals(s, "percent", StringComparison.OrdinalIgnoreCase)) return ValueType.Percent;
        return ValueType.Value;
    }

    static PriceType ToPriceType(object v)
    {
        var s = (v?.ToString() ?? "").Trim();
        if (string.Equals(s, "jewel", StringComparison.OrdinalIgnoreCase)) return PriceType.Jewel;
        return PriceType.Coin;
    }

    static UpgradeStatManager.UpgradeType ParseTypeFallback(string enumKey, int id)
    {
        if (!string.IsNullOrEmpty(enumKey))
        {
            return (UpgradeStatManager.UpgradeType)Enum.Parse(
                typeof(UpgradeStatManager.UpgradeType),
                enumKey,
                true
            );
        }

        return id switch
        {
            1 => UpgradeStatManager.UpgradeType.ATT,
            2 => UpgradeStatManager.UpgradeType.HP,
            3 => UpgradeStatManager.UpgradeType.ATT_SPEED,
            4 => UpgradeStatManager.UpgradeType.PROJECTILE_SPEED,
            5 => UpgradeStatManager.UpgradeType.BOSS_DAMAGE,
            6 => UpgradeStatManager.UpgradeType.COIN_BONUS,
            7 => UpgradeStatManager.UpgradeType.HP_REGEN,
            8 => UpgradeStatManager.UpgradeType.TUNGTUNGTUNG,
            9 => UpgradeStatManager.UpgradeType.BOOMBAR,
            _ => UpgradeStatManager.UpgradeType.ATT
        };
    }
}
