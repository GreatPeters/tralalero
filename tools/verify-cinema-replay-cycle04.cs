using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
// Directed fixture: earlier encounters/route are seeded, initial HP=2 and weapons disabled.
// Death is caused by native audience physics. Replay calls the actual chapter API.
public static class VerifyCinemaReplayCycle04 {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static Chapter45Director d;static PlayerScript p;static CanvasScript canvas;static RestStopHoldout h;static ChapterProgression chapter;static Camera cam;static StableGameplayCamera follow;
 static string output;static int phase,failed,handle,coin;static double began,at;static float fov,elapsed,yaw;static bool followEnabled;static Vector3 locked;static Vector3[] paused;
 static readonly List<object> checks=new();static readonly List<string> errors=new();
 static string Json(object x)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 static void Set(object o,string property,object value)=>o.GetType().GetField("<"+property+">k__BackingField",Private).SetValue(o,value);
 static void Check(bool pass,string label,object evidence=null){checks.Add(new{pass,label,evidence});if(!pass)failed++;}
 static void Bind(){d=Object.FindFirstObjectByType<Chapter45Director>();p=d.Player;canvas=Object.FindFirstObjectByType<CanvasScript>();chapter=Object.FindFirstObjectByType<ChapterProgression>();h=Object.FindObjectsByType<RestStopHoldout>(FindObjectsSortMode.None).Single(x=>x.chapter45Owner==d);cam=Camera.main;follow=cam.GetComponent<StableGameplayCamera>();}
 static void Next(int n){phase=n;at=EditorApplication.timeSinceStartup;File.WriteAllText(Path.Combine(output,"status.json"),Json(new{phase,elapsed=h?h.Elapsed:0,hp=p?p.currentHealth:0}));}
 static void Shot(string label)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,label+".png"));
 static void Log(string m,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert||t==LogType.Warning&&m.Contains("Animator"))errors.Add(m);}
 public static object Main(string folder){if(!EditorApplication.isPlaying)throw new Exception("Play required");output=folder;Directory.CreateDirectory(folder);checks.Clear();errors.Clear();failed=0;Bind();OpeningStoryUI.Instance?.Skip();canvas.PlayerPressedStartButton();var overlay=Object.FindFirstObjectByType<CombatHarness>();if(overlay)typeof(CombatHarness).GetField("overlayVisible",Private).SetValue(overlay,false);began=EditorApplication.timeSinceStartup;Next(0);Application.logMessageReceived+=Log;EditorApplication.update+=Tick;return new{started=true,output};}
 static void Stage(bool fatal){
  foreach(var e in d.encounters){e.Retire();Set(e,"Complete",true);}foreach(var z in d.hazards){z.manualOnly=true;z.Cancel();}
  Set(d,"CurrentSegment",Array.FindIndex(d.route.segments,s=>s.floor==4));foreach(var c in d.choices)c.Commit(3);
  int segment=Array.FindIndex(d.route.segments,s=>s.floor==6);Set(d,"CurrentSegment",segment);float start=d.route.SegmentStart(segment);d.route.SampleSegment(segment,start,out var origin,out var forward);Set(d,"Distance",start+Vector3.Distance(origin,h.center.position));
  p.ApplyContinuousRoutePose(h.center.position+Vector3.up*d.footOffset,h.center.forward,0);Physics.SyncTransforms();foreach(var w in p.GetComponentsInChildren<WeaponScript>(true))w.enabled=false;p.canShoot=false;
  if(fatal){p.currentHealth=2;p.UpdateHealth();}fov=cam.fieldOfView;followEnabled=follow.enabled;coin=MoneyScript.S.Coin;
 }
 static void Tick(){
  EditorApplication.QueuePlayerLoopUpdate();double age=EditorApplication.timeSinceStartup-at;
  try{
   if(EditorApplication.timeSinceStartup-began>75)throw new TimeoutException("phase "+phase);
   switch(phase){
    case 0:if(age<.3)return;Check(!h.Active&&!h.Completed&&h.Elapsed==0&&h.Spawned==0,"Fresh run starts with empty cinema state");Stage(true);Next(1);return;
    case 1:if(!h.Active)return;Check(p.IsStationaryCombat&&p.HoldoutAim==h&&!follow.enabled,"Native 6F arrival locks position and enters cinema camera");Shot("first-entry");Next(2);return;
    case 2:if(p.currentHealth>0)return;Check(p.LastDamageCause==PlayerDamageCause.EnemyContact&&p.LastDamageWasFatal&&Mathf.Approximately(p.LastDamageAmount,2),"Actual audience collision kills seeded 2HP player",new{p.LastDamageAmount,cause=p.LastDamageCause.ToString(),h.Elapsed,h.Spawned});Next(3);return;
    case 3:if(age<.3)return;
     Check(!h.Active&&!h.Completed&&h.Elapsed<30&&!d.Running,"Death aborts the incomplete survival timer",new{h.Active,h.Completed,h.Elapsed,d.Running});
     Check(h.police.All(a=>!a.gameObject.activeSelf)&&!p.IsStationaryCombat&&p.HoldoutAim==null,"Death retires audience and releases stationary controls");
     Check(follow.enabled==followEnabled&&Mathf.Abs(cam.fieldOfView-fov)<.01f,"Death restores camera follow and original field of view",new{fov,actual=cam.fieldOfView,follow.enabled});
     Check(MoneyScript.S.Coin==coin,"Unrewarded audience contact and failed survival grant no coins",new{coin,actual=MoneyScript.S.Coin});Shot("death-cleanup");Next(4);return;
    case 4:if(age<.25)return;handle=SceneManager.GetActiveScene().handle;chapter.Replay();Next(5);return;
    case 5:if(age<.5||SceneManager.GetActiveScene().handle==handle)return;Bind();OpeningStoryUI.Instance?.Skip();
     Check(p.currentHealth>0&&!TimeManager.isGameRunning&&!CanvasScript.isGameOver&&!p.IsStationaryCombat,"Actual Replay reloads alive lobby and free controls",new{p.currentHealth,TimeManager.isGameRunning,CanvasScript.isGameOver});
     Check(!h.Active&&!h.Completed&&h.Elapsed==0&&h.Spawned==0&&h.Killed==0&&h.police.All(a=>!a.gameObject.activeSelf),"Reloaded cinema timer, counters and audience are reset");
     Check(follow.enabled&&Mathf.Abs(cam.fieldOfView-fov)<.01f,"Reloaded camera returns to authored follow setup",new{fov,actual=cam.fieldOfView,follow.enabled});
     canvas.PlayerPressedStartButton();Next(6);return;
    case 6:if(age<.3)return;Check(d.Running&&h.Elapsed==0&&h.Spawned==0&&!h.Completed,"Replay Start initializes a fresh survival run");Stage(false);Next(7);return;
    case 7:if(!h.Active)return;Check(h.Elapsed<.5f&&h.Spawned==0&&p.IsStationaryCombat&&p.HoldoutAim==h,"Reentry starts timer from zero and relocks controls",new{h.Elapsed,h.Spawned});locked=h.LockedPosition;Next(8);return;
    case 8:if(h.Spawned<3)return;Check(h.Spawned==3&&h.police.Where(a=>a.gameObject.activeSelf).All(a=>!a.GetComponent<Chapter45AudienceContact>().RetiredOnContact),"First new wave has three actors and fresh contact latches",new{h.Spawned});canvas.PauseGame();Next(9);return;
    case 9:if(age<.15)return;elapsed=h.Elapsed;paused=h.police.Where(a=>a.gameObject.activeSelf).Select(a=>a.transform.position).ToArray();Next(10);return;
    case 10:if(age<.5)return;var positions=h.police.Where(a=>a.gameObject.activeSelf).Select(a=>a.transform.position).ToArray();float drift=positions.Length==paused.Length?positions.Zip(paused,(a,b)=>Vector3.Distance(a,b)).DefaultIfEmpty(0).Max():999;
     Check(h.Elapsed==elapsed&&drift<.001f,"Reentered timer and audience pause together",new{elapsed,after=h.Elapsed,drift});canvas.ResumeGame();yaw=h.AimYaw;Next(11);return;
    case 11:h.RotateAim(1,Mathf.Min(Time.unscaledDeltaTime,.05f));if(age<.4)return;
     Check(Mathf.Abs(Mathf.DeltaAngle(yaw,h.AimYaw))>5&&Vector3.Distance(p.transform.position,locked)<.02f&&h.Elapsed>elapsed,"Reentered turn input rotates aim while position stays fixed",new{yaw,after=h.AimYaw,positionDrift=Vector3.Distance(p.transform.position,locked),elapsed=h.Elapsed});Shot("reentered-turn");Next(12);return;
    case 12:if(age<.4)return;Finish();return;
   }
  }catch(Exception e){errors.Add(e.ToString());Finish();}
 }
 static void Finish(){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;File.WriteAllText(Path.Combine(output,"summary.json"),Json(new{utc=DateTime.UtcNow.ToString("o"),passed=checks.Count-failed,failed,checks,errors,directedFixture=true,seededEarlierEncounters=true,seededFirstHealth=2,weaponsDisabled=true,manualPlayerDamage=false,manualAudienceContact=false,actualSceneReplay=true,ordinaryPlaythrough=false}));}
}
