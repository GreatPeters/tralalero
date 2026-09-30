using NUnit.Framework;
using UnityEngine;

public sealed class NoryangjinRevampMechanicsTests
{
    [TestCase(0, -1.5f, -1, true)]
    [TestCase(0, 1.8f, -1, false)]
    [TestCase(0, -100, -1, false)]
    [TestCase(0, 100, 1, false)]
    [TestCase(4, 0, 1, false)]
    public void HoseCannotHitAnotherRoadOrTheSafeHalf(float along, float lateral, int side, bool expected)
        => Assert.That(NoryangjinHoseEvent.IsInJet(along,lateral,2,.9f,side,2.7f),Is.EqualTo(expected));

    [Test]
    public void WaveResetPreservesTheAuthoredWidthAndLength()
    {
        var go = new GameObject("WaveFixture"); var visual = new GameObject("Curl");
        try
        {
            var e = go.AddComponent<NoryangjinWaveEvent>();
            visual.transform.localScale = new Vector3(-1,1,19.8f);
            e.waves = new[] {new NoryangjinWaveEvent.Wave {water=visual.transform}};
            e.ResetForRun(); visual.transform.localScale = Vector3.one * .1f; e.ResetForRun();
            Assert.That(visual.transform.localScale,Is.EqualTo(new Vector3(-1,1,19.8f)));
            Assert.That(visual.activeSelf,Is.False);
        }
        finally {Object.DestroyImmediate(visual);Object.DestroyImmediate(go);}
    }
    [TestCase(100, 30, 30)]
    [TestCase(8, 30, 8)]
    [TestCase(0, 30, 0)]
    [TestCase(-4, 30, 0)]
    [TestCase(10, -3, 0)]
    public void MerchantPenaltyNeverOverdrawsWallet(int wallet, int requested, int expected)
        => Assert.That(NoryangjinMerchant.CoinLoss(wallet, requested), Is.EqualTo(expected));

    [Test]
    public void PhysicalBranchesShareEndpointsAndRemainSeparatedInTheMarket()
    {
        var go = new GameObject("BranchFixture");
        try
        {
            go.transform.position = new Vector3(124.3f, 0, 45); go.transform.forward = Vector3.back;
            var branch = go.AddComponent<NoryangjinMarketBranch>();
            Assert.That(Vector3.Distance(branch.Point(0, false), branch.Point(0, true)), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(branch.Point(branch.length, false), branch.Point(branch.length, true)), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(branch.Point(branch.length / 2, false), branch.Point(branch.length / 2, true)), Is.EqualTo(branch.outsideOffset).Within(.001f));
            branch.Sample(0, true, out _, out var entry);
            branch.Sample(branch.length, true, out _, out var exit);
            Assert.That(Vector3.Dot(entry, Vector3.back), Is.GreaterThan(.99f));
            Assert.That(Vector3.Dot(exit, Vector3.back), Is.GreaterThan(.99f));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void BranchResetClearsSelectionMovementAndCompletion()
    {
        var go = new GameObject("BranchFixture");
        try
        {
            var branch = go.AddComponent<NoryangjinMarketBranch>();
            branch.ResetForRun(); branch.Choose(1);
            Assert.That(branch.Selected, Is.False, "A choice before the encounter must not select a route.");
            branch.ResetForRun();
            Assert.That(branch.Driving || branch.Complete || branch.Waiting, Is.False);
            Assert.That(branch.Distance, Is.Zero);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
