using System;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class WorkbookEnemyAssignmentTests
{
    private Scene scene;
    private int undoGroup;
    private GameObject Make(string name){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
    [SetUp] public void Setup(){Undo.IncrementCurrentGroup();undoGroup=Undo.GetCurrentGroup();scene=EditorSceneManager.NewPreviewScene();}
    [TearDown] public void Cleanup(){Undo.RevertAllDownToGroup(undoGroup);EditorSceneManager.ClosePreviewScene(scene);}

    [Test] public void OccupiedSpots_SwapTargetsAndUndoTogether()
    {
        var a=Make("A").AddComponent<EnemyEventController>();var b=Make("B").AddComponent<EnemyEventController>();
        var left=Make("Left").AddComponent<EnemyEventActivationSpot>();var right=Make("Right").AddComponent<EnemyEventActivationSpot>();
        left.Targets=new[]{a};right.Targets=new[]{b};
        WorkbookEnemyAssignment.Assign(right,a,new[]{left,right});
        Assert.That(left.Targets,Is.EqualTo(new[]{b}));Assert.That(right.Targets,Is.EqualTo(new[]{a}));
        Undo.FlushUndoRecordObjects();Undo.PerformUndo();
        Assert.That(left.Targets,Is.EqualTo(new[]{a}));Assert.That(right.Targets,Is.EqualTo(new[]{b}));
    }
    [Test] public void ReclickingAssignedTarget_DoesNotLeaveItWithoutASpot()
    {
        var a=Make("A").AddComponent<EnemyEventController>();var spot=Make("Spot").AddComponent<EnemyEventActivationSpot>();spot.Targets=new[]{a};
        WorkbookEnemyAssignment.Assign(spot,a,new[]{spot});Assert.That(spot.Targets,Is.EqualTo(new[]{a}));
    }
    [Test] public void EmptyDestination_MovesOwnershipWithoutDuplication()
    {
        var a=Make("A").AddComponent<EnemyEventController>();var source=Make("Source").AddComponent<EnemyEventActivationSpot>();var destination=Make("Destination").AddComponent<EnemyEventActivationSpot>();source.Targets=new[]{a};
        WorkbookEnemyAssignment.Assign(destination,a,new[]{source,destination});Assert.That(source.Targets,Is.Empty);Assert.That(destination.Targets,Is.EqualTo(new[]{a}));
    }
    [Test] public void CorruptGroup_IsRejectedBeforeAnyMutation()
    {
        var a=Make("A").AddComponent<EnemyEventController>();var b=Make("B").AddComponent<EnemyEventController>();
        var first=Make("First").AddComponent<EnemyEventActivationSpot>();var second=Make("Second").AddComponent<EnemyEventActivationSpot>();first.Targets=new[]{a,b};second.Targets=new[]{b};
        Assert.Throws<InvalidOperationException>(()=>WorkbookEnemyAssignment.Assign(second,a,new[]{first,second}));
        Assert.That(first.Targets,Is.EqualTo(new[]{a,b}));Assert.That(second.Targets,Is.EqualTo(new[]{b}));
    }
    [Test] public void ReplacingWithAnUnassignedActor_DoesNotOrphanThePreviousActor()
    {
        var a=Make("A").AddComponent<EnemyEventController>();var b=Make("B").AddComponent<EnemyEventController>();var spot=Make("Spot").AddComponent<EnemyEventActivationSpot>();spot.Targets=new[]{a};
        Assert.Throws<InvalidOperationException>(()=>WorkbookEnemyAssignment.Assign(spot,b,new[]{spot}));Assert.That(spot.Targets,Is.EqualTo(new[]{a}));
    }
}
