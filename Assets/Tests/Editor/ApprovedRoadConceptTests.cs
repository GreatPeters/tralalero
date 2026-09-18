using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ApprovedRoadConceptTests
{
    [Test]
    public void HighwayHasTwoCarriagewaysWithDoubleYellowCenterAndSupportedOpposingLanes()
    {
        InScene(HighwaySceneBuilder.ScenePath,scene=>
        {
            var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var route=map.GetComponent<HighwayRoute>();
            var paint=map.Find("Roads/Four_Lane_Markings");Assert.That(paint,Is.Not.Null);
            var centers=paint.GetComponentsInChildren<LineRenderer>().Where(l=>l.name.StartsWith("Yellow_Center_")).ToArray();Assert.That(centers.Length,Is.EqualTo(2));
            var offsets=centers.Select(line=>{route.Sample(0,false,out var p,out var f);return Vector3.Dot(line.GetPosition(0)-p,Vector3.Cross(Vector3.up,f));}).OrderBy(x=>x).ToArray();
            Assert.That(offsets[0],Is.EqualTo(-7.17f).Within(.01f));Assert.That(offsets[1],Is.EqualTo(-6.83f).Within(.01f));
            Assert.That(paint.GetComponentsInChildren<LineRenderer>().Count(l=>l.name=="Outer_White_Edge"),Is.EqualTo(2));
            var roads=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
            for(float d=8;d<route.length-8;d+=16)
            {
                route.Sample(d,false,out var p,out var f);
                foreach(float lane in new[]{-10.5f,-17.5f})
                {var q=p+Vector3.Cross(Vector3.up,f)*lane;Assert.That(roads.Any(c=>c.Raycast(new Ray(q+Vector3.up*3,Vector3.down),out _,6)),Is.True,$"Opposing road support {d}/{lane}");}
            }
            var ambient=map.GetComponentInChildren<HighwayAmbientTraffic>(true);Assert.That(ambient.cars.Length,Is.EqualTo(6));Assert.That(ambient.GetComponentsInChildren<Collider>(true),Is.Empty);
        });
    }
    [Test]
    public void OpenHallKeepsAnUnobstructedCentralFloorAndRoofAboveTheScaledCamera()
    {
        InScene(RestStopChapterBuilder.ScenePath,scene=>
        {
            var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var hall=map.Find("Approved_Open_Hall_20260913");Assert.That(hall,Is.Not.Null);
            var floor=map.Find("Roads/FoodHall_Floor").GetComponent<Renderer>().bounds;Assert.That(floor.size.x,Is.EqualTo(48).Within(.02));Assert.That(floor.size.z,Is.EqualTo(66).Within(.02));
            foreach(var renderer in hall.GetComponentsInChildren<Renderer>(true))
            {
                var b=renderer.bounds;if(b.max.y<.25f||b.min.y>3)continue;
                bool overlap=b.min.x<hall.position.x+18&&b.max.x>hall.position.x-18&&b.min.z<hall.position.z+18&&b.max.z>hall.position.z-18;
                Assert.That(overlap,Is.False,"Central floor obstructed by "+renderer.name);
            }
            var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerScript>(true)).Single();var camera=player.GetComponentInChildren<Camera>(true);
            float cameraHeight=player.transform.TransformVector(camera.transform.localPosition).y;
            var roof=hall.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="Roof side"||r.name=="Roof end").ToArray();Assert.That(roof.Length,Is.EqualTo(4));Assert.That(roof.All(r=>r.bounds.min.y>hall.position.y+cameraHeight+2),Is.True);
            Assert.That(hall.GetComponentsInChildren<Collider>(true),Is.Empty);
        });
    }
    [Test]
    public void ReusedHumanRolesRetainTheirRigActionsAndTallerCollisionVolume()
    {
        foreach(string name in ChapterMascotImporter.Names)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ChapterMascotImporter.PrefabPath(name));var animator=prefab.GetComponentInChildren<Animator>(true);
            Assert.That(animator.runtimeAnimatorController.animationClips.Select(c=>c.name),Is.EquivalentTo(new[]{"idle","walk","run","attack_loop","attack_once","die"}),name);
            Assert.That(animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).All(r=>r.bones.Length==18&&r.bones.All(b=>b!=null)),Is.True,name);
            Assert.That(prefab.GetComponent<CapsuleCollider>().height,Is.GreaterThanOrEqualTo(2.1f),name);
        }
    }
    static void InScene(string path,System.Action<Scene> verify)
    {
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{Physics.SyncTransforms();verify(scene);}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
