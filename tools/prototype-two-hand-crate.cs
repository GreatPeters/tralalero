using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
public static class PrototypeTwoHandCrate
{
 public static string Main()
 {
  string folder="map-concepts/two-hand-crate-2026-09-20/prototype-natural";Directory.CreateDirectory(folder);
  var source=UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(e=>e.name.Contains("FatMan"));
  var go=UnityEngine.Object.Instantiate(source.gameObject);go.hideFlags=HideFlags.HideAndDontSave;go.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
  var a=go.GetComponentInChildren<Animator>();a.transform.localRotation=Quaternion.identity;
  var prop=go.GetComponentsInChildren<SimpleProjectile>(true).First().transform;prop.SetParent(a.transform,false);prop.localRotation=Quaternion.identity;
  var bounds=prop.GetComponent<MeshFilter>().sharedMesh.bounds;
  Vector3 size=new Vector3(.76f,.28f,.36f);prop.localScale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
  foreach(var c in go.GetComponentsInChildren<Canvas>(true))c.gameObject.SetActive(false);
  var clip=a.runtimeAnimatorController.animationClips.First(c=>c.name=="Fatman_ThrowShort");
  clip.SampleAnimation(a.gameObject,0);a.transform.localRotation=Quaternion.identity;
  var handL=a.GetBoneTransform(HumanBodyBones.LeftHand);var handR=a.GetBoneTransform(HumanBodyBones.RightHand);
  var handRotL=handL.rotation;var handRotR=handR.rotation;
  var skin=go.GetComponentInChildren<SkinnedMeshRenderer>(true);var baked=new Mesh();skin.BakeMesh(baked,true);
  Vector3 Palm(Transform hand)
  {
   var vertices=baked.vertices;var weights=skin.sharedMesh.boneWeights;var selected=new List<Vector3>();
   for(int i=0;i<weights.Length;i++)
   {var w=weights[i];float weight=0;foreach(var pair in new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1),(w.boneIndex2,w.weight2),(w.boneIndex3,w.weight3)})if(skin.bones[pair.Item1]==hand||skin.bones[pair.Item1].IsChildOf(hand))weight+=pair.Item2;if(weight>.5f)selected.Add(hand.InverseTransformPoint(skin.transform.TransformPoint(vertices[i])));}
   if(selected.Count==0)throw new Exception("No hand vertices");var b=new Bounds(selected[0],Vector3.zero);foreach(var p in selected)b.Encapsulate(p);return b.center;
  }
  Vector3 palmL=Palm(handL),palmR=Palm(handR);UnityEngine.Object.DestroyImmediate(baked);
  var results=new List<object>();
  foreach(float t in new[]{0,.15f,.3f,.45f,.6f})
  {
   clip.SampleAnimation(a.gameObject,t);a.transform.localRotation=Quaternion.identity;
   var chest=a.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.Chest).position);
   Vector3 center=new Vector3(chest.x,chest.y+.065f,chest.z+.64f);
   prop.localPosition=center-Vector3.Scale(bounds.center,prop.localScale);
   foreach(bool right in new[]{false,true})
   {
    var shoulder=a.GetBoneTransform(right?HumanBodyBones.RightShoulder:HumanBodyBones.LeftShoulder);
    var upper=a.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
    var fore=a.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
    var hand=a.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
    Vector3 before=upper.position-shoulder.position;
    shoulder.rotation=Quaternion.FromToRotation(before,before+a.transform.TransformVector(Vector3.forward*.08f))*shoulder.rotation;
    Vector3 grip=a.transform.TransformPoint(center+new Vector3((right?1:-1)*(size.x*.5f+.035f),size.y*.30f,-size.z*.5f+.04f));
    Vector3 palmOffset=(right?handRotR:handRotL)*Vector3.Scale(right?palmR:palmL,hand.lossyScale);
    Solve(upper,fore,hand,grip-palmOffset,a.transform.TransformDirection(new Vector3(right?1:-1,-.6f,.1f)));
    hand.rotation=right?handRotR:handRotL;
    results.Add(new{t,right,error=Vector3.Distance(hand.TransformPoint(right?palmR:palmL),grip),palm=(right?palmR:palmL).ToString("F6"),handRotation=(right?handRotR:handRotL).ToString("F6")});
   }
   var visible=new GameObject("Pose snapshot");visible.transform.SetParent(skin.transform,false);var poseMesh=new Mesh();skin.BakeMesh(poseMesh,true);visible.AddComponent<MeshFilter>().sharedMesh=poseMesh;visible.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
   foreach(bool side in new[]{false,true})Capture(a.transform.position,center,folder+"/t-"+t.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+(side?"-side":"-front")+".png",side);
   skin.enabled=true;UnityEngine.Object.DestroyImmediate(visible);UnityEngine.Object.DestroyImmediate(poseMesh);
  }
  UnityEngine.Object.DestroyImmediate(go);
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(x=>x.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)results});File.WriteAllText(folder+"/metrics.json",json);return json;
 }
 public static void Solve(Transform upper,Transform fore,Transform hand,Vector3 target,Vector3 pole)
 {
  var origin=upper.position;float a=Vector3.Distance(origin,fore.position),b=Vector3.Distance(fore.position,hand.position);
  var delta=target-origin;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);var forward=delta.normalized;
  var bend=Vector3.ProjectOnPlane(pole,forward).normalized;float along=(a*a-b*b+distance*distance)/(2*distance);float height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
  var elbow=origin+forward*along+bend*height;
  upper.rotation=Quaternion.FromToRotation(fore.position-origin,elbow-origin)*upper.rotation;
  fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,target-fore.position)*fore.rotation;
 }
 static void Capture(Vector3 origin,Vector3 center,string path,bool side)
 {
  var go=new GameObject("Two hand preview"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();var target=origin+Vector3.up*1.65f;cam.transform.position=target+(side?new Vector3(5,1.5f,1):new Vector3(2.6f,1.6f,5));cam.transform.LookAt(target);cam.fieldOfView=42;cam.nearClipPlane=.05f;cam.farClipPlane=30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.24f,.28f,.33f);
  var rt=new RenderTexture(640,640,24);var tex=new Texture2D(640,640,TextureFormat.RGB24,false);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
 }
}
