using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Globalization;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
// Directed save-boundary fixtures. Earlier fights/route placement are seeded;
// choices, lifts, survival completion and goal contacts then execute native code.
// No PlayerPrefs.Save, direct player damage, forced goal claim or forced victory.
public static class RestartBoundaryCycle06 {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 const string Catalog="outputs/chapter4-reference-2026-10-02/current-preferences-20261002T110600Z.tsv";
 public sealed class PersistRow{public string key,kind,value;public bool existed;}
 public sealed class Boundary{public string kind,scene;public int pid;public PersistRow[] persisted;}
 static Type JsonType=>AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
 static string Json(object o)=>(string)JsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
 static T Decode<T>(string s)=>(T)JsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{s,typeof(T)});
 static Chapter45Director d;static PlayerScript p;static CanvasScript canvas;static ChapterProgression chapter;static RestStopHoldout h;static Chapter45Choice choice;static Chapter45Lift lift;static Chapter45Goal goal;
 static string output,kind;static int phase,frame,failed,initialCoins,initialJewels,initialFlag;static double began,at;static float expectedJewels;static bool rewardPlaced;
 static readonly List<object> checks=new();static readonly List<string> errors=new(),externalEditorErrors=new();
 static void Set(object o,string property,object value){var f=o.GetType().GetField("<"+property+">k__BackingField",Private);if(f==null)throw new Exception("Missing fixture property "+property);f.SetValue(o,value);}
 static void Check(bool pass,string label,object evidence=null){checks.Add(new{pass,label,evidence});if(!pass)failed++;}
 static PersistRow Read(string key,string type)=>new PersistRow{key=key,kind=type,existed=PlayerPrefs.HasKey(key),value=type=="int"?PlayerPrefs.GetInt(key).ToString(CultureInfo.InvariantCulture):type=="float"?PlayerPrefs.GetFloat(key).ToString("R",CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(key)))};
 public static PersistRow[] Persisted()=>File.ReadAllLines(Catalog).Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>s.Split('\t')).Select(a=>Read(a[0],a[1])).Concat(new[]{Read("TutorialDone","int"),Read("moveSensitivity","float")}).ToArray();
 static object Transient()=>new{d.Running,d.Elapsed,d.FloorElapsed,d.Distance,d.CurrentFloor,d.CurrentSegment,d.IsTransferring,d.LiftCount,d.GoalClaimed,chapter.Completed,chapter.IsAdvancing,chapter.ClearJewels,p.currentHealth,p.MaxHealth,p.IsStationaryCombat,choices=d.choices.Select(c=>new{c.name,c.Selected,c.Selection,c.Rewarded,c.ResolvedCoins}).ToArray(),holdout=h==null?null:new{h.Active,h.Completed,h.Elapsed,h.Spawned,h.Killed},goals=d.goals.Select(g=>new{g.name,g.Claimed,g.offering}).ToArray()};
 static void Bind(){d=Object.FindFirstObjectByType<Chapter45Director>();p=d.Player;canvas=Object.FindFirstObjectByType<CanvasScript>();chapter=Object.FindFirstObjectByType<ChapterProgression>();h=Object.FindObjectsByType<RestStopHoldout>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x.chapter45Owner==d);}
 static void Log(string m,string stack,LogType t){if(t!=LogType.Error&&t!=LogType.Exception&&t!=LogType.Assert)return;if(m.StartsWith("Error: Unknown")&&m.Contains("https://generators-beta.ai.unity.com"))externalEditorErrors.Add(m);else errors.Add(m);}
 public static object Begin(string folder,string boundaryKind){
  if(!EditorApplication.isPlaying)throw new Exception("Owned Play required");output=folder;kind=boundaryKind;Directory.CreateDirectory(folder);if(File.Exists(Path.Combine(folder,"boundary.json")))throw new Exception("Preserve prior boundary evidence");Bind();OpeningStoryUI.Instance?.Skip();checks.Clear();errors.Clear();externalEditorErrors.Clear();failed=phase=0;frame=-1;rewardPlaced=false;began=at=EditorApplication.timeSinceStartup;initialCoins=MoneyScript.S.Coin;initialJewels=MoneyScript.S.Jewel;initialFlag=PlayerPrefs.GetInt("chapter_rewarded_"+chapter.chapter,0);string rewardKey=(initialFlag==0?"firstClearJewels_":"replayClearJewels_")+ChapterSceneKey.Resolve(d.gameObject.scene.name);expectedJewels=EnvironmentVariableTables.TryGetFloat(rewardKey,out float value)?Mathf.Floor(Mathf.Clamp(value,0,100)):0;
  Check(!TimeManager.isGameRunning&&!d.Running&&!chapter.Completed,"Fresh scene starts outside a run");canvas.PlayerPressedStartButton();var overlay=Object.FindFirstObjectByType<CombatHarness>();if(overlay)typeof(CombatHarness).GetField("overlayVisible",Private).SetValue(overlay,false);Application.logMessageReceived+=Log;EditorApplication.update+=Tick;return new{started=true,folder,kind};
 }
 static void Place(int segment,float distance,float lane=0){Set(d,"CurrentSegment",segment);Set(d,"Distance",distance);Set(d,"FloorElapsed",1000f);d.Sample(distance,out var center,out var forward);p.ApplyContinuousRoutePose(center+Vector3.up*d.footOffset,forward,lane);Physics.SyncTransforms();}
 static void Stage(){
  foreach(var e in d.encounters){e.Retire();Set(e,"Complete",true);}foreach(var t in d.targets){Set(t,"Health",0f);typeof(Chapter45Target).GetField("opening",Private).SetValue(t,1f);t.Tick(0);}foreach(var z in d.hazards){z.manualOnly=true;z.Cancel();}foreach(var w in p.GetComponentsInChildren<WeaponScript>(true))w.enabled=false;p.canShoot=false;
  if(kind.StartsWith("street-")){choice=d.choices.First(c=>c.kind==Chapter45Choice.ChoiceKind.RouteFork);Place(d.route.SegmentAt(choice.distance-.5f),choice.distance-.5f,kind=="street-left"?-2.8f:2.8f);}
  else if(kind=="lift-mid"){lift=d.lifts.OrderBy(l=>l.afterSegment).First();Place(lift.afterSegment,d.route.SegmentEnd(lift.afterSegment)-.2f);}
  else if(kind=="b1-landing"){
   int floor4=Array.FindIndex(d.route.segments,s=>s.floor==4);Set(d,"CurrentSegment",floor4);d.choices.First(c=>c.kind==Chapter45Choice.ChoiceKind.FloorRoute).Commit(-3);lift=d.lifts.First(l=>l.choiceIndex>=0&&l.requiredChoice==0);Place(lift.afterSegment,d.route.SegmentEnd(lift.afterSegment)-.2f);
  }
  else if(kind.StartsWith("cinema-")){
   int floor4=Array.FindIndex(d.route.segments,s=>s.floor==4);Set(d,"CurrentSegment",floor4);d.choices.First(c=>c.kind==Chapter45Choice.ChoiceKind.FloorRoute).Commit(3);int segment=Array.FindIndex(d.route.segments,s=>s.floor==6);float start=d.route.SegmentStart(segment);d.route.SampleSegment(segment,start,out var origin,out var forward);Place(segment,start+Vector3.Distance(origin,h.center.position));p.ApplyContinuousRoutePose(h.center.position+Vector3.up*d.footOffset,h.center.forward,0);Physics.SyncTransforms();if(kind=="cinema-complete"){p.currentHealth=300;p.UpdateHealth();}
  }
  else{
   goal=d.goals.First();int first=Array.FindIndex(d.route.segments,s=>s.floor==goal.floor),last=Array.FindLastIndex(d.route.segments,s=>s.floor==goal.floor);float start=d.route.SegmentStart(first),end=d.route.SegmentEnd(last),best=start,score=float.MaxValue;var target=goal.GetComponent<Collider>().bounds.center;
   for(float x=start;x<=end;x+=.1f){d.Sample(x,out var pos,out var forward);float error=(new Vector2(pos.x-target.x,pos.z-target.z)).sqrMagnitude;if(error<score){score=error;best=x;}}
   float staged=Mathf.Max(start,best-(kind=="shoe-before"?14:8));Place(d.route.SegmentAt(staged),staged);
  }
 }
 static void Tick(){EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;try{
  double now=EditorApplication.timeSinceStartup;if(now-began>65)throw new TimeoutException("Boundary "+kind+" phase "+phase);
  if(phase==0){if(now-at<.4)return;Check(d.Running&&p.currentHealth>0,"Native Start begins a live run");Stage();phase=1;at=now;return;}
  if(phase==1){
   if(kind.StartsWith("street-")){
    if(!choice.Selected)return;if(!rewardPlaced){Place(d.route.SegmentAt(choice.rewardDistance+.3f),choice.rewardDistance+.3f);rewardPlaced=true;return;}if(!choice.Rewarded)return;
    int selected=kind=="street-left"?0:1;Check(choice.Selection==selected&&choice.Rewarded,"Native lane choice and route reward reached",new{choice.Selection,choice.Rewarded});Check(MoneyScript.S.Coin-initialCoins==(selected==1?choice.ResolvedCoins:0),"Branch wallet delta matches authored reward",new{initialCoins,actual=MoneyScript.S.Coin,choice.ResolvedCoins});Reach();return;
   }
   if(kind=="lift-mid"){
    if(!d.IsTransferring||(float)typeof(Chapter45Director).GetField("rideElapsed",Private).GetValue(d)<2)return;Check(d.IsTransferring&&!chapter.Completed,"Native lift is in progress at shutdown boundary");Reach();return;
   }
   if(kind=="b1-landing"){
    if(d.IsTransferring||d.LiftCount<1||d.CurrentFloor!=8)return;Check(d.CurrentFloor==8&&!d.IsTransferring,"Native B1 lift arrival completed");Reach();return;
   }
   if(kind=="cinema-active"){
    if(!h.Active||h.Elapsed<4)return;Check(!h.Completed&&h.Spawned>0&&p.IsStationaryCombat,"Native cinema survival is in progress",new{h.Elapsed,h.Spawned});Reach();return;
   }
   if(kind=="cinema-complete"){
    if(!h.Completed)return;Check(!h.Active&&h.Completed&&!p.IsStationaryCombat,"Native 30-second survival completed and released controls",new{h.Elapsed,h.Spawned,h.Killed});Reach();return;
   }
   if(kind=="shoe-before"){Check(!goal.Claimed&&!d.GoalClaimed&&!chapter.Completed,"White-shoe goal has not been collected at the boundary");Reach();return;}
   if(!d.GoalClaimed||!chapter.Completed)return;
   Check(goal.Claimed&&d.GoalClaimed&&chapter.Completed,"Actual goal trigger completed the chapter");Check(MoneyScript.S.Jewel-initialJewels==expectedJewels,"Native first/replay clear payment matches saved flag",new{initialFlag,expectedJewels,actual=MoneyScript.S.Jewel-initialJewels});
   int jewel=MoneyScript.S.Jewel,coin=MoneyScript.S.Coin;chapter.CompleteChapter();chapter.CompleteChapter();canvas.YouWin();Check(MoneyScript.S.Jewel==jewel&&MoneyScript.S.Coin==coin,"Duplicate completion callbacks do not grant another payment");Reach();return;
  }
  if(phase==2&&now-at>.4)Finish();
 }catch(Exception e){errors.Add(e.ToString());Finish();}}
 static void Reach(){
  Check(p.currentHealth>0,"Player is alive at the selected save boundary");if(!chapter.Completed)Check(MoneyScript.S.Jewel==initialJewels&&PlayerPrefs.GetInt("chapter_rewarded_"+chapter.chapter,0)==initialFlag,"An incomplete run has not written a chapter clear reward");
  var data=new{utc=DateTime.UtcNow.ToString("o"),kind,scene=SceneManager.GetActiveScene().name,pid=System.Diagnostics.Process.GetCurrentProcess().Id,persisted=Persisted(),transient=Transient(),wallet=new{coin=MoneyScript.S.Coin,jewel=MoneyScript.S.Jewel},initialCoins,initialJewels,initialFlag,expectedJewels,directedInitialPosition=true,seededEarlierEncounters=true,weaponsDisabled=true,healthOverride=kind=="cinema-complete"?300:0,manualPlayerDamage=false,manualGoalClaim=false,manualWin=false,manualPlayerPrefsSave=false,checkpointImplementationPresent=false};
  File.WriteAllText(Path.Combine(output,"boundary.json"),Json(data));Time.timeScale=0;ScreenCapture.CaptureScreenshot(Path.Combine(output,"boundary.png"));phase=2;at=EditorApplication.timeSinceStartup;
 }
 static void Finish(){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;File.WriteAllText(Path.Combine(output,"boundary-summary.json"),Json(new{passed=checks.Count-failed,failed,checks,errors,externalEditorErrors,readyForNormalExit=failed==0&&errors.Count==0&&File.Exists(Path.Combine(output,"boundary.json"))}));}
 public static object CheckReopened(string folder){
  if(!EditorApplication.isPlaying)throw new Exception("Fresh Play required");Bind();var b=Decode<Boundary>(File.ReadAllText(Path.Combine(folder,"boundary.json")));int pid=System.Diagnostics.Process.GetCurrentProcess().Id;var actual=Persisted();var bad=b.persisted.Where((r,i)=>Json(r)!=Json(actual[i])).Select(r=>r.key).ToArray();var assertions=new List<object>();int fail=0;
  void Assert(bool pass,string label,object evidence=null){assertions.Add(new{pass,label,evidence});if(!pass)fail++;}
  Assert(pid!=b.pid,"Verification runs in a new native Unity process",new{before=b.pid,after=pid});Assert(bad.Length==0,"All78 observed persistent preference entries preserve existence, type and value",new{mismatches=bad});Assert(!d.Running&&!TimeManager.isGameRunning&&!CanvasScript.isGameOver&&!chapter.Completed&&d.Elapsed==0&&d.Distance==0&&!d.IsTransferring&&d.LiftCount==0,"Reopened chapter initializes a fresh run, not a mid-run checkpoint");Assert(d.choices.All(c=>!c.Selected&&!c.Rewarded)&&d.goals.All(g=>!g.Claimed),"Transient route choices and goal latches reset");Assert(h==null||!h.Active&&!h.Completed&&h.Elapsed==0&&h.Spawned==0&&h.Killed==0,"Transient cinema state and counters reset");Assert(p.currentHealth==p.MaxHealth&&!p.IsStationaryCombat,"Player starts alive at permanent maximum health with free controls");Assert(MoneyScript.S.Coin==PlayerPrefs.GetInt("coin")&&MoneyScript.S.Jewel==PlayerPrefs.GetInt("jewel"),"Wallet UI service loads the persisted balance without another reward");Assert(Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None).All(x=>!x.gameObject.activeInHierarchy)&&Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).All(x=>!x.gameObject.activeInHierarchy),"No projectile survives the process restart");
  var result=new{utc=DateTime.UtcNow.ToString("o"),passed=assertions.Count-fail,failed=fail,checks=assertions,kind=b.kind,scene=SceneManager.GetActiveScene().name,pid,transient=Transient(),persisted=actual,midRunCheckpointRestored=false,sceneSelectedByQa=true};File.WriteAllText(Path.Combine(folder,"reopened-verification.json"),Json(result));ScreenCapture.CaptureScreenshot(Path.Combine(folder,"reopened-lobby.png"));return new{passed=assertions.Count-fail,failed=fail,mismatches=bad,pid,checkpointRestored=false};
 }
}
