using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerHealingLifecycleTests
{
    [TestCase(BuffType.HealthBoost)]
    [TestCase(BuffType.hp_normal)]
    [TestCase(BuffType.hp_unique)]
    public void AHealthPickupCannotConsumeItsWallOrReviveAnAlreadyDeadPlayer(BuffType type)
    {
        var target=new GameObject("Dead pickup target");var reward=new GameObject("Concurrent health wall");
        try
        {
            target.tag="Player";var collider=target.AddComponent<CapsuleCollider>();
            var player=target.AddComponent<PlayerScript>();player.RefreshUpgradeStats();player.lastWallTouchTime=-10;
            player.ApplyDamage(player.currentHealth,PlayerDamageCause.Roadblock);
            var wall=reward.AddComponent<WallScript>();wall.wallType=WallType.BuffWall;wall.buffType=type;wall.healthBoostAmt=100;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(WallScript).GetField("playerScript",flags).SetValue(wall,player);
            typeof(WallScript).GetMethod("OnTriggerEnter",flags).Invoke(wall,new object[]{collider});
            Assert.That(player.currentHealth,Is.Zero);Assert.That(player.lastWallTouchTime,Is.EqualTo(-10));
            Assert.That(reward.activeSelf,Is.True);
        }
        finally{Object.DestroyImmediate(reward);Object.DestroyImmediate(target);}
    }
    [Test]
    public void FatalDamageCannotBeUndoneByHealingOrAConcurrentHealthBonus()
    {
        var go=new GameObject("Healing lifecycle");
        try
        {
            var p=go.AddComponent<PlayerScript>();p.RefreshUpgradeStats();float capacity=p.MaxHealth;
            p.ApplyDamage(capacity,PlayerDamageCause.Roadblock);
            Assert.That(p.Heal(100),Is.Zero);
            p.ApplyRunHealthBonus(20,false);p.ApplyRunHealthBonus(20,true);p.ApplyHarnessHealthDelta(100);
            Assert.That(p.currentHealth,Is.Zero);Assert.That(p.MaxHealth,Is.EqualTo(capacity));
            Assert.That(p.LastDamageCause,Is.EqualTo(PlayerDamageCause.Roadblock));
            Assert.That(HighwayRoute.RecoveredHealth(0,capacity,.1f),Is.Zero);
            p.ResetState();p.ApplyDamage(10,PlayerDamageCause.Crate);
            Assert.That(p.Heal(5),Is.EqualTo(5));Assert.That(p.currentHealth,Is.EqualTo(capacity-5));
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test]
    public void HealingCapsItsActualGrantAndPreservesOverheal()
    {
        var go=new GameObject("Healing amount");
        try
        {
            var p=go.AddComponent<PlayerScript>();p.RefreshUpgradeStats();float cap=p.MaxHealth;int notifications=0;p.DamageTaken+=(_,_)=>notifications++;
            p.ApplyDamage(15,PlayerDamageCause.Crate);Assert.That(p.Heal(100),Is.EqualTo(15));Assert.That(p.currentHealth,Is.EqualTo(cap));
            p.currentHealth=cap+30;p.UpdateHealth();Assert.That(p.Heal(10),Is.Zero);Assert.That(p.currentHealth,Is.EqualTo(cap+30));
            Assert.That(p.Heal(float.PositiveInfinity),Is.Zero);Assert.That(p.Heal(float.NaN),Is.Zero);Assert.That(p.Heal(-5),Is.Zero);
            Assert.That(notifications,Is.EqualTo(1));
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test]
    public void ADeadPlayerCannotWinBeforeTheGameOverUpdateRuns()
    {
        var go=new GameObject("Finish-line priority");bool before=CanvasScript.isGameOver;
        try
        {
            var p=go.AddComponent<PlayerScript>();p.RefreshUpgradeStats();p.ApplyDamage(p.currentHealth,PlayerDamageCause.EnemyContact);
            var canvas=go.AddComponent<CanvasScript>();typeof(CanvasScript).GetField("playerScript",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(canvas,p);
            var chapter=go.AddComponent<ChapterProgression>();CanvasScript.isGameOver=false;
            Assert.DoesNotThrow(()=>canvas.YouWin());Assert.That(chapter.Completed,Is.False);
        }
        finally{CanvasScript.isGameOver=before;Object.DestroyImmediate(go);}
    }
}
