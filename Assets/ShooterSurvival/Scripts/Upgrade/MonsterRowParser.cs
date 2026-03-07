using System;
using System.Collections.Generic;
using ExcelDataReader;

public class MonsterRowParser : ITableParser<MonsterRow>
{
    public MonsterRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId = ExcelUtil.GetIdx(h, "식별 순번");
        int idxChapter = ExcelUtil.GetIdx(h, "챕터");
        int idxStage = ExcelUtil.GetIdx(h, "스테이지");
        int idxTier = ExcelUtil.GetIdx(h, "티어");
        int idxDamage = ExcelUtil.GetIdx(h, "공격력");
        int idxHealth = ExcelUtil.GetIdx(h, "체력");
        int idxNote = ExcelUtil.GetIdxOptional(h, "기타 설명");

        var tierStr = (reader.GetValue(idxTier)?.ToString() ?? "").Trim();
        var tier = (EnemyTier)Enum.Parse(typeof(EnemyTier), tierStr, true);

        return new MonsterRow
        {
            id = ExcelUtil.ToInt(reader.GetValue(idxId)),
            chapter = ExcelUtil.ToInt(reader.GetValue(idxChapter)),
            stage = ExcelUtil.ToInt(reader.GetValue(idxStage)),
            tier = tier,
            damage = ExcelUtil.ToFloat(reader.GetValue(idxDamage)),
            health = ExcelUtil.ToFloat(reader.GetValue(idxHealth)),
            note = idxNote >= 0 ? (reader.GetValue(idxNote)?.ToString() ?? "").Trim() : ""
        };
    }

    public bool IsValidRow(MonsterRow row)
        => row.id != 0 && row.chapter > 0 && row.stage > 0;
}
