#if UNITY_EDITOR
using System.Reflection;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class MobileCombatFeedbackTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [Test]
    public void StationaryThrowStaysConsumedUntilRunReset()
    {
        var root=new GameObject("One-shot FatMan");var target=new GameObject("Target");var prop=new GameObject("Box");prop.transform.SetParent(root.transform);
        try
        {
            var enemy=root.AddComponent<EnemyScript_space>();var player=target.AddComponent<PlayerScript>();var animator=root.AddComponent<Animator>();
            void Set(string name,object value)=>typeof(EnemyScript_space).GetField(name,Private).SetValue(enemy,value);
            Set("stationaryThrow",true);Set("hasThrown",true);Set("projectileReleased",true);Set("heldProjectile",prop.transform);Set("playerScript",player);Set("enemyAnimator",animator);
            TimeManager.isGameRunning=true;
            var update=typeof(EnemyScript_space).GetMethod("Update",Private);
            for(int i=0;i<300;i++)update.Invoke(enemy,null);
            Assert.That(enemy.CanBeginTriggeredFire,Is.False,"No cooldown may reset an already consumed throw.");
        }
        finally{TimeManager.isGameRunning=false;Object.DestroyImmediate(root);Object.DestroyImmediate(target);}
    }
    [Test]
    public void DisablingShooterDoesNotDiscardItsInFlightProjectile()
    {
        var root=new GameObject("Shooter");var shot=new GameObject("Detached shot");
        try
        {
            var enemy=root.AddComponent<EnemyScript_space>();
            var shots=(List<GameObject>)typeof(EnemyScript_space).GetField("launchedProjectiles",Private).GetValue(enemy);shots.Add(shot);
            typeof(EnemyScript_space).GetMethod("OnDisable",Private).Invoke(enemy,null);
            Assert.That(shots,Has.Count.EqualTo(1));Assert.That(shot.activeSelf,Is.True);
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(shot);}
    }
    [Test]
    public void ProjectileUsesAttackDamageAndIgnoresPlayerTaggedChildren()
    {
        var root=new GameObject("Player");var child=new GameObject("Invisible child");child.transform.SetParent(root.transform);var shot=new GameObject("Arrow2");
        try
        {
            root.tag=child.tag="Player";var player=root.AddComponent<PlayerScript>();player.currentHealth=500;
            var body=root.AddComponent<CapsuleCollider>();var childCollider=child.AddComponent<BoxCollider>();
            var projectile=shot.AddComponent<SimpleProjectile>();projectile.damage=137;
            var hit=typeof(SimpleProjectile).GetMethod("OnTriggerEnter",Private);
            hit.Invoke(projectile,new object[]{childCollider});Assert.That(player.currentHealth,Is.EqualTo(500));
            hit.Invoke(projectile,new object[]{body});Assert.That(player.currentHealth,Is.EqualTo(363));
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(shot);}
    }
    [Test]
    public void DistantArtworkCullingPreservesCollisionAndAuthoredHiddenState()
    {
        var root=GameObject.CreatePrimitive(PrimitiveType.Cube);var child=GameObject.CreatePrimitive(PrimitiveType.Cube);child.transform.SetParent(root.transform);
        var target=new GameObject("Distance reference");
        try
        {
            var visible=root.GetComponent<Renderer>();var hidden=child.GetComponent<Renderer>();hidden.forceRenderingOff=true;
            var budget=root.AddComponent<EnemyVisualDistance>();
            typeof(EnemyVisualDistance).GetMethod("Awake",Private).Invoke(budget,null);
            typeof(EnemyVisualDistance).GetField("player",Private).SetValue(budget,target.transform);
            void Refresh(){typeof(EnemyVisualDistance).GetField("nextCheck",Private).SetValue(budget,0f);typeof(EnemyVisualDistance).GetMethod("Update",Private).Invoke(budget,null);}
            target.transform.position=Vector3.forward*100;Refresh();
            Assert.That(budget.IsRelevant,Is.False);Assert.That(visible.forceRenderingOff,Is.True);
            Assert.That(root.GetComponent<Collider>().enabled,Is.True,"Visual culling must not remove gameplay contact.");
            target.transform.position=Vector3.forward*80;Refresh();Assert.That(budget.IsRelevant,Is.False,"Use hysteresis at the distance edge.");
            target.transform.position=Vector3.forward*70;Refresh();Assert.That(budget.IsRelevant,Is.True);
            Assert.That(visible.forceRenderingOff,Is.False);Assert.That(hidden.forceRenderingOff,Is.True);
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(target);}
    }
    [TestCase(-3f)] [TestCase(0f)] [TestCase(3f)]
    public void StationaryCrateReachesBodyHeightWithoutTrackingLateralDodges(float lateral)
    {
        var direction=(Vector3)typeof(EnemyScript_space).GetMethod("CalculateStationaryThrowDirection",BindingFlags.Static|BindingFlags.NonPublic)
            .Invoke(null,new object[]{Vector3.forward,new Vector3(0,2,0),new Vector3(lateral,1,16)});
        Assert.That(direction.x,Is.Zero.Within(.0001f));
        var crossing=new Vector3(0,2,0)+direction*(16/direction.z);
        Assert.That(crossing.y,Is.EqualTo(1).Within(.001f));
    }
}
#endif
