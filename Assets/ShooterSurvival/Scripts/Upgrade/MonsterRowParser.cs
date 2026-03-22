using System;
using System.Collections.Generic;
using ExcelDataReader;

public class MonsterRowParser : ITableParser<MonsterRow>
{
    private const string HeaderId = "\uC21C\uBC88";
    private const string HeaderChapter = "\uCC55\uD130";
    private const string HeaderStage = "\uC2A4\uD14C\uC774\uC9C0";
    private const string HeaderTier = "\uD2F0\uC5B4";
    private const string HeaderDamage = "\uACF5\uACA9\uB825";
    private const string HeaderHealth = "\uCCB4\uB825";
    private const string HeaderNote = "\uAE30\uD0C0\uC124\uBA85";

    public MonsterRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId = ExcelUtil.GetIdx(h, HeaderId);
        int idxChapter = ExcelUtil.GetIdx(h, HeaderChapter);
        int idxStage = ExcelUtil.GetIdx(h, HeaderStage);
        int idxTier = ExcelUtil.GetIdx(h, HeaderTier);
        int idxDamage = ExcelUtil.GetIdx(h, HeaderDamage);
        int idxHealth = ExcelUtil.GetIdx(h, HeaderHealth);
        int idxNote = ExcelUtil.GetIdxOptional(h, HeaderNote);

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
