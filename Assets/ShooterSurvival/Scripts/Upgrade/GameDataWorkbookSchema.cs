using System;
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;

public static class GameDataWorkbookSchema
{
    private const string SourceName = "Data.xlsx snapshot";

    public static void Validate(byte[] workbookBytes)
    {
        if (workbookBytes == null)
            throw new ArgumentNullException(nameof(workbookBytes));
        if (workbookBytes.Length == 0)
            throw new InvalidDataException("The game data workbook is empty.");

        try
        {
            ValidateTable(
                workbookBytes,
                "\uBAAC\uC2A4\uD130",
                new MonsterRowParser());
            if (ContainsSheet(workbookBytes, MonsterGrowthTables.SheetName))
            {
                using var growthStream =
                    new MemoryStream(workbookBytes, writable: false);
                var growthRows = ExcelSheetLoader.LoadBySheetName(
                    growthStream,
                    SourceName,
                    MonsterGrowthTables.SheetName,
                    new MonsterGrowthRowParser());
                MonsterGrowthTables.ValidateRows(growthRows);
            }
            ValidateTable(
                workbookBytes,
                "\uC5C5\uADF8\uB808\uC774\uB4DC",
                new UpgradeRowParser());
            ValidateTable(
                workbookBytes,
                "\uBCF4\uB108\uC2A4",
                new BonusRowParser());
            ValidateTable(
                workbookBytes,
                "\uC2A4\uD0A8",
                new SkinRowParser());
            ValidateTable(
                workbookBytes,
                "\uD328\uD134",
                new PatternSheetRowParser());

            using var environmentStream =
                new MemoryStream(workbookBytes, writable: false);
            EnvironmentVariableTables.ValidateWorkbook(environmentStream);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"The game data workbook schema is invalid: {exception.Message}",
                exception);
        }
    }

    private static bool ContainsSheet(byte[] workbookBytes, string sheetName)
    {
        System.Text.Encoding.RegisterProvider(
            System.Text.CodePagesEncodingProvider.Instance);

        using var stream = new MemoryStream(workbookBytes, writable: false);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        do
        {
            if (string.Equals(
                    reader.Name?.Trim(),
                    sheetName?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        } while (reader.NextResult());

        return false;
    }

    private static List<T> ValidateTable<T>(
        byte[] workbookBytes,
        string sheetName,
        ITableParser<T> parser)
    {
        using var stream = new MemoryStream(workbookBytes, writable: false);
        var rows = ExcelSheetLoader.LoadBySheetName(
            stream,
            SourceName,
            sheetName,
            parser);

        if (rows.Count == 0)
        {
            throw new InvalidDataException(
                $"Sheet '{sheetName}' does not contain any valid rows.");
        }

        return rows;
    }
}
