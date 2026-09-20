using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class InstallSeagullContact
{
 const string Prefix="SeagullContact_";
 public static string Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  var scene=SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18"||scene.isDirty)throw new Exception("Clean SR18 required");
  string path="Assets/ShooterSurvival/Prefabs/Obstacle_Real/Seagull.prefab";var root=PrefabUtility.LoadPrefabContents(path);int count=0;
  try{count=Build(root.GetComponent<ObstacleStats>());PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  var birds=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ObstacleStats>(true)).Where(o=>o.obstaclePattern==ObstaclePattern.Seagull).ToArray();
  foreach(var bird in birds)
  {
   bird.value=20;bird.seagullSpinSeconds=1.2f;bird.seagullLandingOffset=.14625f;
   foreach(var old in bird.balloon.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(old);
   foreach(var collider in bird.balloon.GetComponentsInChildren<Collider>(true)){collider.isTrigger=true;collider.enabled=false;Record(collider);}
   var rb=bird.GetComponent<Rigidbody>();if(rb==null)rb=bird.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
   Record(rb);Record(bird);
  }
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();
  return "Saved "+count+" bone-following contact boxes on prefab; "+birds.Length+" scene bird, 20% maximum HP, two-turn spin";
 }
 static void Record(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
 static int Build(ObstacleStats stats)
 {
  var rig=stats.balloon;rig.gameObject.SetActive(true);var animator=rig.GetComponent<Animator>();
  var bones=rig.GetComponentsInChildren<Transform>(true);
  var original=bones.Select(t=>(t,p:t.localPosition,r:t.localRotation,s:t.localScale)).ToArray();
  foreach(var t in bones.Where(t=>t.name.StartsWith(Prefix)).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  foreach(var c in rig.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
  foreach(var rb in rig.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(rb);
  var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>(true);var weights=skin.sharedMesh.boneWeights;
  var points=new Dictionary<Transform,List<Vector3>>();var mesh=new Mesh();
  foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())foreach(float fraction in new[]{0f,.25f,.5f,.75f})
  {
   clip.SampleAnimation(animator.gameObject,clip.length*fraction);skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
   for(int i=0;i<vertices.Length;i++)
   {
    var w=weights[i];int index=w.boneIndex0;float best=w.weight0;
    if(w.weight1>best){best=w.weight1;index=w.boneIndex1;}if(w.weight2>best){best=w.weight2;index=w.boneIndex2;}if(w.weight3>best)index=w.boneIndex3;
    var bone=skin.bones[index];if(!points.TryGetValue(bone,out var list)){list=new List<Vector3>();points.Add(bone,list);}
    list.Add(bone.InverseTransformPoint(skin.transform.TransformPoint(vertices[i])));
   }
  }
  UnityEngine.Object.DestroyImmediate(mesh);
  foreach(var item in original)if(item.t!=null){item.t.SetLocalPositionAndRotation(item.p,item.r);item.t.localScale=item.s;}
  foreach(var pair in points)
  {
   var bounds=new Bounds(pair.Value[0],Vector3.zero);foreach(var p in pair.Value)bounds.Encapsulate(p);bounds.Expand(.008f);
   var go=new GameObject(Prefix+pair.Key.name);go.transform.SetParent(pair.Key,false);go.layer=stats.gameObject.layer;
   var collider=go.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;collider.isTrigger=true;collider.enabled=false;
  }
  stats.value=20;stats.seagullSpinSeconds=1.2f;stats.seagullLandingOffset=.14625f;
  var owner=stats.GetComponent<Rigidbody>();if(owner==null)owner=stats.gameObject.AddComponent<Rigidbody>();owner.isKinematic=true;owner.useGravity=false;
  var rootCollider=stats.GetComponent<Collider>();if(rootCollider!=null)rootCollider.enabled=false;
  return points.Count;
 }
}
