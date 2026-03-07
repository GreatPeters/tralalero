using System;
using System.Collections.Generic;
using ExcelDataReader;

public class UpgradeRowParser : ITableParser<UpgradeRow>
{
    public UpgradeRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId        = ExcelUtil.GetIdx(h, "식별 순번");
        int idxEnum      = ExcelUtil.GetIdxOptional(h, "식별 Enum"); // 새 컬럼
        int idxLv        = ExcelUtil.GetIdx(h, "레벨");
        int idxItem      = ExcelUtil.GetIdx(h, "항목");
        int idxAmount    = ExcelUtil.GetIdx(h, "수치");
        int idxValType   = ExcelUtil.GetIdx(h, "수치 타입");
        int idxPriceType = ExcelUtil.GetIdx(h, "가격 타입");
        int idxPrice     = ExcelUtil.GetIdx(h, "가격 수치");
        int idxNote      = ExcelUtil.GetIdxOptional(h, "기타 설명");

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

        // 예전 시트/누락 대비용 fallback (가능하면 엑셀에 '식별 Enum' 넣는 게 정답)
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
