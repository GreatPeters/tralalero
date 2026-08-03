using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;

public class GameDataWorkbookTests
{
    [Test]
    public void SourceWorkbook_IsEditorOnlyAndAbsentFromStreamingAssets()
    {
        Assert.That(
            GameDataWorkbookEditor.IsEditorOnlyAssetPath(
                GameDataWorkbook.EditorSourceAssetPath),
            Is.True);
        Assert.That(
            File.Exists(GameDataWorkbook.GetEditorSourceAbsolutePath()),
            Is.True);
        Assert.That(
            File.Exists(GetAbsoluteAssetPath(
                GameDataWorkbookEditor.LegacyStreamingAssetsPath)),
            Is.False);
    }

    [Test]
    public void ProductionSigningKey_StaysOutsideProjectAndAssets()
    {
        Assert.That(
            File.Exists(GetAbsoluteAssetPath(
                GameDataWorkbookEditor.LegacySigningKeyAssetPath)),
            Is.False);
        Assert.That(
            GameDataWorkbookEditor.FindSigningKeyUnderAssets(),
            Is.Null);
        Assert.That(
            GameDataWorkbookEditor.IsPathInsideProject(
                GameDataWorkbookEditor.GetSigningKeyAbsolutePath()),
            Is.False);
    }

    [Test]
    public void ProtectedArchive_RoundTripsWithMatchingSigningKey()
    {
        byte[] workbook = Encoding.UTF8.GetBytes("test workbook payload");
        using RSA signingKey = RSA.Create();

        byte[] archive = GameDataArchive.Protect(
            workbook,
            signingKey.ExportParameters(includePrivateParameters: true));
        byte[] restored = GameDataArchive.Unprotect(
            archive,
            signingKey.ExportParameters(includePrivateParameters: false));

        Assert.That(restored, Is.EqualTo(workbook));
        Assert.That(
            Encoding.ASCII.GetString(archive, 0, 8),
            Is.EqualTo("SSGDATA1"));
    }

    [Test]
    public void ProtectedArchive_RejectsModifiedCiphertext()
    {
        byte[] workbook = Encoding.UTF8.GetBytes("signed payload");
        using RSA signingKey = RSA.Create();
        byte[] archive = GameDataArchive.Protect(
            workbook,
            signingKey.ExportParameters(includePrivateParameters: true));

        const int encryptedPayloadOffset =
            8 + sizeof(int) + sizeof(int) + sizeof(int) + 16;
        archive[encryptedPayloadOffset] ^= 0x01;

        Assert.Throws<GameDataIntegrityException>(() =>
            GameDataArchive.Unprotect(
                archive,
                signingKey.ExportParameters(includePrivateParameters: false)));
    }

    [Test]
    public void WorkbookSchema_RejectsMalformedXlsx()
    {
        byte[] malformedWorkbook =
            Encoding.UTF8.GetBytes("this is not an xlsx workbook");

        Assert.Throws<InvalidDataException>(() =>
            GameDataWorkbookSchema.Validate(malformedWorkbook));
    }

    [Test]
    public void GeneratedRuntimeArchive_IsProtectedAndMatchesExcelSource()
    {
        GameDataRuntimeArchiveStatus status =
            GameDataWorkbookEditor.GetRuntimeArchiveStatus(out string detail);
        UnityEngine.TextAsset protectedWorkbook =
            UnityEngine.Resources.Load<UnityEngine.TextAsset>(
                GameDataWorkbook.RuntimeResourcePath);

        Assert.That(status, Is.EqualTo(GameDataRuntimeArchiveStatus.Current), detail);
        Assert.That(protectedWorkbook, Is.Not.Null);
        byte[] archive = protectedWorkbook.bytes;
        Assert.That(archive.Length, Is.GreaterThan(8));
        Assert.That(archive[0], Is.Not.EqualTo((byte)'P'));
        Assert.That(archive[1], Is.Not.EqualTo((byte)'K'));
        Assert.DoesNotThrow(
            GameDataWorkbookEditor.ValidateRuntimeArchiveOrThrow);
    }

    [Test]
    public void SharedWorkbookLoader_ExposesEveryGameplaySheet()
    {
        var sheetNames =
            ExcelSheetLoader.GetSheetNames(GameDataWorkbook.FileName);

        Assert.That(sheetNames, Does.Contain("\uBAAC\uC2A4\uD130"));
        Assert.That(sheetNames, Does.Contain("\uC5C5\uADF8\uB808\uC774\uB4DC"));
        Assert.That(sheetNames, Does.Contain("\uBCF4\uB108\uC2A4"));
        Assert.That(sheetNames, Does.Contain("\uC2A4\uD0A8"));
        Assert.That(sheetNames, Does.Contain("\uD328\uD134"));
        Assert.That(sheetNames, Does.Contain("\uD658\uACBD \uBCC0\uC218"));
    }

    [Test]
    public void GameplayTables_AllReloadFromSharedWorkbook()
    {
        Assert.DoesNotThrow(MonsterTables.Reload);
        Assert.DoesNotThrow(UpgradeTables.Reload);
        Assert.DoesNotThrow(BonusTables.Reload);
        Assert.DoesNotThrow(SkinTables.Reload);
        Assert.DoesNotThrow(PatternTables.Reload);
        Assert.DoesNotThrow(EnvironmentVariableTables.Reload);

        Assert.That(MonsterTables.GetAll(), Is.Not.Empty);
        Assert.That(PatternTables.GetAll(), Is.Not.Empty);
    }

    [Test]
    public void MapToolConvenienceTab_ExposesExcelWorkflow()
    {
        Assert.That(
            NoryangjinMapToolWindow.GameDataOpenButtonLabel,
            Is.EqualTo("Excel \uC5F4\uAE30"));
        Assert.That(
            NoryangjinMapToolWindow.GameDataSelectButtonLabel,
            Is.EqualTo("\uD504\uB85C\uC81D\uD2B8\uC5D0\uC11C \uBCF4\uAE30"));
        Assert.That(
            NoryangjinMapToolWindow.GameDataBuildButtonLabel,
            Is.EqualTo("\uB7F0\uD0C0\uC784 \uB370\uC774\uD130 \uAC31\uC2E0"));
        Assert.That(
            NoryangjinMapToolWindow.GameDataValidateButtonLabel,
            Is.EqualTo("\uBCF4\uD638 \uB370\uC774\uD130 \uAC80\uC99D"));
    }

    [TestCase(GameDataWorkbook.EditorSourceAssetPath, true)]
    [TestCase("assets/shootersurvival/gamedata/editor/data.xlsx", true)]
    [TestCase("Assets/ShooterSurvival/Resources/GameData/Data.bytes", false)]
    [TestCase("Assets/Other/Data.xlsx", false)]
    public void WorkbookAutoReload_MatchesOnlyTheSourceWorkbook(
        string assetPath,
        bool expected)
    {
        Type autoReloadType = typeof(GameDataWorkbookEditor).Assembly.GetType(
            "GameDataWorkbookAutoReload");
        Assert.That(
            autoReloadType,
            Is.Not.Null,
            "The editor must install an automatic Data.xlsx reload hook.");

        MethodInfo matcher = autoReloadType.GetMethod(
            "IsSourceWorkbookAssetPath",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(matcher, Is.Not.Null);
        Assert.That(
            (bool)matcher.Invoke(null, new object[] { assetPath }),
            Is.EqualTo(expected));
    }

    [Test]
    public void SourceWorkbookPostprocessor_ReloadsEnvironmentVariableCache()
    {
        FieldInfo cacheField = typeof(EnvironmentVariableTables).GetField(
            "_float3Map",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(cacheField, Is.Not.Null);

        var staleCache = new Dictionary<string, EnvironmentVariableTables.Float3>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["playerSpeed"] = new EnvironmentVariableTables.Float3
            {
                value1 = -999f
            }
        };
        cacheField.SetValue(null, staleCache);

        try
        {
            Type postprocessorType = typeof(GameDataWorkbookEditor).Assembly.GetType(
                "GameDataWorkbookAssetPostprocessor");
            Assert.That(postprocessorType, Is.Not.Null);
            MethodInfo onPostprocessAllAssets = postprocessorType.GetMethod(
                "OnPostprocessAllAssets",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(onPostprocessAllAssets, Is.Not.Null);
            onPostprocessAllAssets.Invoke(
                null,
                new object[]
                {
                    new[] { GameDataWorkbook.EditorSourceAssetPath },
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>()
                });
            Assert.That(
                EnvironmentVariableTables.TryGetFloat(
                    "playerSpeed",
                    out float refreshedSpeed),
                Is.True);
            Assert.That(refreshedSpeed, Is.Not.EqualTo(-999f));
        }
        finally
        {
            EnvironmentVariableTables.Reload();
        }
    }

    [Test]
    public void ToolsDataMenus_ExposeTheCompleteWorkbookWorkflow()
    {
        string[] expected =
        {
            "Tools/Data/\uAC8C\uC784 \uB370\uC774\uD130 Excel \uC5F4\uAE30",
            "Tools/Data/\uAC8C\uC784 \uB370\uC774\uD130 \uD504\uB85C\uC81D\uD2B8\uC5D0\uC11C \uCC3E\uAE30",
            "Tools/Data/\uB7F0\uD0C0\uC784 \uBCF4\uD638 \uB370\uC774\uD130 \uAC31\uC2E0",
            "Tools/Data/\uB7F0\uD0C0\uC784 \uBCF4\uD638 \uB370\uC774\uD130 \uAC80\uC99D"
        };

        string[] actual = typeof(GameDataWorkbookEditor)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .SelectMany(method => method.CustomAttributes)
            .Where(attribute => attribute.AttributeType == typeof(MenuItem))
            .Select(attribute => attribute.ConstructorArguments[0].Value as string)
            .Where(path => path != null && path.StartsWith("Tools/Data/", StringComparison.Ordinal))
            .ToArray();

        CollectionAssert.AreEquivalent(expected, actual);
    }

    private static string GetAbsoluteAssetPath(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath);
        return Path.GetFullPath(Path.Combine(
            projectRoot,
            assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
