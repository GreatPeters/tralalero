using System;
using System.Collections.Generic;
using ExcelDataReader;

public class SkinRowParser : ITableParser<SkinRow>
{
    public SkinRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId        = ExcelUtil.GetIdx(h, "식별 순번");
        int idxItem      = ExcelUtil.GetIdx(h, "항목");
        int idxPriceType = ExcelUtil.GetIdx(h, "가격 타입");
        int idxPrice     = ExcelUtil.GetIdx(h, "가격 수치");
        int idxNote      = ExcelUtil.GetIdxOptional(h, "기타 설명");

        return new SkinRow
        {
            id = ExcelUtil.ToInt(reader.GetValue(idxId)),
            item = (reader.GetValue(idxItem)?.ToString() ?? "").Trim(),
            priceType = ToPriceType(reader.GetValue(idxPriceType)),
            price = ExcelUtil.ToInt(reader.GetValue(idxPrice)),
            note = idxNote >= 0 ? (reader.GetValue(idxNote)?.ToString() ?? "").Trim() : ""
        };
    }

    public bool IsValidRow(SkinRow row)
        => row.id != 0 && !string.IsNullOrEmpty(row.item);

    static PriceType ToPriceType(object v)
    {
        var s = (v?.ToString() ?? "").Trim();
        if (string.Equals(s, "jewel", StringComparison.OrdinalIgnoreCase)) return PriceType.Jewel;
        return PriceType.Coin;
    }
}
