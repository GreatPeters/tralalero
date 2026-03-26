#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class StagePatternDataImporter
{
    private const string DefaultAssetSearch = "t:StagePatternData";
    private const string SheetName = "\uD328\uD134";
    private const string SheetFormatMessage =
        "[StagePatternImporter] Sheet format (B2 start, A열/1행 비워둬도 됨): " +
        "\uC21C\uBC88, \uCC55\uD130, \uC2A4\uD14C\uC774\uC9C0, " +
        "1\uD328\uD134, 1\uB09C\uC774\uB3C4, 2\uD328\uD134, 2\uB09C\uC774\uB3C4, " +
        "3\uD328\uD134, 3\uB09C\uC774\uB3C4, 4\uD328\uD134, 4\uB09C\uC774\uB3C4, " +
        "5\uD328\uD134, 5\uB09C\uC774\uB3C4, 6\uD328\uD134, 6\uB09C\uC774\uB3C4, " +
        "\uAE30\uD0C0\uC124\uBA85";

    [MenuItem("Tools/Data/Import Stage Pattern From Excel")]
    public static void ImportSelectedOrFirst()
    {
        var asset = GetSelectedOrFirstAsset();
        if (asset == null)
        {
            Debug.LogError("[StagePatternImporter] No StagePatternData asset found.");
            return;
        }

        ImportInto(asset);
    }

    [MenuItem("Assets/Import Stage Pattern From Excel", true)]
    private static bool ValidateImportSelected()
    {
        return Selection.activeObject is StagePatternData;
    }

    [MenuItem("Assets/Import Stage Pattern From Excel")]
    private static void ImportSelected()
    {
        if (Selection.activeObject is StagePatternData asset)
            ImportInto(asset);
    }

    private static StagePatternData GetSelectedOrFirstAsset()
    {
        if (Selection.activeObject is StagePatternData selected)
            return selected;

        string[] guids = AssetDatabase.FindAssets(DefaultAssetSearch);
        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<StagePatternData>(path);
    }

    private static void ImportInto(StagePatternData asset)
    {
        PatternTables.Reload();
        List<PatternSheetRow> rows = PatternTables.GetAll();

        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning($"[StagePatternImporter] No rows found in Data.xlsx / {SheetName}.");
            return;
        }

        Undo.RecordObject(asset, "Import Stage Pattern Data");
        ApplyRows(asset, rows);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string path = AssetDatabase.GetAssetPath(asset);
        Debug.Log($"[StagePatternImporter] Imported {rows.Count} rows into {path}");
        Debug.Log(SheetFormatMessage);
    }

    private static void ApplyRows(StagePatternData asset, List<PatternSheetRow> rows)
    {
        int maxChapter = 0;
        var maxStageByChapter = new Dictionary<int, int>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            string key = $"{row.chapter}:{row.stage}";
            if (!seenKeys.Add(key))
                throw new Exception($"[StagePatternImporter] Duplicate row found. chapter={row.chapter}, stage={row.stage}");

            maxChapter = Mathf.Max(maxChapter, row.chapter);

            if (!maxStageByChapter.TryGetValue(row.chapter, out int stage) || row.stage > stage)
                maxStageByChapter[row.chapter] = row.stage;
        }

        asset.chapters = new ChapterData[maxChapter];

        for (int chapterIndex = 0; chapterIndex < asset.chapters.Length; chapterIndex++)
        {
            int chapter = chapterIndex + 1;
            int stageCount = maxStageByChapter.TryGetValue(chapter, out int maxStage) ? maxStage : 0;

            asset.chapters[chapterIndex] = new ChapterData
            {
                stages = new StageData[stageCount]
            };

            for (int stageIndex = 0; stageIndex < stageCount; stageIndex++)
            {
                asset.chapters[chapterIndex].stages[stageIndex] = new StageData
                {
                    steps = CreateEmptySteps()
                };
            }
        }

        foreach (var row in rows)
        {
            int chapterIndex = row.chapter - 1;
            int stageIndex = row.stage - 1;

            ValidateIndex(asset, chapterIndex, stageIndex, row);

            var steps = CreateEmptySteps();
            for (int i = 0; i < PatternSheetRow.StepCount; i++)
            {
                steps[i] = new StepData
                {
                    pattern = row.patterns[i],
                    obstacleDifficulty = row.difficulties[i]
                };
            }

            asset.chapters[chapterIndex].stages[stageIndex] = new StageData
            {
                steps = steps
            };
        }
    }

    private static StepData[] CreateEmptySteps()
    {
        var steps = new StepData[PatternSheetRow.StepCount];
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i] = new StepData
            {
                pattern = ObstaclePattern.None,
                obstacleDifficulty = ObstacleDifficulty.Easy
            };
        }

        return steps;
    }

    private static void ValidateIndex(StagePatternData asset, int chapterIndex, int stageIndex, PatternSheetRow row)
    {
        if (chapterIndex < 0 || chapterIndex >= asset.chapters.Length)
            throw new Exception($"[StagePatternImporter] Invalid chapter index. chapter={row.chapter}");

        var chapter = asset.chapters[chapterIndex];
        if (chapter == null || chapter.stages == null || stageIndex < 0 || stageIndex >= chapter.stages.Length)
            throw new Exception($"[StagePatternImporter] Invalid stage index. chapter={row.chapter}, stage={row.stage}");
    }
}
#endif
