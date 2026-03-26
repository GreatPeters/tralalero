using System;
using System.Collections.Generic;
using ExcelDataReader;

public class SkinRowParser : ITableParser<SkinRow>
{
    private const string HeaderId = "식별 순서";
    private const string LegacyHeaderId = "식별 순번";
    private const string HeaderItem = "항목(이름)";
    private const string LegacyHeaderItem = "항목";
    private const string HeaderPriceType = "가격 타입";
    private const string HeaderPrice = "가격 수치";
    private const string HeaderBonusType = "보너스 타입";
    private const string HeaderBonusValueType = "보너스 값 타입";
    private const string HeaderBonusValue = "보너스 수치";
    private const string HeaderNote = "기타 설명";

    public SkinRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId = GetIdIndex(h);
        int idxItem = GetItemIndex(h);
        int idxPriceType = ExcelUtil.GetIdx(h, HeaderPriceType);
        int idxPrice = ExcelUtil.GetIdx(h, HeaderPrice);
        int idxBonusType = ExcelUtil.GetIdxOptional(h, HeaderBonusType);
        int idxBonusValueType = ExcelUtil.GetIdxOptional(h, HeaderBonusValueType);
        int idxBonusValue = ExcelUtil.GetIdxOptional(h, HeaderBonusValue);
        int idxNote = ExcelUtil.GetIdxOptional(h, HeaderNote);

        return new SkinRow
        {
            id = ExcelUtil.ToInt(reader.GetValue(idxId)),
            item = (reader.GetValue(idxItem)?.ToString() ?? "").Trim(),
            priceType = ToPriceType(reader.GetValue(idxPriceType)),
            price = ExcelUtil.ToInt(reader.GetValue(idxPrice)),
            bonusType = idxBonusType >= 0 ? (reader.GetValue(idxBonusType)?.ToString() ?? "").Trim() : "",
            bonusValueType = idxBonusValueType >= 0 ? ToValueType(reader.GetValue(idxBonusValueType)) : ValueType.Value,
            bonusValue = idxBonusValue >= 0 ? ExcelUtil.ToFloat(reader.GetValue(idxBonusValue)) : 0f,
            note = idxNote >= 0 ? (reader.GetValue(idxNote)?.ToString() ?? "").Trim() : ""
        };
    }

    public bool IsValidRow(SkinRow row)
        => row.id != 0 && !string.IsNullOrEmpty(row.item);

    static int GetIdIndex(Dictionary<string, int> headerMap)
    {
        int idx = ExcelUtil.GetIdxOptional(headerMap, HeaderId);
        if (idx >= 0)
            return idx;

        return ExcelUtil.GetIdx(headerMap, LegacyHeaderId);
    }

    static int GetItemIndex(Dictionary<string, int> headerMap)
    {
        int idx = ExcelUtil.GetIdxOptional(headerMap, HeaderItem);
        if (idx >= 0)
            return idx;

        return ExcelUtil.GetIdx(headerMap, LegacyHeaderItem);
    }

    static PriceType ToPriceType(object v)
    {
        var s = (v?.ToString() ?? "").Trim();
        if (string.Equals(s, "jewel", StringComparison.OrdinalIgnoreCase))
            return PriceType.Jewel;
        return PriceType.Coin;
    }

    static ValueType ToValueType(object v)
    {
        var s = (v?.ToString() ?? "").Trim();
        if (string.Equals(s, "percent", StringComparison.OrdinalIgnoreCase))
            return ValueType.Percent;
        return ValueType.Value;
    }
}
