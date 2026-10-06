using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class DepartmentCapture {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle editor required");
  if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Dirty scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();string folder="outputs/department-store-2026-10-02/art-detail-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");Directory.CreateDirectory(folder);
  try{
   EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity");
   var d=Object.FindFirstObjectByType<Chapter45Director>();var p=Object.FindFirstObjectByType<PlayerScript>();var c=Camera.main;var offset=c.transform.position-p.transform.position;var rot=c.transform.rotation;
   foreach(var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))cv.gameObject.SetActive(false);
   foreach(float station in new[]{1720f,1800f,1830f}){
    d.route.Sample(station,out var center,out var forward);var yaw=Quaternion.LookRotation(forward);p.transform.SetPositionAndRotation(center+Vector3.up*d.footOffset,yaw);c.transform.SetPositionAndRotation(p.transform.position+yaw*offset,yaw*rot);
    foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible(g.floor<=0&&(g.alwaysVisible||g.floor<0&&g.endDistance<=g.startDistance||station>=g.startDistance-100&&station<=g.endDistance+100));
    var added=d.transform.Find("Scenery/DepartmentStore_20261002");added.gameObject.SetActive(false);Shot(c,folder+"/entrance-before-"+station+".png");added.gameObject.SetActive(true);Shot(c,folder+"/entrance-after-"+station+".png");
   }
   EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");d=Object.FindFirstObjectByType<Chapter45Director>();c=Camera.main;
   foreach(var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))cv.gameObject.SetActive(false);
   foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible(g.floor==0);
   c.transform.position=new Vector3(-61,17,-45);c.transform.LookAt(new Vector3(-40,8,-12));Shot(c,folder+"/atrium-architecture.png");
   c.transform.position=new Vector3(-40,9,-45);c.transform.LookAt(new Vector3(-40,7,-13));Shot(c,folder+"/escalator-landings.png");
   File.WriteAllText(folder+"/conditions.txt","Entrance pairs: identical native lens/pose, only this task's new zero-collider presentation toggled in unsaved scene. Atrium/landing: directed architectural review cameras, not the ordinary gameplay camera. UI hidden. No save or completion/performance claim.");
   return new {folder,images=Directory.GetFiles(folder,"*.png")};
  }finally{EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
 static void Shot(Camera c,string path){var rt=RenderTexture.GetTemporary(720,1280,24,RenderTextureFormat.ARGB32);var target=c.targetTexture;var active=RenderTexture.active;float aspect=c.aspect;try{c.targetTexture=rt;c.aspect=720f/1280;c.Render();RenderTexture.active=rt;var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);}finally{c.targetTexture=target;c.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}}
}
