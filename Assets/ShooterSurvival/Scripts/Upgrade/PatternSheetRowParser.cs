using System;
using System.Collections.Generic;
using ExcelDataReader;

public class PatternSheetRowParser : ITableParser<PatternSheetRow>
{
    private const string HeaderId = "\uC21C\uBC88";
    private const string HeaderChapter = "\uCC55\uD130";
    private const string HeaderStage = "\uC2A4\uD14C\uC774\uC9C0";
    private const string HeaderNote = "\uAE30\uD0C0\uC124\uBA85";
    private const string HeaderPattern = "\uD328\uD134";
    private const string HeaderDifficulty = "\uB09C\uC774\uB3C4";

    public PatternSheetRow ParseRow(IExcelDataReader reader, Dictionary<string, int> h)
    {
        int idxId = ExcelUtil.GetIdx(h, HeaderId);
        int idxChapter = ExcelUtil.GetIdx(h, HeaderChapter);
        int idxStage = ExcelUtil.GetIdx(h, HeaderStage);
        int idxNote = ExcelUtil.GetIdxOptional(h, HeaderNote);

        var row = new PatternSheetRow
        {
            id = ExcelUtil.ToInt(reader.GetValue(idxId)),
            chapter = ExcelUtil.ToInt(reader.GetValue(idxChapter)),
            stage = ExcelUtil.ToInt(reader.GetValue(idxStage)),
            note = idxNote >= 0 ? (reader.GetValue(idxNote)?.ToString() ?? string.Empty).Trim() : string.Empty
        };

        for (int i = 0; i < PatternSheetRow.StepCount; i++)
        {
            int step = i + 1;
            int idxPattern = FindStepHeader(h, step, HeaderPattern, "Pattern");
            int idxDifficulty = FindStepHeader(h, step, HeaderDifficulty, "Difficulty");

            row.patterns[i] = ParsePattern(idxPattern >= 0 ? reader.GetValue(idxPattern) : null);
            row.difficulties[i] = ParseDifficulty(idxDifficulty >= 0 ? reader.GetValue(idxDifficulty) : null);
        }

        return row;
    }

    public bool IsValidRow(PatternSheetRow row)
    {
        return row.id != 0 && row.chapter > 0 && row.stage > 0;
    }

    private static int FindStepHeader(Dictionary<string, int> map, int step, params string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            string[] candidates =
            {
                $"{step}{suffix}",
                $"{suffix}{step}",
                $"Step{step}{suffix}",
                $"Step {step} {suffix}"
            };

            foreach (var candidate in candidates)
            {
                int idx = ExcelUtil.GetIdxOptional(map, candidate);
                if (idx >= 0)
                    return idx;
            }
        }

        return -1;
    }

    private static ObstaclePattern ParsePattern(object value)
    {
        var s = (value?.ToString() ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(s))
            return ObstaclePattern.None;

        if (Enum.TryParse(s, true, out ObstaclePattern pattern))
            return pattern;

        throw new Exception($"[PatternSheet] Unknown pattern value '{s}'. Use ObstaclePattern enum names.");
    }

    private static ObstacleDifficulty ParseDifficulty(object value)
    {
        var s = (value?.ToString() ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(s))
            return ObstacleDifficulty.Easy;

        if (int.TryParse(s, out int level) && Enum.IsDefined(typeof(ObstacleDifficulty), level))
            return (ObstacleDifficulty)level;

        if (Enum.TryParse(s, true, out ObstacleDifficulty difficulty))
            return difficulty;

        throw new Exception($"[PatternSheet] Unknown difficulty value '{s}'. Use Easy/Normal/Hard or 1/2/3.");
    }
}
