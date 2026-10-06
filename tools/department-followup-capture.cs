using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class DepartmentFollowupCapture {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle editor required");
  if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Dirty scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();string folder="outputs/department-store-2026-10-02/followup-v4/visual-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");Directory.CreateDirectory(folder);
  try{
   EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");var d=Object.FindFirstObjectByType<Chapter45Director>();var player=Object.FindFirstObjectByType<PlayerScript>();var c=Camera.main;
   var offset=c.transform.position-player.transform.position;var rotation=c.transform.rotation;
   foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
   foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible(g.floor==0);
   var lamps=d.transform.Find("Scenery/DepartmentStore_20261002").GetComponentsInChildren<Light>(true);
   foreach(float station in new[]{0f,45f,175f,320f}){
    d.route.Sample(station,out var center,out var forward);var yaw=Quaternion.LookRotation(forward);player.transform.SetPositionAndRotation(center+Vector3.up*d.footOffset,yaw);c.transform.SetPositionAndRotation(player.transform.position+yaw*offset,yaw*rotation);
    foreach(var l in lamps)l.enabled=false;Shot(c,folder+"/lights-off-"+station.ToString("0000")+".png");
    foreach(var l in lamps)l.enabled=true;Shot(c,folder+"/final-"+station.ToString("0000")+".png");
   }
   c.transform.position=new Vector3(-61,17,-45);c.transform.LookAt(new Vector3(-40,8,-12));
   foreach(var l in lamps)l.enabled=false;Shot(c,folder+"/architecture-lights-off.png");foreach(var l in lamps)l.enabled=true;Shot(c,folder+"/architecture-final.png");
   c.transform.position=new Vector3(-43,5.5f,12);c.transform.LookAt(new Vector3(-40,2.0f,6));Shot(c,folder+"/reused-F10-kiosk.png");
   File.WriteAllText(folder+"/conditions.txt","Native final saved scene. Route poses use unchanged gameplay camera offset/lens with UI hidden. Off/on pairs toggle only six newly-owned shadowless warm lights; all other geometry/light/camera state fixed. Architecture and kiosk views are explicitly directed review cameras. No scene save, no ordinary-play or mobile-FPS claim.");
   return new{folder,images=Directory.GetFiles(folder,"*.png"),lamps=lamps.Length};
  }finally{EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
 static void Shot(Camera c,string path){var rt=RenderTexture.GetTemporary(720,1280,24,RenderTextureFormat.ARGB32);var target=c.targetTexture;var active=RenderTexture.active;float aspect=c.aspect;try{c.targetTexture=rt;c.aspect=720f/1280;c.Render();RenderTexture.active=rt;var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);}finally{c.targetTexture=target;c.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}}
}
