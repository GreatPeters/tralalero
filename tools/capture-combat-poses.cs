using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
public static class CaptureCombatPoses
{
 public static string Main(string suffix="before")
 {
  string folder="map-concepts/combat-route-fixes-2026-09-20/poses-"+suffix;Directory.CreateDirectory(folder);
  foreach(string kind in new[]{"Guard","FatMan"})
  {
   var source=UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(e=>e.name.Contains(kind));
   var go=UnityEngine.Object.Instantiate(source.gameObject);go.name="PoseSample";go.hideFlags=HideFlags.HideAndDontSave;
   go.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);
   var a=go.GetComponentInChildren<Animator>();a.transform.localRotation=Quaternion.identity;
   foreach(var c in go.GetComponentsInChildren<Canvas>(true))c.gameObject.SetActive(false);
   var clips=a.runtimeAnimatorController.animationClips.Distinct().Where(c=>c.name.IndexOf("Act",StringComparison.OrdinalIgnoreCase)>=0||c.name.Contains("ThrowShort")).ToArray();
   foreach(var clip in clips)foreach(float t in new[]{.0f,.35f,.7f})
   {
    clip.SampleAnimation(a.gameObject,Mathf.Min(t,clip.length));
    Capture(go,folder+"/"+kind+"-"+clip.name.Replace(" ","_")+"-"+t.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png",6f);
   }
   UnityEngine.Object.DestroyImmediate(go);
  }
  var ship=UnityEngine.Object.FindObjectsByType<ObstacleStats>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(s=>s.obstaclePattern==ObstaclePattern.Ship);
  var boat=UnityEngine.Object.Instantiate(ship.gameObject);boat.hideFlags=HideFlags.HideAndDontSave;boat.transform.SetPositionAndRotation(new Vector3(10000,0,10000),Quaternion.identity);Capture(boat,folder+"/ship.png",8f);UnityEngine.Object.DestroyImmediate(boat);
  return folder;
 }
 static void Capture(GameObject go,string path,float distance)
 {
  var cameraGo=new GameObject("PoseCamera"){hideFlags=HideFlags.HideAndDontSave};var c=cameraGo.AddComponent<Camera>();
  c.backgroundColor=new Color(.22f,.26f,.31f);c.clearFlags=CameraClearFlags.SolidColor;c.nearClipPlane=.05f;c.farClipPlane=50f;c.fieldOfView=42;
  var center=go.transform.position+Vector3.up*1.4f;c.transform.position=center+new Vector3(1,.55f,1.5f).normalized*distance;c.transform.LookAt(center);
  var rt=new RenderTexture(640,640,24);c.targetTexture=rt;c.Render();var old=RenderTexture.active;RenderTexture.active=rt;
  var image=new Texture2D(640,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
  RenderTexture.active=old;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraGo);
 }
}
