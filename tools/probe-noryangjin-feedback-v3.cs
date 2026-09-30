using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

public static class ProbeNoryangjinFeedbackV3
{
 const string Root="outputs/noryangjin-feedback-v3-2026-09-28";
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static string Json(object o)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
 public static object Main()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play required");EditorApplication.isPaused=false;
  var p=Object.FindFirstObjectByType<PlayerScript>();var d=NoryangjinRevampDirector.Active;
  if(!TimeManager.isGameRunning){Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();}
  var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.Close();tutorial.enabled=false;}
  foreach(var w in Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None)){w.CancelInvoke();w.enabled=false;}
  foreach(var h in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))h.enabled=false;
  foreach(var e in d.GetComponentsInChildren<NoryangjinRevampEvent>())e.enabled=false;
  foreach(var e in Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
  foreach(var c in Object.FindObjectsByType<CoinPickup>(FindObjectsSortMode.None))c.gameObject.SetActive(false);
  var hold=p.GetType().GetField("stationaryCombatOwner",Flags);hold.SetValue(p,null);p.GetType().GetField("currentForwardMoveSpeed",Flags).SetValue(p,0f);
  typeof(NoryangjinMarketBranch).GetProperty("Driving").SetValue(d.Branch,false);typeof(NoryangjinMarketBranch).GetProperty("Selected").SetValue(d.Branch,true);
  var patches=d.GetComponentsInChildren<NoryangjinWetPatch>();foreach(var v in patches)v.ResetPatch();var patch=patches[0];
  void Place(Vector3 at,Vector3 forward){p.ApplyContinuousRoutePose(at,forward,0);Physics.SyncTransforms();}
  Place(new Vector3(patch.transform.position.x,.22f,patch.transform.position.z),Vector3.back);Vector3 start=p.transform.position;float sensitivity=p.moveSensitivity_Devision;
  patch.SetSpraying(true);var results=new List<object>();var deathSamples=new List<float>();float deathFloor=0;double nextDeath=0;int stage=0;double due=EditorApplication.timeSinceStartup+1.1;EnemyScript_space merchant=null;int wallet=0,expected=0;Vector3 coinAt=Vector3.zero;
  string folder="tmp/image-previews/noryangjin-feedback-v3-2026-09-28/probes-"+DateTime.Now.ToString("HHmmss");Directory.CreateDirectory(folder);
  EditorApplication.CallbackFunction tick=null;
  tick=()=>
  {
   try
   {
    if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}
    if(merchant!=null&&merchant.IsDead&&merchant.gameObject.activeInHierarchy&&EditorApplication.timeSinceStartup>=nextDeath)
    {
     nextDeath=EditorApplication.timeSinceStartup+.15;float min=float.PositiveInfinity;
     foreach(var skin in merchant.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh,false);foreach(var v in mesh.vertices)min=Mathf.Min(min,skin.transform.TransformPoint(v).y-deathFloor);Object.Destroy(mesh);}
     deathSamples.Add(min);
    }
    if(EditorApplication.timeSinceStartup<due)return;
    if(stage==0)
    {
     var slip=p.GetComponent<NoryangjinWetSteering>();float delta=Mathf.Abs(p.transform.position.x-start.x);
     results.Add(new{name="actual wet-floor displacement",pass=delta>.25f&&slip!=null&&slip.Remaining>0,metres=delta,sensitivityBefore=sensitivity,sensitivityDuring=p.moveSensitivity_Devision,orangeWarnings=d.GetComponentInChildren<NoryangjinHoseEvent>().jets.Count(j=>j.warning!=null)});
     patch.SetSpraying(false);Place(start+Vector3.forward*5,Vector3.back);stage++;due=EditorApplication.timeSinceStartup+2.2;
    }
    else if(stage==1)
    {
     var slip=p.GetComponent<NoryangjinWetSteering>();results.Add(new{name="wet mark persists and steering recovers",pass=patch.Wetness>.5f&&slip.Remaining==0&&Mathf.Abs(sensitivity-p.moveSensitivity_Devision)<.001f,patch.Wetness,slip.Remaining,sensitivity=p.moveSensitivity_Devision});
     var template=d.GetComponentInChildren<NoryangjinRushEvent>().templates[0];var at=p.transform.position+Vector3.back*10;at.y=0;var go=Object.Instantiate(template,at,Quaternion.identity);
     if(!p.GetComponent<NoryangjinRoadHeightFollower>().TryProjectPosition(at+Vector3.up*.22f,Vector3.back,out var support))throw new Exception("No floor below death fixture");deathFloor=support.y-.12f;
     NoryangjinRushEvent.StripToCombat(go);go.AddComponent<NoryangjinMerchant>();go.SetActive(true);merchant=go.GetComponent<EnemyScript_space>();merchant.ConfigureRewards(false,3);merchant.ApplyStat(100,100,EnemyTier.Normal);wallet=MoneyScript.S.Coin;expected=CoinDropUtility.ApplyCoinBonus(3);merchant.ReceiveVehicleDamage(1000);stage++;due=EditorApplication.timeSinceStartup+.35;
    }
    else if(stage==2)
    {
     var coins=Object.FindObjectsByType<CoinPickup>(FindObjectsSortMode.None);int amount=coins.Sum(c=>(int)typeof(CoinPickup).GetField("amount",Flags).GetValue(c));
     results.Add(new{name="merchant death drops coin pickup, no numbered box",pass=merchant.IsDead&&coins.Length==1&&amount==expected&&Object.FindObjectsByType<NoryangjinNumberedHat>(FindObjectsSortMode.None).Length==0,pickups=coins.Length,amount,expected});
     ScreenCapture.CaptureScreenshot(folder+"/death-coins.png");coinAt=coins.First().transform.position;stage++;due=EditorApplication.timeSinceStartup+.4;
    }
    else if(stage==3){Place(new Vector3(coinAt.x,.22f,coinAt.z),Vector3.back);stage++;due=EditorApplication.timeSinceStartup+.6;}
    else if(stage==4)
    {
     results.Add(new{name="coin pickup credits wallet once",pass=MoneyScript.S.Coin-wallet==expected,credited=MoneyScript.S.Coin-wallet,expected});
     var gate=d.GetComponentInChildren<NoryangjinShutterEvent>();gate.ResetForRun();foreach(var wall in gate.walls)wall.gameObject.SetActive(false);
     Place(gate.transform.position-gate.transform.forward*5+Vector3.up*.22f,gate.transform.forward);
     typeof(NoryangjinShutterEvent).GetMethod("OnTriggered",Flags).Invoke(gate,null);
     typeof(NoryangjinShutterEvent).GetMethod("OnTick",Flags).Invoke(gate,new object[]{100f});typeof(NoryangjinShutterEvent).GetMethod("OnTick",Flags).Invoke(gate,new object[]{1f});
     results.Add(new{name="roller closes and blocks",pass=gate.CurrentOpening<.01f&&gate.shutter.BlocksNow,opening=gate.CurrentOpening,blocking=gate.shutter.BlocksNow});
     stage++;due=EditorApplication.timeSinceStartup+.8;
    }
    else if(stage==5)
    {
     ScreenCapture.CaptureScreenshot(folder+"/closed-shutter.png");stage++;due=EditorApplication.timeSinceStartup+.5;
    }
    else if(stage==6)
    {
     var gate=d.GetComponentInChildren<NoryangjinShutterEvent>();bool wasBlocked=gate.shutter.BlocksNow;gate.shutter.Damage(gate.shutter.Health);results.Add(new{name="closed shutter breaks and releases path",pass=wasBlocked&&gate.shutter.Health==0&&!gate.shutter.BlocksNow});
     var follower=p.GetComponent<NoryangjinRoadHeightFollower>();int samples=0,failed=0;float maxError=0;Vector3[] route={new(124.3f,.22f,-326),new(124.3f,.22f,-348),new(-67,.22f,-348),new(-67,.22f,-328)};
     for(int part=0;part<3;part++){var delta=route[part+1]-route[part];for(float m=0;m<=delta.magnitude;m+=.5f){samples++;if(!follower.TryProjectPosition(route[part]+delta.normalized*m,delta.normalized,out var supported))failed++;else maxError=Mathf.Max(maxError,Mathf.Abs(supported.y-.22f));}}
     results.Add(new{name="connected bridge floor projection",pass=failed==0&&maxError<.25f,samples,failed,maxError});
     Place(new Vector3(-67,.22f,-302),Vector3.forward);stage++;due=EditorApplication.timeSinceStartup+1;
    }
    else if(stage>=7&&stage<=11)
    {
     var a=d.GetComponentInChildren<NoryangjinAuctionActivity>();ScreenCapture.CaptureScreenshot(folder+"/auction-"+(stage-7)+".png");
     if(stage==11)results.Add(new{name="auction raises bids and calls prices",pass=a.BidsRaised>=3&&a.Calls>=1&&a.GetComponentsInChildren<Collider>().Length==0,stateTransitions=a.GestureChanges,a.BidsRaised,a.Calls,actors=a.bidders.Length+1});stage++;due=EditorApplication.timeSinceStartup+1.7;
    }
    else
    {
     results.Add(new{name="runtime death stays on floor",pass=deathSamples.Count>=5&&deathSamples.Min()>-.025f&&deathSamples.Max()<.1f,samples=deathSamples.Count,minY=deathSamples.Min(),maxY=deathSamples.Max()});
     File.WriteAllText(Root+"/native-probes.json",Json(new{kind="isolated actual Unity components; stationary forward speed, no difficulty claim",folder,power9999=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false),fastLateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),results}));EditorApplication.update-=tick;EditorApplication.isPaused=true;
    }
   }
   catch(Exception e){File.WriteAllText(Root+"/native-probes-error.txt",e.ToString());EditorApplication.update-=tick;EditorApplication.isPaused=true;}
  };
  EditorApplication.update+=tick;return new{folder,scheduled=true};
 }
}
