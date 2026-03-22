using System;
using System.Collections.Generic;
using ExcelDataReader;

public class UpgradeRowParser : ITableParser<UpgradeRow>
{
    private const string HeaderId = "\uC2DD\uBCC4\uC21C\uBC88";
    private const string HeaderEnum = "\uC2DD\uBCC4Enum";
    private const string HeaderLevel = "\uB808\uBCA8";
    private const string HeaderItem = "\uD56D\uBAA9";
    private const string HeaderAmount = "\uC218\uCE58";
    private const string HeaderValueType = "\uC218\uCE58\uD0C0\uC785";
    private const string HeaderPriceType = "\uAC00\uACA9\uD0C0\uC785";
    private const string HeaderPrice = "\uAC00\uACA9\uC218\uCE58";
    private const string HeaderNote = "\uAE30\uD0C0\uC124\uBA85";

    public UpgradeRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId = ExcelUtil.GetIdx(h, HeaderId);
        int idxEnum = ExcelUtil.GetIdxOptional(h, HeaderEnum);
        int idxLv = ExcelUtil.GetIdx(h, HeaderLevel);
        int idxItem = ExcelUtil.GetIdx(h, HeaderItem);
        int idxAmount = ExcelUtil.GetIdx(h, HeaderAmount);
        int idxValType = ExcelUtil.GetIdx(h, HeaderValueType);
        int idxPriceType = ExcelUtil.GetIdx(h, HeaderPriceType);
        int idxPrice = ExcelUtil.GetIdx(h, HeaderPrice);
        int idxNote = ExcelUtil.GetIdxOptional(h, HeaderNote);

        int id = ExcelUtil.ToInt(reader.GetValue(idxId));
        string enumKey = idxEnum >= 0 ? (reader.GetValue(idxEnum)?.ToString() ?? "").Trim() : "";
        var type = ParseTypeFallback(enumKey, id);

        return new UpgradeRow
        {
            id = id,
            level = ExcelUtil.ToInt(reader.GetValue(idxLv)),
            type = type,
            item = (reader.GetValue(idxItem)?.ToString() ?? "").Trim(),
            amount = ExcelUtil.ToFloat(reader.GetValue(idxAmount)),
            valueType = ToValueType(reader.GetValue(idxValType)),
            priceType = ToPriceType(reader.GetValue(idxPriceType)),
            price = ExcelUtil.ToInt(reader.GetValue(idxPrice)),
            note = idxNote >= 0 ? (reader.GetValue(idxNote)?.ToString() ?? "").Trim() : ""
        };
    }

    public bool IsValidRow(UpgradeRow row)
        => row.id != 0 && row.level != 0 && !string.IsNullOrEmpty(row.item);

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

    static ValueType ToValueType(object v)
    {
        var s = (v?.ToString() ?? "").Trim();
        if (string.Equals(s, "percent", StringComparison.OrdinalIgnoreCase)) return ValueType.Percent;
        if (string.Equals(s, "value", StringComparison.OrdinalIgnoreCase)) return ValueType.Value;
        return ValueType.Value;
    }

    static PriceType ToPriceType(object v)
    {
        var s = (v?.ToString() ?? "").Trim();
        if (string.Equals(s, "jewel", StringComparison.OrdinalIgnoreCase)) return PriceType.Jewel;
        if (string.Equals(s, "coin", StringComparison.OrdinalIgnoreCase)) return PriceType.Coin;
        return PriceType.Coin;
    }
}
