using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RestStopContactPairTests
{
    [Test]
    public void SavedRestStopPairsHaveOneContactClaimAndKeepPatrolsInTheirOwnLane()
    {
        const string path="Assets/ShooterSurvival/Scenes/Tools/RestStop.unity";
        var existing=SceneManager.GetSceneByPath(path);bool opened=!existing.isLoaded;
        var scene=opened?EditorSceneManager.OpenScene(path,OpenSceneMode.Additive):existing;
        try
        {
            var rows=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HighwayEncounterRow>(true)).ToArray();
            Assert.That(rows.Length,Is.EqualTo(25));
            foreach(var row in rows)
            {
                Assert.That(row.left,Is.Not.Null);Assert.That(row.right,Is.Not.Null);
                Assert.That(row.left.GetComponent<HighwayEncounterMember>().row,Is.EqualTo(row));
                Assert.That(row.right.GetComponent<HighwayEncounterMember>().row,Is.EqualTo(row));
                Assert.That(row.barriers,Is.Not.Null,"Every constrained passage must have visible rails.");
                foreach(var actor in new[]{row.left,row.right})
                {
                    var events=actor.GetComponent<EnemyEventController>();
                    if(events.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget)Assert.That(events.PatrolAcrossRoad,Is.False,actor.name);
                    var center=events.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget&&events.HasUsableTarget?(actor.transform.position+events.TargetPoint.position)*.5f:actor.transform.position;
                    var onRoad=RestStopChapterBuilder.ProjectRoadCenter(center,actor.transform.forward,out var roadForward);
                    Assert.That(Vector3.Dot(row.transform.forward,roadForward),Is.GreaterThan(.999f),actor.name+" passage follows player road direction");
                    float lane=Vector3.Dot(center-onRoad,actor.transform.right);
                    Assert.That(lane,Is.EqualTo(actor==row.left?-1.1f:1.1f).Within(.02f),actor.name+" must remain in a shootable lane");
                }
            }
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }

    [Test]
    public void OppositeActorFacingDoesNotReverseTheRoadTravelDirection()
    {
        var center=RestStopChapterBuilder.ProjectRoadCenter(new Vector3(234.7f,.12f,162.7f),Vector3.back,out var forward);
        Assert.That(center.x,Is.EqualTo(240).Within(.001));Assert.That(forward,Is.EqualTo(Vector3.forward));
    }
    [Test]
    public void ProjectingTheSameRoadCenterDoesNotDriftOnRepeatedAuthoring()
    {
        var first=RestStopChapterBuilder.ProjectRoadCenter(new Vector3(234.7f,.12f,162.7f),Vector3.forward);
        Assert.That(first.x,Is.EqualTo(240).Within(.001));
        for(int i=0;i<5;i++)first=RestStopChapterBuilder.ProjectRoadCenter(first,Vector3.forward);
        Assert.That(first.x,Is.EqualTo(240).Within(.001));Assert.That(first.z,Is.EqualTo(162.7f).Within(.001));
    }
}
