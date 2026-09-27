using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HighwayCombatRevisionTests
{
    static void InHighway(Action<HighwayChapter2Controller> verify)
    {
        const string path="Assets/ShooterSurvival/Scenes/Tools/HighWay.unity";
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{EnvironmentVariableTables.Reload();verify(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HighwayChapter2Controller>(true)).Single());}
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    [Test]
    public void EveryOrdinaryFrontRowNeedsOneDestroyedCarToOpenAPassage()
    {
        InHighway(chapter=>
        {
            var groups=chapter.GetComponentsInChildren<HighwayVehicleEnemy>(true).Where(c=>c.group>=100).GroupBy(c=>c.group).ToArray();
            Assert.That(groups.Length,Is.EqualTo(105));
            foreach(var group in groups)
            {
                float station=group.Min(c=>c.station);var front=group.Where(c=>c.station-station<10).ToArray();
                var occupied=front.Select(c=>new Vector2(HighwayChapter2Data.Lane(c.laneIndex),c.HalfWidth)).ToArray();
                Assert.That(HighwayChapter2Rules.LeavesPassage(occupied[0].x,occupied[0].y,occupied.Skip(1).ToArray(),4.4f,.7f),Is.False,"Unbroken wave "+group.Key);
                // Cover the coordinates between lane centres too; a narrow visual gap must not
                // admit the player's capsule while every front member is alive.
                for(float lane=-4.4f;lane<=4.4f;lane+=.05f)
                    Assert.That(occupied.Any(v=>Mathf.Abs(lane-v.x)<v.y+.7f),Is.True,"Weaving gap in wave "+group.Key+" at "+lane);
                var weak=front.First(c=>c.kind==HighwayVehicleKind.Sedan||c.kind==HighwayVehicleKind.Taxi);
                var remaining=front.Where(c=>c!=weak).Select(c=>new Vector2(HighwayChapter2Data.Lane(c.laneIndex),c.HalfWidth)).ToArray();
                Assert.That(HighwayChapter2Rules.LeavesPassage(remaining[0].x,remaining[0].y,remaining.Skip(1).ToArray(),4.4f,.7f),Is.True,"Broken wave "+group.Key);
                Assert.That(group.Select(c=>c.speed).Distinct().Count(),Is.EqualTo(1),"Shared row movement "+group.Key);
            }
        });
    }
    [Test]
    public void TollRewardsHaveTwoPhysicalPickupVolumesBeforeTheFork()
    {
        InHighway(chapter=>
        {
            Assert.That(chapter.exitPickups.Length,Is.EqualTo(2));
            for(int i=0;i<2;i++)
            {
                var pickup=chapter.exitPickups[i];Assert.That(pickup.owner,Is.SameAs(chapter));Assert.That(pickup.choice,Is.EqualTo(i));Assert.That(pickup.GetComponent<BoxCollider>().isTrigger,Is.True);
                Assert.That(chapter.GetComponent<HighwayRoute>().NearestDistance(pickup.transform.position),Is.EqualTo(1936).Within(1));
            }
        });
    }
    [Test]
    public void LogIsFiftyPercentLongerAndDamageUsesItsPhysicalBody()
    {
        InHighway(chapter=>{var shape=chapter.singleLog.GetComponent<BoxCollider>();Assert.That(shape,Is.Not.Null);Assert.That(shape.isTrigger,Is.True);Assert.That(shape.size.z*Mathf.Abs(chapter.singleLog.lossyScale.z),Is.EqualTo(5.1f).Within(.01f));});
    }
    [Test]
    public void VehicleNumbersRespectDepthAndPoliceUseKoreanSedanLivery()
    {
        InHighway(chapter=>
        {
            var vehicles=chapter.GetComponentsInChildren<HighwayVehicleEnemy>(true);
            Assert.That(vehicles.Length,Is.EqualTo(351));Assert.That(vehicles[0].healthNumber.canvas.renderMode,Is.EqualTo(RenderMode.WorldSpace));Assert.That(vehicles[0].healthNumber.isOverlay,Is.False);
            var police=vehicles.Where(c=>c.kind==HighwayVehicleKind.Police).ToArray();Assert.That(police.Length,Is.EqualTo(9));
            foreach(var car in police){Assert.That(car.body.Find("Korean police livery"),Is.Not.Null);Assert.That(car.body.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t=>t.text=="경찰"),Is.True);}
        });
    }
    [TestCase(true,1,467,false)] [TestCase(true,1,468,true)] [TestCase(true,1,1169,true)] [TestCase(true,1,1170,false)] [TestCase(false,1,700,false)] [TestCase(true,0,700,false)]
    public void RushOnlyExistsInsideTheChosenOpenSegment(bool running,int choice,float distance,bool expected)
        =>Assert.That(HighwayChapter2Rules.IsRushSegment(running,choice,distance,468,1170),Is.EqualTo(expected));
}
