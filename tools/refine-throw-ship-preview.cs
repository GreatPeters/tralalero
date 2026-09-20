using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class RefineThrowShipPreview
{
 public static string Main(float lower=.25f)
 {
  string folder="map-concepts/throw-ship-refinement-2026-09-20";Directory.CreateDirectory(folder);
  var source=UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(e=>e.name.Contains("FatMan")&&!e.name.EndsWith("_Right"));
  var report=new List<object>();
  foreach(float arm in new[]{-1f,0f,1f})
  {
   float size=35f;
   var go=UnityEngine.Object.Instantiate(source.gameObject);go.hideFlags=HideFlags.HideAndDontSave;go.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
   var a=go.GetComponentInChildren<Animator>();a.transform.localRotation=Quaternion.identity;
   var box=go.GetComponentsInChildren<SimpleProjectile>(true).First().transform;box.localScale=Vector3.one*size;
   foreach(var c in go.GetComponentsInChildren<Canvas>(true))c.gameObject.SetActive(false);
   var clip=UnityEngine.Object.Instantiate(a.runtimeAnimatorController.animationClips.First(c=>c.name=="Fatman_ThrowShort"));
   var shape=box.GetComponent<MeshFilter>().sharedMesh.bounds;
   var grip=shape.center+new Vector3(shape.extents.x*arm,shape.extents.y,shape.extents.z*.65f);
   box.localPosition=-(box.localRotation*Vector3.Scale(grip,box.localScale));
   report.Add(new{arm,position=box.localPosition.ToString("F6")});
   foreach(float time in new[]{0,.15f,.3f,.45f,.6f})
   {
    clip.SampleAnimation(a.gameObject,time);var head=a.GetBoneTransform(HumanBodyBones.Head).position;
    var b=box.GetComponent<Renderer>().bounds;
    report.Add(new{arm,size,time,head=(head-go.transform.position).ToString(),min=(b.min-go.transform.position).ToString(),max=(b.max-go.transform.position).ToString(),distance=Vector3.Distance(b.ClosestPoint(head),head)});
    if(time==.45f||time==.6f)Capture(go.transform.position+Vector3.up*1.4f,folder+"/front-anchor-"+arm.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+"-"+time.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png");
   }
   UnityEngine.Object.DestroyImmediate(go);
   UnityEngine.Object.DestroyImmediate(clip);
  }
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)report});File.WriteAllText(folder+"/crate-samples.json",json);return json;
 }
 static void Capture(Vector3 target,string path)
 {
  var go=new GameObject("Crate preview"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.transform.position=target+new Vector3(3,2,5);cam.transform.LookAt(target);cam.fieldOfView=42;cam.nearClipPlane=.05f;cam.farClipPlane=30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.24f,.28f,.33f);
  var rt=new RenderTexture(640,640,24);var image=new Texture2D(640,640,TextureFormat.RGB24,false);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
 }
}
