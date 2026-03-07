using System;
using System.Collections.Generic;
using ExcelDataReader;

public class BonusRowParser : ITableParser<BonusRow>
{
    public BonusRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId = ExcelUtil.GetIdx(h, "식별 순번");
        int idxEnum = ExcelUtil.GetIdxOptional(h, "식별 Enum");
        int idxLv = ExcelUtil.GetIdxOptional(h, "레벨");
        int idxItem = ExcelUtil.GetIdxOptional(h, "항목");
        int idxType = ExcelUtil.GetIdxOptional(h, "수치 타입");

        int idxMin = ExcelUtil.GetIdxOptional(h, "최소");
        int idxMax = ExcelUtil.GetIdxOptional(h, "최대");

        if (idxMin < 0) idxMin = ExcelUtil.GetIdxOptional(h, "수치");
        if (idxMax < 0) idxMax = ExcelUtil.GetIdxOptional(h, "가격 수치");

        int idxNote = ExcelUtil.GetIdxOptional(h, "기타 설명");

        return new BonusRow
        {
            id = ExcelUtil.ToInt(reader.GetValue(idxId)),
            rarity = idxEnum >= 0 ? (reader.GetValue(idxEnum)?.ToString() ?? "").Trim() : "",
            level = idxLv >= 0 ? ExcelUtil.ToInt(reader.GetValue(idxLv)) : 0,
            stat = idxItem >= 0 ? (reader.GetValue(idxItem)?.ToString() ?? "").Trim() : "",
            min = idxMin >= 0 ? ExcelUtil.ToFloat(reader.GetValue(idxMin)) : 0f,
            max = idxMax >= 0 ? ExcelUtil.ToFloat(reader.GetValue(idxMax)) : 0f,
            valueType = ToBonusValueType(reader.GetValue(idxType)),
            note = idxNote >= 0 ? (reader.GetValue(idxNote)?.ToString() ?? "").Trim() : ""
        };
    }

    public bool IsValidRow(BonusRow row)
        => row.id != 0 && !string.IsNullOrEmpty(row.rarity) && !string.IsNullOrEmpty(row.stat);

    static BonusValueType ToBonusValueType(object v)
    {
        var s = (v?.ToString() ?? "").Trim();
        if (string.Equals(s, "percent", StringComparison.OrdinalIgnoreCase)) return BonusValueType.Percent;
        if (string.Equals(s, "ratio", StringComparison.OrdinalIgnoreCase)) return BonusValueType.Ratio;
        if (string.Equals(s, "value", StringComparison.OrdinalIgnoreCase)) return BonusValueType.Value;
        return BonusValueType.Value;
    }
}
