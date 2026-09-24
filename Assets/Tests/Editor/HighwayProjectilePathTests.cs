using System.Collections.Generic;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class HighwayProjectilePathTests
{
    readonly List<GameObject> objects=new();
    GameObject Make(string name){var go=new GameObject(name);objects.Add(go);return go;}
    HighwayRoute Road()
    {
        var r=Make("Curved sloping test road").AddComponent<HighwayRoute>();r.length=60;
        r.centers=new[]{Vector3.zero,new Vector3(0,0,20),new Vector3(20,4,20),new Vector3(40,0,20)};
        return r;
    }
    [TearDown] public void Cleanup(){foreach(var go in objects)if(go!=null)Object.DestroyImmediate(go);objects.Clear();}

    [Test]
    public void SpawnHasNoJumpAndShotKeepsItsLaneAndRoadClearanceThroughTheBend()
    {
        var road=Road();road.Sample(0,false,out var center,out var forward);var right=Vector3.Cross(Vector3.up,forward);
        var start=center+right*1.1f+Vector3.up*1.4f+forward*.7f;
        var shot=new HighwayProjectilePath(road,0,false,start,forward);
        Assert.That(Vector3.Distance(shot.Advance(0,out _),start),Is.LessThan(.0001f));
        for(int d=10;d<=50;d+=10)
        {
            var p=shot.Advance(10,out _);road.Sample(d,false,out center,out forward);
            Assert.That(p.y-center.y,Is.EqualTo(1.4f).Within(.001));
            Assert.That(Vector3.Dot(p-center,Vector3.Cross(Vector3.up,forward)),Is.EqualTo(1.1f).Within(.001));
            Assert.That(Vector3.Dot(p-center,forward),Is.EqualTo(.7f).Within(.001));
        }
        Assert.That(road.Distance,Is.Zero,"The projectile turns before its stationary owner reaches the bend.");
    }
    [Test]
    public void SpreadAndExitTravelArePreserved()
    {
        var road=Road();road.Sample(0,false,out var center,out var forward);var right=Vector3.Cross(Vector3.up,forward);
        var shot=new HighwayProjectilePath(road,0,false,center+Vector3.up,forward*.9f+right*.1f);
        var p=shot.Advance(10,out _);road.Sample(9,false,out center,out forward);
        Assert.That(Vector3.Dot(p-center,Vector3.Cross(Vector3.up,forward)),Is.EqualTo(1).Within(.001));
        road.Sample(road.length,false,out center,out forward);
        shot=new HighwayProjectilePath(road,road.length,false,center+Vector3.up,forward);
        Assert.That(Vector3.Distance(shot.Advance(10,out _),center+forward*10+Vector3.up),Is.LessThan(.001));
    }
    [Test]
    public void FiredShotKeepsItsSelectedRecoveryBranch()
    {
        var road=Road();road.forks=new[]{new HighwayRoute.Fork{start=10,end=50,offset=-8}};
        road.Sample(20,true,out var center,out var forward);
        var shot=new HighwayProjectilePath(road,20,true,center+Vector3.up,forward);
        road.forks[0].bypass=false;road.forks[0].decided=true;
        var position=shot.Advance(10,out _);road.Sample(30,true,out center,out _);
        Assert.That(Vector3.Distance(position,center+Vector3.up),Is.LessThan(.001));
    }
    [Test]
    public void PooledBulletUsesOwnRoadProgressAndIgnoresLaterOwnerYaw()
    {
        var road=Road();var owner=Make("Owner").AddComponent<PlayerScript>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(PlayerScript).GetField("highwayRoute",flags).SetValue(owner,road);
        road.Sample(0,false,out var center,out var forward);
        var bullet=Make("Water or bomber projectile").AddComponent<BulletScript>();bullet.transform.position=center+Vector3.up;
        float speed=BulletScript.BaseMissileSpeed,duration=BulletScript.BaseMissileDuration,factor=TimeManager.timeFactor;
        try
        {
            TimeManager.timeFactor=1;BulletScript.ConfigureMissileDefaults(10,100);bullet.SetDirection(forward,owner,37);
            var tick=typeof(BulletScript).GetMethod("FixedUpdate",flags);
            for(int i=0;i<100;i++)tick.Invoke(bullet,null);
            road.Sample(100*10*Time.fixedDeltaTime,false,out center,out _);
            Assert.That(Vector3.Distance(bullet.transform.position,center+Vector3.up),Is.LessThan(.003));
            var rotation=bullet.transform.rotation;
            typeof(BulletScript).GetMethod("ApplyRouteTurn",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{owner,Quaternion.Euler(0,90,0)});
            Assert.That(Quaternion.Angle(rotation,bullet.transform.rotation),Is.LessThan(.001));Assert.That(bullet.LaunchDamage,Is.EqualTo(37));
        }
        finally
        {
            typeof(BulletScript).GetMethod("OnDisable",flags).Invoke(bullet,null);
            BulletScript.ConfigureMissileDefaults(speed,duration);TimeManager.timeFactor=factor;
        }
    }
    [Test]
    public void StationaryDefenseKeepsFreeAimInsteadOfFollowingTheRoad()
    {
        var road=Road();var owner=Make("Defender").AddComponent<PlayerScript>();
        typeof(PlayerScript).GetField("highwayRoute",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,road);
        Assert.That(owner.ProjectileRoute,Is.EqualTo(road));
        var holdout=Make("Defense owner").AddComponent<RestStopHoldout>();owner.SetStationaryCombat(holdout,true);
        Assert.That(owner.ProjectileRoute,Is.Null);owner.SetStationaryCombat(holdout,false);Assert.That(owner.ProjectileRoute,Is.EqualTo(road));
    }
}
