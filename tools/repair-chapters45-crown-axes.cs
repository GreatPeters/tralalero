// Final pass dependency: after any rebuild here, run RepairCrownInteriorReadability.Main and PolishTowerFinalText.Main; verify native portrait views before acceptance.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class RepairChapter45CrownAxes
{
 const string Root="Assets/ShooterSurvival/Models/Chapters/Chapters45/Crown";
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle editor required");
  var setup=EditorSceneManager.GetSceneManagerSetup();var log=new List<string>();
  try{foreach(var name in new[]{"Jamsil","ShoeTower"})
  {
   var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
   if(name=="Jamsil")Repair(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="GeneratedCrownAnchor"),84,0,false,log);
   else
   {
    var d=Object.FindFirstObjectByType<Chapter45Director>();
    Repair(d.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="CrownShell"),91,-90,true,log);
    Repair(d.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="WhiteShoeOffering"),2.7f,-90,false,log);
   }
   // New chapters must not ship the copied editor combat debug overlay.
   foreach(var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(b.GetType().Name=="CombatHarness"&&b.gameObject.scene==scene)b.enabled=false;
   var director=Object.FindFirstObjectByType<Chapter45Director>();foreach(var e in director.encounters)e.health=e.statPrefix=="c45_archive"?40:name=="Jamsil"?28:32;
   foreach(var t in director.targets)t.health=t.captain?224:40;
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  AssetDatabase.SaveAssets();File.WriteAllLines("outputs/chapters45-2026-10-02/qa/crown-native-axis-repair.txt",log);return log;
 }
 static void Repair(Transform anchor,float length,float yaw,bool interior,List<string> log)
 {
  foreach(int i in new[]{0,1,2})
  {
   var src=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/ShoeCrown_LOD"+i+".fbx");
   var child=anchor.Find("Crown_LOD"+i);if(child==null)throw new Exception("Missing crown LOD"+i);
   child.localPosition=Vector3.zero;child.localRotation=Quaternion.Euler(0,yaw,0)*src.transform.localRotation;child.localScale=src.transform.localScale;
   var sourceMeshes=src.GetComponentsInChildren<MeshFilter>(true);var filters=child.GetComponentsInChildren<MeshFilter>(true);
   if(sourceMeshes.Length!=filters.Length)throw new Exception("Crown mesh hierarchy changed");
   for(int j=0;j<filters.Length;j++)filters[j].sharedMesh=sourceMeshes[j].sharedMesh;
   var rs=child.GetComponentsInChildren<Renderer>(true);Bounds Bounds(){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
   var bounds=Bounds();child.localScale*=length/Mathf.Max(bounds.size.x,bounds.size.z);bounds=Bounds();
   child.position+=new Vector3(anchor.position.x-bounds.center.x,anchor.position.y-bounds.min.y,anchor.position.z-bounds.center.z);
   bounds=Bounds();log.Add(anchor.name+" LOD"+i+" authoredAxis="+src.transform.localEulerAngles+" full bounds="+bounds);
   if(interior)
   {
    float floor=235;float centerX=anchor.position.x;
    foreach(var filter in filters)
    {
     var mesh=Object.Instantiate(filter.sharedMesh);var v=mesh.vertices;
     for(int s=0;s<mesh.subMeshCount;s++)
     {
      var tri=mesh.GetTriangles(s);var kept=new List<int>();
      for(int k=0;k<tri.Length;k+=3)
      {
       var a=filter.transform.TransformPoint(v[tri[k]]);var b=filter.transform.TransformPoint(v[tri[k+1]]);var c=filter.transform.TransformPoint(v[tri[k+2]]);var centroid=(a+b+c)/3;
       // A real central cutaway in world space. Keep low perimeter/interior
       // structure, remove roof and heel/toe walls from the full play corridor.
       if(centroid.y>floor+6.5f || centroid.y>floor+.08f&&Mathf.Abs(centroid.x-centerX)<8.5f)continue;
       kept.Add(tri[k]);kept.Add(tri[k+1]);kept.Add(tri[k+2]);
      }
      mesh.SetTriangles(kept,s);
     }
     mesh.RecalculateBounds();string path=Root+"/WorldCutaway_"+i+"_"+filter.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}filter.sharedMesh=mesh;
    }
   }
  }
  anchor.GetComponent<LODGroup>()?.RecalculateBounds();
 }
}
