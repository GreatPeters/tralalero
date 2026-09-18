#if UNITY_EDITOR
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Sr18RuntimeFixSceneTests
{
    [Test] public void Seagull_DeliversMovingColliderContactsToTheDamageOwner()
    {
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Seagull.prefab");
        var stats=root.GetComponent<ObstacleStats>();
        Assert.That(stats,Is.Not.Null);
        Assert.That(root.GetComponent<Rigidbody>(),Is.Not.Null);
        Assert.That(stats.balloon.GetComponent<Collider>(),Is.Not.Null);
        Assert.That(stats.balloon.GetComponent<Rigidbody>(),Is.Null,"A nested body intercepts the parent's physical damage callback.");
    }

    [Test] public void ExitAndLegacyScenery_AreSafeWithoutChangingRoadGeometry()
    {
        const string path="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
            Assert.That(map.Find("Roads").childCount,Is.EqualTo(230));
            Assert.That(map.Find("Props").Cast<Transform>().Count(t=>!t.name.StartsWith("SR18_Polish_")),Is.EqualTo(717));
            var lamps=map.Find("Props/Prop_Lights_X-49_Z-139").GetComponentsInChildren<ObstacleStats>(true);
            Assert.That(lamps.Length,Is.EqualTo(4));
            foreach(var lamp in lamps)
            {
                Assert.That(lamp.gameObject.activeInHierarchy,Is.True);Assert.That(lamp.enabled,Is.True);
                Assert.That(lamp.GetComponents<Collider>().All(c=>c.enabled),Is.True);
                Assert.That(lamp.canBeShotDown,Is.True);
                Assert.That(lamp.GetComponent<LampImpactFeedback>(),Is.Not.Null);
                Assert.That(lamp.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),Is.True);
            }
            var exit=map.Find("SR18_StageExit");Assert.That(exit,Is.Not.Null);Assert.That(exit.CompareTag("GameEndTriggerTag"),Is.True);
            var box=exit.GetComponent<BoxCollider>();Assert.That(box.enabled&&box.isTrigger,Is.True);
            Assert.That(exit.position.z,Is.InRange(332f,336f));Assert.That(box.size.x,Is.GreaterThan(6));
            Assert.That(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NoryangjinCameraOcclusion>(true)).Count(),Is.EqualTo(1));
            var canvas=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CanvasScript>(true)).Single();
            Assert.That(canvas.youWinUI.transform.Find("Text (TMP)").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("STAGE CLEAR"));
            Assert.That(canvas.youWinUI.transform.Find("NextChapter").gameObject.activeSelf,Is.True);
            Assert.That(canvas.GetComponent<ChapterProgression>().nextScene,Is.EqualTo("HighWay"));
        }
        finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
#endif
