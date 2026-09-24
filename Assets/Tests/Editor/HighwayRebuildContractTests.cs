using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public sealed class HighwayRebuildContractTests
{
    [Test]
    public void SixSourceModelsHaveOneRigAndCompleteBoundMotion()
    {
        foreach(string name in HighwayEnemyBuilder.Names)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(HighwayEnemyBuilder.Folder+"/"+name+".prefab");
            var animators=root.GetComponentsInChildren<Animator>(true);Assert.That(animators.Length,Is.EqualTo(1),name);
            var animator=animators[0];var clips=animator.runtimeAnimatorController.animationClips;
            foreach(string action in new[]{"idle","walk","run","attack_loop","attack_once","hit","die"})
            {
                var clip=clips.Single(c=>c.name==action||c.name.EndsWith("|"+action));Assert.That(clip.length,Is.GreaterThan(.1f),name+"/"+action);
                Assert.That(AnimationUtility.GetCurveBindings(clip).All(b=>animator.transform.Find(b.path)!=null),Is.True,name+"/"+action+" targets");
            }
            Assert.That(root.GetComponent<HighwayEnemyAnimation>().deathSeconds,Is.GreaterThanOrEqualTo(clips.Single(c=>c.name.EndsWith("|die")).length));
        }
    }
    [Test]
    public void SavedHighwayHasNoDuplicateModelsOrBrokenRangedOverrides()
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/HighWay.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EnemyScript_space>(true)).ToArray();
            Assert.That(actors.Length,Is.EqualTo(50));
            foreach(var actor in actors)
            {
                Assert.That(actor.GetComponentsInChildren<Animator>(true).Length,Is.EqualTo(1),actor.name);
                var member=actor.GetComponent<HighwayEncounterMember>();Assert.That(member,Is.Not.Null,actor.name);Assert.That(member.row,Is.Not.Null,actor.name);
                var controller=actor.GetComponent<EnemyEventController>();if(controller.EventMode==EnemyEventMode.PatrolBetweenStartAndTarget)Assert.That(controller.PatrolAcrossRoad,Is.False,actor.name);
            }
            Assert.That(actors.Count(a=>a.HasConfiguredProjectile),Is.EqualTo(34));
            foreach(var actor in actors.Where(a=>a.HasConfiguredProjectile))
            {
                var data=new SerializedObject(actor);var held=data.FindProperty("heldProjectile").objectReferenceValue as Transform;var muzzle=data.FindProperty("throwPoint").objectReferenceValue as Transform;
                Assert.That(held.IsChildOf(actor.transform),Is.True,actor.name);Assert.That(muzzle,Is.Not.Null,actor.name);Assert.That(muzzle.IsChildOf(actor.transform),Is.True,actor.name);
            }
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    [Test]
    public void ResetCancelsAPostPoseShotFromThePreviousRun()
    {
        var root=new GameObject("Queued shot fixture");
        try
        {
            var combat=root.AddComponent<EnemyScript_space>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var queued=typeof(EnemyScript_space).GetField("highwayReleaseQueued",flags);queued.SetValue(combat,true);
            typeof(EnemyScript_space).GetMethod("ResetTriggeredFireForNewRun",flags).Invoke(combat,null);
            Assert.That((bool)queued.GetValue(combat),Is.False);
            Assert.That((bool)typeof(EnemyScript_space).GetMethod("ReleaseQueuedHighwayProjectile",flags).Invoke(combat,null),Is.False);
        }
        finally{Object.DestroyImmediate(root);}
    }
}
