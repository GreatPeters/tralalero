using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HighwayChapterIntegrationTests
{
    [Test] public void AuthoredChapter_MatchesWorkbookAndHasAllSixCombatModels()
    {
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(HighwaySceneBuilder.ScenePath);
        bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(HighwaySceneBuilder.ScenePath,OpenSceneMode.Additive);
        try
        {
            var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
            var actors=map.Find("Enemies").GetComponentsInChildren<EnemyScript_space>(true);
            Assert.That(actors.Length,Is.EqualTo(50));
            Assert.That(actors.Select(a=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(a.gameObject)).Distinct().Count(),Is.EqualTo(6));
            var rows=EncounterPlacementTables.Rows.Where(r=>r.scene=="HighWay").ToArray();Assert.That(rows.Length,Is.EqualTo(100));
            foreach(var row in rows)
            {
                var parent=map.Find(row.kind=="적 배치"?"Enemies":row.kind=="보너스 배치"?"Bonuses":"Props");
                var placement=parent.Find(row.id);Assert.That(placement,Is.Not.Null,row.id);
                if(row.kind=="적 배치")
                {
                    Assert.That(row.hasCombatStats&&row.health>0,Is.True,row.id);
                    if(row.mode==EnemyEventMode.Shoot)Assert.That(placement.GetComponent<EnemyScript_space>().HasConfiguredProjectile,Is.True,row.id);
                }
                else if(row.kind=="기믹 배치")
                {var parts=placement.GetComponentsInChildren<ObstacleStats>(true).Where(p=>p.gameObject.activeSelf).ToArray();Assert.That(parts.Length,Is.GreaterThan(0));Assert.That(parts.All(p=>p.obstaclePattern==row.pattern),Is.True);Assert.That(row.hasHighwaySettings,Is.True);}
            }
            var colliders=map.Find("Roads").GetComponentsInChildren<MeshCollider>();
            var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerScript>(true)).Single();
            var follower=player.GetComponent<NoryangjinRoadHeightFollower>();
            Assert.That(follower.RoadRoot,Is.SameAs(map.Find("Roads")),"The copied player must reference the new chapter's roads.");
            var occlusion=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NoryangjinCameraOcclusion>(true)).Single();
            Assert.That(new SerializedObject(occlusion).FindProperty("roadRoot").objectReferenceValue,Is.SameAs(map.Find("Roads")));
            Assert.That(new SerializedObject(occlusion).FindProperty("additionalOccluderGroups").arraySize,Is.GreaterThan(0),"Overhead scenery must join camera visibility checks.");
            float previousHeight=.12f;
            for(float d=0;d<=HighwaySceneBuilder.Length;d+=4)
            {
                var route=HighwaySceneBuilder.Sample(d,out var forward);var proposed=route;proposed.y=previousHeight;
                Assert.That(follower.TryProjectPosition(proposed,forward,out var supported),Is.True,"Height follow at "+d);
                // Flat14m turn pads overlap the shallow ramp ends by at most7cm.
                Assert.That(supported.y,Is.EqualTo(route.y+.12f).Within(.08f));previousHeight=supported.y;
            }
            foreach(float distance in new[]{-5f,0f,40f,419f,420f,421f,679f,680f,681f,1219f,1220f,1221f,1579f,1580f,1581f,2079f,2080f,2081f,2330f})
            {
                var p=distance<0?HighwaySceneBuilder.Points[0]+Vector3.forward*distance:HighwaySceneBuilder.Sample(distance,out _);
                Assert.That(colliders.Any(c=>c.Raycast(new Ray(p+Vector3.up*2,Vector3.down),out _,4)),Is.True,"Missing road support at "+distance);
            }
            Assert.That(EditorBuildSettings.scenes.Any(s=>s.path==HighwaySceneBuilder.ScenePath&&s.enabled),Is.True);
        }
        finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
