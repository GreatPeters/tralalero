using System;
using System.Collections.Generic;
using ExcelDataReader;

public class SkinRowParser : ITableParser<SkinRow>
{
    private const string HeaderId = "\uC2DD\uBCC4\uC21C\uBC88";
    private const string HeaderItem = "\uD56D\uBAA9";
    private const string HeaderPriceType = "\uAC00\uACA9\uD0C0\uC785";
    private const string HeaderPrice = "\uAC00\uACA9\uC218\uCE58";
    private const string HeaderNote = "\uAE30\uD0C0\uC124\uBA85";

    public SkinRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId = ExcelUtil.GetIdx(h, HeaderId);
        int idxItem = ExcelUtil.GetIdx(h, HeaderItem);
        int idxPriceType = ExcelUtil.GetIdx(h, HeaderPriceType);
        int idxPrice = ExcelUtil.GetIdx(h, HeaderPrice);
        int idxNote = ExcelUtil.GetIdxOptional(h, HeaderNote);

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
