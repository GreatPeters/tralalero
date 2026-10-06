using NUnit.Framework;
using UnityEngine;

public sealed class Chapter4StreetLifeTests
{
    [Test]
    public void WalkingNeverEntersTheCombatCorridorOrChangesSupportHeight()
    {
        foreach (float side in new[] { -9.2f, 9.2f })
        {
            var origin = new Vector3(side, .12f, 30);
            for (int frame = 0; frame < 1200; frame++)
            {
                var position = Chapter4StreetLife.Position(origin, frame * .1f, 2.7f, 4);
                Assert.AreEqual(origin.x, position.x);
                Assert.AreEqual(origin.y, position.y);
                Assert.That(position.z, Is.InRange(26f, 34f));
            }
        }
    }

    [Test]
    public void ResettingTheClockRestoresTheSamePoseAndNegativeTravelStaysStill()
    {
        var origin = new Vector3(10, .12f, 5);
        var initial = Chapter4StreetLife.Position(origin, 0, 1.3f, 4);
        Chapter4StreetLife.Position(origin, 500, 1.3f, 4);
        Assert.AreEqual(initial, Chapter4StreetLife.Position(origin, 0, 1.3f, 4));
        Assert.AreEqual(origin, Chapter4StreetLife.Position(origin, 50, 1.3f, -4));
    }
}
