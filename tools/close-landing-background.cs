using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class CloseLandingBackground {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene");
  const string path="Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity";EditorSceneManager.OpenScene(path);var d=Object.FindFirstObjectByType<Chapter45Director>();var scenery=d.transform.Find("DetailedMall_20261003/Scenery");
  if(scenery.GetComponentsInChildren<MeshFilter>(true).Any(m=>m.name=="Retained lower floor visual background"))throw new Exception("Already applied");
  File.Copy(path,"outputs/chapter45-detailed-design-2026-10-03/scene-recovery/ShoeTower-before-landing-background.unity",false);var rows=new List<object>();
  foreach(var lift in d.lifts.Where(l=>l.escalator)){
   int sourceFloor=d.route.segments[lift.afterSegment].floor,destFloor=d.route.segments[lift.Destination(lift.afterSegment)].floor;
   var lower=scenery.Cast<Transform>().Single(t=>t.GetComponent<Chapter45SceneryGroup>()?.floor==sourceFloor&&t.Find(sourceFloor+"F curved supported floor")!=null);
   var slab=lower.Find(sourceFloor+"F curved supported floor");var sourceMesh=slab.GetComponent<MeshFilter>().sharedMesh;string originalPath=AssetDatabase.GetAssetPath(sourceMesh).Replace("-escalator-opening.asset",".asset");var uncut=AssetDatabase.LoadAssetAtPath<Mesh>(originalPath);if(uncut==null)throw new Exception("Preserved uncut source slab missing");
   var group=scenery.Find(destFloor+"F retained lower escalator structure");var visibility=group.GetComponent<Chapter45SceneryGroup>();if(visibility.floor!=destFloor||!visibility.hideDuringTransfer)throw new Exception("Wrong visibility ownership");
   var copy=new GameObject("Retained lower floor visual background");copy.transform.SetParent(slab.parent,false);copy.transform.localPosition=slab.localPosition;copy.transform.localRotation=slab.localRotation;copy.transform.localScale=slab.localScale;copy.transform.SetParent(group,true);
   copy.AddComponent<MeshFilter>().sharedMesh=uncut;var render=copy.AddComponent<MeshRenderer>();render.sharedMaterials=slab.GetComponent<MeshRenderer>().sharedMaterials;render.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   if(Mathf.Abs(copy.transform.position.y-slab.position.y)>.001f||copy.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Visual copy changed height/collision");
   rows.Add(new{sourceFloor,destFloor,sourceMesh=originalPath,worldHeight=copy.transform.position.y,triangles=uncut.triangles.Length/3,colliders=0,hideDuringTransfer=visibility.hideDuringTransfer});
  }
  EditorSceneManager.MarkSceneDirty(d.gameObject.scene);if(!EditorSceneManager.SaveScene(d.gameObject.scene))throw new Exception("Save failed");return new{rows,routeAndCollisionUnchanged=true,source="Reused preserved original floor mesh; render-only lower background at original world height."};
 }
}
