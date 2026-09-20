using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class PrototypeSyncedCrateThrow
{
 public static string Main()
 {
  string folder="map-concepts/synced-crate-throw-2026-09-20/prototype";Directory.CreateDirectory(folder);
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JH/Model/Prefab/Enemy_FatMan.prefab");var go=UnityEngine.Object.Instantiate(source);go.hideFlags=HideFlags.HideAndDontSave;
  go.transform.localScale=Vector3.one*1.6875f;go.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
  var pose=go.GetComponent<FatManCratePose>();var a=pose.Model;var prop=pose.Crate;a.transform.localRotation=Quaternion.identity;
  a.runtimeAnimatorController.animationClips.First(c=>c.name=="Fatman Idle").SampleAnimation(a.gameObject,0);a.transform.localRotation=Quaternion.identity;
  var bones=a.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")).Select(t=>(t,p:t.localPosition,r:t.localRotation,s:t.localScale)).ToArray();
  var chest=a.GetBoneTransform(HumanBodyBones.Chest);var spine=a.GetBoneTransform(HumanBodyBones.Spine);
  var baseChest=a.transform.InverseTransformPoint(chest.position);var shape=prop.GetComponent<MeshFilter>().sharedMesh.bounds;
  var skin=go.GetComponentInChildren<SkinnedMeshRenderer>(true);var weights=skin.sharedMesh.boneWeights;var mesh=new Mesh();
  bool Body(int index){var n=skin.bones[index].name;return n.Contains("Spine")||n.Contains("Hips")||n.Contains("Neck")||n.Contains("Head");}
  var ids=Enumerable.Range(0,weights.Length).Where(i=>{var w=weights[i];return (Body(w.boneIndex0)?w.weight0:0)+(Body(w.boneIndex1)?w.weight1:0)+(Body(w.boneIndex2)?w.weight2:0)+(Body(w.boneIndex3)?w.weight3:0)>.5f;}).ToArray();
  foreach(var c in go.GetComponentsInChildren<Canvas>(true))c.gameObject.SetActive(false);
  var results=new List<object>();
  foreach(float p in new[]{0f,.3f,.5f,.75f,1f})
  {
   foreach(var b in bones){b.t.SetLocalPositionAndRotation(b.p,b.r);b.t.localScale=b.s;}
   float wind=Mathf.SmoothStep(0,1,Mathf.Clamp01(p/.3f));float stroke=Mathf.Clamp01((p-.3f)/.7f);stroke*=stroke;
   spine.rotation=Quaternion.AngleAxis(10*stroke,a.transform.right)*spine.rotation;chest.rotation=Quaternion.AngleAxis(8*stroke,a.transform.right)*chest.rotation;
   var center=baseChest+new Vector3(0,.065f-.035f*wind+.05f*stroke,.64f+.16f*stroke);
   prop.localPosition=center-Vector3.Scale(shape.center,prop.localScale);prop.localRotation=Quaternion.identity;
   float maxError=0;
   foreach(bool r in new[]{false,true})
   {
    var shoulder=a.GetBoneTransform(r?HumanBodyBones.RightShoulder:HumanBodyBones.LeftShoulder);var upper=a.GetBoneTransform(r?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var fore=a.GetBoneTransform(r?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var hand=a.GetBoneTransform(r?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
    var direction=upper.position-shoulder.position;shoulder.rotation=Quaternion.FromToRotation(direction,direction+a.transform.TransformVector(Vector3.forward*(.08f+.24f*stroke)))*shoulder.rotation;
    var palm=r?new Vector3(-.022836f,.124187f,.046591f):new Vector3(.030800f,.131140f,.022211f);
    var rotation=a.transform.rotation*(r?new Quaternion(.292698f,-.349589f,-.696775f,.553733f):new Quaternion(.331203f,.373332f,.723902f,.476336f));
    var grip=a.transform.TransformPoint(center+new Vector3((r?1:-1)*.415f,.084f,-.14f));
    FatManCratePose.SolveArm(upper,fore,hand,grip-rotation*Vector3.Scale(palm,hand.lossyScale),a.transform.TransformDirection(new Vector3(r?1:-1,-.6f,.1f)));hand.rotation=rotation;
    maxError=Mathf.Max(maxError,Vector3.Distance(hand.TransformPoint(palm),grip));
   }
   skin.BakeMesh(mesh,true);var vertices=mesh.vertices;int inside=ids.Count(i=>shape.Contains(prop.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]))));
   results.Add(new{p,stroke,maxError,inside,center=center.ToString("F4")});
   var snapshot=new GameObject("Baked pose");snapshot.transform.SetParent(skin.transform,false);snapshot.AddComponent<MeshFilter>().sharedMesh=mesh;snapshot.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
   Capture(go.transform.position,folder+"/pose-"+p.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png");skin.enabled=true;UnityEngine.Object.DestroyImmediate(snapshot);
  }
  UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(go);
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(x=>x.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)results});File.WriteAllText(folder+"/metrics.json",json);return json;
 }
 static void Capture(Vector3 origin,string path)
 {
  var go=new GameObject("Synchronized throw preview");var c=go.AddComponent<Camera>();var target=origin+Vector3.up*1.65f;c.transform.position=target+new Vector3(3,1.5f,5);c.transform.LookAt(target);c.fieldOfView=44;c.farClipPlane=30;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.24f,.28f,.33f);
  var rt=new RenderTexture(640,640,24);var tex=new Texture2D(640,640,TextureFormat.RGB24,false);var previous=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);
 }
}
