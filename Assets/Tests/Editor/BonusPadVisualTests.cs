using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class BonusPadVisualTests
{
    [Test]
    public void PadWidthShrinksForTightPairsButStaysReadable()
    {
        Assert.That(BonusPadVisual.WidthForSpacing(99), Is.EqualTo(BonusPadVisual.DesignWidth));
        Assert.That(BonusPadVisual.WidthForSpacing(3.8f), Is.EqualTo(3.8f * .68f).Within(.001f)); // Noryangjin SR18 pairs
        Assert.That(BonusPadVisual.WidthForSpacing(1f), Is.EqualTo(BonusPadVisual.MinWidth));
    }

    [Test]
    public void EachBonusFamilyHasItsOwnPadColour()
    {
        var colours = new[] { BuffType.attPer_normal, BuffType.hp_normal, BuffType.attackSpeed_normal, BuffType.missileDistance_normal, BuffType.tungtung_rare, BuffType.boombar_rare }
            .Select(BonusTalismanPresentation.PadColor).ToArray();
        Assert.That(colours.Distinct().Count(), Is.EqualTo(colours.Length));
        Assert.That(BonusTalismanPresentation.PadColor(BuffType.att_normmal), Is.EqualTo(BonusTalismanPresentation.PadColor(BuffType.attPer_unique)));
    }

    [Test]
    public void HexagonKeepsAFlatSideTowardThePlayer()
    {
        var band = BonusPadVisual.BandMesh().bounds;
        // Flat to flat along Z (2.6 m), corner to corner along X (3.0 m).
        Assert.That(band.size.z, Is.LessThan(band.size.x));
        Assert.That(band.size.z, Is.EqualTo(BonusPadVisual.DesignWidth * 1.012f).Within(.02f));
    }

    [Test]
    public void PadPrefabCarriesBothMeshyBasesAndMaterials()
    {
        var pad = Resources.Load<BonusPadVisual>(BonusPadVisual.ResourcePath);
        Assert.That(pad, Is.Not.Null);
        Assert.That(pad.transform.Find("NormalBase"), Is.Not.Null);
        Assert.That(pad.transform.Find("CrackedBase"), Is.Not.Null);
        Assert.That(pad.GetComponentsInChildren<Collider>(true), Is.Empty);
    }
}
