using System.Reflection;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ReviewReadabilityTests
{
    [Test]
    public void FatalCauseRetainsTheActualKillingHitAndResetClearsIt()
    {
        var root=new GameObject("Damage record player");
        try
        {
            var player=root.AddComponent<PlayerScript>();player.currentHealth=500;
            Assert.That(player.ApplyDamage(100,PlayerDamageCause.Crate),Is.EqualTo(100));
            Assert.That(player.LastDamageWasFatal,Is.False);Assert.That(player.currentHealth,Is.EqualTo(400));
            Assert.That(player.ApplyDamage(900,PlayerDamageCause.EnemyContact),Is.EqualTo(400));
            Assert.That(player.LastDamageCause,Is.EqualTo(PlayerDamageCause.EnemyContact));Assert.That(player.LastDamageAmount,Is.EqualTo(400));Assert.That(player.LastDamageWasFatal,Is.True);
            player.ApplyDamage(20,PlayerDamageCause.GuardShot);Assert.That(player.LastDamageCause,Is.EqualTo(PlayerDamageCause.EnemyContact));
            player.ResetState();Assert.That(player.LastDamageCause,Is.EqualTo(PlayerDamageCause.None));Assert.That(player.LastDamageWasFatal,Is.False);
        }
        finally{Object.DestroyImmediate(root);}
    }
    [TestCase(0,500,500,false)] [TestCase(40,500,500,false)] [TestCase(373,715,1006,true)] [TestCase(600,500,500,true)]
    public void ContactWarningUsesLiveHealth(float enemy,float player,float maximum,bool expected)
        =>Assert.That(EnemyContactWarning.IsDangerous(enemy,player,maximum),Is.EqualTo(expected));
    [Test]
    public void WarningPredictsLethalityInsteadOfJustShowingLargeNumbers()
    {
        Assert.That(EnemyContactWarning.Label(500,500),Does.Contain("사망"));
        Assert.That(EnemyContactWarning.Label(373,715),Does.Contain("373"));
        Assert.That(PlayerDamageCauseText.Label(PlayerDamageCause.Crate),Is.EqualTo("뚱보 상자"));
        Assert.That(PlayerDamageCauseText.Label(PlayerDamageCause.Traffic),Is.EqualTo("차량과 충돌"));
    }
    [Test]
    public void HighwayHandProportionsDoNotCompoundAcrossFrames()
    {
        var root=new GameObject("Highway model");var hand=new GameObject("Hand.L");hand.transform.SetParent(root.transform);
        try
        {
            var shape=root.AddComponent<HighwayCharacterProportions>();shape.leftHand=hand.transform;
            var update=typeof(HighwayCharacterProportions).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            update.Invoke(shape,null);update.Invoke(shape,null);Assert.That(hand.transform.localScale.x,Is.EqualTo(.82f).Within(.001f));
        }
        finally{Object.DestroyImmediate(root);}
    }
    [TestCase(ObstaclePattern.HighwayTraffic,PlayerDamageCause.Traffic)]
    [TestCase(ObstaclePattern.HighwayToll,PlayerDamageCause.TollGate)]
    [TestCase(ObstaclePattern.HighwayRoadblock,PlayerDamageCause.Roadblock)]
    public void HazardRecapIdentifiesVehicleAndBarrierSeparately(ObstaclePattern pattern,PlayerDamageCause cause)
        =>Assert.That(HighwayHazard.DamageCauseFor(pattern),Is.EqualTo(cause));
    [Test]
    public void AuthoredBoatPaddleIsNotReportedAsAGuardBullet()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Boat.prefab");
        try
        {
            var obstacle=root.GetComponentsInChildren<ObstacleStats>(true).First(o=>o.obstaclePattern==ObstaclePattern.Oldman);
            typeof(ObstacleStats).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obstacle,null);
            var paddle=obstacle.GetComponentInChildren<SimpleProjectile>();
            Assert.That(paddle.damageCause,Is.EqualTo(PlayerDamageCause.Paddle));
            Assert.That(paddle.damage,Is.EqualTo(obstacle.value));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
