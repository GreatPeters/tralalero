using NUnit.Framework;
using UnityEngine;

public sealed class Chapter45RulesTests
{
    [Test]
    public void CaptainRetainsDamageAndRequiresThreeVisibleOpeningsEvenWithHighDamage()
    {
        float health = 900;
        health = Chapter45Rules.CaptainHealthAfterHit(health, 900, 0, 10000, out int phase);
        Assert.That(health, Is.EqualTo(600).Within(.001)); Assert.That(phase, Is.EqualTo(1));
        health = Chapter45Rules.CaptainHealthAfterHit(health, 900, phase, 50, out phase);
        Assert.That(health, Is.EqualTo(550)); Assert.That(phase, Is.EqualTo(1));
        health = Chapter45Rules.CaptainHealthAfterHit(health, 900, phase, 10000, out phase);
        Assert.That(health, Is.EqualTo(300).Within(.001)); Assert.That(phase, Is.EqualTo(2));
        health = Chapter45Rules.CaptainHealthAfterHit(health, 900, phase, 10000, out phase);
        Assert.That(health, Is.Zero); Assert.That(phase, Is.EqualTo(2));
    }
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-10)]
    public void InvalidCaptainDamageNeverHealsOrAdvancesPhase(float damage)
    {
        Assert.That(Chapter45Rules.CaptainHealthAfterHit(250, 900, 2, damage, out int phase), Is.EqualTo(250));
        Assert.That(phase, Is.EqualTo(2));
    }
    [Test]
    public void LaunchDeckRejectsAnotherDeckEvenAtOverlappingHeight()
    {
        Assert.That(Chapter45Rules.SameDeck(1, 80, 2, 80), Is.False);
        Assert.That(Chapter45Rules.SameDeck(1, 80, 1, 82), Is.True);
        Assert.That(Chapter45Rules.SameDeck(1, 80, -1, 160), Is.False);
        Assert.That(Chapter45Rules.SameDeck(-1, 0, 2, 160), Is.True, "Earlier chapters retain their existing collision contract.");
    }
    [Test]
    public void LiftIsContinuousMonotonicAndLandsAtSixSeconds()
    {
        var start = new Vector3(3, .12f, 42); var end = new Vector3(3, 80.12f, 42);
        Vector3 previous = start;
        for (int i = 0; i <= 300; i++)
        {
            Vector3 position = Chapter45Rules.LiftPosition(start, end, i * .02f, 6);
            Assert.That(position.y, Is.GreaterThanOrEqualTo(previous.y));
            Assert.That(position.x, Is.EqualTo(3)); Assert.That(position.z, Is.EqualTo(42));
            Assert.That(position.y, Is.InRange(start.y, end.y)); previous = position;
        }
        Assert.That(Vector3.Distance(previous, end), Is.LessThan(.001));
    }
    [Test]
    public void PhysicalForkSeparatesChoicesAndMergesWithoutJumps()
    {
        Assert.That(Chapter45Route.BranchOffset(100, 100, 300, 18, 20), Is.Zero);
        Assert.That(Chapter45Route.BranchOffset(200, 100, 300, 18, 20), Is.EqualTo(18));
        Assert.That(Chapter45Route.BranchOffset(200, 100, 300, -18, 20), Is.EqualTo(-18));
        Assert.That(Chapter45Route.BranchOffset(300, 100, 300, 18, 20), Is.Zero.Within(.001));
    }
    [Test]
    public void HelperWaitsAtCurrentDeckEndAndDoesNotRunOntoNextFloor()
    {
        float distance = 520;
        for (int i = 0; i < 100; i++) distance = Chapter45Rules.HelperProgress(distance, 1, 520, 530);
        Assert.That(distance, Is.EqualTo(530));
        Assert.That(Chapter45Rules.HelperProgress(800, 1, 1000, 1120), Is.EqualTo(801), "A trailing helper keeps its real progress instead of teleporting to the player.");
        Assert.That(Chapter45Rules.HelperProgress(100, -1, 100, 530), Is.EqualTo(100));
    }
    [Test]
    public void SceneryVisibilityPreservesSupportColliderAndOriginalRendererState()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var renderer = root.GetComponent<Renderer>(); var collider = root.GetComponent<Collider>();
            renderer.forceRenderingOff = true;
            var group = root.AddComponent<Chapter45SceneryGroup>();
            group.SetVisible(false); group.Restore();
            Assert.That(collider.enabled, Is.True);
            Assert.That(root.activeSelf, Is.True);
            Assert.That(renderer.forceRenderingOff, Is.True);
        }
        finally { Object.DestroyImmediate(root); }
    }
    [Test]
    public void CombatDeckSamplingKeepsHeightAndHorizontalFireDirection()
    {
        var root = new GameObject("Route");
        try
        {
            var route = root.AddComponent<Chapter45Route>();
            route.segments = new[]
            {
                new Chapter45Route.Segment { start = Vector3.zero, end = new Vector3(0,0,100), floor = 0, length = 100 },
                new Chapter45Route.Segment { start = new Vector3(0,80,100), end = new Vector3(0,80,0), floor = 1, length = 100 }
            };
            route.SampleSegment(0, 100, out var firstLanding, out _);
            route.SampleSegment(1, 100, out var nextLanding, out var direction);
            Assert.That(nextLanding - firstLanding, Is.EqualTo(Vector3.up * 80));
            Assert.That(direction, Is.EqualTo(Vector3.back));
            route.Sample(150, out var point, out direction);
            Assert.That(point.y, Is.EqualTo(80)); Assert.That(direction.y, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
