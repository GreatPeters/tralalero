using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
// Directed fixture: earlier encounters/route are seeded, initial HP=2 and weapons disabled.
// Death is caused by native audience physics. Replay calls the actual chapter API.
public static class VerifyCinemaReplayCycle05 {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static Chapter45Director d;static PlayerScript p;static CanvasScript canvas;static RestStopHoldout h;static ChapterProgression chapter;static Camera cam;static StableGameplayCamera follow;
 static string output;static int iteration,frame=-1;static int phase,failed,handle,coin;static double began,at;static float fov,elapsed,yaw;static bool followEnabled;static Vector3 locked;static Vector3[] paused;
 static readonly List<object> checks=new();static readonly List<string> errors=new();
 static string Json(object x)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 static void Set(object o,string property,object value)=>o.GetType().GetField("<"+property+">k__BackingField",Private).SetValue(o,value);
 static void Check(bool pass,string label,object evidence=null){checks.Add(new{pass,label,iteration,evidence});if(!pass)failed++;}
 static void Bind(){d=Object.FindFirstObjectByType<Chapter45Director>();p=d.Player;canvas=Object.FindFirstObjectByType<CanvasScript>();chapter=Object.FindFirstObjectByType<ChapterProgression>();h=Object.FindObjectsByType<RestStopHoldout>(FindObjectsSortMode.None).Single(x=>x.chapter45Owner==d);cam=Camera.main;follow=cam.GetComponent<StableGameplayCamera>();}
 static void Next(int n){phase=n;at=EditorApplication.timeSinceStartup;File.WriteAllText(Path.Combine(output,"status.json"),Json(new{phase,elapsed=h?h.Elapsed:0,hp=p?p.currentHealth:0}));}
 static void Shot(string label)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,iteration.ToString("00")+"-"+label+".png"));
 static void Log(string m,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert||t==LogType.Warning&&m.Contains("Animator"))errors.Add(m);}
 public static object Main(string folder){if(!EditorApplication.isPlaying)throw new Exception("Play required");output=folder;Directory.CreateDirectory(folder);checks.Clear();errors.Clear();failed=0;iteration=0;frame=-1;Bind();OpeningStoryUI.Instance?.Skip();canvas.PlayerPressedStartButton();var overlay=Object.FindFirstObjectByType<CombatHarness>();if(overlay)typeof(CombatHarness).GetField("overlayVisible",Private).SetValue(overlay,false);began=EditorApplication.timeSinceStartup;Next(0);Application.logMessageReceived+=Log;EditorApplication.update+=Tick;return new{started=true,output};}
 static void Stage(bool fatal){
  foreach(var e in d.encounters){e.Retire();Set(e,"Complete",true);}foreach(var z in d.hazards){z.manualOnly=true;z.Cancel();}
  Set(d,"CurrentSegment",Array.FindIndex(d.route.segments,s=>s.floor==4));foreach(var c in d.choices)c.Commit(3);
  int segment=Array.FindIndex(d.route.segments,s=>s.floor==6);Set(d,"CurrentSegment",segment);float start=d.route.SegmentStart(segment);d.route.SampleSegment(segment,start,out var origin,out var forward);Set(d,"Distance",start+Vector3.Distance(origin,h.center.position));
  p.ApplyContinuousRoutePose(h.center.position+Vector3.up*d.footOffset,h.center.forward,0);Physics.SyncTransforms();foreach(var w in p.GetComponentsInChildren<WeaponScript>(true))w.enabled=false;p.canShoot=false;
  if(fatal){p.currentHealth=2;p.UpdateHealth();}fov=cam.fieldOfView;followEnabled=follow.enabled;coin=MoneyScript.S.Coin;
 }
 static void Tick(){
  EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;double age=EditorApplication.timeSinceStartup-at;
  try{
   if(EditorApplication.timeSinceStartup-began>180)throw new TimeoutException("phase "+phase);
   switch(phase){
    case 0:if(age<.3)return;File.WriteAllText(Path.Combine(output,iteration.ToString("00")+"-fresh-run.json"),Json(Cycle05LifecycleMetrics.Snapshot("fresh-cinema-run",iteration)));Check(!h.Active&&!h.Completed&&h.Elapsed==0&&h.Spawned==0,"Fresh run starts with empty cinema state");Stage(true);Next(1);return;
    case 1:if(!h.Active)return;Check(p.IsStationaryCombat&&p.HoldoutAim==h&&!follow.enabled,"Native 6F arrival locks position and enters cinema camera");Shot("first-entry");Next(2);return;
    case 2:if(p.currentHealth>0)return;Check(p.LastDamageCause==PlayerDamageCause.EnemyContact&&p.LastDamageWasFatal&&Mathf.Approximately(p.LastDamageAmount,2),"Actual audience collision kills seeded 2HP player",new{p.LastDamageAmount,cause=p.LastDamageCause.ToString(),h.Elapsed,h.Spawned});Next(3);return;
    case 3:if(age<3.5)return;File.WriteAllText(Path.Combine(output,iteration.ToString("00")+"-death.json"),Json(Cycle05LifecycleMetrics.Snapshot("cinema-death-ui",iteration)));
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
    case 12:if(age<.4)return;File.WriteAllText(Path.Combine(output,iteration.ToString("00")+"-reentered.json"),Json(Cycle05LifecycleMetrics.Snapshot("reentered-cinema",iteration)));if(iteration>=3){Finish();return;}handle=SceneManager.GetActiveScene().handle;chapter.Replay();Next(13);return;
    case 13:if(age<.6||SceneManager.GetActiveScene().handle==handle)return;Bind();OpeningStoryUI.Instance?.Skip();Check(!h.Active&&!h.Completed&&h.Elapsed==0&&!p.IsStationaryCombat&&follow.enabled,"Scene unload during reentered holdout releases state before next repetition");canvas.PlayerPressedStartButton();iteration++;Next(0);return;
   }
  }catch(Exception e){errors.Add(e.ToString());Finish();}
 }
 static void Finish(){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;File.WriteAllText(Path.Combine(output,"summary.json"),Json(new{utc=DateTime.UtcNow.ToString("o"),passed=checks.Count-failed,failed,checks,errors,directedFixture=true,seededEarlierEncounters=true,seededFirstHealth=2,weaponsDisabled=true,manualPlayerDamage=false,manualAudienceContact=false,actualSceneReplay=true,iterations=iteration+1,samePlaySession=true,ordinaryPlaythrough=false}));}
}

public static class Cycle05LifecycleMetrics {
 const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 public sealed class ListenerRow{public string owner,eventName,method,targetType;public bool destroyedTarget;}
 public static object Memory(){using var process=System.Diagnostics.Process.GetCurrentProcess();return new{unityAllocated=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),unityReserved=UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong(),monoUsed=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),monoHeap=UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong(),managed=GC.GetTotalMemory(false),processPrivate=process.PrivateMemorySize64,workingSet=process.WorkingSet64};}
 public static ListenerRow[] Delegates(){
  var rows=new List<ListenerRow>();
  void Add(Type type,object instance,bool onlyStatic){if(type==null)return;foreach(var f in type.GetFields(Flags)){if(f.IsStatic!=onlyStatic||!typeof(Delegate).IsAssignableFrom(f.FieldType))continue;var action=f.GetValue(instance) as Delegate;if(action==null)continue;foreach(var d in action.GetInvocationList()){object target=d.Target;bool destroyed=target is Object u&&u==null;rows.Add(new ListenerRow{owner=type.FullName,eventName=f.Name,method=d.Method.DeclaringType?.FullName+"."+d.Method.Name,targetType=target?.GetType().FullName,destroyedTarget=destroyed});}}}
  Add(typeof(SceneManager),null,true);Add(typeof(ChapterUpgradeService),null,true);Add(typeof(CosmeticService),null,true);Add(typeof(PlayerScript),null,true);
  var money=MoneyScript.S;if(money!=null)Add(money.GetType(),money,false);var stats=UpgradeStatManager.S;if(stats!=null)Add(stats.GetType(),stats,false);var player=Object.FindFirstObjectByType<PlayerScript>();if(player!=null)Add(player.GetType(),player,false);
  var ads=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x!=null&&x.GetType().Name=="RewardedAdsService");if(ads!=null)Add(ads.GetType(),ads,false);
  return rows.ToArray();
 }
 public static int ActiveProjectiles()=>Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None).Count(x=>x.gameObject.activeInHierarchy)+Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Count(x=>x.gameObject.activeInHierarchy);
 public static object Snapshot(string label,int run){
  // Read memory first: the inventories below allocate QA arrays and dictionaries.
  var memory=Memory();var gos=Resources.FindObjectsOfTypeAll<GameObject>().Where(x=>x!=null&&x.scene.IsValid()).ToArray();var objects=Resources.FindObjectsOfTypeAll<Object>();
  var liveScene=SceneManager.GetActiveScene();var d=Object.FindFirstObjectByType<Chapter45Director>();var p=Object.FindFirstObjectByType<PlayerScript>();var h=Object.FindObjectsByType<RestStopHoldout>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x.chapter45Owner!=null).ToArray();
  var counts=objects.Where(x=>x!=null).GroupBy(x=>x.GetType().FullName).OrderByDescending(g=>g.Count()).Select(g=>new{type=g.Key,count=g.Count()}).ToArray();
  var nonAssetObjects=objects.Where(x=>x!=null&&!EditorUtility.IsPersistent(x)).GroupBy(x=>x.GetType().FullName).OrderByDescending(g=>g.Count()).Select(g=>new{type=g.Key,count=g.Count()}).ToArray();
  var persistent=gos.Where(x=>x.scene.name=="DontDestroyOnLoad").Select(x=>new{x.name,id=x.GetInstanceID(),active=x.activeInHierarchy,components=x.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray()}).ToArray();
  var enemies=gos.SelectMany(x=>x.GetComponents<EnemyScript_space>()).ToArray();var bullets=gos.SelectMany(x=>x.GetComponents<BulletScript>()).ToArray();var simple=gos.SelectMany(x=>x.GetComponents<SimpleProjectile>()).ToArray();
  return new{utc=DateTime.UtcNow.ToString("o"),label,run,scene=liveScene.name,handle=liveScene.handle,memory,totalGameObjects=gos.Length,activeGameObjects=gos.Count(x=>x.activeInHierarchy),byScene=gos.GroupBy(x=>x.scene.name).Select(g=>new{scene=g.Key,count=g.Count(),active=g.Count(x=>x.activeInHierarchy)}).ToArray(),persistent,objectCounts=counts,nonAssetObjectCounts=nonAssetObjects,listeners=Delegates(),uiEvents=UiEvents(),materials=objects.OfType<Material>().Where(m=>m!=null&&!EditorUtility.IsPersistent(m)).Select(m=>new{id=m.GetInstanceID(),m.name,shader=m.shader?m.shader.name:null,flags=m.hideFlags.ToString()}).ToArray(),enemies=new{total=enemies.Length,active=enemies.Count(x=>x.gameObject.activeInHierarchy),aliveActive=enemies.Count(x=>x.gameObject.activeInHierarchy&&!x.IsDead)},bullets=new{total=bullets.Length,active=bullets.Count(x=>x.gameObject.activeInHierarchy)},roleProjectiles=new{total=simple.Length,active=simple.Count(x=>x.gameObject.activeInHierarchy)},director=d==null?null:new{d.Running,d.Elapsed,d.FloorElapsed,d.Distance,d.CurrentFloor,d.IsTransferring,d.LiftCount,d.GoalClaimed,encounters=d.encounters.Select(e=>new{e.name,e.Activated,e.Complete}).ToArray(),choices=d.choices.Select(c=>new{c.name,c.Selected,c.Selection,c.Rewarded}).ToArray()},holdouts=h.Select(x=>new{x.name,x.Active,x.Completed,x.Elapsed,x.Spawned,x.Killed}).ToArray(),player=p==null?null:new{p.currentHealth,p.IsStationaryCombat},renderer=new{UnityStats.batches,UnityStats.setPassCalls,UnityStats.triangles,frameMs=Time.unscaledDeltaTime*1000}};
 }
 public static object[] UiEvents(){
  string PathOf(Transform t){string path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path;}
  int RuntimeCount(UnityEngine.Events.UnityEventBase evt){var f=typeof(UnityEngine.Events.UnityEventBase).GetField("m_Calls",Flags);var calls=f?.GetValue(evt);var list=calls?.GetType().GetField("m_RuntimeCalls",Flags)?.GetValue(calls) as System.Collections.ICollection;return list==null?-1:list.Count;}
  var scene=SceneManager.GetActiveScene();return Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b.gameObject.scene==scene).Select(b=>(object)new{path=PathOf(b.transform),active=b.gameObject.activeInHierarchy,persistent=b.onClick.GetPersistentEventCount(),runtime=RuntimeCount(b.onClick)}).ToArray();
 }
}
