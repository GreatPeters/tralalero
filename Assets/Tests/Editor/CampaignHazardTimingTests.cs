using NUnit.Framework;
using UnityEngine;

public sealed class CampaignHazardTimingTests
{
    [TestCase(1f)] [TestCase(6f)] [TestCase(7f)]
    public void AnimatedGatesAlwaysLeaveAtLeastOnePhysicalPassage(float cycle)
    {
        var angles = new[]{82f, 0f, 0f};
        for(float t=0;t<cycle*6;t+=.02f)
        {
            int passable=0;
            for(int lane=0;lane<3;lane++)
            {
                angles[lane]=Mathf.MoveTowardsAngle(angles[lane],HighwayHazard.GateTargetAngle(t,cycle,lane),HighwayHazard.GateAngularSpeed*.02f);
                if(HighwayHazard.GatePassable(angles[lane]))passable++;
            }
            Assert.That(passable,Is.GreaterThanOrEqualTo(1),"No lane at "+t);
        }
    }
    [Test]
    public void NextGateOpensDuringWarningWhileCurrentGateStaysOpen()
    {
        Assert.That(HighwayHazard.GateTargetAngle(5,6,1),Is.Zero);
        Assert.That(HighwayHazard.GateTargetAngle(5.2f,6,1),Is.EqualTo(82));
        Assert.That(HighwayHazard.GateTargetAngle(5.2f,6,0),Is.EqualTo(82));
        Assert.That(HighwayHazard.GatePassable(30),Is.False);
        Assert.That(HighwayHazard.GatePassable(80),Is.True);
    }
}
