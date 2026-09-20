using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class RecordSyncedCrateThrow
{
 const string Folder="map-concepts/synced-crate-throw-2026-09-20/play-v1";
 public static string Main(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");Directory.CreateDirectory(Folder);var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas));return Folder;}
 static IEnumerator Run(CanvasScript canvas)
 {
  var story=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);if(story!=null)story.Skip();canvas.PlayerPressedStartButton();yield return null;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();player.currentHealth=99999;player.canShoot=false;
  foreach(var w in UnityEngine.Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None)){w.StopAllCoroutines();w.enabled=false;}
  var actors=UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsInactive.Include,FindObjectsSortMode.None);foreach(var e in actors)e.gameObject.SetActive(false);
  var map=GameObject.Find("Noryangjin_MapTool").transform;map.Find("Bonuses").gameObject.SetActive(false);
  foreach(var o in map.GetComponentsInChildren<ObstacleStats>(true))o.gameObject.SetActive(false);
  foreach(var s in map.GetComponentsInChildren<EnemyEventActivationSpot>(true))s.gameObject.SetActive(false);
  var enemy=actors.First(e=>e.name.Contains("FatMan")&&!e.name.EndsWith("_Right"));var pose=enemy.GetComponent<FatManCratePose>();var combat=enemy.GetComponent<EnemyScript_space>();
  var skin=enemy.GetComponentInChildren<SkinnedMeshRenderer>(true);var weights=skin.sharedMesh.boneWeights;
  bool Body(int index){string n=skin.bones[index].name;return n.Contains("Spine")||n.Contains("Hips")||n.Contains("Neck")||n.Contains("Head");}
  var ids=Enumerable.Range(0,weights.Length).Where(i=>{var w=weights[i];return (Body(w.boneIndex0)?w.weight0:0)+(Body(w.boneIndex1)?w.weight1:0)+(Body(w.boneIndex2)?w.weight2:0)+(Body(w.boneIndex3)?w.weight3:0)>.5f;}).ToArray();
  var holdout=new GameObject("Throw proof movement lock").AddComponent<RestStopHoldout>();holdout.enabled=false;holdout.police=Array.Empty<EnemyEventController>();
  Vector3 route=enemy.transform.forward,row=enemy.transform.position;float range=player.ForwardMoveSpeed*5;
  player.ApplyContinuousRoutePose(row-route*(range+2),route,0);player.SetStationaryCombat(holdout,true);enemy.gameObject.SetActive(true);combat.ConfigureRewards(false,0);combat.ApplyStat(10,10000,EnemyTier.Elite);
  TimeManager.isGameRunning=true;TimeManager.timeFactor=1;float oldCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/30;Time.timeScale=1;
  var mesh=new Mesh();var shape=pose.Crate.GetComponent<MeshFilter>().sharedMesh.bounds;var renderer=pose.Crate.GetComponent<Renderer>();
  var shots=new HashSet<int>();var launches=new List<object>();var frames=new List<object>();
  float gripError=0,restJitter=0,restElbowJitter=0;int inside=0,flightInside=0,earlyShots=0,enabledAnimatorFrames=0;
  Vector3 restBox=Vector3.zero,restElbow=Vector3.zero;bool restCaptured=false;
  object pause=null,death=null;
  try
  {
   for(int frame=0;frame<240;frame++)
   {
    if(frame==20){player.ApplyContinuousRoutePose(row-route*(range-1),route,0);player.SetStationaryCombat(holdout,true);}
    yield return new WaitForEndOfFrame();
    if(pose.Model.enabled)enabledAnimatorFrames++;
    skin.BakeMesh(mesh,true);var vertices=mesh.vertices;var body=ids.Select(i=>skin.transform.TransformPoint(vertices[i])).ToArray();
    if(pose.IsHolding&&renderer.enabled)
    {
     gripError=Mathf.Max(gripError,Vector3.Distance(pose.LeftPalmPosition,pose.LeftGrip),Vector3.Distance(pose.RightPalmPosition,pose.RightGrip));
     inside=Mathf.Max(inside,body.Count(p=>shape.Contains(pose.Crate.InverseTransformPoint(p))));
    }
    if(frame<20)
    {
     var elbow=pose.Model.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
     if(!restCaptured){restCaptured=true;restBox=pose.Crate.position;restElbow=elbow;}
     restJitter=Mathf.Max(restJitter,Vector3.Distance(restBox,pose.Crate.position));restElbowJitter=Mathf.Max(restElbowJitter,Vector3.Distance(restElbow,elbow));
    }
    foreach(var shot in UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(p=>p.name=="Arrow2_Shot"))if(shots.Add(shot.GetInstanceID()))
    {
     if(pose.ThrowProgress<.999f||pose.StrokeProgress<.999f)earlyShots++;
     int overlap=body.Count(p=>shape.Contains(shot.transform.InverseTransformPoint(p)));flightInside=Mathf.Max(flightInside,overlap);
     launches.Add(new{frame,pose.ThrowProgress,pose.StrokeProgress,handoffError=Vector3.Distance(shot.transform.position,pose.Crate.position),rotationJump=Quaternion.Angle(shot.transform.rotation,pose.Crate.rotation),pushMetres=Vector3.Dot(pose.Crate.position-restBox,pose.Model.transform.forward)});
    }
    frames.Add(new{frame,pose.ThrowProgress,pose.StrokeProgress,holding=pose.IsHolding,box=pose.Crate.localPosition.ToString("F5")});
    Capture(frame,row+Vector3.up*1.65f,pose.Model.transform.forward,false);Capture(frame,row+Vector3.up*1.65f,pose.Model.transform.forward,true);
   }
   // Re-enable/cancel, pause during wind-up, then check the original death path.
   enemy.gameObject.SetActive(false);yield return null;enemy.gameObject.SetActive(true);combat.ApplyStat(10,10000,EnemyTier.Elite);
   while(pose.ThrowProgress<.35f)yield return null;
   TimeManager.isGameRunning=false;float progress=pose.ThrowProgress;int before=UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Count(p=>p.name=="Arrow2_Shot");
   for(int i=0;i<15;i++)yield return new WaitForEndOfFrame();
   pause=new{before,after=UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Count(p=>p.name=="Arrow2_Shot"),progressBefore=progress,progressAfter=pose.ThrowProgress};
   TimeManager.isGameRunning=true;while(pose.IsHolding)yield return null;
   combat.EnemyDeath();yield return null;death=new{state=enemy.RuntimeState.ToString(),animatorEnabled=pose.Model.enabled};
   var report=new{shots=shots.Count,earlyShots,gripError,inside,flightInside,restJitter,restElbowJitter,enabledAnimatorFrames,launches,pause,death,shader=skin.sharedMaterial.shader.name};
   Write(Folder+"/report.json",report);Write(Folder+"/frames.json",frames);
  }
  finally{Time.captureDeltaTime=oldCapture;TimeManager.isGameRunning=false;player.SetStationaryCombat(holdout,false);UnityEngine.Object.Destroy(holdout.gameObject);UnityEngine.Object.Destroy(mesh);}
 }
 static void Write(string path,object value)
 {
  var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});File.WriteAllText(path,json);
 }
 static void Capture(int frame,Vector3 center,Vector3 forward,bool side)
 {
  var go=new GameObject("Synced throw proof camera");var c=go.AddComponent<Camera>();c.CopyFrom(Camera.main);c.enabled=false;var right=Vector3.Cross(Vector3.up,forward);if(side)center+=forward*.45f;
  c.transform.position=center+(side?-right*3.3f+forward*.75f+Vector3.up*.7f:forward*5.8f-right*2.2f+Vector3.up*1.6f);c.transform.LookAt(center);c.fieldOfView=side?70:46;c.aspect=1;c.nearClipPlane=.05f;
  var rt=new RenderTexture(540,540,24);var tex=new Texture2D(540,540,TextureFormat.RGB24,false);var previous=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,540,540),0,0);tex.Apply();File.WriteAllBytes(Folder+"/"+(side?"side":"front")+"-"+frame.ToString("D3")+".png",tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);UnityEngine.Object.Destroy(go);
 }
}
