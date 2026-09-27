using NUnit.Framework;
using UnityEngine;

public sealed class HighwayChapter2RulesTests
{
    [Test]
    public void SeparatedForkOpensSmoothlyThenHoldsItsOwnCarriageway()
    {
        Assert.That(HighwayRoute.SeparatedBranchOffset(468,468,1170,36,180),Is.Zero);
        Assert.That(HighwayRoute.SeparatedBranchOffset(558,468,1170,36,180),Is.EqualTo(18));
        Assert.That(HighwayRoute.SeparatedBranchOffset(648,468,1170,36,180),Is.EqualTo(36));
        Assert.That(HighwayRoute.SeparatedBranchOffset(900,468,1170,36,180),Is.EqualTo(36));
        Assert.That(HighwayRoute.SeparatedBranchOffset(1170,468,1170,36,180),Is.Zero);
        Assert.That(HighwayRoute.SeparatedBranchOffset(468.1f,468,1170,36,180),Is.LessThan(.0001f));
    }
    [Test]
    public void ForkPaintKeepsBranchLinesOutsideTheSharedCarriageway()
    {
        Assert.That(HighwayChapter2Rules.ShowForkPaint(true, 4.4f, 8, 6.6f, false), Is.False);
        Assert.That(HighwayChapter2Rules.ShowForkPaint(true, 7, 8, 6.6f, false), Is.True);
        Assert.That(HighwayChapter2Rules.ShowForkPaint(false, 6.6f, 8, 6.6f, true), Is.False);
        Assert.That(HighwayChapter2Rules.ShowForkPaint(false, 6.6f, 16, 6.6f, true), Is.True);
        Assert.That(HighwayChapter2Rules.ShowForkPaint(false, 2.2f, 8, 6.6f, false), Is.True);
    }
    [Test]
    public void SlowRoadExchangesRemainingHealthAndFastRoadUsesMaxHealthFraction()
    {
        Assert.That(HighwayChapter2Rules.ContactDamage(17, 500, false, .25f), Is.EqualTo(17));
        Assert.That(HighwayChapter2Rules.ContactDamage(17, 500, true, .25f), Is.EqualTo(125));
        Assert.That(HighwayChapter2Rules.ContactDamage(-8, 500, false, .25f), Is.Zero);
    }

    [Test]
    public void FollowingBrakesWithoutReversingNorth()
    {
        Assert.That(HighwayChapter2Rules.FollowingDistance(80, 100, 85, 5, 4, 3), Is.EqualTo(97));
        Assert.That(HighwayChapter2Rules.FollowingDistance(95, 96, 94, 5, 4, 3), Is.EqualTo(96));
    }

    [Test]
    public void LongitudinalFollowingGapDoesNotOccupyAnAdjacentLane()
    {
        Assert.That(HighwayChapter2Rules.Overlaps(100, 0, 5, 1.7f, 100, 4.4f, 5, 1.7f, 3, .1f), Is.False);
        Assert.That(HighwayChapter2Rules.Overlaps(100, 0, 5, 1.7f, 109, 0, 5, 1.7f, 3, .1f), Is.True);
    }

    [Test]
    public void OrdinaryThreeLaneWallIsRejectedIncludingAWideBus()
    {
        var twoLanes = new[] { new Vector2(-4.4f, 1.7f), new Vector2(0, 1.7f) };
        Assert.That(HighwayChapter2Rules.LeavesPassage(4.4f, 1.7f, twoLanes, 4.4f, .48f), Is.False);
        Assert.That(HighwayChapter2Rules.LeavesPassage(2.2f, 3.8f, new Vector2[0], 4.4f, .48f), Is.True);
        Assert.That(HighwayChapter2Rules.LeavesPassage(2.2f, 3.8f, new[] { new Vector2(-4.4f, 1.7f) }, 4.4f, .48f), Is.False);
    }

    [TestCase(0, 0)]
    [TestCase(.19999f, 0)]
    [TestCase(.20f, 1)]
    [TestCase(.74999f, 1)]
    [TestCase(.75f, 2)]
    [TestCase(.94999f, 2)]
    [TestCase(.95f, 3)]
    [TestCase(1, 3)]
    public void MysteryProbabilitiesHaveExactBoundaries(float roll, int result)
        => Assert.That(HighwayChapter2Rules.BonusOutcome(roll, 20, 55, 20, 5), Is.EqualTo(result));

    [Test]
    public void HazardsRespectTheTwentyThreeMetreSeparation()
    {
        Assert.That(HighwayChapter2Rules.ConflictsWithWindow(177, 200, 220, 23), Is.True);
        Assert.That(HighwayChapter2Rules.ConflictsWithWindow(176.9f, 200, 220, 23), Is.False);
        Assert.That(HighwayChapter2Rules.ConflictsWithWindow(243, 200, 220, 23), Is.True);
    }

    [Test]
    public void PopupForkChoiceCannotBeOverwrittenByLateralPosition()
    {
        var go = new GameObject("Explicit Highway fork test");
        try
        {
            var route = go.AddComponent<HighwayRoute>(); route.popupBranches = true;
            route.forks = new[] { new HighwayRoute.Fork { start = 100, end = 200, offset = 36 } };
            route.SelectFork(0, true); route.SelectFork(0, false);
            Assert.That(route.forks[0].decided, Is.True);
            Assert.That(route.forks[0].bypass, Is.True);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
