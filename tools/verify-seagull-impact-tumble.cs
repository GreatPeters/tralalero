using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class VerifySeagullImpactTumble
{
 static string Folder;
 public static string Main(string outputFolder)
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
  if(Directory.Exists(outputFolder)&&Directory.EnumerateFileSystemEntries(outputFolder).Any())throw new IOException("Use an empty recording folder; existing evidence is preserved.");
  Folder=outputFolder;Directory.CreateDirectory(Folder);
  var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas));return Folder;
 }
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
   foreach(string name in new[]{"contact","dodge-left","dodge-right","landed-contact"})
   {
    bird.gameObject.SetActive(false);player.ResetState();player.currentHealth=player.MaxHealth;player.canShoot=false;foreach(var w in player.GetComponentsInChildren<WeaponScript>(true)){w.StopAllCoroutines();w.enabled=false;}
    typeof(PlayerScript).GetField("healthRegenPerSecond",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,0f);
    float lane=0f;
    var start=anchor-forward*(name=="landed-contact"?12f:32f)+right*lane;start.y=.12f;
    player.ApplyContinuousRoutePose(start,forward,0);if(name=="landed-contact")player.SetStationaryCombat(holdout,true);
    bird.gameObject.SetActive(true);TimeManager.isGameRunning=true;TimeManager.timeFactor=1;
    float initial=player.currentHealth,previousHealth=initial,previousAngle=0,spinDegrees=0,headingChange=0;int firstHit=-1,damageEvents=0,visibleFrames=0;bool damageBeforeVisible=false,teleported=false;
    bool dodge=name.StartsWith("dodge");float direction=name=="dodge-left"?-1f:1f;
    int warningFrame=-1,roadMisses=0,landingFrame=-1,departureFrame=-1,passedFrame=-1;float landingDistance=-999,departureProgress=-999;float warningDistance=-1;float maxShadow=0,maxLane=0;
    var roads=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
    var move=typeof(PlayerScript).GetMethod("PlayerMove",BindingFlags.NonPublic|BindingFlags.Instance);
    float birdSpinDegrees=0,birdArcHeight=0,birdTravel=0;int activeHitShapes=0;
    Vector3 previousBirdPosition=bird.balloon.position,knockbackStart=Vector3.zero;
    Quaternion previousBirdRotation=bird.balloon.rotation;
    var resting=model.localRotation;var rootHeading=player.transform.rotation;var hitFrames=new List<object>();
    for(int frame=0;frame<180;frame++)
    {
     if(name=="landed-contact"&&!teleported&&frame==84)
     {var p=anchor+Vector3.up*.12f;player.ApplyContinuousRoutePose(p,forward,0);player.SetStationaryCombat(holdout,true);teleported=true;}
     if(bird.shadowSprite.enabled&&warningFrame<0)
     {warningFrame=frame;warningDistance=Vector3.ProjectOnPlane(player.transform.position-anchor,Vector3.up).magnitude;Capture("early-"+name,player.transform.position,forward);}
     float offset=Vector3.Dot(player.transform.position-anchor,right);
     if(dodge&&warningFrame>=0&&frame-warningFrame>=5&&direction*offset<1.4f)
       move.Invoke(player,new object[]{15f*direction});
     yield return new WaitForEndOfFrame();
     offset=Vector3.Dot(player.transform.position-anchor,right);maxLane=Mathf.Max(maxLane,Mathf.Abs(offset));
     if(Mathf.Abs(Vector3.Dot(player.transform.position-anchor,forward))<9f)
     foreach(float side in new[]{-.7f,0f,.7f})
     {
       var ray=new Ray(player.transform.position+right*side+Vector3.up*3,Vector3.down);
       if(!roads.Any(c=>c.Raycast(ray,out var hit,6)))roadMisses++;
     }
     if(bird.shadowSprite.enabled)
     {
       float width=bird.shadowSprite.sprite.bounds.size.x*Mathf.Abs(bird.shadowSprite.transform.lossyScale.x);
       maxShadow=Mathf.Max(maxShadow,width);
       if(!bird.balloon.gameObject.activeInHierarchy)Capture("warning-"+name,player.transform.position,forward);
     }
     bool visible=bird.balloon.gameObject.activeInHierarchy;if(visible)visibleFrames++;
     float progress=Vector3.Dot(player.transform.position-anchor,forward);
     if(passedFrame<0&&progress>=0)passedFrame=frame;
     float landingY=anchor.y+bird.seagullLandingOffset;
     if(visible&&landingFrame<0&&Mathf.Abs(bird.balloon.position.y-landingY)<.01f)
     {landingFrame=frame;landingDistance=-progress;Capture("landed-"+name,player.transform.position,forward);}
     if(visible&&landingFrame>=0&&frame>landingFrame&&departureFrame<0&&bird.balloon.position.y>landingY+.1f)
     {departureFrame=frame;departureProgress=progress;}
     if(player.currentHealth<previousHealth-.001f)
     {
      if(firstHit<0){firstHit=frame;knockbackStart=previousBirdPosition;}damageEvents++;damageBeforeVisible|=!visible;if(name=="contact"){var screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/hit-hud.png",screenshot.EncodeToPNG());UnityEngine.Object.Destroy(screenshot);}
      hitFrames.Add(new{frame,damage=previousHealth-player.currentHealth,position=player.transform.position.ToString(),birdY=bird.balloon.position.y});
     }
     previousHealth=player.currentHealth;
     if(firstHit>=0)
     {
       birdSpinDegrees+=Quaternion.Angle(previousBirdRotation,bird.balloon.rotation);
       birdArcHeight=Mathf.Max(birdArcHeight,bird.balloon.position.y-knockbackStart.y);
       birdTravel=Mathf.Max(birdTravel,Vector3.ProjectOnPlane(bird.balloon.position-knockbackStart,Vector3.up).magnitude);
       activeHitShapes=Mathf.Max(activeHitShapes,bird.balloon.GetComponentsInChildren<Collider>(true).Count(c=>c.enabled));
     }
     previousBirdPosition=bird.balloon.position;previousBirdRotation=bird.balloon.rotation;
     var relative=Quaternion.Inverse(resting)*model.localRotation;
     float angle=Vector3.SignedAngle(Vector3.forward,relative*Vector3.forward,Vector3.up);
     spinDegrees+=Mathf.DeltaAngle(previousAngle,angle);previousAngle=angle;
     headingChange=Mathf.Max(headingChange,Quaternion.Angle(rootHeading,player.transform.rotation));
     if(name!="landed-contact")Capture(name+"-"+frame.ToString("D3"),player.transform.position,forward);
    }
    reports.Add(new{name,birdSpinDegrees,birdArcHeight,birdTravel,activeHitShapes,landingFrame,landingDistance,departureFrame,departureProgress,passedFrame,warningDistance,maxShadow,maxLane,roadMisses,warningFrame,maximum=player.MaxHealth,initial,final=player.currentHealth,damageEvents,firstHit,spinDegrees,headingChange,damageBeforeVisible,visibleFrames,departed=!bird.balloon.gameObject.activeSelf,hitFrames});
    Write(reports);
   }
   bird.gameObject.SetActive(false);player.ResetState();player.currentHealth=player.MaxHealth;player.canShoot=false;
   foreach(var w in player.GetComponentsInChildren<WeaponScript>(true)){w.StopAllCoroutines();w.enabled=false;}
   player.ApplyContinuousRoutePose(anchor-forward*12+Vector3.up*.12f,forward,0);player.SetStationaryCombat(holdout,true);
   bird.gameObject.SetActive(true);TimeManager.isGameRunning=true;TimeManager.timeFactor=1;
   while(!bird.balloon.gameObject.activeSelf)yield return null;
   var airborneStart=bird.balloon.position;
   bool acceptedMidair=bird.TrySeagullContact(player.GetComponent<Collider>());
   for(int frame=0;frame<8;frame++)yield return new WaitForEndOfFrame();
   float midairRise=bird.balloon.position.y-airborneStart.y;
   TimeManager.isGameRunning=false;var pausePosition=bird.balloon.position;var pauseRotation=bird.balloon.rotation;var pauseShark=model.localRotation;
   for(int frame=0;frame<12;frame++)yield return new WaitForEndOfFrame();
   float pausePositionError=Vector3.Distance(pausePosition,bird.balloon.position),pauseRotationError=Quaternion.Angle(pauseRotation,bird.balloon.rotation),pauseSharkError=Quaternion.Angle(pauseShark,model.localRotation);
   TimeManager.isGameRunning=true;bird.gameObject.SetActive(false);bird.gameObject.SetActive(true);
   bool resetHidden=!bird.balloon.gameObject.activeSelf;
   while(!bird.balloon.gameObject.activeSelf)yield return null;
   float retryRotationError=Quaternion.Angle(bird.balloon.rotation,bird.transform.rotation);
   bool resetHit=(bool)typeof(ObstacleStats).GetField("_seagullHit",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(bird);
   bool retryAnimatorEnabled=bird.balloon.GetComponent<Animator>().enabled;
   int retryContacts=bird.balloon.GetComponentsInChildren<Collider>(true).Count(c=>c.enabled);
   reports.Add(new{name="midair-pause-retry",acceptedMidair,midairRise,pausePositionError,pauseRotationError,pauseSharkError,resetHidden,retryRotationError,resetHit,retryAnimatorEnabled,retryContacts});Write(reports);
  }
  finally{Time.captureDeltaTime=previousCapture;TimeManager.isGameRunning=false;player.SetStationaryCombat(holdout,false);UnityEngine.Object.Destroy(holdout.gameObject);}
 }
 static void Write(object report)
 {
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{report});File.WriteAllText(Folder+"/report.json",json);
 }
 static void Capture(string frame,Vector3 player,Vector3 forward)
 {
  var go=new GameObject("Seagull gameplay proof camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.aspect=1;
  var target=player+forward*3+Vector3.up*1.2f;camera.transform.position=player-forward*7+Vector3.Cross(Vector3.up,forward)*2+Vector3.up*7;camera.transform.LookAt(target);camera.fieldOfView=55;camera.nearClipPlane=.05f;
  var rt=new RenderTexture(640,640,24);var pixels=new Texture2D(640,640,TextureFormat.RGB24,false);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,640,640),0,0);pixels.Apply();File.WriteAllBytes(Folder+"/"+frame+".png",pixels.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);
 }
}
