using NUnit.Framework;
using UnityEngine;

public sealed class HighwayThreeLaneTrafficTests
{
    [Test]
    public void ThirdSimultaneousLaneIsRejectedButFollowingSameLaneIsNotAnotherWall()
    {
        var reservations = new[] { new Vector2(-4.4f, 3), new Vector2(0, 3.4f) };
        Assert.That(OncomingLaneTraffic.KeepsEscapeLane(4.4f, 3.2f, reservations, 3, 1.5f), Is.False);
        Assert.That(OncomingLaneTraffic.KeepsEscapeLane(0, 3.2f, reservations, 3, 1.5f), Is.True);
    }

    [Test]
    public void SeparatedArrivalsLeaveTimeToChangeLane()
    {
        var reservations = new[] { new Vector2(-4.4f, 1), new Vector2(0, 3) };
        Assert.That(OncomingLaneTraffic.KeepsEscapeLane(4.4f, 5, reservations, 3, 1.5f), Is.True);
    }

    [Test]
    public void BoundaryArrivalStillReservesItsLane()
    {
        var reservations = new[] { new Vector2(-4.4f, 1.5f), new Vector2(0, 3) };
        Assert.That(OncomingLaneTraffic.KeepsEscapeLane(4.4f, 3, reservations, 3, 1.5f), Is.False);
    }

    [Test]
    public void MeetingPointIncludesBothSpeeds()
    {
        Assert.That(OncomingLaneTraffic.MeetingDistance(100, 195, 7.8f, 17), Is.EqualTo(129.879f).Within(.001f));
        Assert.That(OncomingLaneTraffic.MeetingDistance(100, 90, 7.8f, 17), Is.EqualTo(100));
        Assert.That(OncomingLaneTraffic.MeetingDistance(100, 195, 0, 17), Is.EqualTo(100));
    }

    [TestCase(480, 0, true)]
    [TestCase(500, 0, true)]
    [TestCase(509, 0, true)]
    [TestCase(510, 0, false)]
    [TestCase(470, 0, false)]
    [TestCase(500, -4.4f, false)]
    [TestCase(500, 4.4f, false)]
    public void EncounterPassageKeepsCentreClearButAllowsOuterTraffic(float distance, float lane, bool expected)
        => Assert.That(OncomingLaneTraffic.CrossesCombatPassage(distance, lane, 500, 0, 1.85f, 1.9f), Is.EqualTo(expected));

    [Test]
    public void HighwaySpillTruckApproachesWhileLegacyDirectionRemainsAvailable()
    {
        Assert.That(LogTruckSpill.AdvanceTruck(100, 2, 3.5f, true), Is.EqualTo(93));
        Assert.That(LogTruckSpill.AdvanceTruck(100, 2, 3.5f, false), Is.EqualTo(107));
        Assert.That(LogTruckSpill.AdvanceTruck(100, 0, 9, true), Is.EqualTo(100));
    }

    [Test]
    public void InactiveLongVehicleIsMeasuredBeforeItsFirstSpawn()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            go.transform.localScale = new Vector3(2.8f, 3, 14);
            go.SetActive(false);
            Assert.That(OncomingLaneTraffic.TemplateHalfLength(go), Is.EqualTo(7).Within(.001f));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [TestCase(150, -3.5f, -4.4f, true)]
    [TestCase(150, -3.5f, 0, false)]
    [TestCase(150, -3.5f, 4.4f, false)]
    [TestCase(69, -3.5f, -4.4f, false)]
    [TestCase(206, -3.5f, -4.4f, false)]
    [TestCase(205, -3.5f, -4.4f, true)]
    public void IncomingCarsDoNotCrossAnUnbrokenRoadblock(float distance, float obstacleLane, float carLane, bool expected)
        => Assert.That(OncomingLaneTraffic.SpawnCrossesRoadblock(100, 195, carLane, distance, obstacleLane, .8f, 1.9f), Is.EqualTo(expected));

    [Test]
    public void NewSceneTrafficClearsPriorSessionSuppression()
    {
        var go = new GameObject("Traffic lifecycle test");
        var traffic = go.AddComponent<OncomingLaneTraffic>();
        try
        {
            OncomingLaneTraffic.Suppress(1000);
            Assert.That(OncomingLaneTraffic.Suppressed, Is.True);
            typeof(OncomingLaneTraffic).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(traffic, null);
            Assert.That(OncomingLaneTraffic.Suppressed, Is.False, "A previous play session must not keep new traffic suppressed.");
        }
        finally { traffic.ResetForRun(); Object.DestroyImmediate(go); }
    }

    [Test]
    public void SubsystemResetClearsSuppressionAndOldBlockerReferences()
    {
        var blocker = new GameObject("Prior-session blocker");
        try
        {
            OncomingLaneTraffic.Suppress(1000);
            OncomingLaneTraffic.Blockers.Add(blocker.transform);
            typeof(OncomingLaneTraffic).GetMethod("ResetSession", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            Assert.That(OncomingLaneTraffic.Suppressed, Is.False);
            Assert.That(OncomingLaneTraffic.Blockers, Is.Empty);
        }
        finally { OncomingLaneTraffic.Blockers.Remove(blocker.transform); Object.DestroyImmediate(blocker); }
    }

    [Test]
    public void DisablingSpillClearsWarningPhaseBeforeReenable()
    {
        var go = new GameObject("Spill disable lifecycle test");
        var spill = go.AddComponent<LogTruckSpill>();
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var phase = typeof(LogTruckSpill).GetField("phase", flags);
        try
        {
            phase.SetValue(spill, System.Enum.Parse(phase.FieldType, "Warn"));
            typeof(LogTruckSpill).GetMethod("OnDisable", flags).Invoke(spill, null);
            Assert.That(phase.GetValue(spill).ToString(), Is.EqualTo("Idle"), "Re-enabling must not resume Warn with an already destroyed truck.");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
