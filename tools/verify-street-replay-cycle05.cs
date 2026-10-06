using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
// Initial position/earlier encounters are fixtures; full HP, actual hole physics, actual Replay.
public static class VerifyStreetReplayCycle05 {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static Chapter45Director d;static PlayerScript p;static CanvasScript canvas;static ChapterProgression chapter;static Chapter45Hazard hole;static int phase,iteration,frame=-1,failed,handle,jewel,coin;static double at,began;static string output;
 static readonly List<object> checks=new();static readonly List<string> errors=new();
 static string Json(object x)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 static void Set(object o,string property,object value)=>o.GetType().GetField("<"+property+">k__BackingField",Private).SetValue(o,value);
 static void Check(bool pass,string label,object evidence=null){checks.Add(new{pass,label,iteration,evidence});if(!pass)failed++;}
 static void Bind(){d=Object.FindFirstObjectByType<Chapter45Director>();p=d.Player;canvas=Object.FindFirstObjectByType<CanvasScript>();chapter=Object.FindFirstObjectByType<ChapterProgression>();hole=d.hazards.Single(h=>h.name=="Open manhole");}
 static void Next(int n){phase=n;at=EditorApplication.timeSinceStartup;File.WriteAllText(Path.Combine(output,"status.json"),Json(new{phase,iteration,hp=p.currentHealth}));}
 static void Snapshot(string label){File.WriteAllText(Path.Combine(output,iteration.ToString("00")+"-"+label+".json"),Json(Cycle05LifecycleMetrics.Snapshot(label,iteration)));ScreenCapture.CaptureScreenshot(Path.Combine(output,iteration.ToString("00")+"-"+label+".png"));}
 static void Log(string m,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert||t==LogType.Warning&&m.Contains("Animator"))errors.Add(m);}
 public static object Main(string root){if(!EditorApplication.isPlaying)throw new Exception("Play required");output=root;Directory.CreateDirectory(root);checks.Clear();errors.Clear();iteration=failed=0;frame=-1;Bind();OpeningStoryUI.Instance?.Skip();began=EditorApplication.timeSinceStartup;Next(0);Application.logMessageReceived+=Log;EditorApplication.update+=Tick;return new{started=true,output};}
 static void Stage(){
  canvas.PlayerPressedStartButton();foreach(var e in d.encounters){e.Retire();Set(e,"Complete",true);}foreach(var h in d.hazards)if(h!=hole){h.manualOnly=true;h.Cancel();}foreach(var c in d.choices)c.Commit(-2.8f);d.GrantShield();
  float distance=hole.distance-hole.warningDistance-2;Set(d,"Distance",distance);Set(d,"CurrentSegment",d.route.SegmentAt(distance));d.Sample(distance,out var center,out var forward);p.ApplyContinuousRoutePose(center+Vector3.up*d.footOffset,forward,1.8f);Physics.SyncTransforms();p.canShoot=false;foreach(var w in p.GetComponentsInChildren<WeaponScript>(true))w.enabled=false;jewel=MoneyScript.S.Jewel;coin=MoneyScript.S.Coin;
 }
 static void Tick(){EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;double age=EditorApplication.timeSinceStartup-at;try{
  if(EditorApplication.timeSinceStartup-began>80)throw new TimeoutException("phase "+phase);
  switch(phase){
   case 0:if(age<.5)return;Check(p.currentHealth==60&&!TimeManager.isGameRunning&&!CanvasScript.isGameOver&&!d.Running&&d.Elapsed==0&&d.Distance==0,"Fresh street lobby has full health and reset clocks");Snapshot("lobby");Stage();Next(1);return;
   case 1:if(p.currentHealth>0)return;Check(hole.Contacts==1&&p.LastDamageCause==PlayerDamageCause.Hole&&p.LastDamageWasFatal,"Native hole contact kills full-health shielded player once",new{hole.Contacts,cause=p.LastDamageCause.ToString(),p.LastDamageAmount});Next(2);return;
   case 2:if(age<3.5)return;Check(!d.Running&&!d.IsTransferring&&!p.IsStationaryCombat&&!chapter.Completed&&!d.GoalClaimed,"Fatal collision releases gameplay without clear state");Check(MoneyScript.S.Jewel==jewel,"Death does not pay a clear reward",new{jewel,after=MoneyScript.S.Jewel});Snapshot("death-ui");handle=SceneManager.GetActiveScene().handle;chapter.Replay();Next(3);return;
   case 3:if(age<.6||SceneManager.GetActiveScene().handle==handle)return;Bind();OpeningStoryUI.Instance?.Skip();Check(p.currentHealth==60&&!CanvasScript.isGameOver&&!TimeManager.isGameRunning&&!d.Running&&d.Elapsed==0&&!d.IsTransferring,"Actual Replay reloads alive street lobby",new{p.currentHealth,d.Elapsed});Check(d.choices.All(c=>!c.Selected&&!c.Rewarded)&&d.goals.All(g=>!g.Claimed)&&hole.Contacts==0&&!hole.Triggered&&!hole.Active,"Replay resets route choice, rewards, goals and hole contact latch");Check(Cycle05LifecycleMetrics.ActiveProjectiles()==0,"No active projectile survives street Replay");Check(!Cycle05LifecycleMetrics.Delegates().Any(x=>x.destroyedTarget),"Observed subscriptions contain no destroyed Unity target after street Replay");Snapshot("replay-lobby");Next(4);return;
   case 4:if(age<.5)return;if(iteration>=1){Finish();return;}iteration++;Next(0);return;
  }
 }catch(Exception e){errors.Add(e.ToString());Finish();}}
 static void Finish(){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;File.WriteAllText(Path.Combine(output,"summary.json"),Json(new{utc=DateTime.UtcNow.ToString("o"),passed=checks.Count-failed,failed,checks,errors,iterations=iteration+1,samePlaySession=true,directedInitialPosition=true,seededEarlierEncounters=true,healthOverride=false,manualPlayerDamage=false,manualHazardHit=false,actualSceneReplay=true,ordinaryPlaythrough=false}));}
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
