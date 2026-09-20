using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class InspectSeagullContact
{
 public static string Main()
 {
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Obstacle_Real/Seagull.prefab");
  var go=UnityEngine.Object.Instantiate(source);go.hideFlags=HideFlags.HideAndDontSave;go.transform.position=new Vector3(10000,0,10000);
  var stats=go.GetComponent<ObstacleStats>();stats.balloon.gameObject.SetActive(true);var animator=stats.balloon.GetComponent<Animator>();
  var skin=stats.balloon.GetComponentInChildren<SkinnedMeshRenderer>(true);var mesh=new Mesh();var output=new List<object>();
  foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())foreach(float fraction in new[]{0f,.25f,.5f,.75f})
  {
   clip.SampleAnimation(animator.gameObject,clip.length*fraction);skin.BakeMesh(mesh,true);
   var vertices=mesh.vertices.Select(v=>stats.balloon.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();
   var b=new Bounds(vertices[0],Vector3.zero);foreach(var p in vertices)b.Encapsulate(p);
   output.Add(new{clip=clip.name,fraction,center=b.center.ToString("F5"),size=b.size.ToString("F5"),minimum=b.min.ToString("F5"),bone=skin.rootBone.name});
  }
  UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(go);
  return (string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)output});
 }
}
