using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class VerifyThrowShipRefinement
{
 const string Folder="map-concepts/throw-ship-refinement-2026-09-20/play-final";
 public static string Main()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");Directory.CreateDirectory(Folder);
  var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas));return Folder;
 }
 static IEnumerator Run(CanvasScript canvas)
 {
  var story=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);if(story!=null)story.Skip();
  canvas.PlayerPressedStartButton();yield return null;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();player.currentHealth=99999;
  foreach(var w in UnityEngine.Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None))w.enabled=false;
  var enemies=UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var map=GameObject.Find("Noryangjin_MapTool").transform;
  var report=new List<object>();
  foreach(var e in enemies)e.gameObject.SetActive(false);
  foreach(var obstacle in map.GetComponentsInChildren<ObstacleStats>(true))obstacle.enabled=false;
  foreach(var spot in map.GetComponentsInChildren<EnemyEventActivationSpot>(true))spot.gameObject.SetActive(false);
  var holdout=new GameObject("Verification movement lock").AddComponent<RestStopHoldout>();holdout.enabled=false;player.SetStationaryCombat(holdout,true);TimeManager.timeFactor=1;TimeManager.isGameRunning=true;
  float previousCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/30;Time.timeScale=1;
  try
  {
   foreach(string kind in new[]{"FatMan"})
   {
    var enemy=enemies.First(e=>e.name.Contains(kind)&&!e.name.EndsWith("_Right"));
    var combat=enemy.GetComponent<EnemyScript_space>();var animator=enemy.GetComponentInChildren<Animator>(true);
    Vector3 row=kind=="Guard"?enemy.TargetPoint.position:enemy.transform.position;
    Vector3 route=enemy.transform.forward;float range=kind=="FatMan"?player.ForwardMoveSpeed*5:30;
    player.ApplyContinuousRoutePose(row-route*(range+2),route,0);player.SetStationaryCombat(holdout,true);enemy.gameObject.SetActive(true);combat.ApplyStat(10,10000,EnemyTier.Elite);
    if(kind=="Guard")enemy.ActivateFromSpot();
    Vector3 start=enemy.transform.position;var shotIds=new HashSet<int>();var launches=new List<object>();int outsideShots=0,attackFrames=0;
    float maxMovement=0,maxGripError=0,minHeadClearance=float.PositiveInfinity; var held=enemy.GetComponentsInChildren<SimpleProjectile>(true).First().GetComponent<Renderer>();
    for(int frame=0;frame<210;frame++)
    {
     if(frame==20&&kind=="FatMan")player.ApplyContinuousRoutePose(row-route*(range-1),route,0);player.SetStationaryCombat(holdout,true);
     yield return new WaitForEndOfFrame();
     maxMovement=Mathf.Max(maxMovement,Vector3.Distance(start,enemy.transform.position)); if(held.enabled){var head=animator.GetBoneTransform(HumanBodyBones.Head).position;minHeadClearance=Mathf.Min(minHeadClearance,Vector3.Distance(head,held.bounds.ClosestPoint(head)));var bounds=held.GetComponent<MeshFilter>().sharedMesh.bounds;var grip=bounds.center+new Vector3(0,bounds.extents.y,bounds.extents.z*.65f);var hand=animator.GetBoneTransform(HumanBodyBones.RightHand).position;maxGripError=Mathf.Max(maxGripError,Vector3.Distance(hand,held.transform.TransformPoint(grip)));}
     if(animator.GetCurrentAnimatorStateInfo(0).shortNameHash==Animator.StringToHash("attack_once"))attackFrames++;
     var aim=enemy.GetComponent<EnemyGunAim>();
     if(aim!=null&&enemy.RuntimeState==EnemyEventRuntimeState.Attacking)maxGripError=Mathf.Max(maxGripError,Vector3.Distance(aim.gun.TransformPoint(aim.localGripPoint),aim.grip.position));
     foreach(var shot in UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(p=>p.name=="Arrow2_Shot"))
      if(shotIds.Add(shot.GetInstanceID()))
      {
       var body=shot.GetComponent<Rigidbody>();Vector3 direction=body.linearVelocity.normalized;
       if(frame<20)outsideShots++;
       launches.Add(new{frame,direction=new[]{direction.x,direction.y,direction.z},expectedStraightDot=Vector3.Dot(direction,combat.AuthoredThrowDirection),arrowAxisDot=Vector3.Dot(shot.transform.right,direction),size=shot.GetComponent<Renderer>().bounds.size.magnitude});
       Capture(kind+"-launch-"+shotIds.Count,enemy.transform.position+Vector3.up*1.2f,enemy.transform.forward,7f);
      }
     if(frame%10==0)Capture(kind+"-frame-"+frame.ToString("D3"),enemy.transform.position+Vector3.up*1.2f,enemy.transform.forward,7f);
    }
    report.Add(new{kind,shots=shotIds.Count,outsideShots,attackFrames,maxMovement,maxGripError,minHeadClearance,launches});enemy.gameObject.SetActive(false);
   }
   var ship=map.GetComponentsInChildren<ObstacleStats>(true).First(o=>o.obstaclePattern==ObstaclePattern.Ship);
   var waitingRotation=ship.transform.rotation;float headingDrift=0,barrelDot=0;var shipForward=Vector3.left;float shipRange=player.ForwardMoveSpeed*7.5f;
   Vector3 roadCenter=ship.transform.position;roadCenter.y=.12f;roadCenter.z=135.9989f;
   player.ApplyContinuousRoutePose(roadCenter-shipForward*(shipRange+2),shipForward,0);player.SetStationaryCombat(holdout,true);
   ship.enabled=true;int firstShotFrame=-1;Vector3 ballSize=Vector3.zero;float firePosError=0;Vector3 launch=Vector3.zero;
   for(int frame=0;frame<95;frame++)
   {
    if(frame==20)player.ApplyContinuousRoutePose(roadCenter-shipForward*(shipRange-1),shipForward,0);player.SetStationaryCombat(holdout,true);
    yield return new WaitForEndOfFrame();
    var ball=UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).FirstOrDefault(s=>s.name=="CannonBall_Shot");
    if(ball!=null&&firstShotFrame<0){firstShotFrame=frame;barrelDot=Vector3.Dot(ball.GetComponent<Rigidbody>().linearVelocity.normalized,ship.firePos.forward);ballSize=ball.GetComponent<Renderer>().bounds.size;firePosError=Vector3.Distance(ball.transform.position,ship.firePos.position);launch=ball.transform.position;}
    headingDrift=Mathf.Max(headingDrift,Quaternion.Angle(waitingRotation,ship.transform.rotation)); if(frame%5==0)Capture("Ship-frame-"+frame.ToString("D3"),ship.transform.position+Vector3.up,Vector3.right,15f);
   }
   report.Add(new{kind="Ship",firstShotFrame,headingDrift,barrelDot,ballSize=new[]{ballSize.x,ballSize.y,ballSize.z},firePosError,shipWidth=ship.GetComponent<Renderer>().bounds.size.magnitude,firePos=ship.firePos.position.ToString()});
   Write(report);
  }
  finally{Time.captureDeltaTime=previousCapture;TimeManager.isGameRunning=false;player.SetStationaryCombat(holdout,false);UnityEngine.Object.Destroy(holdout.gameObject);}
 }
 static void Write(object report)
 {
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{report});File.WriteAllText(Folder+"/report.json",json);
 }
 static void Capture(string name,Vector3 center,Vector3 forward,float distance)
 {
  var go=new GameObject("Combat verification camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
  Vector3 side=Vector3.Cross(Vector3.up,forward);camera.transform.position=center+(-forward*.8f+side*.7f+Vector3.up*.5f).normalized*distance;camera.transform.LookAt(center);
  camera.fieldOfView=50;camera.aspect=1;camera.nearClipPlane=.05f;
  var rt=new RenderTexture(720,720,24);var pixels=new Texture2D(720,720,TextureFormat.RGB24,false);var old=RenderTexture.active;
  camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,720,720),0,0);pixels.Apply();File.WriteAllBytes(Folder+"/"+name+".png",pixels.EncodeToPNG());
  camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);
 }
}
