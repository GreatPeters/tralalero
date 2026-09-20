using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class DiagnoseTwoHandCrate
{
 const string Folder="map-concepts/two-hand-crate-2026-09-20/play-diagnostics";
 public static string Main(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");Directory.CreateDirectory(Folder);var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas));return Folder;}
 static IEnumerator Run(CanvasScript canvas)
 {
  var story=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);if(story!=null)story.Skip();canvas.PlayerPressedStartButton();yield return null;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();player.currentHealth=99999;
  foreach(var w in UnityEngine.Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None))w.enabled=false;
  var actors=UnityEngine.Object.FindObjectsByType<EnemyEventController>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  foreach(var e in actors)e.gameObject.SetActive(false);
  var map=GameObject.Find("Noryangjin_MapTool").transform;
  map.Find("Bonuses").gameObject.SetActive(false);
  foreach(var o in map.GetComponentsInChildren<ObstacleStats>(true))o.enabled=false;
  foreach(var spot in map.GetComponentsInChildren<EnemyEventActivationSpot>(true))spot.gameObject.SetActive(false);
  var enemy=actors.First(e=>e.name.Contains("FatMan")&&!e.name.EndsWith("_Right"));
  var pose=enemy.GetComponent<FatManCratePose>();var combat=enemy.GetComponent<EnemyScript_space>();var skin=enemy.GetComponentInChildren<SkinnedMeshRenderer>(true);
  var holdout=new GameObject("Pose proof movement lock").AddComponent<RestStopHoldout>();holdout.enabled=false;
  var route=enemy.transform.forward;var row=enemy.transform.position;float range=player.ForwardMoveSpeed*5;
  player.ApplyContinuousRoutePose(row-route*(range+2),route,0);player.SetStationaryCombat(holdout,true);enemy.gameObject.SetActive(true);combat.ApplyStat(10,10000,EnemyTier.Elite);
  TimeManager.isGameRunning=true;TimeManager.timeFactor=1;float oldCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/30;Time.timeScale=1;
  var weights=skin.sharedMesh.boneWeights;
  bool Body(int index){string n=skin.bones[index].name;return n.Contains("Spine")||n.Contains("Hips")||n.Contains("Neck")||n.Contains("Head");}
  var bodyIndices=Enumerable.Range(0,weights.Length).Where(i=>{var w=weights[i];return (Body(w.boneIndex0)?w.weight0:0)+(Body(w.boneIndex1)?w.weight1:0)+(Body(w.boneIndex2)?w.weight2:0)+(Body(w.boneIndex3)?w.weight3:0)>.5f;}).ToArray();
  var baked=new Mesh();var shape=pose.Crate.GetComponent<MeshFilter>().sharedMesh.bounds;var renderer=pose.Crate.GetComponent<Renderer>();
  float leftError=0,rightError=0,minTorsoGap=float.PositiveInfinity,maxMovement=0;int maxInside=0,maxFlightInside=0,holdingFrames=0;
  var diagnostics=new List<object>();var shots=new HashSet<int>();var launches=new List<object>();var start=enemy.transform.position;
  try
  {
   for(int frame=0;frame<70;frame++)
   {
    if(frame==20){player.ApplyContinuousRoutePose(row-route*(range-1),route,0);player.SetStationaryCombat(holdout,true);}
    yield return new WaitForEndOfFrame();skin.BakeMesh(baked,true);var vertices=baked.vertices;
    var bodyPoints=bodyIndices.Select(i=>skin.transform.TransformPoint(vertices[i])).ToArray();
    if(pose.IsHolding&&renderer.enabled)
    {
     holdingFrames++;diagnostics.Add(new{frame,left=(pose.LeftPalmPosition-pose.LeftGrip).ToString("F4"),right=(pose.RightPalmPosition-pose.RightGrip).ToString("F4"),state=pose.Model.GetCurrentAnimatorStateInfo(0).shortNameHash,transition=pose.Model.IsInTransition(0),normalized=pose.Model.GetCurrentAnimatorStateInfo(0).normalizedTime});leftError=Mathf.Max(leftError,Vector3.Distance(pose.LeftPalmPosition,pose.LeftGrip));rightError=Mathf.Max(rightError,Vector3.Distance(pose.RightPalmPosition,pose.RightGrip));
     maxInside=Mathf.Max(maxInside,bodyPoints.Count(p=>shape.Contains(pose.Crate.InverseTransformPoint(p))));
     var local=bodyPoints.Select(p=>pose.Crate.InverseTransformPoint(p)).Where(p=>p.x>=shape.min.x&&p.x<=shape.max.x&&p.y>=shape.min.y&&p.y<=shape.max.y).ToArray();
     if(local.Length>0)minTorsoGap=Mathf.Min(minTorsoGap,(shape.min.z-local.Max(p=>p.z))*pose.Crate.lossyScale.z);
    }
    maxMovement=Mathf.Max(maxMovement,Vector3.Distance(start,enemy.transform.position));
    foreach(var shot in UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(p=>p.name=="Arrow2_Shot"))if(shots.Add(shot.GetInstanceID()))
    {
     maxFlightInside=Mathf.Max(maxFlightInside,bodyPoints.Count(p=>shape.Contains(shot.transform.InverseTransformPoint(p))));
     launches.Add(new{frame,rotationJump=Quaternion.Angle(shot.transform.rotation,pose.Crate.rotation),forwardDot=Vector3.Dot(shot.GetComponent<Rigidbody>().linearVelocity.normalized,combat.AuthoredThrowDirection)});
    }
    Capture(frame,enemy.transform.position+Vector3.up*1.65f,pose.Model.transform.forward,false);
    Capture(frame,enemy.transform.position+Vector3.up*1.65f,pose.Model.transform.forward,true);
   }
   var report=new{diagnostics,shots=shots.Count,holdingFrames,leftError,rightError,maxInside,maxFlightInside,minTorsoGap,maxMovement,launches};
   var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)report});File.WriteAllText(Folder+"/report.json",json);
  }
  finally{Time.captureDeltaTime=oldCapture;TimeManager.isGameRunning=false;player.SetStationaryCombat(holdout,false);UnityEngine.Object.Destroy(holdout.gameObject);UnityEngine.Object.Destroy(baked);}
 }
 static void Capture(int frame,Vector3 center,Vector3 forward,bool side)
 {
  var go=new GameObject("Native two-hand proof camera");var c=go.AddComponent<Camera>();c.CopyFrom(Camera.main);c.enabled=false;
  var right=Vector3.Cross(Vector3.up,forward);c.transform.position=center+(side?right*5.5f+forward*.8f+Vector3.up*1.1f:forward*5.8f+right*2.8f+Vector3.up*1.6f);c.transform.LookAt(center);c.fieldOfView=46;c.aspect=1;c.nearClipPlane=.05f;
  var rt=new RenderTexture(540,540,24);var tex=new Texture2D(540,540,TextureFormat.RGB24,false);var previous=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,540,540),0,0);tex.Apply();File.WriteAllBytes(Folder+"/"+(side?"side":"front")+"-"+frame.ToString("D3")+".png",tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);UnityEngine.Object.Destroy(go);
 }
}
