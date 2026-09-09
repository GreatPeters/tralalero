using System.Collections.Generic;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class BonusWallCooldownTests
{
    private readonly List<GameObject> objects = new();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private PlayerScript player;
    private Collider playerCollider;
    private GameObject Make(string name) { var go = new GameObject(name); objects.Add(go); return go; }

    [SetUp]
    public void Setup()
    {
        var go = Make("Player"); go.tag = "Player";
        playerCollider = go.AddComponent<BoxCollider>();
        player = go.AddComponent<PlayerScript>(); player.currentHealth = 100;
    }
    [TearDown]
    public void Cleanup() { foreach (var go in objects) if (go != null) Object.DestroyImmediate(go); objects.Clear(); }
    private WallScript Wall(string name)
    {
        var go = Make(name); go.AddComponent<BoxCollider>(); go.AddComponent<RuntimeBonusWall>();
        var wall = go.AddComponent<WallScript>();
        wall.wallType = WallType.BuffWall; wall.buffType = BuffType.hp_normal;
        typeof(WallScript).GetField("playerScript", Private).SetValue(wall, player);
        typeof(WallScript).GetField("bonusValue", Private).SetValue(wall, 10f);
        return wall;
    }
    private void Touch(WallScript wall) => typeof(WallScript).GetMethod("OnTriggerEnter", Private).Invoke(wall, new object[] { playerCollider });

    [TestCase(0f, false)]
    [TestCase(1.5f, false)]
    [TestCase(1.99f, false)]
    [TestCase(2f, true)]
    public void AnotherWall_IsBlockedForTwoSeconds(float elapsed, bool allowed)
    {
        var wall = Wall("Another drop");
        player.lastWallTouchTime = Time.time - elapsed;
        Touch(wall);
        Assert.That(player.currentHealth, Is.EqualTo(allowed ? 110 : 100));
        Assert.That(wall.gameObject.activeSelf, Is.EqualTo(!allowed));
    }

    [Test]
    public void NewPlayerAndRestart_CanCollectImmediately()
    {
        Assert.That(player.lastWallTouchTime, Is.LessThanOrEqualTo(Time.time - 2));
        Touch(Wall("First")); Assert.That(player.currentHealth, Is.EqualTo(110));
        Touch(Wall("Simultaneous drop")); Assert.That(player.currentHealth, Is.EqualTo(110));
        player.ResetState();
        Touch(Wall("After restart")); Assert.That(player.currentHealth, Is.EqualTo(player.originalHealth + 10));
    }

    [Test]
    public void BlockedPair_DoesNotClaimOrDisableEitherChoice()
    {
        var leftWall = Wall("Left"); var rightWall = Wall("Right");
        var left = leftWall.gameObject.AddComponent<AuthoredBonusWall>();
        var right = rightWall.gameObject.AddComponent<AuthoredBonusWall>();
        var pair = left.gameObject.AddComponent<BonusWallChoicePair>(); pair.Configure(left, right);
        foreach (var wall in new[] { leftWall, rightWall })
            typeof(WallScript).GetField("hasSelectedBonusRow", Private).SetValue(wall, true);
        player.lastWallTouchTime = Time.time - 1.5f;
        Touch(leftWall);
        Assert.That(pair.Selected, Is.Null);
        Assert.That(left.gameObject.activeSelf && right.gameObject.activeSelf, Is.True);
        player.lastWallTouchTime = Time.time - 2;
        Touch(rightWall);
        Assert.That(pair.Selected, Is.EqualTo(right));
        Assert.That(left.gameObject.activeSelf, Is.False);
    }
}
