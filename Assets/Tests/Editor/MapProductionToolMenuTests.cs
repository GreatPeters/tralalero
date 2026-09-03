#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MapProductionToolMenuTests
{
    private const string MenuRoot = "Tools/맵 제작 도구/";

    [Test]
    public void MapProductionToolMenus_AreGroupedByUserWorkflow()
    {
        string[] expected =
        {
            "Tools/맵 제작 도구/문서/개발 문서 열기",
            "Tools/맵 제작 도구/문서/기획서 열기",
            "Tools/맵 제작 도구/문서/맵 기획서 열기",
            "Tools/맵 제작 도구/문서/밸런스 문서 열기",
            "Tools/맵 제작 도구/노량진 맵 제작/맵툴 열기",
            "Tools/맵 제작 도구/자료/자료 위치 안내"
        };

        CollectionAssert.AreEquivalent(expected, FindMapProductionToolMenuPaths());
    }

    [TestCase("GameDesignDocumentRelativePath", "GAME_DESIGN_OVERVIEW.md")]
    [TestCase("BalanceDocumentRelativePath", "BALANCE_OVERVIEW.md")]
    [TestCase("DevelopmentDocumentRelativePath", "DEVELOPMENT_OVERVIEW.md")]
    [TestCase("MapDesignDocumentRelativePath", "MAP_DESIGN_OVERVIEW.md")]
    public void DocumentMenus_TargetRootDocuments(
        string fieldName,
        string expectedRelativePath)
    {
        FieldInfo field = typeof(DesignReferenceWindow).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null);
        Assert.That(field.GetRawConstantValue(), Is.EqualTo(expectedRelativePath));

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        Assert.That(
            File.Exists(Path.Combine(projectRoot, expectedRelativePath)),
            Is.True,
            $"Canonical document is missing: {expectedRelativePath}");
    }

    [Test]
    public void NoryangjinMapPlanMenu_TargetsCurrentPlanOutputFolder()
    {
        FieldInfo field = typeof(DesignReferenceWindow).GetField(
            "NoryangjinMapPlanFolderRelativePath",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null);
        Assert.That(
            field.GetRawConstantValue(),
            Is.EqualTo("outputs/chapter_campaign_reference_orthogonal_20min"));
    }

    [Test]
    public void CodexGeneratedImagesMenu_TargetsRequestedSessionFolder()
    {
        PropertyInfo property = typeof(DesignReferenceWindow).GetProperty(
            "CodexGeneratedImagesFolderPath",
            BindingFlags.NonPublic | BindingFlags.Static);
        string expected = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex",
            "generated_images",
            "019f22f4-e2cc-73b1-8fdf-68dd8b36147a"));

        Assert.That(property, Is.Not.Null);
        Assert.That(property.GetValue(null), Is.EqualTo(expected));
    }

    private static IReadOnlyList<string> FindMapProductionToolMenuPaths()
    {
        return typeof(DesignReferenceWindow).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .SelectMany(method => method.CustomAttributes)
            .Where(attribute => attribute.AttributeType == typeof(MenuItem))
            .Select(attribute => attribute.ConstructorArguments[0].Value as string)
            .Where(path => path != null && path.StartsWith(MenuRoot, StringComparison.Ordinal))
            .ToArray();
    }
}
#endif
