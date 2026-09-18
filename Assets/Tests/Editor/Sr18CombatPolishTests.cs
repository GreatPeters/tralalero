#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class Sr18CombatPolishTests
{
    private readonly List<GameObject> objects=new();
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private GameObject Make(string name) { var g=new GameObject(name); objects.Add(g); return g; }
    [TearDown] public void Cleanup() { foreach(var g in objects) if(g!=null) Object.DestroyImmediate(g); objects.Clear(); }

    [Test] public void ExactFacing_IgnoresPlacementHalfTurnAndTracksLateralPlayer()
    {
        var player=Make("Player").AddComponent<PlayerScript>();
        var enemy=Make("Enemy"); var body=Make("Body"); body.transform.SetParent(enemy.transform);
        body.transform.localRotation=Quaternion.Euler(0,180,0); body.AddComponent<Animator>();
        var controller=enemy.AddComponent<EnemyEventController>();
        typeof(EnemyEventController).GetField("player",Private).SetValue(controller,player);
        Assert.That(controller.ActivateFromSpot(),Is.True);
        foreach(var point in new[]{new Vector3(8,3,3),new Vector3(-2,0,-7)})
        {
            player.transform.position=point;
            typeof(EnemyEventController).GetMethod("FacePlayerExactly",Private).Invoke(controller,null);
            Assert.That(Vector3.Angle(body.transform.forward,Vector3.ProjectOnPlane(point,Vector3.up)),Is.LessThan(.02));
            Assert.That(enemy.transform.rotation,Is.EqualTo(Quaternion.identity));
        }
    }
    [Test] public void Helper_FollowsEveryCornerWithoutDiagonalShortcuts()
    {
        var helper=Make("Tung").AddComponent<NoryangjinHelperRouteFollower>();
        helper.ConfigureRoute(Vector3.forward,new[]{
            new ChapterRouteTurn(new Vector3(0,0,10),Vector3.right),
            new ChapterRouteTurn(new Vector3(10,0,10),Vector3.back),
            new ChapterRouteTurn(new Vector3(10,0,0),Vector3.left)},null);
        helper.Advance(15); Assert.That(Vector3.Distance(helper.transform.position,new Vector3(5,0,10)),Is.LessThan(.001));
        helper.Advance(17); Assert.That(Vector3.Distance(helper.transform.position,new Vector3(8,0,0)),Is.LessThan(.001));
        Assert.That(helper.CompletedTurns,Is.EqualTo(3));
        helper.Advance(0); Assert.That(helper.transform.position.x,Is.EqualTo(8).Within(.001));
    }
    [TestCase(0f)]
    [TestCase(90f)]
    public void BucketAttachment_OpeningFacesDownOnScaledTurningPlayer(float yaw)
    {
        var player = Make("Player").AddComponent<PlayerScript>();
        player.transform.SetPositionAndRotation(new Vector3(12f, 3f, -7f), Quaternion.Euler(10f, yaw, 0f));
        player.transform.localScale = Vector3.one * 1.5f;
        player.canShoot = true;
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ShooterSurvival/Prefabs/Obstacle_Real/Bucket.prefab");
        var bucket = Object.Instantiate(prefab);
        objects.Add(bucket);
        bucket.transform.localScale = Vector3.one * 70f;
        var obstacle = bucket.GetComponent<ObstacleStats>();
        typeof(ObstacleStats).GetMethod("InitBucket", Private).Invoke(obstacle, null);
        var routine = (System.Collections.IEnumerator)typeof(ObstacleStats)
            .GetMethod("AttachBucketRoutine", Private).Invoke(obstacle, new object[] { player });

        Assert.That(routine.MoveNext(), Is.True);
        // The original mesh's wide open rim is at +Z; its closed base is at -Z.
        var bounds = bucket.GetComponent<MeshFilter>().sharedMesh.bounds;
        Vector3 rim = bucket.transform.TransformPoint(new Vector3(bounds.center.x, bounds.center.y, bounds.max.z));
        Vector3 closedBase = bucket.transform.TransformPoint(new Vector3(bounds.center.x, bounds.center.y, bounds.min.z));
        Assert.That(Vector3.Dot((rim - closedBase).normalized, -player.transform.up), Is.GreaterThan(.95f),
            "The open rim must face down over the head, including after a route turn or on a slope.");
        Assert.That(Vector3.Distance(bucket.transform.lossyScale, Vector3.one * 70f), Is.LessThan(.001f));
        Assert.That(player.canShoot, Is.False);
    }

    [Test] public void OverlappingBuckets_ReleaseOnlyTheirOwnShootBlock()
    {
        var p=Make("Player").AddComponent<PlayerScript>(); p.canShoot=true;
        var a=Make("Bucket A"); var b=Make("Bucket B");
        p.SetBucketShootBlock(a,true); p.SetBucketShootBlock(b,true);
        p.SetBucketShootBlock(a,false); Assert.That(p.canShoot,Is.False);
        p.SetBucketShootBlock(b,false); Assert.That(p.canShoot,Is.True);
        p.canShoot=false; p.SetBucketShootBlock(a,true); p.SetBucketShootBlock(a,false);
        Assert.That(p.canShoot,Is.False,"Do not undo another system's shooting lock");
    }
    [TestCase(0f,1f,EnemyTier.Normal)] [TestCase(43f,72f,EnemyTier.Elite)]
    public void PlacementCombat_AcceptsExplicitTierAndStats(float attack,float health,EnemyTier tier)
    {
        var row=new EncounterPlacementRow {scene="Test",id="E",kind="적 배치",hasCombatStats=true,damage=attack,health=health,tier=tier};
        Assert.DoesNotThrow(()=>EncounterPlacementTables.ValidateRows(new[]{row}));
    }
    [TestCase(float.NaN,10f)] [TestCase(10f,0f)] [TestCase(-1f,20f)] [TestCase(10f,float.PositiveInfinity)]
    public void PlacementCombat_RejectsInvalidValues(float attack,float health)
    {
        var row=new EncounterPlacementRow {scene="Test",id="E",kind="적 배치",hasCombatStats=true,damage=attack,health=health};
        Assert.Throws<InvalidDataException>(()=>EncounterPlacementTables.ValidateRows(new[]{row}));
    }
    [Test] public void Workbook_AllEnemyRowsOwnCombatStats()
    {
        using var stream=GameDataWorkbook.OpenRead("Data.xlsx");
        var rows=EncounterPlacementTables.Read(stream).Where(r=>r.kind=="적 배치"&&r.scene=="Noryangjin_MapTool_Mode_SR18").ToArray();
        // The current layout has 25 paired formations, each with explicit stats.
        Assert.That(rows.Length,Is.EqualTo(50)); Assert.That(rows.All(r=>r.hasCombatStats && r.health>0 && r.damage>=0),Is.True);
        var formations=rows.GroupBy(r=>r.id.EndsWith("_Right")?r.id.Substring(0,r.id.Length-6):r.id).ToArray();
        Assert.That(formations.Length,Is.EqualTo(25)); Assert.That(formations.All(g=>g.Count()==2),Is.True);
        Assert.That(rows.Where(r=>r.id.StartsWith("SR18_L_E25_")).Count(r=>r.tier==EnemyTier.Boss),Is.EqualTo(2));
    }
}
#endif
