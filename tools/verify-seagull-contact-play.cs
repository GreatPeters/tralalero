using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class VerifySeagullContactPlay
{
 const string Folder="map-concepts/seagull-contact-2026-09-20/play-final";
 public static string Main(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");Directory.CreateDirectory(Folder);var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas));return Folder;}
 static IEnumerator Run(CanvasScript canvas)
 {
  var story=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);if(story!=null)story.Skip();canvas.PlayerPressedStartButton();yield return null;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var model=player.GetComponentInChildren<PlayerCosmeticCustomizer>(true).modelRoot;
  foreach(var w in UnityEngine.Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None))w.enabled=false;
  foreach(var e in UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
  var map=GameObject.Find("Noryangjin_MapTool").transform;map.Find("Bonuses").gameObject.SetActive(false);
  var hazards=map.GetComponentsInChildren<ObstacleStats>(true);var bird=hazards.First(o=>o.obstaclePattern==ObstaclePattern.Seagull);
  foreach(var o in hazards)o.gameObject.SetActive(false);
  var holdout=new GameObject("Late landing contact lock").AddComponent<RestStopHoldout>();holdout.enabled=false;holdout.police=Array.Empty<EnemyEventController>();
  var reports=new List<object>();float previousCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/30;Time.timeScale=1;
  var anchor=bird.transform.position;var forward=bird.transform.forward;var right=Vector3.Cross(Vector3.up,forward);
  try
  {
   foreach(string name in new[]{"contact","dodge","landed-contact"})
   {
    bird.gameObject.SetActive(false);player.ResetState();player.currentHealth=player.MaxHealth;player.canShoot=false;foreach(var w in player.GetComponentsInChildren<WeaponScript>(true)){w.StopAllCoroutines();w.enabled=false;}
    typeof(PlayerScript).GetField("healthRegenPerSecond",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,0f);
    float lane=name=="dodge"?-3.4f:0f;
    var start=anchor-forward*(name=="landed-contact"?12f:20f)+right*lane;start.y=.12f;
    player.ApplyContinuousRoutePose(start,forward,0);if(name=="landed-contact")player.SetStationaryCombat(holdout,true);
    bird.gameObject.SetActive(true);TimeManager.isGameRunning=true;TimeManager.timeFactor=1;
    float initial=player.currentHealth,previousHealth=initial,previousAngle=0,spinDegrees=0,headingChange=0;int firstHit=-1,damageEvents=0,visibleFrames=0;bool damageBeforeVisible=false,teleported=false;
    var resting=model.localRotation;var rootHeading=player.transform.rotation;var hitFrames=new List<object>();
    for(int frame=0;frame<180;frame++)
    {
     if(name=="landed-contact"&&!teleported&&frame==54)
     {var p=anchor+Vector3.up*.12f;player.ApplyContinuousRoutePose(p,forward,0);player.SetStationaryCombat(holdout,true);teleported=true;}
     yield return new WaitForEndOfFrame();
     bool visible=bird.balloon.gameObject.activeInHierarchy;if(visible)visibleFrames++;
     if(player.currentHealth<previousHealth-.001f)
     {
      if(firstHit<0)firstHit=frame;damageEvents++;damageBeforeVisible|=!visible;if(name=="contact"){var screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/hit-hud.png",screenshot.EncodeToPNG());UnityEngine.Object.Destroy(screenshot);}
      hitFrames.Add(new{frame,damage=previousHealth-player.currentHealth,position=player.transform.position.ToString(),birdY=bird.balloon.position.y});
     }
     previousHealth=player.currentHealth;
     var relative=Quaternion.Inverse(resting)*model.localRotation;
     float angle=Vector3.SignedAngle(Vector3.forward,relative*Vector3.forward,Vector3.up);
     spinDegrees+=Mathf.DeltaAngle(previousAngle,angle);previousAngle=angle;
     headingChange=Mathf.Max(headingChange,Quaternion.Angle(rootHeading,player.transform.rotation));
     if(name=="contact")Capture(frame,player.transform.position,forward);
    }
    reports.Add(new{name,maximum=player.MaxHealth,initial,final=player.currentHealth,damageEvents,firstHit,spinDegrees,headingChange,damageBeforeVisible,visibleFrames,departed=!bird.balloon.gameObject.activeSelf,hitFrames});
    Write(reports);
   }
  }
  finally{Time.captureDeltaTime=previousCapture;TimeManager.isGameRunning=false;player.SetStationaryCombat(holdout,false);UnityEngine.Object.Destroy(holdout.gameObject);}
 }
 static void Write(object report)
 {
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{report});File.WriteAllText(Folder+"/report.json",json);
 }
 static void Capture(int frame,Vector3 player,Vector3 forward)
 {
  var go=new GameObject("Seagull gameplay proof camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.aspect=1;
  var target=player+forward*3+Vector3.up*1.2f;camera.transform.position=player-forward*7+Vector3.Cross(Vector3.up,forward)*2+Vector3.up*7;camera.transform.LookAt(target);camera.fieldOfView=55;camera.nearClipPlane=.05f;
  var rt=new RenderTexture(640,640,24);var pixels=new Texture2D(640,640,TextureFormat.RGB24,false);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,640,640),0,0);pixels.Apply();File.WriteAllBytes(Folder+"/frame-"+frame.ToString("D3")+".png",pixels.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);
 }
}
