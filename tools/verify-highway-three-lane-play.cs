using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

// Real movement, collisions and completion with the existing map-tool 9999 test preset.
// Never disables hazards, pins health, teleports, or saves scenes. Not a campaign balance cohort.
public static class VerifyHighwayThreeLanePlay
{
    const string Root="outputs/highway-three-lane-rebuild-2026-09-26/play-v6";
    static int cohortStartRun;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly MethodInfo Move=typeof(PlayerScript).GetMethod("PlayerMove",Private);
    static readonly FieldInfo LaneOrigin=typeof(PlayerScript).GetField("routeLaneOrigin",Private),LaneRight=typeof(PlayerScript).GetField("routeRight",Private),Cars=typeof(OncomingLaneTraffic).GetField("cars",Private),Logs=typeof(LogTruckSpill).GetField("logs",Private);
    static PlayerScript player;static HighwayRoute route;static ChapterProgression chapter;static OncomingLaneTraffic traffic;
    static EnemyScript_space[] enemies;static Collider[] staticHazards;static LogTruckSpill[] spills;static HighwayHazard[] gates;
    struct RoadblockView {public HighwayHazard hazard;public Collider collider;public float d,lane,width,half;}
    static readonly List<RoadblockView> roadblocks=new();
    static int run,frame=-1,moves,shotIndex;static bool active,started,ended;static double readyAt,endedAt;
    static float began,nextSample,initialHp;static string Folder=>Root+"/"+(run==0?"main":"bypass");
    static readonly float[] Shots={40,180,380,710,1020,1350,1740,1830,2020,2240,2640,2790};
    static readonly HashSet<int> seenCars=new();static readonly HashSet<float> usedLanes=new();static readonly HashSet<int> seenLogs=new();
    static readonly List<object> damage=new();static int reversedFrames,fullWallFrames,roadblockOverlapFrames,wrongTruckDirectionFrames;static float minGap;static string pendingError;
    static string Json(object o)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
    static void Write(string path,object o)=>File.WriteAllText(path,Json(o));
    static void SelectStartStage(int stage)
    {
        var type=typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage");
        type.GetMethod("SelectStage",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{stage});
    }
    public static object Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");Directory.CreateDirectory(Root);
        var snapshot=ChapterPlaytestPreferences.SnapshotAt(Root+"/before-prefs.tsv");
        File.WriteAllText(Root+"/start-stage-before.txt",SessionState.GetInt("NoryangjinMapTool.TestStartStage",0).ToString());SelectStartStage(2);
        var keys=new[]{"TutorialDone"}.Concat(Enumerable.Range(1,5).SelectMany(c=>new[]{ChapterUpgradeService.LevelKey(c),ChapterUpgradeService.OwnedKey(c)}));
        File.WriteAllLines(Root+"/extra-prefs.tsv",keys.Select(k=>k+"\t"+PlayerPrefs.HasKey(k)+"\t"+PlayerPrefs.GetInt(k)));
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",true);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",2);
        Write(Root+"/conditions.json",new{mode="map-tool HP/ATT 9999 once at run start",normalLateral=true,timeScale=2,hazardsEnabled=true,collidersEnabled=true,healthPin=false,teleports=false,transitionSuppressedForRepeat=true});return snapshot;
    }
    public static object Begin()
    {
        if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="HighWay"||!File.Exists(Root+"/before-prefs.tsv")||Directory.Exists(Root+"/main"))throw new Exception("Fresh prepared HighWay Play Mode required");
        cohortStartRun=run=0;active=true;started=ended=false;player=null;EditorApplication.update+=Tick;return new{active};
    }
    public static object BeginBypass()
    {
        if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="HighWay"||!File.Exists(Root+"/before-prefs.tsv")||Directory.Exists(Root+"/bypass"))throw new Exception("Fresh prepared HighWay Play Mode required");
        cohortStartRun=run=1;active=true;started=ended=false;player=null;EditorApplication.update+=Tick;return new{active,run};
    }
    public static object StopDrivers()
    {
        int stopped=0;
        foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type=assembly.GetType("VerifyHighwayThreeLanePlay");if(type==null)continue;
            var flag=type.GetField("active",BindingFlags.Static|BindingFlags.NonPublic);if(flag==null||!(bool)flag.GetValue(null))continue;
            flag.SetValue(null,false);var tick=type.GetMethod("Tick",BindingFlags.Static|BindingFlags.NonPublic);
            EditorApplication.update-=(EditorApplication.CallbackFunction)Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction),tick);stopped++;
        }
        return new{stopped};
    }
    struct CarView {public Transform t;public float d,lane,speed,half,dir;}
    static float F(object o,string name)=>(float)o.GetType().GetField(name).GetValue(o);
    static List<CarView> ReadCars()
    {
        var result=new List<CarView>();if(traffic==null)return result;
        foreach(var c in (IEnumerable)Cars.GetValue(traffic))result.Add(new CarView{t=(Transform)c.GetType().GetField("t").GetValue(c),d=F(c,"d"),lane=F(c,"lane"),speed=F(c,"speed"),half=F(c,"half"),dir=F(c,"dir")});return result;
    }
    static void Tick()
    {
        if(!active||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isPaused||frame==Time.frameCount)return;frame=Time.frameCount;
        try
        {
            if(ended)
            {
                if(EditorApplication.timeSinceStartup-endedAt<3)return;
                if(run==1||pendingError!=null){active=false;EditorApplication.update-=Tick;Time.timeScale=1;ScreenCapture.CaptureScreenshot(Folder+"/result-panel.png");EditorApplication.delayCall+=()=>EditorApplication.isPaused=true;Write(Root+"/complete.json",new{finished=true,runs=run-cohortStartRun+1,error=pendingError});return;}
                player.DamageTaken-=OnDamage;run++;player=null;started=ended=false;SceneManager.LoadScene("HighWay");return;
            }
            if(player==null)
            {
                if(SceneManager.GetActiveScene().name!="HighWay")throw new Exception("Unexpected scene during Highway cohort");
                player=Object.FindFirstObjectByType<PlayerScript>();route=Object.FindFirstObjectByType<HighwayRoute>();chapter=Object.FindFirstObjectByType<ChapterProgression>();traffic=Object.FindFirstObjectByType<OncomingLaneTraffic>();if(player==null)return;if(route==null||traffic==null)throw new Exception("Highway route/traffic missing");readyAt=EditorApplication.timeSinceStartup+1.5;return;
            }
            if(!started)
            {
                if(EditorApplication.timeSinceStartup<readyAt)return;
                OpeningStoryUI.Instance?.Skip();UnityEngine.Random.InitState(202609260+run);
                chapter.nextScene=""; // completion still fires; keep a repeatable HighWay test session.
                Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();if(!TimeManager.isGameRunning)return;
                var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.Close();tutorial.enabled=false;}
                foreach(var h in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))h.enabled=false;
                Time.timeScale=2;TimeManager.timeFactor=1;began=Time.time;initialHp=player.currentHealth;moves=shotIndex=0;nextSample=0;minGap=float.PositiveInfinity;reversedFrames=fullWallFrames=0;damage.Clear();seenCars.Clear();seenLogs.Clear();usedLanes.Clear();pendingError=null;
                enemies=Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None);spills=Object.FindObjectsByType<LogTruckSpill>(FindObjectsSortMode.None);gates=Object.FindObjectsByType<HighwayHazard>(FindObjectsSortMode.None).Where(g=>g.GetComponent<ObstacleStats>()?.obstaclePattern==ObstaclePattern.HighwayToll).ToArray();
                staticHazards=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.GetComponentInParent<RoadPotholeHazard>()!=null||c.GetComponentInParent<HighwayHazard>()!=null).ToArray();
                roadblocks.Clear();roadblockOverlapFrames=wrongTruckDirectionFrames=0;
                foreach(var h in Object.FindObjectsByType<HighwayHazard>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(h.GetComponent<ObstacleStats>()?.obstaclePattern!=ObstaclePattern.HighwayRoadblock)continue;var col=h.GetComponent<Collider>();if(col==null)continue;var b=col.bounds;float d=route.NearestDistance(b.center);route.Sample(d,false,out var p,out var f);var side=Vector3.Cross(Vector3.up,f);
                    roadblocks.Add(new RoadblockView{hazard=h,collider=col,d=d,lane=Vector3.Dot(b.center-p,side),width=Mathf.Abs(side.x)*b.extents.x+Mathf.Abs(side.z)*b.extents.z,half=Mathf.Abs(f.x)*b.extents.x+Mathf.Abs(f.z)*b.extents.z});
                }
                player.DamageTaken+=OnDamage;Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/timeline.csv","time,distance,hp,lane,bypass,cars\n");started=true;return;
            }
            float elapsed=Time.time-began;var cars=ReadCars();
            foreach(var c in cars){if(c.t==null)continue;seenCars.Add(c.t.GetInstanceID());usedLanes.Add(c.lane);route.Sample(c.d,false,out _,out var forward);if(c.dir>=0||Vector3.Dot(c.t.forward,forward)>-.9f)reversedFrames++;}
            var closeLanes=new HashSet<float>(cars.Where(c=>Mathf.Abs(c.d-route.Distance)<5).Select(c=>c.lane));
            foreach(var lane in cars.GroupBy(c=>c.lane)){var sorted=lane.OrderBy(c=>c.d).ToArray();for(int i=1;i<sorted.Length;i++)minGap=Mathf.Min(minGap,sorted[i].d-sorted[i-1].d-sorted[i].half-sorted[i-1].half);}
            foreach(var block in roadblocks)if(block.hazard!=null&&block.hazard.isActiveAndEnabled&&!block.hazard.Broken&&block.collider.enabled)
                foreach(var c in cars)if(Mathf.Abs(c.d-block.d)<c.half+block.half&&Mathf.Abs(c.lane-block.lane)<traffic.hitRadius+block.width)roadblockOverlapFrames++;
            foreach(var spill in spills)
            {
                foreach(var log in (IEnumerable)Logs.GetValue(spill)){var t=(Transform)log.GetType().GetField("t").GetValue(log);if(t!=null)seenLogs.Add(t.GetInstanceID());}
                var truck=(Transform)typeof(LogTruckSpill).GetField("truck",Private).GetValue(spill);if(truck!=null)
                {
                    float d=(float)typeof(LogTruckSpill).GetField("truckD",Private).GetValue(spill);route.Sample(d,false,out _,out var forward);if(Vector3.Dot(truck.forward,forward)>-.9f)wrongTruckDirectionFrames++;
                    if(Mathf.Abs(d-route.Distance)<5)closeLanes.Add(spill.truckLane);
                    foreach(var block in roadblocks)if(block.hazard!=null&&block.hazard.isActiveAndEnabled&&!block.hazard.Broken&&block.collider.enabled&&Mathf.Abs(d-block.d)<7+block.half&&Mathf.Abs(spill.truckLane-block.lane)<1.6f+block.width)roadblockOverlapFrames++;
                }
            }
            if(closeLanes.Count>=3)fullWallFrames++;
            if(elapsed>=nextSample)
            {
                nextSample=elapsed+.5f;var origin=(Vector3)LaneOrigin.GetValue(player);var right=(Vector3)LaneRight.GetValue(player);float lane=Vector3.Dot(player.transform.position-origin,right);
                File.AppendAllText(Folder+"/timeline.csv",string.Join(",",elapsed,route.Distance,player.currentHealth,lane,route.OnBypass,cars.Count)+"\n");Write(Root+"/status.json",new{run,elapsed,distance=route.Distance,hp=player.currentHealth,cars=seenCars.Count,usedLanes=usedLanes.ToArray(),reversedFrames,wrongTruckDirectionFrames,roadblockOverlapFrames,fullWallFrames,minGap});
            }
            if(shotIndex<Shots.Length&&route.Distance>=Shots[shotIndex]){ScreenCapture.CaptureScreenshot(Folder+"/d-"+((int)Shots[shotIndex]).ToString("0000")+".png");shotIndex++;}
            if(player.currentHealth<=0||!TimeManager.isGameRunning||elapsed>420)
            {
                ended=true;endedAt=EditorApplication.timeSinceStartup;Write(Folder+"/result.json",new{run,outcome=chapter.Completed?"clear":player.currentHealth<=0?"death":"timeout",elapsed,distance=route.Distance,initialHp,health=player.currentHealth,damage,seenCars=seenCars.Count,usedLanes=usedLanes.OrderBy(x=>x).ToArray(),seenLogs=seenLogs.Count,reversedFrames,wrongTruckDirectionFrames,roadblockOverlapFrames,fullWallFrames,minGap,moves,killed=enemies.Count(e=>e!=null&&e.IsDead),forks=route.forks.Select(f=>new{f.decided,f.bypass,f.rewarded}).ToArray()});ScreenCapture.CaptureScreenshot(Folder+"/end.png");return;
            }
            Steer(cars);
        }
        catch(Exception error){File.WriteAllText(Root+"/error.txt",error.ToString());active=false;EditorApplication.update-=Tick;EditorApplication.isPaused=true;}
    }
    static void OnDamage(float amount,PlayerDamageCause cause)=>damage.Add(new{time=Time.time-began,distance=route.Distance,amount,cause=cause.ToString(),hp=player.currentHealth});
    static void Steer(List<CarView> cars)
    {
        var origin=(Vector3)LaneOrigin.GetValue(player);var side=(Vector3)LaneRight.GetValue(player);var forward=player.transform.forward;float current=Vector3.Dot(player.transform.position-origin,side),desired=-1.1f;
        var target=enemies.Where(e=>e!=null&&!e.IsDead&&e.gameObject.activeInHierarchy).Where(e=>{var v=e.transform.position-player.transform.position;return Vector3.Dot(v,forward)>0&&Vector3.Dot(v,forward)<38&&Mathf.Abs(Vector3.Dot(v,side))<5&&Mathf.Abs(v.y)<3;}).OrderBy(e=>(e.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
        if(target!=null){float d=route.NearestDistance(target.transform.position);route.Sample(d,route.OnBypass,out var p,out var f);desired=Vector3.Dot(target.transform.position-p,Vector3.Cross(Vector3.up,f));}
        var selecting=route.forks.FirstOrDefault(f=>!f.decided&&route.Distance>=f.start-45);if(selecting!=null)desired=run==0?-1.5f:1.5f;
        if(route.OnBypass)desired=0;
        var obstacles=new List<Vector2>();
        foreach(var c in staticHazards)
        {
            if(c==null||!c.enabled||!c.gameObject.activeInHierarchy)continue;var h=c.GetComponentInParent<HighwayHazard>();if(h!=null&&!h.enabled)continue;
            var v=c.bounds.center-player.transform.position;float ahead=Vector3.Dot(v,forward),lane=Vector3.Dot(c.bounds.center-origin,side);
            if(ahead< -1||ahead>25||Mathf.Abs(lane)>7||Mathf.Abs(v.y)>4)continue;
            float half=Mathf.Abs(side.x)*c.bounds.extents.x+Mathf.Abs(side.z)*c.bounds.extents.z;obstacles.Add(new Vector2(lane,half+.55f));
        }
        if(!route.OnBypass)
        {
            foreach(var c in cars)if(c.d-route.Distance> -4&&c.d-route.Distance<38)obstacles.Add(new Vector2(c.lane,traffic.hitRadius+.25f));
            foreach(var spill in spills)
            {
                // The red lane warning is visible before the logs spawn. React to that cue,
                // not only to physical logs that may already be too close for keyboard movement.
                if((int)typeof(LogTruckSpill).GetField("phase",Private).GetValue(spill)==2)
                    foreach(float lane in spill.logLanes)obstacles.Add(new Vector2(lane,spill.hitRadius+.2f));
                foreach(var log in (IEnumerable)Logs.GetValue(spill))
                {var t=(Transform)log.GetType().GetField("t").GetValue(log);float d=F(log,"d");if(t!=null&&t.gameObject.activeInHierarchy&&d-route.Distance> -3&&d-route.Distance<38)obstacles.Add(new Vector2(F(log,"lane"),spill.hitRadius+.2f));}
                var truck=(Transform)typeof(LogTruckSpill).GetField("truck",Private).GetValue(spill);if(truck!=null){float d=(float)typeof(LogTruckSpill).GetField("truckD",Private).GetValue(spill);if(d-route.Distance> -6&&d-route.Distance<40)obstacles.Add(new Vector2(spill.truckLane,2.2f));}
            }
        }
        // Predict the toll arm state on arrival; do not steer toward the gate that is about to close.
        var gate=gates.Where(g=>g!=null&&g.isActiveAndEnabled).Select(g=>new{g,d=route.NearestDistance(g.transform.position)}).Where(x=>x.d-route.Distance>0&&x.d-route.Distance<36).OrderBy(x=>x.d).FirstOrDefault();
        if(gate!=null&&!route.OnBypass)
        {float at=chapter.Elapsed+(gate.d-route.Distance)/Mathf.Max(1,player.originalMoveSpeed);int open=HighwayHazard.OpenLane(at,gate.g.cycleSeconds);desired=traffic.lanes[open];}
        float Cost(float lane)
        {
            float actual=route.ConstrainEncounterLane(lane);float cost=Mathf.Abs(actual-desired)*.4f+Mathf.Abs(actual-current)*.08f;
            if(selecting!=null&&Mathf.Sign(actual)!=Mathf.Sign(desired))cost+=40;
            foreach(var obstacle in obstacles){float overlap=obstacle.y-Mathf.Abs(actual-obstacle.x);if(overlap>0)cost+=100+overlap*20;}
            return cost;
        }
        desired=Enumerable.Range(0,45).Select(i=>-4.4f+i*.2f).OrderBy(Cost).First();float error=desired-current;
        if(Mathf.Abs(error)>.04f){Move.Invoke(player,new object[]{Mathf.Sign(error)*Mathf.Min(15,Mathf.Abs(error)*12)});moves++;}
    }
    public static object Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        var result=ChapterPlaytestPreferences.RestoreAt(Root+"/before-prefs.tsv");foreach(var line in File.ReadAllLines(Root+"/extra-prefs.tsv")){var p=line.Split('\t');if(bool.Parse(p[1]))PlayerPrefs.SetInt(p[0],int.Parse(p[2]));else PlayerPrefs.DeleteKey(p[0]);}PlayerPrefs.Save();
        SelectStartStage(int.Parse(File.ReadAllText(Root+"/start-stage-before.txt")));
        var errors=new List<string>();foreach(var line in File.ReadAllLines(Root+"/before-prefs.tsv")){var p=line.Split('\t');string v=p[1]=="int"?PlayerPrefs.GetInt(p[0]).ToString(CultureInfo.InvariantCulture):p[1]=="float"?PlayerPrefs.GetFloat(p[0]).ToString("R",CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(p[0])));if(PlayerPrefs.HasKey(p[0])!=bool.Parse(p[2])||bool.Parse(p[2])&&v!=p[3])errors.Add(p[0]);}
        foreach(var line in File.ReadAllLines(Root+"/extra-prefs.tsv")){var p=line.Split('\t');if(PlayerPrefs.HasKey(p[0])!=bool.Parse(p[1])||bool.Parse(p[1])&&PlayerPrefs.GetInt(p[0])!=int.Parse(p[2]))errors.Add(p[0]);}
        Write(Root+"/restored.json",new{result,mismatches=errors,scene=SceneManager.GetActiveScene().name,dirty=SceneManager.GetActiveScene().isDirty});return new{result,mismatches=errors};
    }
}
