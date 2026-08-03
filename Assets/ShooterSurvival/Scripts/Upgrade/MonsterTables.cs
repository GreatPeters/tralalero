using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ExcelDataReader;
using UnityEngine;

public static class MonsterTables
{
    private const string FileName = "Data.xlsx";
    private const string SheetName = "\uBAAC\uC2A4\uD130";

    private static Dictionary<string, MonsterRow> _map;
    private static List<MonsterRow> _allRows;

    private static void EnsureInit()
    {
        if (_map != null) return;

        var rows = TableCache.Load(FileName, SheetName, new MonsterRowParser(),
            dbg: new ExcelSheetLoader.DebugOptions
            {
                logSheetNames = false,
                logHeaderRow = true,
                logFirstDataRows = true,
                firstDataRowsCount = 3
            });

        _map = new Dictionary<string, MonsterRow>(StringComparer.OrdinalIgnoreCase);
        _allRows = rows;

        foreach (var r in rows)
            _map[MakeKey(r.chapter, r.stage, r.tier)] = r;

        Debug.Log($"[MonsterTables] Ready. rows={rows.Count}");
    }

    public static bool TryGet(int chapter, int stage, EnemyTier tier, out MonsterRow row)
    {
        EnsureInit();
        return _map.TryGetValue(MakeKey(chapter, stage, tier), out row);
    }

    public static List<MonsterRow> GetAll()
    {
        EnsureInit();
        return _allRows;
    }

    public static void Reload()
    {
        _map = null;
        _allRows = null;
        TableCache.Clear(FileName, SheetName);
    }

    private static string MakeKey(int chapter, int stage, EnemyTier tier)
        => $"{chapter}:{stage}:{tier}";
}

[Serializable]
public struct MonsterGrowthRow
{
    public int chapter;
    public EnemyTier tier;
    public float initialDamage;
    public float finalDamage;
    public float initialHealth;
    public float finalHealth;
    public float coefficient;

    public override string ToString()
        => $"chapter={chapter} tier={tier} coefficient={coefficient} " +
           $"dmg={initialDamage}->{finalDamage} hp={initialHealth}->{finalHealth}";
}

public sealed class MonsterGrowthRowParser : ITableParser<MonsterGrowthRow>
{
    private const string HeaderChapter = "\uCC55\uD130";
    private const string HeaderTier = "\uD2F0\uC5B4";
    private const string HeaderInitialDamage = "\uCD08\uAE30 \uACF5\uACA9\uB825";
    private const string HeaderFinalDamage = "\uCD5C\uC885 \uACF5\uACA9\uB825";
    private const string HeaderInitialHealth = "\uCD08\uAE30 \uCCB4\uB825";
    private const string HeaderFinalHealth = "\uCD5C\uC885 \uCCB4\uB825";
    private const string HeaderCoefficient = "\uACC4\uC218";

    public MonsterGrowthRow ParseRow(IExcelDataReader reader, Dictionary<string, int> headers)
    {
        int chapterIndex = ExcelUtil.GetIdx(headers, HeaderChapter);
        int tierIndex = ExcelUtil.GetIdx(headers, HeaderTier);
        int initialDamageIndex = ExcelUtil.GetIdx(headers, HeaderInitialDamage);
        int finalDamageIndex = ExcelUtil.GetIdx(headers, HeaderFinalDamage);
        int initialHealthIndex = ExcelUtil.GetIdx(headers, HeaderInitialHealth);
        int finalHealthIndex = ExcelUtil.GetIdx(headers, HeaderFinalHealth);
        int coefficientIndex = ExcelUtil.GetIdx(headers, HeaderCoefficient);

        string tierText = (reader.GetValue(tierIndex)?.ToString() ?? string.Empty).Trim();
        if (!Enum.TryParse(tierText, true, out EnemyTier tier) ||
            !Enum.IsDefined(typeof(EnemyTier), tier))
        {
            throw new InvalidDataException(
                $"Sheet '{MonsterGrowthTables.SheetName}' has an invalid tier '{tierText}'.");
        }

        return new MonsterGrowthRow
        {
            chapter = ReadRequiredInt(reader.GetValue(chapterIndex), HeaderChapter),
            tier = tier,
            initialDamage = ReadRequiredFloat(reader.GetValue(initialDamageIndex), HeaderInitialDamage),
            finalDamage = ReadRequiredFloat(reader.GetValue(finalDamageIndex), HeaderFinalDamage),
            initialHealth = ReadRequiredFloat(reader.GetValue(initialHealthIndex), HeaderInitialHealth),
            finalHealth = ReadRequiredFloat(reader.GetValue(finalHealthIndex), HeaderFinalHealth),
            coefficient = ReadRequiredFloat(reader.GetValue(coefficientIndex), HeaderCoefficient)
        };
    }

    public bool IsValidRow(MonsterGrowthRow row)
        => row.chapter > 0;

    private static int ReadRequiredInt(object value, string header)
    {
        if (value is double doubleValue &&
            doubleValue >= int.MinValue &&
            doubleValue <= int.MaxValue &&
            Math.Truncate(doubleValue) == doubleValue)
        {
            return (int)doubleValue;
        }

        string text = value?.ToString()?.Trim() ?? string.Empty;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ||
            int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
        {
            return parsed;
        }

        throw new InvalidDataException(
            $"Sheet '{MonsterGrowthTables.SheetName}' has an invalid integer in '{header}'.");
    }

    private static float ReadRequiredFloat(object value, string header)
    {
        float parsed;
        if (value is double doubleValue)
        {
            parsed = (float)doubleValue;
        }
        else
        {
            string text = value?.ToString()?.Trim() ?? string.Empty;
            bool parsedSuccessfully = float.TryParse(
                text,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out parsed);
            if (!parsedSuccessfully)
            {
                parsedSuccessfully = float.TryParse(
                    text,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.CurrentCulture,
                    out parsed);
            }

            if (!parsedSuccessfully)
            {
                throw new InvalidDataException(
                    $"Sheet '{MonsterGrowthTables.SheetName}' has an invalid number in '{header}'.");
            }
        }

        if (float.IsNaN(parsed) || float.IsInfinity(parsed))
        {
            throw new InvalidDataException(
                $"Sheet '{MonsterGrowthTables.SheetName}' has a non-finite number in '{header}'.");
        }

        return parsed;
    }
}

public static class MonsterGrowthTables
{
    private const string FileName = "Data.xlsx";
    internal const string SheetName = "\uBAAC\uC2A4\uD130 \uC131\uC7A5";

    private static List<MonsterGrowthRow> allRows;

    public static List<MonsterGrowthRow> GetAll()
    {
        if (allRows != null)
            return allRows;

        List<MonsterGrowthRow> loadedRows = TableCache.Load(
            FileName,
            SheetName,
            new MonsterGrowthRowParser(),
            new ExcelSheetLoader.DebugOptions
            {
                logSheetNames = false,
                logHeaderRow = true,
                logFirstDataRows = true,
                firstDataRowsCount = 3
            });
        ValidateRows(loadedRows);
        allRows = loadedRows;
        Debug.Log($"[MonsterGrowthTables] Ready. rows={allRows.Count}");
        return allRows;
    }

    public static bool TryGetAll(out List<MonsterGrowthRow> rows)
    {
        foreach (string sheetName in ExcelSheetLoader.GetSheetNames(FileName))
        {
            if (!string.Equals(sheetName?.Trim(), SheetName, StringComparison.OrdinalIgnoreCase))
                continue;

            rows = GetAll();
            return true;
        }

        rows = null;
        return false;
    }

    public static void ValidateRows(IReadOnlyList<MonsterGrowthRow> rows)
    {
        if (rows == null || rows.Count == 0)
            throw new InvalidDataException($"Sheet '{SheetName}' could not be read.");

        var tiersByChapter = new Dictionary<int, HashSet<EnemyTier>>();
        int maxChapter = 0;
        foreach (MonsterGrowthRow row in rows)
        {
            if (row.chapter <= 0)
                throw new InvalidDataException($"Sheet '{SheetName}' requires positive chapter numbers.");
            if (!Enum.IsDefined(typeof(EnemyTier), row.tier))
                throw new InvalidDataException($"Sheet '{SheetName}' contains an unsupported tier '{row.tier}'.");
            if (!tiersByChapter.TryGetValue(row.chapter, out HashSet<EnemyTier> chapterTiers))
            {
                chapterTiers = new HashSet<EnemyTier>();
                tiersByChapter.Add(row.chapter, chapterTiers);
            }
            if (!chapterTiers.Add(row.tier))
                throw new InvalidDataException(
                    $"Sheet '{SheetName}' contains more than one chapter {row.chapter} '{row.tier}' row.");
            if (!IsFinite(row.initialDamage) || !IsFinite(row.finalDamage) ||
                row.initialDamage < 0f || row.finalDamage < 0f)
            {
                throw new InvalidDataException($"Sheet '{SheetName}' requires finite, non-negative damage endpoints.");
            }
            if (!IsFinite(row.initialHealth) || !IsFinite(row.finalHealth) ||
                row.initialHealth <= 0f || row.finalHealth <= 0f)
            {
                throw new InvalidDataException($"Sheet '{SheetName}' requires finite, positive health endpoints.");
            }
            if (!IsFinite(row.coefficient) || row.coefficient <= 0f)
                throw new InvalidDataException($"Sheet '{SheetName}' requires finite, positive coefficients.");

            maxChapter = Math.Max(maxChapter, row.chapter);
        }

        for (int chapter = 1; chapter <= maxChapter; chapter++)
        {
            if (!tiersByChapter.TryGetValue(chapter, out HashSet<EnemyTier> chapterTiers) ||
                chapterTiers.Count != 3 ||
                !chapterTiers.Contains(EnemyTier.Normal) ||
                !chapterTiers.Contains(EnemyTier.Elite) ||
                !chapterTiers.Contains(EnemyTier.Boss))
            {
                throw new InvalidDataException(
                    $"Sheet '{SheetName}' must contain exactly one Normal, Elite, and Boss row for chapter {chapter}.");
            }
        }
    }

    private static bool IsFinite(float value)
        => !float.IsNaN(value) && !float.IsInfinity(value);

    public static void Reload()
    {
        allRows = null;
        TableCache.Clear(FileName, SheetName);
    }
}

public static class MonsterStatInterpolator
{
    public static float CalculateProgress(int enemyIndex, int enemyCount)
    {
        if (enemyCount <= 1)
            return 0f;

        return Mathf.Clamp01(enemyIndex / (float)(enemyCount - 1));
    }

    public static void Evaluate(
        MonsterGrowthRow growth,
        float progress,
        out float damage,
        out float health)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        damage = Mathf.Lerp(growth.initialDamage, growth.finalDamage, clampedProgress);
        health = Mathf.Lerp(growth.initialHealth, growth.finalHealth, clampedProgress);
    }
}

