using System;
using System.Collections.Generic;
using ExcelDataReader;

public class BonusRowParser : ITableParser<BonusRow>
{
    private const string HeaderId = "\uC2DD\uBCC4\uC21C\uBC88";
    private const string HeaderEnum = "\uC2DD\uBCC4Enum";
    private const string HeaderLevel = "\uB808\uBCA8";
    private const string HeaderItem = "\uD56D\uBAA9";
    private const string HeaderValueType = "\uC218\uCE58\uD0C0\uC785";
    private const string HeaderValue = "\uC218\uCE58";
    private const string HeaderPrice = "\uAC00\uACA9\uC218\uCE58";
    private const string HeaderMin = "\uCD5C\uC18C";
    private const string HeaderMax = "\uCD5C\uB300";
    private const string HeaderNote = "\uAE30\uD0C0\uC124\uBA85";

    public BonusRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId = ExcelUtil.GetIdx(h, HeaderId);
        int idxEnum = ExcelUtil.GetIdxOptional(h, HeaderEnum);
        int idxLv = ExcelUtil.GetIdxOptional(h, HeaderLevel);
        int idxItem = ExcelUtil.GetIdxOptional(h, HeaderItem);
        int idxType = ExcelUtil.GetIdxOptional(h, HeaderValueType);
        int idxMin = ExcelUtil.GetIdxOptional(h, HeaderMin);
        int idxMax = ExcelUtil.GetIdxOptional(h, HeaderMax);

        if (idxMin < 0) idxMin = ExcelUtil.GetIdxOptional(h, HeaderValue);
        if (idxMax < 0) idxMax = ExcelUtil.GetIdxOptional(h, HeaderPrice);

        int idxNote = ExcelUtil.GetIdxOptional(h, HeaderNote);

        return new BonusRow
        {
            id = ExcelUtil.ToInt(reader.GetValue(idxId)),
            rarity = idxEnum >= 0 ? (reader.GetValue(idxEnum)?.ToString() ?? "").Trim() : "",
            level = idxLv >= 0 ? ExcelUtil.ToInt(reader.GetValue(idxLv)) : 0,
            stat = idxItem >= 0 ? (reader.GetValue(idxItem)?.ToString() ?? "").Trim() : "",
            min = idxMin >= 0 ? ExcelUtil.ToFloat(reader.GetValue(idxMin)) : 0f,
            max = idxMax >= 0 ? ExcelUtil.ToFloat(reader.GetValue(idxMax)) : 0f,
            valueType = ToBonusValueType(idxType >= 0 ? reader.GetValue(idxType) : null),
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
