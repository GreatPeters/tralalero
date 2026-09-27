using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class RestStopEnemyBehaviorTests
{
    [Test] public void Shield_ReducesOnlyFrontalDamage()
    {
        var facing = Vector3.forward;
        Assert.That(RestStopEnemyBehavior.ShieldMultiplier(facing, new Vector3(0, 0, 5), 60, .3f), Is.EqualTo(.3f));
        Assert.That(RestStopEnemyBehavior.ShieldMultiplier(facing, new Vector3(4, 0, 4), 60, .3f), Is.EqualTo(.3f));
        Assert.That(RestStopEnemyBehavior.ShieldMultiplier(facing, new Vector3(5, 0, 0), 60, .3f), Is.EqualTo(1f));
        Assert.That(RestStopEnemyBehavior.ShieldMultiplier(facing, new Vector3(0, 0, -5), 60, .3f), Is.EqualTo(1f));
        Assert.That(RestStopEnemyBehavior.ShieldMultiplier(facing, Vector3.zero, 60, .3f), Is.EqualTo(1f));
    }

    [Test] public void Charger_StandsStillDuringTelegraphThenCharges()
    {
        Assert.That(RestStopEnemyBehavior.ChargeSpeed(3, 0, .8f, 2.6f), Is.EqualTo(0));
        Assert.That(RestStopEnemyBehavior.ChargeSpeed(3, .79f, .8f, 2.6f), Is.EqualTo(0));
        Assert.That(RestStopEnemyBehavior.ChargeSpeed(3, .8f, .8f, 2.6f), Is.EqualTo(7.8f).Within(1e-4f));
    }
}

public sealed class RestStopConeContactTests
{
    [NUnit.Framework.Test] public void DroppedConeCostsAShareOfHealthNotALife()
    {
        NUnit.Framework.Assert.That(RestStopConeContact.Damage(1000, .12f), NUnit.Framework.Is.EqualTo(120).Within(1e-3f));
        NUnit.Framework.Assert.That(RestStopConeContact.Damage(1000, 2f), NUnit.Framework.Is.EqualTo(1000));
        NUnit.Framework.Assert.That(RestStopConeContact.Damage(-5, .12f), NUnit.Framework.Is.EqualTo(0));
    }
}

public sealed class RouteArcDriverTests
{
    [NUnit.Framework.Test] public void RightArcEndsOffsetByRadiusAndFacingEast()
    {
        IndianOceanAssets.ShooterSurvival.RouteArcDriver.Evaluate(new UnityEngine.Vector3(0, 0, -20), 0, 20, 90, UnityEngine.Mathf.PI * 10, out var end, out var forward);
        NUnit.Framework.Assert.That(end.x, NUnit.Framework.Is.EqualTo(20).Within(1e-3f));
        NUnit.Framework.Assert.That(end.z, NUnit.Framework.Is.EqualTo(0).Within(1e-3f));
        NUnit.Framework.Assert.That(forward.x, NUnit.Framework.Is.EqualTo(1).Within(1e-3f));
        IndianOceanAssets.ShooterSurvival.RouteArcDriver.Evaluate(new UnityEngine.Vector3(0, 0, -20), 0, 20, 90, UnityEngine.Mathf.PI * 5, out var mid, out _);
        NUnit.Framework.Assert.That((mid - new UnityEngine.Vector3(20, 0, -20)).magnitude, NUnit.Framework.Is.EqualTo(20).Within(1e-3f));
    }
}

public sealed class RoadGimmickRulesTests
{
    [NUnit.Framework.Test] public void RowCarsArriveWithTheShark()
    {
        float spawn = OncomingLaneTraffic.SyncedSpawnDistance(500, 440, 7.8f, 17f);
        float t = (500 - 440) / 7.8f;
        NUnit.Framework.Assert.That(spawn - 17f * t, NUnit.Framework.Is.EqualTo(500).Within(1e-3f));
        NUnit.Framework.Assert.That(OncomingLaneTraffic.InRanges(new[] { new UnityEngine.Vector2(10, 20), new UnityEngine.Vector2(40, 50) }, 45), NUnit.Framework.Is.True);
        NUnit.Framework.Assert.That(OncomingLaneTraffic.InRanges(new[] { new UnityEngine.Vector2(10, 20) }, 25), NUnit.Framework.Is.False);
    }
    [NUnit.Framework.Test] public void SpawnedCarsKeepAGapInTheirLane()
    {
        var cars = new System.Collections.Generic.List<(float d, float lane, float half)> { (100, 4.2f, 3.3f) };
        NUnit.Framework.Assert.That(OncomingLaneTraffic.GapFree(104, 3.3f, 4.2f, cars, 2.5f), NUnit.Framework.Is.False, "overlaps the car ahead");
        NUnit.Framework.Assert.That(OncomingLaneTraffic.GapFree(104, 3.3f, -4.2f, cars, 2.5f), NUnit.Framework.Is.True, "other lane is independent");
        NUnit.Framework.Assert.That(OncomingLaneTraffic.GapFree(110, 3.3f, 4.2f, cars, 2.5f), NUnit.Framework.Is.True, "clear of the car ahead");
    }
    [NUnit.Framework.Test] public void LogsWaitForTheirStaggerThenRollBack()
    {
        NUnit.Framework.Assert.That(LogTruckSpill.LogDistance(100, .2f, .45f, 13), NUnit.Framework.Is.EqualTo(100));
        NUnit.Framework.Assert.That(LogTruckSpill.LogDistance(100, 1.45f, .45f, 13), NUnit.Framework.Is.EqualTo(87).Within(1e-3f));
    }
    [NUnit.Framework.Test] public void DeerReachesTheSharkLaneAsTheSharkArrives()
    {
        // 11 m from the right shoulder to lane -1 while the shark covers 39 m at 7.8 m/s (5 s): 3.0 m/s, clamped to 3.2.
        NUnit.Framework.Assert.That(WaterDeerCrossing.CrossingSpeed(11, -1, 39, 7.8f, 3.2f, 5.5f), NUnit.Framework.Is.EqualTo(3.2f).Within(1e-4f));
        NUnit.Framework.Assert.That(WaterDeerCrossing.CrossingSpeed(11, -4, 23.4f, 7.8f, 3.2f, 5.5f), NUnit.Framework.Is.EqualTo(5f).Within(1e-3f));
        NUnit.Framework.Assert.That(WaterDeerCrossing.CrossingSpeed(11, -4, 2, 7.8f, 3.2f, 5.5f), NUnit.Framework.Is.EqualTo(5.5f));
    }
    [NUnit.Framework.Test] public void DeerHopTouchesGroundBetweenBounds()
    {
        NUnit.Framework.Assert.That(DeerHop.Height(0, .7f), NUnit.Framework.Is.EqualTo(0).Within(1e-4f));
        NUnit.Framework.Assert.That(DeerHop.Height(.5f, .7f), NUnit.Framework.Is.EqualTo(.7f).Within(1e-4f));
        NUnit.Framework.Assert.That(DeerHop.Height(1f, .7f), NUnit.Framework.Is.EqualTo(0).Within(1e-4f));
    }
}
