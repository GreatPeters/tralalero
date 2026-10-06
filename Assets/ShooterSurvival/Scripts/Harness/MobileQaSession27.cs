#if UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using IndianOceanAssets.ShooterSurvival;

// Opt-in, local development-player instrumentation. Not compiled into the
// release player. No sockets, arbitrary code, purchases or persistent upgrades.
public sealed class MobileQaSession27 : MonoBehaviour
{
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 const string Marker="tralalero-local-qa-20261005-27";
 static readonly string[] Scenes={"Noryangjin_MapTool_Mode_SR18_Revamp","HighWay","RestStop","Jamsil","ShoeTower"};
 [Serializable] sealed class Request{public int id;public string action,scene;public float distance,lane,value;public bool enabled,start;public int choice;}
 [Serializable] sealed class State{
  public string utc,scene,error,lastAction,adsStatus;public bool adsReady,adsLoading;public int command,frames,floor,segment,lifts,choice,activeActors,actions,contacts;
  public float realtime,sceneAge,distance,hp,maxHp,attack,lane,medianMs,p95Ms,maxMs,cpuMs,gpuMs,loadSeconds;
  public long allocatedBytes,reservedBytes,monoBytes;public int over50,over100,coins,jewels;
  public bool running,transferring,drive,sustain,goal,opening,development;
  public string[] decisions;public float[] position;
 }
 bool previousAdsLoading;float adsLoadStart;string root,lastAction="boot",error="";int lastId=-1,routeChoice;float pollAt,reportAt,sceneAt,loadSeconds;bool busy,drive,sustain;
 PlayerScript player;Chapter45Director director;CanvasScript canvas;
 readonly List<float> frames=new(512);readonly FrameTiming[] timing=new FrameTiming[1];readonly List<string> decisions=new();
 static MethodInfo move=typeof(PlayerScript).GetMethod("PlayerMove",Private);
 static FieldInfo origin=typeof(PlayerScript).GetField("routeLaneOrigin",Private),right=typeof(PlayerScript).GetField("routeRight",Private),range=typeof(PlayerScript).GetField("xRange",Private);
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){string p=Path.Combine(Application.persistentDataPath,"codex-qa27.enable");if(!Debug.isDebugBuild||!File.Exists(p)||File.ReadAllText(p).Trim()!=Marker)return;var g=new GameObject("Local mobile QA27");DontDestroyOnLoad(g);g.AddComponent<MobileQaSession27>();}
 void Awake(){root=Path.Combine(Application.persistentDataPath,"codex-qa27");Directory.CreateDirectory(root);sceneAt=Time.realtimeSinceStartup;SceneManager.sceneLoaded+=Loaded;Application.logMessageReceived+=Log;Refresh();StartCoroutine(HideDevelopmentOverlays());}
 void OnDestroy(){SceneManager.sceneLoaded-=Loaded;Application.logMessageReceived-=Log;}
 void Log(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert){error=message;File.AppendAllText(Path.Combine(root,"errors.txt"),DateTime.UtcNow.ToString("O")+" "+kind+": "+message+"\n"+stack+"\n");}}
 IEnumerator HideDevelopmentOverlays(){yield return null;yield return null;var h=FindFirstObjectByType<CombatHarness>();if(h!=null)typeof(CombatHarness).GetField("overlayVisible",Private)?.SetValue(h,false);var t=FindFirstObjectByType<MobileFrameTelemetry>();if(t!=null)t.enabled=false;}
 void Refresh(){player=FindFirstObjectByType<PlayerScript>();director=FindFirstObjectByType<Chapter45Director>();canvas=FindFirstObjectByType<CanvasScript>();}
 void Loaded(Scene s,LoadSceneMode m){Refresh();sceneAt=Time.realtimeSinceStartup;frames.Clear();decisions.Clear();StartCoroutine(HideDevelopmentOverlays());if(drive)StartCoroutine(StartWhenReady());}
 IEnumerator StartWhenReady(){yield return new WaitForSecondsRealtime(2);OpeningStoryUI.Instance?.Skip();canvas?.PlayerPressedStartButton();}
 void Update(){ObserveNetwork();frames.Add(Time.unscaledDeltaTime*1000);FrameTimingManager.CaptureFrameTimings();float now=Time.realtimeSinceStartup;if(now>=reportAt){reportAt=now+1;Report();frames.Clear();}if(now<pollAt||busy)return;pollAt=now+.5f;string file=Path.Combine(root,"request.json");if(!File.Exists(file))return;try{var r=JsonUtility.FromJson<Request>(File.ReadAllText(file));if(r==null||r.id<=lastId)return;lastId=r.id;lastAction=r.action;Apply(r);}catch(Exception ex){error=ex.Message;File.WriteAllText(Path.Combine(root,"command-error.txt"),ex.ToString());}Report();}
 void Apply(Request r){
  switch(r.action){
   case "snapshot":break;
   case "skip-opening":OpeningStoryUI.Instance?.Skip();break;
   case "start":canvas?.PlayerPressedStartButton();break;
   case "pause":TimeManager.isGameRunning=!r.enabled;break;
   case "drive":drive=r.enabled;routeChoice=r.choice;break;
   case "sustain":sustain=r.enabled;break;
   case "move":if(player!=null)move.Invoke(player,new object[]{Mathf.Clamp(r.value,-15,15)});break;
   case "load":if(!Scenes.Contains(r.scene))throw new InvalidOperationException("Only five authored chapters allowed");StartCoroutine(Load(r));break;
   case "place":if(director==null)throw new InvalidOperationException("Chapter45 only");if(director.IsTransferring)throw new InvalidOperationException("Do not relocate during a transfer");float distance=Mathf.Clamp(r.distance,0,director.route.Length-.1f);int segment=director.route.SegmentAt(distance);typeof(Chapter45Director).GetField("<CurrentSegment>k__BackingField",Private).SetValue(director,segment);typeof(Chapter45Director).GetField("<Distance>k__BackingField",Private).SetValue(director,distance);director.SampleOnCurrentDeck(distance,out var center,out var forward);player.ApplyContinuousRoutePose(center+Vector3.up*director.footOffset,forward,r.lane);typeof(Chapter45Director).GetMethod("RefreshVisibility",Private).Invoke(director,null);decisions.Add("DIRECTED placement "+distance);break;
   default:throw new InvalidOperationException("Unsupported local QA action");
  }
 }
 IEnumerator Load(Request r){busy=true;drive=false;sustain=false;TimeManager.isGameRunning=false;TimeManager.timeFactor=1;float began=Time.realtimeSinceStartup;yield return SceneManager.LoadSceneAsync(r.scene);loadSeconds=Time.realtimeSinceStartup-began;Refresh();if(r.start)yield return StartWhenReady();busy=false;Report();}
 void FixedUpdate(){if(player==null||!TimeManager.isGameRunning)return;if(sustain&&player.currentHealth>0)player.ApplyHarnessHealthDelta(player.MaxHealth-player.currentHealth);if(!drive||director==null||director.IsTransferring||player.currentHealth<=0||player.IsWorldYawTurnActive)return;
  var o=(Vector3)origin.GetValue(player);var side=(Vector3)right.GetValue(player);var limits=(Vector2)range.GetValue(player);
  if(player.IsStationaryCombat){var hold=player.HoldoutAim;if(hold!=null&&hold.Active){var target=hold.police.Where(a=>a!=null&&a.gameObject.activeInHierarchy&&!a.GetComponent<EnemyScript_space>().IsDead).OrderBy(a=>Vector3.Distance(a.transform.position,player.transform.position)).FirstOrDefault();if(target!=null){var delta=target.transform.position-player.transform.position;float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;hold.RotateAim(Mathf.Clamp(Mathf.DeltaAngle(hold.AimYaw,yaw)/12,-1,1),Time.fixedDeltaTime*TimeManager.timeFactor);}}return;}
  float lane=0;var actors=director.encounters.Where(e=>e.Activated&&!e.Complete&&e.Eligible&&e.floor==director.CurrentFloor).SelectMany(e=>e.actors).Where(a=>a!=null&&a.gameObject.activeInHierarchy&&!a.IsDead&&Vector3.Dot(a.transform.position-player.transform.position,player.transform.forward)>-1&&Vector3.Distance(a.transform.position,player.transform.position)<32).ToArray();var actor=actors.OrderBy(a=>Vector3.Distance(a.transform.position,player.transform.position)).FirstOrDefault();if(actor!=null)lane=Vector3.Dot(actor.transform.position-o,side);
  var warning=actors.Select(a=>a.GetComponent<Chapter45RoleAction>()).FirstOrDefault(r=>r!=null&&r.Phase==Chapter45RoleAction.ActionPhase.Warning);if(warning!=null){float threat=Vector3.Dot(warning.transform.position-o,side);lane=threat>0?limits.x+.65f:limits.y-.65f;}
  var targetPanel=director.targets.Where(t=>t.floor==director.CurrentFloor&&t.Eligible&&!t.Open&&t.distance>=director.Distance-1&&t.distance-director.Distance<32).OrderBy(t=>t.distance).FirstOrDefault();if(actor==null&&targetPanel!=null)lane=targetPanel.CurrentLane;
  foreach(var c in director.choices)if(!c.Selected&&c.OnCurrentFloor&&director.Distance>=c.distance-20&&director.Distance<=c.distance&&(c.kind!=Chapter45Choice.ChoiceKind.FloorRoute||director.CurrentFloorEncountersComplete))lane=routeChoice==0?-3:3;
  foreach(var goal in director.goals.Where(g=>g.floor==director.CurrentFloor&&!g.Claimed))if(Vector3.Distance(goal.transform.position,player.transform.position)<15)lane=Vector3.Dot(goal.transform.position-o,side);
  float deltaLane=Mathf.Clamp(lane,limits.x,limits.y)-Vector3.Dot(player.transform.position-o,side);if(Mathf.Abs(deltaLane)>.15f)move.Invoke(player,new object[]{Mathf.Clamp(deltaLane*6,-15,15)});
 }
 void ObserveNetwork(){var ads=IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsService.Instance;if(ads==null)return;if(ads.Loading&&!previousAdsLoading){var f=typeof(IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsService).GetField("loadStartedAt",Private);adsLoadStart=f!=null?(float)f.GetValue(ads):Time.unscaledTime;File.AppendAllText(Path.Combine(root,"network.txt"),DateTime.UtcNow.ToString("O")+" existing test-ad SDK request started\n");}if(!ads.Loading&&previousAdsLoading)File.AppendAllText(Path.Combine(root,"network.txt"),DateTime.UtcNow.ToString("O")+" existing test-ad SDK callback seconds="+(Time.unscaledTime-adsLoadStart).ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" ready="+ads.Ready+" status="+ads.Status+"\n");previousAdsLoading=ads.Loading;}
 void Report(){if(!Directory.Exists(root))return;var sorted=frames.OrderBy(x=>x).ToArray();uint available=FrameTimingManager.GetLatestTimings(1,timing);var state=new State{utc=DateTime.UtcNow.ToString("O"),scene=SceneManager.GetActiveScene().name,realtime=Time.realtimeSinceStartup,sceneAge=Time.realtimeSinceStartup-sceneAt,lastAction=lastAction,command=lastId,error=error,frames=sorted.Length,medianMs=sorted.Length>0?sorted[sorted.Length/2]:0,p95Ms=sorted.Length>0?sorted[Mathf.Min(sorted.Length-1,(int)(sorted.Length*.95f))]:0,maxMs=sorted.Length>0?sorted[sorted.Length-1]:0,over50=sorted.Count(x=>x>50),over100=sorted.Count(x=>x>100),cpuMs=available>0?(float)timing[0].cpuFrameTime:-1,gpuMs=available>0?(float)timing[0].gpuFrameTime:-1,allocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),reservedBytes=Profiler.GetTotalReservedMemoryLong(),monoBytes=Profiler.GetMonoUsedSizeLong(),hp=player!=null?player.currentHealth:-1,maxHp=player!=null?player.MaxHealth:-1,attack=player!=null?player.ResolvedAttackDamage:-1,position=player!=null?new[]{player.transform.position.x,player.transform.position.y,player.transform.position.z}:null,running=TimeManager.isGameRunning,drive=drive,sustain=sustain,choice=routeChoice,loadSeconds=loadSeconds,development=Debug.isDebugBuild,opening=OpeningStoryUI.IsBlockingGameplay,coins=MoneyScript.S!=null?MoneyScript.S.Coin:-1,jewels=MoneyScript.S!=null?MoneyScript.S.Jewel:-1};if(director!=null){state.distance=director.Distance;state.floor=director.CurrentFloor;state.segment=director.CurrentSegment;state.lifts=director.LiftCount;state.transferring=director.IsTransferring;state.goal=director.GoalClaimed;state.lane=director.Lane;state.activeActors=director.encounters.SelectMany(e=>e.actors).Count(a=>a!=null&&a.gameObject.activeInHierarchy&&!a.IsDead);state.decisions=director.choices.Select(c=>c.name+":"+c.Selected+":"+c.Selection+":"+c.Rewarded).ToArray();var roles=director.encounters.SelectMany(e=>e.actors).Select(a=>a.GetComponent<Chapter45RoleAction>()).Where(r=>r!=null);state.actions=roles.Sum(r=>r.Actions);state.contacts=roles.Sum(r=>r.Contacts);}var ads=IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsService.Instance;if(ads!=null){state.adsReady=ads.Ready;state.adsLoading=ads.Loading;state.adsStatus=ads.Status;}string json=JsonUtility.ToJson(state);File.WriteAllText(Path.Combine(root,"status.json"),json);File.AppendAllText(Path.Combine(root,"telemetry.jsonl"),json+"\n");}
}
#endif
