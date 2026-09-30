using NUnit.Framework;
using UnityEngine;

public sealed class NoryangjinDebugReviewTests
{
    [TestCase(0,4)] [TestCase(12,20)]
    public void HighCargoDropsCoinsWithinGroundPickupHeight(float floor,float cargoHeight)
    {
        var go=new GameObject("Wall");
        try
        {
            go.transform.position=Vector3.up*floor;var block=go.AddComponent<NoryangjinBreakable>();
            var point=block.CoinDropPosition(new Vector3(3,cargoHeight,7));
            Assert.That(point.x,Is.EqualTo(3));Assert.That(point.z,Is.EqualTo(7));
            Assert.That(point.y+1.1f+.28f-floor,Is.LessThan(2.5f),"CoinPickup adds1.1m and bobs0.28m; the pickup must remain reachable.");
        }
        finally{Object.DestroyImmediate(go);}
    }
    [TestCase(0,1,false,true)]
    [TestCase(12,1,false,false)]
    [TestCase(0,-1,false,false)]
    [TestCase(0,1,true,false)]
    public void MarketVisibilityRequiresCorrectFloorHeadingAndBranch(float y,float direction,bool outside,bool expected)
        =>Assert.That(NoryangjinInteriorDetailVisibility.IsInside(new Vector3(0,y,30),new Vector3(0,0,direction),outside),Is.EqualTo(expected));
    [TestCase(15)] [TestCase(1)] [TestCase(.01f)]
    public void PositiveShutterTimeKeepsSharkClearance(float remaining)
        =>Assert.That(NoryangjinShutterEvent.PanelLift(remaining,30,4.4f,3.6f),Is.GreaterThanOrEqualTo(3.6f));
    [Test]
    public void ExpiredShutterHasNoOpening()=>Assert.That(NoryangjinShutterEvent.PanelLift(0,30,4.4f,3.6f),Is.Zero);
    [TestCase(2.5f,true)] [TestCase(2.1f,false)] [TestCase(0,false)] [TestCase(1.4f,false)]
    public void HoseWarnsBeforeTheNextSprayWithoutChangingTheActiveWindow(float phase,bool expected)
        =>Assert.That(NoryangjinHoseEvent.WarningPhase(phase,0,2.8f,1.5f,.45f),Is.EqualTo(expected));
}
