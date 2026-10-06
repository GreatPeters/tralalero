using NUnit.Framework;
using UnityEngine;

// The single highway log used to spin about its bottom-edge pivot, sinking up to ~1.8 m
// into the road every half turn. These checks pin the centre-axis roll.
public sealed class HighwayRollingLogTests
{
    // Measured in Play: BoxCollider (3.4, 3.3, 7.6) centred at y 1.65, lossy scale 0.67.
    const float Radius = 1.105f, HalfLength = 2.551f;
    static readonly Vector3 PivotToCentre = new Vector3(0, Radius, 0);
    static readonly Vector3 Road = new Vector3(3, 5, -7), Forward = new Vector3(.2f, 0, 1).normalized;

    static float LowestPoint(float t, float bounce, out Vector3 centre)
    {
        HighwayChapter2Rules.RollingLogPose(Road, Forward, 1.3f, t, bounce, 12, Radius, HalfLength, PivotToCentre, out var pivot, out var rotation, out centre);
        float lowest = float.PositiveInfinity;
        for (int a = 0; a < 48; a++)
        for (int z = -1; z <= 1; z++)
        {
            float phi = a * Mathf.PI * 2 / 48;
            var local = PivotToCentre + new Vector3(Mathf.Cos(phi) * Radius, Mathf.Sin(phi) * Radius, z * HalfLength);
            lowest = Mathf.Min(lowest, (pivot + rotation * local).y);
        }
        return lowest;
    }

    [Test]
    public void NoPartOfTheLogGoesBelowTheAsphaltAtAnyRollAngle()
    {
        float surface = Road.y + .12f, worst = float.PositiveInfinity;
        for (float t = 0; t <= 6; t += .005f) worst = Mathf.Min(worst, LowestPoint(t, 0, out _) - surface);
        Assert.That(worst, Is.GreaterThanOrEqualTo(-.0005f));
        Assert.That(worst, Is.LessThan(.06f), "resting bounces still touch the road");
    }

    [Test]
    public void LogRollsTowardThePlayerLikeAWheel()
    {
        const float t = 1.7f, dt = .01f;
        HighwayChapter2Rules.RollingLogPose(Road, Forward, 0, t, 0, 12, Radius, HalfLength, PivotToCentre, out var p0, out var r0, out var c0);
        HighwayChapter2Rules.RollingLogPose(Road, Forward, 0, t + dt, 0, 12, Radius, HalfLength, PivotToCentre, out var p1, out var r1, out var c1);
        // The material point at the top of the mid cross-section must swing in the travel direction (-forward).
        Vector3 top = PivotToCentre; float best = float.NegativeInfinity;
        for (int a = 0; a < 72; a++)
        {
            var local = PivotToCentre + new Vector3(Mathf.Cos(a * Mathf.PI / 36) * Radius, Mathf.Sin(a * Mathf.PI / 36) * Radius, 0);
            float y = (p0 + r0 * local).y; if (y > best) { best = y; top = local; }
        }
        var swing = (p1 + r1 * top - c1) - (p0 + r0 * top - c0);
        Assert.That(Vector3.Dot(swing, Forward), Is.LessThan(0));
        // Rolling without slipping: the top moves relative to the centre at about speed * dt.
        Assert.That(swing.magnitude, Is.EqualTo(12 * dt).Within(.03f));
    }
}
