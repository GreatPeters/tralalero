#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ProjectilePoolLifetimeTests
{
    private readonly List<GameObject> objects = new();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject Make(string name) { var go=new GameObject(name); objects.Add(go); return go; }
    [TearDown] public void Cleanup() { foreach(var go in objects) if(go!=null) Object.DestroyImmediate(go); objects.Clear(); }
    private BulletPooler Pool()
    {
        var prefab=Make("Test projectile prefab");
        var gfx=Make("GFX"); gfx.transform.SetParent(prefab.transform); gfx.AddComponent<BulletScript>();
        var pool=Make("Isolated pool").AddComponent<BulletPooler>();
        typeof(BulletPooler).GetField("bulletPrefab",Private).SetValue(pool,prefab);
        typeof(BulletPooler).GetField("bulletPrefab_bomb",Private).SetValue(pool,prefab);
        return pool;
    }
    [TestCase(BulletKind.Water)] [TestCase(BulletKind.Bomb)]
    public void DuplicatePoolReturn_EnqueuesOnlyOnceAndRentsDistinctInstances(BulletKind kind)
    {
        var pool=Pool(); var caller=Make("Caller").transform;
        var first=pool.Get(kind,caller); objects.Add(first);
        pool.Return(first); pool.Return(first);
        var a=pool.Get(kind,caller); var b=pool.Get(kind,caller); objects.Add(b);
        Assert.That(a,Is.SameAs(first)); Assert.That(b,Is.Not.SameAs(a),"One live projectile cannot have two owners");
    }
    [TestCase(BulletKind.Water)] [TestCase(BulletKind.Bomb)]
    public void DuplicateProjectileReturn_DoesNotRemoveGfxAndNextRentalResetsLifetime(BulletKind kind)
    {
        var pool=Pool(); var caller=Make("Caller").transform;
        var root=pool.Get(kind,caller); objects.Add(root); root.transform.SetParent(null);
        var script=root.GetComponentInChildren<BulletScript>(true);
        typeof(BulletScript).GetField("bulletPooler",Private).SetValue(script,pool);
        script.SetDirection(Vector3.forward);
        var giveBack=typeof(BulletScript).GetMethod("ReturnToPool",Private);
        giveBack.Invoke(script,null);
        // Reproduce the queued contact after OnDisable cleared the old root pointer.
        typeof(BulletScript).GetMethod("OnDisable",Private).Invoke(script,null);
        giveBack.Invoke(script,null);
        Assert.That(root.GetComponentInChildren<BulletScript>(true),Is.SameAs(script));
        var rented=pool.Get(kind,caller); rented.transform.SetParent(null);
        script.SetDirection(Vector3.right);
        Assert.That(rented,Is.SameAs(root));
        Assert.That(typeof(BulletScript).GetField("elapsedDuration",Private).GetValue(script),Is.EqualTo(0f));
        giveBack.Invoke(script,null); Assert.That(rented.activeSelf,Is.False);
    }
}
#endif
