using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RoadChapterPatternTests
{
    [Test]
    public void CurvedRouteReportsProgressWithoutOldTurnTriggers()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Route analytics");SceneManager.MoveGameObjectToScene(root,scene);
            var route=root.AddComponent<HighwayRoute>();route.length=100;
            typeof(HighwayRoute).GetField("<Distance>k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(route,50f);
            root.AddComponent<IndianOceanAssets.ShooterSurvival.Analytics.GameplayAnalyticsSceneContext>().Configure(2,1,6,"forward_march",true);
            Assert.That(IndianOceanAssets.ShooterSurvival.Analytics.GameplayAnalyticsSceneContext.TryResolve(scene,out int chapter,out int stage,out int max,out _,out double percent),Is.True);
            Assert.That(chapter,Is.EqualTo(2));Assert.That(stage,Is.EqualTo(4));Assert.That(max,Is.EqualTo(6));Assert.That(percent,Is.EqualTo(50));
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    [Test]
    public void ReusedPoliceCaptureTheNewDoorInsteadOfResettingToTheFirstDoor()
    {
        var root=new GameObject("Reusable actor");root.SetActive(false);
        try
        {
            var actor=root.AddComponent<EnemyEventController>();
            var enable=typeof(EnemyEventController).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            foreach(var point in new[]{new Vector3(-12,0,0),new Vector3(12,0,0),new Vector3(0,0,-18),new Vector3(0,0,18)})
            {
                root.SetActive(false);actor.PrepareSpawnAt(point,Quaternion.identity);root.SetActive(true);enable.Invoke(actor,null);actor.ActivateFromSpot();
                Assert.That(Vector3.Distance(actor.transform.position,point),Is.Zero);
                actor.transform.position=Vector3.zero;
            }
        }
        finally{Object.DestroyImmediate(root);}
    }
    [TestCase(20,100,.1f,30)] [TestCase(95,100,.1f,100)] [TestCase(130,100,.1f,130)]
    public void RecoveryNeverRemovesOverheal(float current,float maximum,float fraction,float expected)
        => Assert.That(HighwayRoute.RecoveredHealth(current,maximum,fraction),Is.EqualTo(expected));
    [Test]
    public void StationaryCombatRejectsTranslationAndRestoresThePriorRouteFrame()
    {
        var root = new GameObject("Stationary test"); var ownerObject = new GameObject("Encounter owner");
        var secondObject = new GameObject("Unrelated owner");
        bool running = TimeManager.isGameRunning; float factor = TimeManager.timeFactor;
        try
        {
            var player = root.AddComponent<PlayerScript>(); player.currentHealth = 100;
            var owner = ownerObject.AddComponent<RestStopHoldout>(); var second = secondObject.AddComponent<RestStopHoldout>();
            player.ApplyContinuousRoutePose(new Vector3(10,0,20),Vector3.right,2);
            var position = player.transform.position; var rotation = player.transform.rotation;
            TimeManager.isGameRunning = true; TimeManager.timeFactor = 1;
            player.SetStationaryCombat(owner,true);
            typeof(PlayerScript).GetMethod("PlayerMove",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(player,new object[]{500f});
            Assert.That(Vector3.Distance(position,player.transform.position),Is.Zero);
            Assert.That(Quaternion.Angle(rotation,player.transform.rotation),Is.Zero);
            player.SetStationaryCombat(second,false); Assert.That(player.IsStationaryCombat,Is.True);
            player.SetStationaryCombat(owner,false); Assert.That(player.IsStationaryCombat,Is.False);Assert.That(player.HoldoutAim,Is.Null);
        }
        finally { TimeManager.isGameRunning=running;TimeManager.timeFactor=factor;Object.DestroyImmediate(root);Object.DestroyImmediate(ownerObject);Object.DestroyImmediate(secondObject); }
    }
    [Test]
    public void RouteRunResetClearsBothDecisionsAndRecoveryClaims()
    {
        var root=new GameObject("Route reset");
        try
        {
            var route=root.AddComponent<HighwayRoute>();route.centers=new[]{Vector3.zero,Vector3.forward*100};route.length=100;
            route.forks=new[]{new HighwayRoute.Fork{start=10,end=80,decided=true,bypass=true,rewarded=true}};
            route.BeginRun();Assert.That(route.Distance,Is.Zero);
            Assert.That(route.forks[0].decided||route.forks[0].bypass||route.forks[0].rewarded,Is.False);
            Assert.That(route.Point(0),Is.EqualTo(Vector3.zero));Assert.That(route.Point(100),Is.EqualTo(Vector3.forward*100));
        }
        finally {Object.DestroyImmediate(root);}
    }
    [Test]
    public void StationaryHoldoutKeepsNormalCameraWhileBodyAimsAroundFullCircle()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        try
        {
            var root = new GameObject("Camera continuity player"); SceneManager.MoveGameObjectToScene(root, scene);
            var player = root.AddComponent<PlayerScript>(); player.currentHealth = 100;
            player.ApplyContinuousRoutePose(new Vector3(10, .12f, 20), Vector3.forward, 0);
            var visual = new GameObject("Original").transform; visual.SetParent(root.transform, false);
            var cameraObject = new GameObject("Normal gameplay camera"); cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = 40;
            camera.transform.localPosition = new Vector3(.08f, 8.36f, -12.85f);
            camera.transform.localRotation = Quaternion.Euler(17.47f, 0, 0);
            var cameraPosition = camera.transform.localPosition; var cameraRotation = camera.transform.localRotation;
            var playerPosition = player.transform.position; var playerRotation = player.transform.rotation;
            var ownerObject = new GameObject("Open hall"); SceneManager.MoveGameObjectToScene(ownerObject, scene);
            var owner = ownerObject.AddComponent<RestStopHoldout>(); owner.center = ownerObject.transform;
            owner.police = System.Array.Empty<EnemyEventController>();
            typeof(RestStopHoldout).GetField("player", flags).SetValue(owner, player);
            typeof(RestStopHoldout).GetField("battleCamera", flags).SetValue(owner, camera);
            typeof(RestStopHoldout).GetMethod("BeginEncounter", flags).Invoke(owner, null);
            var enemyObject = new GameObject("Human police target"); SceneManager.MoveGameObjectToScene(enemyObject, scene);
            var enemy = enemyObject.AddComponent<EnemyScript_space>();
            typeof(EnemyScript_space).GetField("_health", flags).SetValue(enemy, 700f);
            typeof(RestStopHoldout).GetField("target", flags).SetValue(owner, enemy);
            foreach (var direction in new[] { Vector3.left, Vector3.right, Vector3.back, Vector3.forward })
            {
                enemyObject.transform.position = playerPosition + direction * 8;
                typeof(RestStopHoldout).GetMethod("LateUpdate", flags).Invoke(owner, null);
                Assert.That(Vector3.Distance(camera.transform.localPosition, cameraPosition), Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(camera.transform.localRotation, cameraRotation), Is.LessThan(.0001f));
                Assert.That(camera.fieldOfView, Is.EqualTo(40));
                Assert.That(Vector3.Angle(visual.forward, direction), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(player.transform.position, playerPosition), Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(player.transform.rotation, playerRotation), Is.LessThan(.0001f));
            }
            typeof(RestStopHoldout).GetMethod("End", flags).Invoke(owner, new object[] { false });
            Assert.That(player.IsStationaryCombat, Is.False);
            Assert.That(Quaternion.Angle(visual.localRotation, Quaternion.identity), Is.LessThan(.0001f));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    [TestCase(0, 0)] [TestCase(50, -30)] [TestCase(100, 0)]
    public void BypassSeparatesAndRejoinsWithoutAJump(float distance, float expected)
        => Assert.That(HighwayRoute.BranchOffset(distance, 0, 100, -30), Is.EqualTo(expected).Within(.0001));

    [TestCase(0, 0)] [TestCase(9.99f, 0)] [TestCase(10, 1)] [TestCase(20, 2)] [TestCase(30, 2)]
    public void HoldoutHasThreeTenSecondPhases(float time, int expected)
        => Assert.That(RestStopHoldout.Phase(time, 30), Is.EqualTo(expected));

    [Test]
    public void PoliceUseDifferentOppositeDoorsThenAllFour()
    {
        Assert.That(Enumerable.Range(0, 4).Select(i => RestStopHoldout.Door(i, 0)).Distinct(), Is.EquivalentTo(new[] { 0, 1 }));
        Assert.That(Enumerable.Range(0, 4).Select(i => RestStopHoldout.Door(i, 1)).Distinct(), Is.EquivalentTo(new[] { 2, 3 }));
        Assert.That(Enumerable.Range(0, 4).Select(i => RestStopHoldout.Door(i, 2)).Distinct(), Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
    }
    [Test]
    public void FastTrafficUsesSweptContactAndLeavesAdjacentLaneSafe()
    {
        Assert.That(HighwayOncomingTraffic.SweptContact(new Vector3(0,0,12), new Vector3(0,0,-12), 1.65f), Is.True);
        Assert.That(HighwayOncomingTraffic.SweptContact(new Vector3(3.3f,0,12), new Vector3(3.3f,0,-12), 1.65f), Is.False);
        Assert.That(HighwayOncomingTraffic.SweptContact(new Vector3(0,0,12), new Vector3(0,0,8), 1.65f), Is.False);
    }
    [Test]
    public void HighwayHasSupportedContinuousCurvesAndBothBranches()
    {
        InScene(HighwaySceneBuilder.ScenePath, scene =>
        {
            var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool");
            var route = map.GetComponent<HighwayRoute>(); Assert.That(route, Is.Not.Null);
            Assert.That(route.forks.Length, Is.EqualTo(2));
            Assert.That(map.GetComponentsInChildren<NoryangjinTurnSpot>(true), Is.Empty, "Continuous route must not be interrupted by old paused turns");
            var roads = map.transform.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
            foreach (bool branch in new[] { false, true })
                for (float d = 0; d < route.length; d += 4)
                {
                    route.Sample(d, branch, out var p, out var f);
                    foreach (float lane in new[] { -4.4f, 0, 4.4f })
                    {
                        var q = p + Vector3.Cross(Vector3.up, f) * lane;
                        Assert.That(roads.Any(c => c.Raycast(new Ray(q + Vector3.up, Vector3.down), out _, 2)), Is.True, $"Unsupported {d}, lane {lane}, bypass {branch}");
                    }
                    route.Sample(d + 1, branch, out var next, out var tangent);
                    Assert.That(Vector3.Distance(p, next), Is.LessThan(2), "No centerline discontinuity");
                    Assert.That(Vector3.Angle(f, tangent), Is.LessThan(12), "No abrupt heading snap");
                }
            var traffic = map.GetComponentInChildren<HighwayOncomingTraffic>();
            Assert.That(traffic.cars.Length, Is.EqualTo(4));
            foreach (var beat in traffic.beats)
            {
                Assert.That(beat.lanes.Length, Is.EqualTo(beat.delays.Length));
                Assert.That(beat.lanes.Distinct().Count(), Is.LessThan(3), "At least one whole lane remains free");
            }
        });
    }
    [Test]
    public void FoodHallHasFourDoorsAndBoundedRealRiggedPolice()
    {
        InScene(RestStopChapterBuilder.ScenePath, scene =>
        {
            var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool");
            var holdout = map.GetComponentInChildren<RestStopHoldout>(true);
            Assert.That(holdout, Is.Not.Null); Assert.That(holdout.entrances.Length, Is.EqualTo(4));
            Assert.That(holdout.police.Length, Is.EqualTo(16)); Assert.That(holdout.hud, Is.Not.Null);
            foreach (var police in holdout.police)
            {
                Assert.That(police.gameObject.activeSelf, Is.False);
                Assert.That(police.TargetPoint, Is.Not.Null);
                Assert.That(police.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.Not.Null);
                Assert.That(police.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length, Is.GreaterThan(10));
            }
            var rows = EncounterPlacementTables.Rows.Where(r => r.scene == "RestStop").ToArray();
            Assert.That(rows.Count(r => !r.enabled), Is.EqualTo(8), "Outdoor stations 11/12 are replaced, not stacked on the interior");
            var roads = map.transform.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
            foreach (var entrance in holdout.entrances)
                for (int i = 0; i <= 20; i++)
                {
                    var p = Vector3.Lerp(entrance.position, holdout.center.position, i / 20f);
                    Assert.That(roads.Any(c => c.Raycast(new Ray(p + Vector3.up, Vector3.down), out _, 2)), Is.True);
                }
        });
    }
    private static void InScene(string path, System.Action<Scene> verify)
    {
        var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try { Physics.SyncTransforms(); verify(scene); }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
