#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class CombatRouteFeedbackTests
{
    private readonly List<GameObject> objects = new();
    private GameObject Make(string name) { var go = new GameObject(name); objects.Add(go); return go; }
    [TearDown] public void Cleanup() { foreach (var go in objects) if (go != null) Object.DestroyImmediate(go); objects.Clear(); }

    [TestCase(100, 250, 0, 150)]
    [TestCase(1200, 250, 950, 0)]
    [TestCase(2458, 300, 2158, 0)]
    [TestCase(250, 250, 0, 0)]
    public void Contact_ExchangesCurrentHealthSimultaneously(float enemy, float helper, float enemyAfter, float helperAfter)
    {
        EnemyScript_space.ExchangeContactHealth(ref enemy, ref helper);
        Assert.That(enemy, Is.EqualTo(enemyAfter)); Assert.That(helper, Is.EqualTo(helperAfter));
    }

    [TestCase(8.84f, 5f, 44.2f)]
    [TestCase(8.84f, 7.5f, 66.3f)]
    [TestCase(12f, 5f, 60f)]
    public void ApproachWindow_UsesTravelSeconds(float speed, float seconds, float distance)
    {
        Assert.That(EnemyScript_space.IsWithinApproachWindow(Vector3.zero, Vector3.forward, Vector3.forward * (distance - .01f), speed, seconds, 6), Is.True);
        Assert.That(EnemyScript_space.IsWithinApproachWindow(Vector3.zero, Vector3.forward, Vector3.forward * (distance + .01f), speed, seconds, 6), Is.False);
        Assert.That(EnemyScript_space.IsWithinApproachWindow(Vector3.zero, Vector3.forward, Vector3.back, speed, seconds, 6), Is.False);
        Assert.That(EnemyScript_space.IsWithinApproachWindow(Vector3.zero, Vector3.forward, new Vector3(7, 0, 10), speed, seconds, 6), Is.False);
        Assert.That(EnemyScript_space.IsWithinApproachWindow(Vector3.zero, Vector3.forward, new Vector3(0, 12, 10), speed, seconds, 6), Is.False);
    }

    [TestCase("Default")]
    [TestCase("Enemy")]
    public void HelperSweep_HitsAnEnemyBetweenFrames_AndQueuedContactCannotDamageTwice(string layer)
    {
        var enemyGo = Make("High health elite"); enemyGo.layer = LayerMask.NameToLayer(layer);
        var enemyCollider = enemyGo.AddComponent<CapsuleCollider>(); enemyCollider.height = 2; enemyCollider.radius = .5f; enemyCollider.isTrigger = true;
        var enemy = enemyGo.AddComponent<EnemyScript_space>(); enemy.ApplyStat(20, 1200, EnemyTier.Elite);
        var go = Make("Fast Tungtung"); var capsule = go.AddComponent<CapsuleCollider>(); capsule.height = 2; capsule.radius = .5f;
        var helper = go.AddComponent<ExtraHelpBuffScript>(); helper.helpType = HelpType.Tungtungtung; helper.currentHealth = 300;
        var from = Vector3.back * 5; var to = Vector3.forward * 5; go.transform.position = to;
        helper.ResolveContactsAlongMove(from, to);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(900)); Assert.That(helper.currentHealth, Is.Zero);
        Assert.That(go.transform.position.z, Is.LessThan(0));
        Assert.That(enemy.TryResolveHelperContact(helper), Is.False); Assert.That(enemy.CurrentHealth, Is.EqualTo(900));
    }

    [Test]
    public void Ship_FiresFromTheAuthoredMuzzleWithoutChangingItsWaitingHeading()
    {
        bool running=TimeManager.isGameRunning;float factor=TimeManager.timeFactor;
        var ship=Make("Road-facing ship").AddComponent<ObstacleStats>();ship.obstaclePattern=ObstaclePattern.Ship;
        var muzzle=Make("FirePos").transform;muzzle.SetParent(ship.transform);muzzle.localPosition=new Vector3(4,1,0);muzzle.localRotation=Quaternion.Euler(0,90,0);ship.firePos=muzzle;
        var template=Make("CannonBall");template.AddComponent<SphereCollider>();template.AddComponent<SimpleProjectile>();ship.projectilePrefab=template;
        ship.transform.rotation=Quaternion.Euler(0,-90,0);var heading=ship.transform.rotation;
        var field=typeof(ObstacleStats).GetField("shipShot",BindingFlags.Instance|BindingFlags.NonPublic);
        try
        {
            TimeManager.isGameRunning=true;TimeManager.timeFactor=1;
            Assert.That((bool)typeof(ObstacleStats).GetMethod("FireFromBow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ship,null),Is.True);
            var shot=(GameObject)field.GetValue(ship);objects.Add(shot);field.SetValue(ship,null);
            Assert.That(Quaternion.Angle(ship.transform.rotation,heading),Is.LessThan(.001));
            Assert.That(Vector3.Distance(shot.transform.position,muzzle.position),Is.LessThan(.001));
            Assert.That(Vector3.Angle(shot.GetComponent<Rigidbody>().linearVelocity,muzzle.forward),Is.LessThan(.001));
        }
        finally{TimeManager.isGameRunning=running;TimeManager.timeFactor=factor;}
    }

    [Test]
    public void LaunchedProjectile_AddsItsMissingPhysicsBodyAndPausesWithTheGame()
    {
        bool running=TimeManager.isGameRunning;float factor=TimeManager.timeFactor;
        try
        {
            TimeManager.isGameRunning=true;TimeManager.timeFactor=1;
            var go=Make("Arrow2 clone without rigidbody");go.AddComponent<SphereCollider>();
            var flight=go.AddComponent<SimpleProjectile>();flight.Launch(Vector3.right,18,12,8);
            var body=go.GetComponent<Rigidbody>();Assert.That(body,Is.Not.Null);Assert.That(body.linearVelocity,Is.EqualTo(Vector3.right*18));
            TimeManager.isGameRunning=false;
            typeof(SimpleProjectile).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flight,null);
            Assert.That(body.linearVelocity,Is.EqualTo(Vector3.zero));Assert.That(flight.damage,Is.EqualTo(12));
        }
        finally{TimeManager.isGameRunning=running;TimeManager.timeFactor=factor;}
    }

    [Test]
    public void FatManPrefab_IsStationaryAndUsesItsAuthoredTwoHandPose()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_FatMan.prefab");
        Assert.That(prefab.GetComponent<EnemyEventController>().EventMode, Is.EqualTo(EnemyEventMode.Shoot));
        Assert.That(prefab.GetComponent<EnemyScript_space>().StationaryThrow, Is.True);
        var pose = prefab.GetComponent<FatManCratePose>();
        Assert.That(pose, Is.Not.Null); Assert.That(pose.HasReferencePose, Is.True);
        Assert.That(pose.Crate.parent, Is.EqualTo(pose.Model.transform));
        var controller = (AnimatorOverrideController)pose.Model.runtimeAnimatorController;
        Assert.That(controller["ForwardEnemy_Die"], Is.Not.Null);
    }

    [Test]
    public void OwnedMissile_TurnsWithRoute_WithoutChangingDamageOrLifetime()
    {
        var owner = Make("Owner").AddComponent<PlayerScript>();
        var missile = Make("Helper missile").AddComponent<BulletScript>();
        missile.SetDirection(Vector3.forward, owner, 37);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(BulletScript).GetField("elapsedDuration", flags).SetValue(missile, .6f);
        typeof(BulletScript).GetMethod("ApplyRouteTurn", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { owner, Quaternion.Euler(0, 90, 0) });
        Assert.That(Vector3.Angle((Vector3)typeof(BulletScript).GetField("direction", flags).GetValue(missile), Vector3.right), Is.LessThan(.01));
        Assert.That(missile.LaunchDamage, Is.EqualTo(37));
        Assert.That(typeof(BulletScript).GetField("elapsedDuration", flags).GetValue(missile), Is.EqualTo(.6f));
    }
}
#endif
