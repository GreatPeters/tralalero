using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public sealed class RestStopEncounterLaneTests
{
    GameObject root,rowObject,rails;
    HighwayEncounterLanes lanes;
    HighwayEncounterRow row;
    [SetUp]public void Setup()
    {
        root=new GameObject("Local road constraints");lanes=root.AddComponent<HighwayEncounterLanes>();
        rowObject=new GameObject("Encounter");rowObject.transform.SetParent(root.transform);row=rowObject.AddComponent<HighwayEncounterRow>();
        row.transform.position=new Vector3(40,0,80);row.halfWidth=1.85f;
        rails=new GameObject("Visible passage");rails.transform.SetParent(row.transform,false);row.barriers=rails.transform;
        lanes.rows=new[]{row};
    }
    [TearDown]public void Teardown()=>Object.DestroyImmediate(root);
    [TestCase(0,-4.4f)] [TestCase(0,4.4f)] [TestCase(90,-4.4f)] [TestCase(90,4.4f)]
    [TestCase(180,-4.4f)] [TestCase(180,4.4f)] [TestCase(270,-4.4f)] [TestCase(270,4.4f)]
    public void EveryRoadHeadingKeepsEdgeInputInsideTheVisiblePassage(float yaw,float lane)
    {
        row.transform.rotation=Quaternion.Euler(0,yaw,0);var right=row.transform.right;var forward=row.transform.forward;
        var position=row.transform.position-forward*4+right*lane+Vector3.up*.12f;
        var actual=lanes.ConstrainWorldPosition(position,forward);
        Assert.That(Vector3.Dot(actual-row.transform.position,right),Is.EqualTo(Mathf.Sign(lane)*1.85f).Within(.001));
        Assert.That(Vector3.Dot(actual-position,forward),Is.Zero.Within(.001));Assert.That(actual.y,Is.EqualTo(position.y));
    }
    [Test]
    public void OtherRoadsUnmarkedRowsAndDistantSectionsDoNotPullThePlayer()
    {
        var forward=row.transform.forward;var side=row.transform.right;
        foreach(var offset in new[]{side*12,Vector3.up*6+side*4,-forward*25+side*4,forward*8+side*4})
        {var p=row.transform.position+offset;Assert.That(lanes.ConstrainWorldPosition(p,forward),Is.EqualTo(p));}
        var position=row.transform.position+side*4;
        Assert.That(lanes.ConstrainWorldPosition(position,-forward),Is.EqualTo(position));
        row.barriers=null;Assert.That(lanes.ConstrainWorldPosition(position,forward),Is.EqualTo(position));
        row.barriers=rails.transform;row.enabled=false;Assert.That(lanes.ConstrainWorldPosition(position,forward),Is.EqualTo(position));
        row.enabled=true;lanes.enabled=false;Assert.That(lanes.ConstrainWorldPosition(position,forward),Is.EqualTo(position));
    }
    [Test]
    public void AuthoredDisabledFoodHallRowsKeepTheirFullWidth()
    {
        var controller=root.AddComponent<EncounterPlacementController>();
        row.left=new GameObject("Disabled left").AddComponent<EnemyScript_space>();row.left.transform.SetParent(row.transform);
        row.right=new GameObject("Disabled right").AddComponent<EnemyScript_space>();row.right.transform.SetParent(row.transform);
        var disabled=(HashSet<string>)typeof(EncounterPlacementController).GetField("disabledEnemies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controller);
        disabled.Add(row.left.name);disabled.Add(row.right.name);
        var position=row.transform.position+Vector3.right*4.4f;
        Assert.That(lanes.ConstrainWorldPosition(position,Vector3.forward),Is.EqualTo(position));
    }
    [Test]
    public void ThePlayerPositionPathEnforcesThePassageWithoutHorizontalInput()
    {
        var actor=new GameObject("Ordinary player movement");bool running=TimeManager.isGameRunning;
        try
        {
            var player=actor.AddComponent<PlayerScript>();const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(PlayerScript).GetField("encounterLanes",flags).SetValue(player,lanes);
            var apply=typeof(PlayerScript).GetMethod("ApplyPlayerPosition",flags);TimeManager.isGameRunning=true;
            var position=row.transform.position+Vector3.right*4.4f+Vector3.back*4;
            apply.Invoke(player,new object[]{position});Assert.That(actor.transform.position.x,Is.EqualTo(row.transform.position.x+1.85f).Within(.001));
            TimeManager.isGameRunning=false;apply.Invoke(player,new object[]{position});Assert.That(actor.transform.position,Is.EqualTo(position));
        }
        finally{TimeManager.isGameRunning=running;Object.DestroyImmediate(actor);}
    }
}
