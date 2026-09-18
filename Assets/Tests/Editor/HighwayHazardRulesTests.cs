using NUnit.Framework;

public sealed class HighwayHazardRulesTests
{
    [Test] public void TollCycle_AlwaysOffersExactlyOneLane()
    {
        for (float time=0;time<90;time+=.1f)
        {
            int open=HighwayHazard.OpenLane(time,6);
            Assert.That(open,Is.InRange(0,2));
        }
        Assert.That(HighwayHazard.OpenLane(0,6),Is.EqualTo(0));
        Assert.That(HighwayHazard.OpenLane(6,6),Is.EqualTo(1));
        Assert.That(HighwayHazard.OpenLane(12,6),Is.EqualTo(2));
        Assert.That(HighwayHazard.OpenLane(18,6),Is.EqualTo(0));
    }
}
