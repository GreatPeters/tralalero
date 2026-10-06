using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class FitChapters45Hazards
{
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  var setup=EditorSceneManager.GetSceneManagerSetup();var records=new List<string>();
  try{foreach(var name in new[]{"Jamsil","ShoeTower"})
  {
   var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
   var director=Object.FindFirstObjectByType<Chapter45Director>();
   foreach(var h in director.GetComponentsInChildren<Chapter45Hazard>(true))
   {
    if(h.body==null)continue;
    float side=h.safeLane<0?1:-1;
    h.sweepFrom=new Vector3(side*6.5f,0,0);h.sweepTo=new Vector3(side*2.8f,0,0);
    if(!h.manualOnly)h.warningDistance=name=="Jamsil"?17:15;
    if(h.body.name=="SecuritySweep")
    {
     h.body.localScale=new Vector3(5.4f,1.8f,2.4f);
     var box=h.body.GetComponent<BoxCollider>();box.center=Vector3.zero;box.size=Vector3.one;box.isTrigger=true;
    }
    else
    {
     // Fit the actual taxi mesh in its own coordinate frame; no invisible oversized box.
     var meshes=h.body.GetComponentsInChildren<MeshFilter>(true);var points=new List<Vector3>();
     foreach(var mf in meshes)if(mf.sharedMesh!=null)
     {
      var b=mf.sharedMesh.bounds;
      for(int i=0;i<8;i++){var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));points.Add(h.body.InverseTransformPoint(mf.transform.TransformPoint(p)));}
     }
     if(points.Count==0)throw new Exception("Taxi mesh missing");var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
     var current=h.body.GetComponentsInChildren<Renderer>(true);var world=current[0].bounds;foreach(var r in current)world.Encapsulate(r.bounds);
     // Taxi is sideways to traffic. Normalize its visible long axis to 5.4m.
     float horizontal=Mathf.Max(world.size.x,world.size.z);h.body.localScale*=5.4f/horizontal;
     foreach(var collider in h.body.GetComponentsInChildren<Collider>(true))if(collider.gameObject!=h.body.gameObject)Object.DestroyImmediate(collider);
     var box=h.body.GetComponent<BoxCollider>()??h.body.gameObject.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;box.isTrigger=true;
    }
    if(h.footprint!=null)
    {
     var p=h.body.position+h.transform.TransformDirection(new Vector3(side*2.8f,0,0));p.y-=h.body.name=="SecuritySweep"?.965f: -.035f;
     h.footprint.transform.position=p;h.footprint.transform.rotation=h.transform.rotation;h.footprint.transform.localScale=new Vector3(5.4f,.035f,4.4f);
    }
    var fitted=h.body.GetComponent<BoxCollider>();records.Add(name+"/"+h.name+" collider="+fitted.bounds.size.ToString("F3")+" bodyScale="+h.body.localScale.ToString("F3"));
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  Directory.CreateDirectory("outputs/chapters45-2026-10-02/qa");File.WriteAllLines("outputs/chapters45-2026-10-02/qa/hazard-geometry.txt",records);return new{fitted=records.Count};
 }
}
