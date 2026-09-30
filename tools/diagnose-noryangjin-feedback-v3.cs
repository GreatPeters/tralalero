using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
public static class DiagnoseNoryangjinFeedbackV3
{
 public static object Main()
 {
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var cam=GameObject.Find("MapTool_Camera").GetComponent<Camera>();var oc=cam.GetComponent<NoryangjinCameraOcclusion>();
  var records=new System.Collections.Generic.List<object>();
  foreach(var bridge in new[]{false,true})
  {
   p.ApplyContinuousRoutePose(bridge?new Vector3(124.3f,.22f,-340):new Vector3(80,.16f,-7.15f),bridge?Vector3.back:Vector3.right,0);
   cam.transform.position=bridge?new Vector3(124.18f,12.72f,-320.74f):new Vector3(60.73f,12.71f,-7.27f);cam.transform.LookAt(p.transform.position+Vector3.up*2+ p.transform.forward*5);oc.RefreshVisibility(1);
   var groups=(Transform[])typeof(NoryangjinCameraOcclusion).GetField("clearViewGroups",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(oc);
   var body=p.transform.position+Vector3.up;var delta=body-cam.transform.position;var ray=new Ray(cam.transform.position,delta.normalized);
   var rows=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.forceRenderingOff&&r.bounds.IntersectRay(ray,out float d)&&d<delta.magnitude).Select(r=>new{r.name,path=Path(r.transform),bounds=r.bounds.ToString(),opacity=oc.SceneryOpacity(r),registered=groups.Any(t=>t!=null&&r.transform.IsChildOf(t)),mats=r.sharedMaterials.Select(m=>new{m.name,shader=m.shader.name,queue=m.renderQueue,color=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():m.HasProperty("_Color")?m.GetColor("_Color").ToString():"none",surface=m.HasProperty("_Surface")?m.GetFloat("_Surface"):-1,src=m.HasProperty("_SrcBlend")?m.GetFloat("_SrcBlend"):-1,dst=m.HasProperty("_DstBlend")?m.GetFloat("_DstBlend"):-1}).ToArray()}).ToArray();
   records.Add(new{bridge,faded=oc.FadedCount,rows});
  }
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{records});File.WriteAllText("outputs/noryangjin-feedback-v3-2026-09-28/occlusion-diagnosis.json",json);return records;
 }
 static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
}
