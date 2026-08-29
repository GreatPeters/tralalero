using System.Collections.Generic;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

public sealed class BonusAltarRulesTests
{
    [OneTimeSetUp]
    public void ReloadBonusRows()
    {
        BonusTables.Reload();
    }

    [TestCase(Rarity.Normal, 6)]
    [TestCase(Rarity.Rare, 2)]
    [TestCase(Rarity.Unique, 7)]
    public void WorkbookRows_AreClassifiedBySelectedAltarGrade(
        Rarity rarity,
        int expectedCount)
    {
        IReadOnlyList<BonusRow> rows = BonusTables.GetAll(
            BonusAltarRules.DataRarityFor(rarity));
        List<BonusRow> candidates = BonusAltarRules.BuildCandidates(
            rows,
            rarity,
            new HashSet<string>());

        Assert.That(candidates, Has.Count.EqualTo(expectedCount));
        Assert.That(
            candidates,
            Has.All.Matches<BonusRow>(row =>
                BonusAltarRules.TryResolveBuffType(rarity, row.stat, out _)));
    }

    [Test]
    public void NearbyStat_IsExcludedWhenAnotherChoiceExists()
    {
        IReadOnlyList<BonusRow> rows = BonusTables.GetAll("Normal");
        List<BonusRow> candidates = BonusAltarRules.BuildCandidates(
            rows,
            Rarity.Normal,
            new HashSet<string> { "att" });

        Assert.That(candidates, Has.Count.EqualTo(5));
        Assert.That(candidates, Has.None.Matches<BonusRow>(row => row.stat == "att"));
    }

    [Test]
    public void AllNearbyStatsExhausted_ReturnsNoDuplicateCandidate()
    {
        IReadOnlyList<BonusRow> rows = BonusTables.GetAll("Elite");
        List<BonusRow> candidates = BonusAltarRules.BuildCandidates(
            rows,
            Rarity.Rare,
            new HashSet<string> { "tungtungAdd", "boombarAdd" });

        Assert.That(candidates, Is.Empty);
    }

    [Test]
    public void WorkbookAliasesAndNames_AreUsedForEveryBonusRow()
    {
        foreach (string rarity in new[] { "Normal", "Rare", "Unique" })
        {
            foreach (BonusRow row in BonusTables.GetAll(rarity))
            {
                Assert.That(row.alias, Is.Not.Empty, $"Missing alias: {rarity}/{row.stat}");
                Assert.That(row.displayName, Is.Not.Empty, $"Missing name: {rarity}/{row.stat}");
                string resolvedAlias = BonusAltarRules.ResolveAlias(row);
                string resolvedName = BonusAltarRules.ResolveDisplayName(row);
                Assert.That(resolvedAlias, Is.EqualTo(row.alias.Trim()));
                Assert.That(resolvedName, Is.EqualTo(row.displayName.Trim()));
            }
        }
    }

    [Test]
    public void EmptyAlias_FallsBackOnlyToWorkbookNameThenStatKey()
    {
        BonusRow row = new()
        {
            alias = " ",
            displayName = " 엑셀 이름 ",
            stat = "excelStat"
        };

        Assert.That(BonusAltarRules.ResolveAlias(row), Is.EqualTo("엑셀 이름"));

        row.displayName = "";
        Assert.That(BonusAltarRules.ResolveAlias(row), Is.EqualTo("excelStat"));
    }

    [Test]
    public void RatioValue_UsesWorkbookRangeAndPlayerBaseStat()
    {
        BonusRow attack = FindRow("Normal", "att");

        Assert.That(BonusAltarRules.ResolveValue(attack, 0f, 100f), Is.EqualTo(12f));
        Assert.That(BonusAltarRules.ResolveValue(attack, 1f, 100f), Is.EqualTo(18f));
    }

    [Test]
    public void PercentValue_RemainsPercentagePointsWithFractionalInterpolation()
    {
        BonusRow row = new()
        {
            stat = "attPercent",
            min = 10f,
            max = 15f,
            valueType = BonusValueType.Percent
        };

        Assert.That(
            BonusAltarRules.ResolveValue(row, 0.25f, 999f),
            Is.EqualTo(11.25f).Within(0.0001f));
    }

    [Test]
    public void NonRoundedStat_PreservesFractionalValue()
    {
        BonusRow row = new()
        {
            stat = "missileDistance",
            min = 1.1f,
            max = 1.4f,
            valueType = BonusValueType.Value
        };

        Assert.That(
            BonusAltarRules.ResolveValue(row, 0.5f, 0f),
            Is.EqualTo(1.25f).Within(0.0001f));
    }

    [Test]
    public void CountStat_RoundsToWholeNumber()
    {
        BonusRow row = new()
        {
            stat = "missileAdd",
            min = 1.6f,
            max = 1.6f,
            valueType = BonusValueType.Value
        };

        float resolved = BonusAltarRules.ResolveValue(row, 0.5f, 0f);

        Assert.That(resolved, Is.EqualTo(2f));
        Assert.That(resolved % 1f, Is.Zero);
    }

    [Test]
    public void InvalidAuthoredRoll_HidesPresentationAndDisablesTrigger()
    {
        GameObject altar = new("Invalid Authored Altar");
        altar.SetActive(false);
        try
        {
            altar.AddComponent<AuthoredBonusWall>();
            BoxCollider trigger = altar.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            WallScript wall = altar.AddComponent<WallScript>();

            TextMeshProUGUI title = CreateText(altar.transform, "Choice_Title", "stale alias");
            TextMeshProUGUI statName = CreateText(altar.transform, "Stat_Name", "stale stat");
            TextMeshProUGUI value = CreateText(altar.transform, "Value_Text", "+99");
            LocalizeStringEvent statNameLoc =
                statName.gameObject.AddComponent<LocalizeStringEvent>();
            wall.statNameLoc = statNameLoc;
            wall.statValueTmp = value;

            wall.SetWallSprite();

            Assert.That(title.text, Is.Empty);
            Assert.That(title.enabled, Is.False);
            Assert.That(statName.text, Is.Empty);
            Assert.That(statName.enabled, Is.False);
            Assert.That(statNameLoc.enabled, Is.False);
            Assert.That(value.text, Is.Empty);
            Assert.That(value.enabled, Is.False);
            Assert.That(trigger.enabled, Is.False);
            Assert.That(trigger.isTrigger, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(altar);
        }
    }

    [Test]
    public void ValidAuthoredRoll_ShowsCompactStrengtheningTitleNameAndValue()
    {
        GameObject altar = new("Valid Authored Altar");
        try
        {
            WallScript wall = altar.AddComponent<WallScript>();
            AuthoredBonusWall authored = altar.AddComponent<AuthoredBonusWall>();
            authored.Configure(Rarity.Unique);

            TextMeshProUGUI title = CreateText(altar.transform, "Choice_Title", "old alias");
            TextMeshProUGUI statName = CreateText(altar.transform, "Stat_Name", "old stat");
            TextMeshProUGUI value = CreateText(altar.transform, "Value_Text", "old value");
            wall.statNameLoc = statName.gameObject.AddComponent<LocalizeStringEvent>();
            wall.statValueTmp = value;

            wall.SetRandomStat();
            wall.SetStats();

            Assert.That(
                BonusTables.TryGet("Unique", authored.RolledStat, out BonusRow row),
                Is.True);
            MethodInfo updateStatUi = typeof(WallScript).GetMethod(
                "UpdateStatUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateStatUi, Is.Not.Null);
            updateStatUi.Invoke(
                wall,
                new object[] { wall.buffType, wall.CurrentBonusDisplayValue });

            string expectedValue = BonusAltarRules.FormatDisplayValue(
                wall.CurrentBonusDisplayValue,
                row.valueType);
            BonusRow expectedNameRow = BonusTables.ResolveDisplayRow(row);
            Assert.That(
                title.text,
                Is.EqualTo(
                    BonusAltarRules.ResolveDisplayName(expectedNameRow) + " 강화"));
            Assert.That(
                statName.text,
                Is.EqualTo(BonusAltarRules.ResolveDisplayName(expectedNameRow)));
            Assert.That(value.text, Is.EqualTo(expectedValue));
            Assert.That(
                value.text.EndsWith("%"),
                Is.EqualTo(row.valueType == BonusValueType.Percent));
        }
        finally
        {
            Object.DestroyImmediate(altar);
        }
    }

    [Test]
    public void PercentPresentation_UsesSuffixAndPlainIcons()
    {
        BonusRow ratio = new()
        {
            stat = "att",
            min = 0.15f,
            max = 0.15f,
            valueType = BonusValueType.Ratio
        };
        float appliedValue = BonusAltarRules.ResolveValue(ratio, 0.5f, 80f);
        float displayValue = BonusAltarRules.ResolveDisplayValue(ratio, 0.5f);

        Assert.That(appliedValue, Is.EqualTo(12f));
        Assert.That(displayValue, Is.EqualTo(15f));
        Assert.That(
            BonusAltarRules.FormatDisplayValue(displayValue, BonusValueType.Ratio),
            Is.EqualTo("+15"));
        Assert.That(
            BonusAltarRules.FormatDisplayValue(38f, BonusValueType.Percent),
            Is.EqualTo("+38%"));
        Assert.That(
            BonusAltarRules.ResolveIconResourceName(BuffType.attPer_unique),
            Is.EqualTo("WallBonus_Attack"));
        Assert.That(
            BonusAltarRules.ResolveIconResourceName(BuffType.hpPer_unique),
            Is.EqualTo("WallBonus_Health"));
        Assert.That(
            BonusAltarRules.ResolveLocalizationKey(BuffType.attPer_unique),
            Is.EqualTo("att"));
        Assert.That(
            BonusAltarRules.ResolveLocalizationKey(BuffType.hpPer_unique),
            Is.EqualTo("hp"));
        Assert.That(
            BonusAltarRules.ResolveDisplayStatKey("attPercent"),
            Is.EqualTo("att"));
        Assert.That(
            BonusAltarRules.ResolveDisplayStatKey("hpPercent"),
            Is.EqualTo("hp"));
        Assert.That(
            BonusTables.ResolveDisplayRow(FindRow("Unique", "attPercent")).stat,
            Is.EqualTo("att"));
        Assert.That(
            BonusTables.ResolveDisplayRow(FindRow("Unique", "hpPercent")).stat,
            Is.EqualTo("hp"));
    }

    [Test]
    public void AttackPresentation_UsesSameNameAndPercentControlsSuffix()
    {
        GameObject altar = new("Attack Presentation Altar");
        try
        {
            WallScript wall = altar.AddComponent<WallScript>();
            TextMeshProUGUI statName = CreateText(altar.transform, "Stat_Name", "old");
            TextMeshProUGUI value = CreateText(altar.transform, "Value_Text", "old");
            wall.statNameLoc = statName.gameObject.AddComponent<LocalizeStringEvent>();
            wall.statValueTmp = value;

            MethodInfo updateStatUi = typeof(WallScript).GetMethod(
                "UpdateStatUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo selectedRowField = typeof(WallScript).GetField(
                "selectedBonusRow",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo hasSelectedRowField = typeof(WallScript).GetField(
                "hasSelectedBonusRow",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo selectedDisplayRowField = typeof(WallScript).GetField(
                "selectedDisplayRow",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo valueTypeField = typeof(WallScript).GetField(
                "bonusValueType",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateStatUi, Is.Not.Null);
            Assert.That(selectedRowField, Is.Not.Null);
            Assert.That(hasSelectedRowField, Is.Not.Null);
            Assert.That(selectedDisplayRowField, Is.Not.Null);
            Assert.That(valueTypeField, Is.Not.Null);

            BonusRow percentAttack = FindRow("Unique", "attPercent");
            selectedRowField.SetValue(wall, percentAttack);
            selectedDisplayRowField.SetValue(
                wall,
                BonusTables.ResolveDisplayRow(percentAttack));
            hasSelectedRowField.SetValue(wall, true);
            valueTypeField.SetValue(wall, BonusValueType.Percent);
            updateStatUi.Invoke(wall, new object[] { BuffType.attPer_unique, 15f });

            string attackName = FindRow("Unique", "att").displayName;
            Assert.That(statName.text, Is.EqualTo(attackName));
            Assert.That(value.text, Is.EqualTo("+15%"));

            BonusRow ratioAttack = FindRow("Unique", "att");
            selectedRowField.SetValue(wall, ratioAttack);
            selectedDisplayRowField.SetValue(
                wall,
                BonusTables.ResolveDisplayRow(ratioAttack));
            valueTypeField.SetValue(wall, BonusValueType.Ratio);
            updateStatUi.Invoke(wall, new object[] { BuffType.att_unique, 15f });

            Assert.That(statName.text, Is.EqualTo(attackName));
            Assert.That(value.text, Is.EqualTo("+15"));
        }
        finally
        {
            Object.DestroyImmediate(altar);
        }
    }

    [Test]
    public void EveryWorkbookBonus_UsesItsMinimumAndMaximumRange()
    {
        foreach (string rarity in new[] { "Normal", "Rare", "Unique" })
        {
            foreach (BonusRow row in BonusTables.GetAll(rarity))
            {
                Assert.That(row.min, Is.LessThanOrEqualTo(row.max),
                    $"Invalid range: {rarity}/{row.stat}");
                Assert.That(row.max, Is.GreaterThan(0f),
                    $"Missing range: {rarity}/{row.stat}");

                float baseValue = row.valueType == BonusValueType.Ratio ? 100f : 0f;
                float low = BonusAltarRules.ResolveValue(row, 0f, baseValue);
                float high = BonusAltarRules.ResolveValue(row, 1f, baseValue);

                Assert.That(low, Is.LessThanOrEqualTo(high),
                    $"Unordered resolved range: {rarity}/{row.stat}");
            }
        }
    }

    [Test]
    public void CloseAltars_ShareRolledStatsButFarAltarsDoNot()
    {
        GameObject leftObject = new("Left Altar");
        GameObject closeObject = new("Close Altar");
        GameObject farObject = new("Far Altar");
        try
        {
            AuthoredBonusWall left = leftObject.AddComponent<AuthoredBonusWall>();
            AuthoredBonusWall close = closeObject.AddComponent<AuthoredBonusWall>();
            AuthoredBonusWall far = farObject.AddComponent<AuthoredBonusWall>();
            leftObject.transform.position = Vector3.zero;
            closeObject.transform.position = new Vector3(5f, 20f, 0f);
            farObject.transform.position = new Vector3(30f, 0f, 0f);
            left.CommitRoll("att");
            far.CommitRoll("hp");

            HashSet<string> nearby = close.CollectNearbyRolledStats();

            Assert.That(nearby, Contains.Item("att"));
            Assert.That(nearby, Does.Not.Contain("hp"));
        }
        finally
        {
            Object.DestroyImmediate(leftObject);
            Object.DestroyImmediate(closeObject);
            Object.DestroyImmediate(farObject);
        }
    }

    [Test]
    public void LateOnEnable_DoesNotEraseCommittedStatBeforeNextRoll()
    {
        GameObject altarObject = new("Enabled Altar");
        try
        {
            AuthoredBonusWall altar = altarObject.AddComponent<AuthoredBonusWall>();
            altar.CommitRoll("missileDistance");
            MethodInfo onEnable = typeof(AuthoredBonusWall).GetMethod(
                "OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onEnable, Is.Not.Null);

            onEnable.Invoke(altar, null);

            Assert.That(altar.RolledStat, Is.EqualTo("missileDistance"));

            altar.BeginRoll();

            Assert.That(altar.RolledStat, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(altarObject);
        }
    }

    private static BonusRow FindRow(string rarity, string stat)
    {
        foreach (BonusRow row in BonusTables.GetAll(rarity))
        {
            if (row.stat == stat)
                return row;
        }

        Assert.Fail($"Missing bonus row. rarity={rarity} stat={stat}");
        return default;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string value)
    {
        GameObject textObject = new(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        return text;
    }
}
