using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

// Observation-only QA: real PlayerMove input, ordinary stats/collisions/rewards.
// No health override, teleport, enemy disabling or forced pickup. Speed is recorded.
public static class TenRunReview
{
    static PlayerScript player;static CanvasScript canvas;static ChapterProgression chapter;static HighwayRoute highway;static HighwayOncomingTraffic traffic;
    static EnemyScript_space[] enemies;static BonusWallChoicePair[] pairs;static ObstacleStats[] hazards;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly MethodInfo Move=typeof(PlayerScript).GetMethod("PlayerMove",Private);
    static readonly FieldInfo Origin=typeof(PlayerScript).GetField("routeLaneOrigin",Private),Right=typeof(PlayerScript).GetField("routeRight",Private);
    static readonly FieldInfo LaneRange=typeof(PlayerScript).GetField("xRange",Private);
    static readonly FieldInfo CrossingStart=typeof(HighwayHazard).GetField("startPosition",Private),CrossingActivated=typeof(HighwayHazard).GetField("activatedAt",Private);
    static string folder,policy,scene;static float began,lastHP,lastAttack,nextShot,nextSample,distance,limit,pausedSince,startedAt;
    static int shots,hits,frame,moves;static Vector3 previous;static bool finished,started,occlusionCaptured;
    static readonly List<object> events=new();static readonly HashSet<int> selected=new();
    static readonly HashSet<string> encountered=new();
    static double readyAt,finishAt;static string outcome;
    static float testSpeed=1,endedAt;static string profile="user";
    static float captureInterval=4;static int initialCoins;
    static int fixedGameFps;
    static int renderFrameCap;
    static float nextSteerTrace;
    static EnemyScript_space[][] formations;
    static readonly Dictionary<string,EnemyScript_space> committedTargets=new();
    static NoryangjinCameraOcclusion occlusion;static Renderer[] curtains;static bool fadeCaptured;
    static float editorPausedAt=-1;
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});

    public static string Main(int run,string strategy,float maxSeconds=360,string outputRoot="tmp/image-previews/ten-run-review-2026-09-22",float speed=1,string entryProfile="user",int seed=0,float screenshotInterval=4,int gameFps=0,int renderCap=0)
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new InvalidOperationException("Fresh Play Mode lobby required.");
        folder=outputRoot+"/run-"+run.ToString("D2");testSpeed=speed;profile=entryProfile;endedAt=0;editorPausedAt=-1;
        captureInterval=screenshotInterval;initialCoins=MoneyScript.S.Coin;
        fixedGameFps=gameFps;
        renderFrameCap=renderCap;
        nextSteerTrace=0;
        if(seed!=0){UnityEngine.Random.InitState(seed);Object.FindFirstObjectByType<EncounterPlacementController>()?.BeginNewRun();}
        if(Directory.Exists(folder))throw new IOException("Preserve existing run evidence.");Directory.CreateDirectory(folder);
        player=Object.FindFirstObjectByType<PlayerScript>();canvas=Object.FindFirstObjectByType<CanvasScript>();chapter=Object.FindFirstObjectByType<ChapterProgression>();
        highway=Object.FindFirstObjectByType<HighwayRoute>();traffic=Object.FindFirstObjectByType<HighwayOncomingTraffic>();
        occlusion=Camera.main.GetComponent<NoryangjinCameraOcclusion>();fadeCaptured=false;
        curtains=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name.Contains("Harbor_lane_signal_gantry")||r.transform.parent!=null&&r.transform.parent.name=="Korean_Direction_Sign").ToArray();
        enemies=Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var holdout=Object.FindFirstObjectByType<RestStopHoldout>();
        var police=new HashSet<EnemyScript_space>(holdout!=null?holdout.police.Select(e=>e.GetComponent<EnemyScript_space>()):Array.Empty<EnemyScript_space>());
        formations=enemies.Where(e=>!e.name.EndsWith("_Right")&&!police.Contains(e)).Select(e=>new[]{e,enemies.FirstOrDefault(r=>r.name==e.name+"_Right")}).ToArray();committedTargets.Clear();
        pairs=Object.FindObjectsByType<BonusWallChoicePair>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        hazards=Object.FindObjectsByType<ObstacleStats>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        scene=player.gameObject.scene.name;policy=strategy;limit=maxSeconds;events.Clear();selected.Clear();encountered.Clear();
        shots=hits=moves=0;frame=-1;distance=0;finished=started=occlusionCaptured=false;nextSample=nextShot=0;pausedSince=-1;
        Time.timeScale=testSpeed;Time.captureDeltaTime=gameFps>0?1f/(gameFps*testSpeed):0;readyAt=EditorApplication.timeSinceStartup+2;
        OpeningStoryUI.Instance?.Skip();
        File.WriteAllText(folder+"/timeline.jsonl","");
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.pauseStateChanged+=PauseChanged;
        return $"Run {run}: {scene}, {strategy}; {profile}, {testSpeed}x, real movement. {folder}";
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||player==null){EditorApplication.update-=Tick;EditorApplication.pauseStateChanged-=PauseChanged;return;}
        if(EditorApplication.isPaused){if(editorPausedAt<0)editorPausedAt=Time.realtimeSinceStartup;return;}
        if(editorPausedAt>=0){startedAt+=Time.realtimeSinceStartup-editorPausedAt;editorPausedAt=-1;}
        if(frame==Time.frameCount)return;frame=Time.frameCount;
        // QA wall-clock acceleration only. Captured game delta and ordinary
        // FixedUpdate input/physics stay unchanged; no player build setting is edited.
        if(renderFrameCap>0)Application.targetFrameRate=renderFrameCap;
        try
        {
            if(finished)
            {
                if(EditorApplication.timeSinceStartup>=finishAt){Capture("result");WriteSummary();EditorApplication.update-=Tick;EditorApplication.pauseStateChanged-=PauseChanged;EditorApplication.isPaused=true;}
                return;
            }
            if(!started)
            {
                if(EditorApplication.timeSinceStartup<readyAt)return;
                Capture("lobby");canvas.PlayerPressedStartButton();
                if(!TimeManager.isGameRunning){readyAt=EditorApplication.timeSinceStartup+2;return;}
                started=true;began=Time.time;startedAt=Time.realtimeSinceStartup;previous=player.transform.position;lastHP=player.currentHealth;lastAttack=player.currentDamage;
                events.Add(new{kind="start",health=lastHP,attack=lastAttack,speed=player.ForwardMoveSpeed,policy,scene,profile,testSpeed,levels=new[]{1,2,3}.Select(i=>PlayerPrefs.GetInt("upgrade_lv_"+i)).ToArray(),statOverrides=false});
                File.WriteAllText(folder+"/start.json",Json(events[0]));
                canvas.StartCoroutine(Drive());return;
            }
            float elapsed=Time.time-began;Vector3 p=player.transform.position;
            if(!fadeCaptured&&occlusion!=null&&curtains.Any(r=>r!=null&&occlusion.SceneryOpacity(r)<=.41f)){Capture("scenery-fade");fadeCaptured=true;}
            if(!occlusionCaptured&&elapsed>=8)
            {
                occlusionCaptured=true;var camera=Camera.main;var ray=camera.ViewportPointToRay(new Vector3(.5f,.3f,0));
                var candidates=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.forceRenderingOff&&r.isVisible&&r.bounds.IntersectRay(ray,out var d)&&d<Vector3.Distance(camera.transform.position,p)+5)
                    .Select(r=>new{r.name,parent=r.transform.parent?.name,center=r.bounds.center.ToString(),size=r.bounds.size.ToString(),material=r.sharedMaterial?.name}).ToArray();
                File.WriteAllText(folder+"/foreground-candidates.json",Json(candidates));
            }
            distance+=Vector3.ProjectOnPlane(p-previous,Vector3.up).magnitude;previous=p;
            if(player.currentHealth<lastHP-.01f)
            {
                hits++;var near=enemies.Where(e=>e!=null&&e.gameObject.activeInHierarchy).OrderBy(e=>(e.transform.position-p).sqrMagnitude).Take(2).Select(e=>new{e.name,hp=e.CurrentHealth,range=Vector3.Distance(e.transform.position,p)}).ToArray();
                events.Add(new{kind="damage",t=elapsed,before=lastHP,after=player.currentHealth,cause=player.LastDamageCause.ToString(),near});Capture("hit-"+hits.ToString("D2"));
            }
            if(player.currentDamage!=lastAttack)events.Add(new{kind="attack",t=elapsed,before=lastAttack,after=player.currentDamage});
            lastHP=player.currentHealth;lastAttack=player.currentDamage;
            foreach(var pair in pairs)if(pair!=null&&pair.Selected!=null&&selected.Add(pair.GetInstanceID()))
            {events.Add(new{kind="bonus",t=elapsed,pair=pair.name,choice=pair.Selected.RolledStat,label=pair.Selected.Wall.CurrentBonusDisplayText,hp=lastHP,attack=lastAttack});Capture("bonus-"+selected.Count.ToString("D2"));}
            if(elapsed>=nextShot){nextShot=elapsed+captureInterval;Capture("route");}
            if(elapsed>=nextSample)
            {
                nextSample=elapsed+.5f;
                var nearby=enemies.Where(e=>e!=null&&e.gameObject.activeInHierarchy&&e.CurrentHealth>0&&Vector3.Distance(e.transform.position,p)<22).ToArray();foreach(var e in nearby)encountered.Add(e.name);
                File.AppendAllText(folder+"/timeline.jsonl",Json(new{t=elapsed,hp=lastHP,attack=lastAttack,distance,position=new[]{p.x,p.y,p.z},running=TimeManager.isGameRunning,near=nearby.Select(e=>new{e.name,hp=e.CurrentHealth,pos=e.transform.position.ToString()}).ToArray(),bonuses=selected.Count})+"\n");
                File.WriteAllText(folder+"/status.json",Json(new{scene,policy,t=elapsed,hp=lastHP,attack=lastAttack,distance,hits,bonuses=selected.Count,finished}));
            }
            if(player.currentHealth<=0||CanvasScript.isGameOver||chapter!=null&&chapter.Completed){Finish(player.currentHealth<=0?"death":chapter!=null&&chapter.Completed?"clear":"game-over-without-clear");return;}
            if(elapsed>=limit){Finish("time-limit");return;}
            if(!TimeManager.isGameRunning)
            {if(pausedSince<0){pausedSince=Time.realtimeSinceStartup;Capture("paused-dialog");}else if(Time.realtimeSinceStartup-pausedSince>15)Finish("blocked-dialog");}
            else pausedSince=-1;
            if(Time.realtimeSinceStartup-startedAt>limit+60)Finish("wall-clock-limit");
        }
        catch(Exception error){File.WriteAllText(folder+"/error.txt",error.ToString());EditorApplication.update-=Tick;EditorApplication.pauseStateChanged-=PauseChanged;EditorApplication.isPaused=true;}
    }
    static void PauseChanged(PauseState state)
    {
        if(!finished)File.AppendAllText(folder+"/editor-pause.log",DateTime.UtcNow.ToString("O")+" "+state+" at gameTime="+(Time.time-began)+"\n");
    }
    static IEnumerator Drive()
    {
        while(!finished&&player!=null)
        {
            yield return new WaitForFixedUpdate();
            if(!TimeManager.isGameRunning||player.currentHealth<=0||player.IsWorldYawTurnActive||player.IsStationaryCombat)continue;
            var p=player.transform.position;var origin=(Vector3)Origin.GetValue(player);var right=(Vector3)Right.GetValue(player);var forward=Vector3.ProjectOnPlane(player.transform.forward,Vector3.up).normalized;
            var range=(Vector2)LaneRange.GetValue(player);float safeLeft=range.x< -3?range.x+.25f:-2.3f,safeRight=range.y>3?range.y-.25f:2.3f;
            float desired=policy=="main"?2:policy=="bypass"?-2:0,nearest=23;
            // Aim using the same lateral controls, only at nearby route-facing actors.
            foreach(var enemy in enemies)
            {
                if(policy=="campaign")break;
                if(enemy==null||!enemy.gameObject.activeInHierarchy||enemy.CurrentHealth<=0)continue;
                Vector3 delta=enemy.transform.position-p;float ahead=Vector3.Dot(delta,forward);
                if(ahead<0||ahead>nearest||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>5)continue;
                nearest=ahead;desired=TargetLane(enemy.transform.position,origin,right);
                if(policy!="left"&&policy!="attack"&&policy!="campaign"&&ahead<(range.y>3?8:5))desired=desired>=0?safeLeft:safeRight;
            }
            if(policy=="campaign")foreach(var row in formations)
            {
                var first=row[0];if(first==null)continue;
                var center=row[1]!=null?(first.transform.position+row[1].transform.position)*.5f:first.transform.position;
                var delta=center-p;float ahead=Vector3.Dot(delta,forward);
                if(ahead< -3||ahead>nearest||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>5)continue;
                if(!committedTargets.TryGetValue(first.name,out var target))
                {
                    target=row.Where(e=>e!=null&&!EncounterPlacementController.IsExplicitlyDisabled(e)).OrderBy(e=>e.CurrentHealth).ThenBy(e=>Mathf.Abs(Vector3.Dot(e.transform.position-p,right))).FirstOrDefault();
                    if(target==null)continue;
                    committedTargets[first.name]=target;
                }
                // Keep the cleared opening; do not swerve into the second opponent.
                nearest=ahead;desired=TargetLane(target.transform.position,origin,right);
            }
            foreach(var pair in pairs)
            {
                if(pair==null||pair.Selected!=null||pair.Left==null||!pair.Left.gameObject.activeInHierarchy)continue;
                Vector3 delta=(pair.Left.transform.position+pair.Right.transform.position)*.5f-p;float ahead=Vector3.Dot(delta,forward);
                if(ahead<0||ahead>Mathf.Min(nearest,19)||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>5)continue;
                var choice=Score(pair.Left)>=Score(pair.Right)?pair.Left:pair.Right;
                if(policy=="left")choice=pair.Left;if(policy=="late")choice=(selected.Count%2==0)?pair.Right:pair.Left;
                nearest=ahead;desired=TargetLane(choice.transform.position,origin,right);
            }
            if(policy!="left")foreach(var hazard in hazards)
            {
                if(hazard==null||!hazard.gameObject.activeInHierarchy||hazard.GetComponent<HighwayHazard>()!=null)continue;
                var delta=hazard.transform.position-p;float ahead=Vector3.Dot(delta,forward);
                if(ahead< -1||ahead>Mathf.Min(nearest,policy=="late"?7:16)||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>5)continue;
                string type=hazard.obstaclePattern.ToString();if(type.Contains("Ship"))continue;
                nearest=ahead;float lane=TargetLane(hazard.transform.position,origin,right);desired=lane>=0?safeLeft:safeRight;
            }
            if(policy!="left"&&policy!="attack")foreach(var shot in Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None))
            {
                if(!shot.TryGetComponent<Rigidbody>(out var body)||body.linearVelocity.sqrMagnitude<1)continue;
                var delta=shot.transform.position-p;float ahead=Vector3.Dot(delta,forward);
                if(ahead<0||ahead>(policy=="late"?6:13)||Mathf.Abs(delta.y)>3||Mathf.Abs(Vector3.Dot(delta,right))>4)continue;
                float lane=Vector3.Dot(shot.transform.position-origin,right);
                if(scene=="RestStop"&&policy=="campaign")
                {
                    // Do not abandon a nearly cleared opening for the stronger
                    // partner at the last moment. Use a short sidestep earlier.
                    if(nearest<=7)continue;
                    var collider=shot.GetComponent<Collider>();var ext=collider!=null?collider.bounds.extents:Vector3.one*.3f;
                    float clearance=Mathf.Abs(right.x)*ext.x+Mathf.Abs(right.z)*ext.z+.85f;
                    if(Mathf.Abs(lane-desired)>=clearance)continue;
                    var candidates=new[]{Mathf.Clamp(lane-clearance,range.x,range.y),Mathf.Clamp(lane+clearance,range.x,range.y)};
                    desired=candidates.Where(c=>Mathf.Abs(c-lane)>=clearance-.05f).OrderBy(c=>Mathf.Abs(c-desired)+Mathf.Abs(c)*.05f).DefaultIfEmpty(desired).First();
                }
                else if(Mathf.Abs(lane-desired)<1.8f)desired=lane>=0?safeLeft:safeRight;
            }
            if(highway!=null&&(policy=="main"||policy=="bypass")&&highway.forks.Any(f=>!f.decided&&highway.Distance>=f.start-25))desired=policy=="bypass"?-3.2f:3.2f;
            if(traffic!=null&&highway!=null&&!highway.OnBypass)
            {
                var lanes=traffic.warnings.Where(w=>w!=null&&w.gameObject.activeInHierarchy&&w.enabled&&w.positionCount>0).Select(w=>TargetLane(w.GetPosition(0),origin,right)).ToArray();
                if(lanes.Length>0)desired=new[]{-3.2f,0f,3.2f}.OrderByDescending(l=>lanes.Min(x=>Mathf.Abs(x-l))).First();
            }
            // Predict a moving car or gate at arrival, rather than changing sides
            // whenever its current position crosses the middle of the road.
            desired=Mathf.Clamp(desired,range.x,range.y);
            foreach(var hazard in hazards.Where(h=>h!=null&&h.gameObject.activeInHierarchy)
                .OrderByDescending(h=>Vector3.Dot(h.transform.position-p,forward)))
            {
                var obstacle=hazard.GetComponent<HighwayHazard>();if(obstacle==null||obstacle.Broken)continue;
                var delta=hazard.transform.position-p;float ahead=Vector3.Dot(delta,forward);
                if(ahead< -2||ahead>32||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>Mathf.Max(Mathf.Abs(range.x),Mathf.Abs(range.y))+3)continue;
                float arrival=Mathf.Max(0,ahead)/Mathf.Max(1,player.ForwardMoveSpeed);
                if(hazard.obstaclePattern==ObstaclePattern.HighwayToll)
                {
                    int open=HighwayHazard.OpenLane(chapter.Elapsed+arrival,obstacle.cycleSeconds);
                    if(obstacle.laneIndex==open)desired=TargetLane(hazard.transform.position,origin,right);
                    continue;
                }
                var collider=hazard.GetComponent<Collider>();if(collider==null||!collider.enabled)continue;
                Vector3 projected=hazard.transform.position;
                if(hazard.obstaclePattern==ObstaclePattern.HighwayTraffic)
                {
                    float activated=(float)CrossingActivated.GetValue(obstacle);
                    if(activated>=0)
                    {
                        float progress=Mathf.Clamp01((chapter.Elapsed+arrival-activated-obstacle.warningSeconds)/Mathf.Max(1,obstacle.cycleSeconds));
                        projected=(Vector3)CrossingStart.GetValue(obstacle)+obstacle.transform.right*(obstacle.crossingDistance*progress);
                    }
                }
                float lane=TargetLane(projected,origin,right);
                var ext=collider.bounds.extents;float halfWidth=Mathf.Abs(right.x)*ext.x+Mathf.Abs(right.z)*ext.z;
                if(Mathf.Abs(lane-desired)<halfWidth+.7f)desired=lane>=0?safeLeft:safeRight;
            }
            desired=Mathf.Clamp(desired,range.x,range.y);
            float error=desired-Vector3.Dot(p-origin,right);
            if(scene=="RestStop"&&chapter.Elapsed>=175&&chapter.Elapsed<=195&&Time.time>=nextSteerTrace)
            {
                nextSteerTrace=Time.time+.1f;
                File.AppendAllText(folder+"/traffic-steering.jsonl",Json(new{t=chapter.Elapsed,desired,current=Vector3.Dot(p-origin,right),position=p.ToString(),origin=origin.ToString(),right=right.ToString(),player.movement,player.IsWorldYawTurnActive,
                    cars=hazards.Where(h=>h!=null&&h.gameObject.activeInHierarchy&&h.obstaclePattern==ObstaclePattern.HighwayTraffic&&Vector3.Distance(h.transform.position,p)<45).Select(h=>{var car=h.GetComponent<HighwayHazard>();return new{h.name,position=h.transform.position.ToString(),ahead=Vector3.Dot(h.transform.position-p,forward),lane=TargetLane(h.transform.position,origin,right),activated=(float)CrossingActivated.GetValue(car),start=CrossingStart.GetValue(car).ToString(),car.warningSeconds,car.cycleSeconds,car.crossingDistance};}).ToArray()})+"\n");
            }
            if(Mathf.Abs(error)>.06f){Move.Invoke(player,new object[]{Mathf.Clamp(error*12,-15,15)});moves++;}
        }
    }
    static float TargetLane(Vector3 position,Vector3 origin,Vector3 right)
    {
        if(highway==null)return Vector3.Dot(position-origin,right);
        highway.Sample(highway.NearestDistance(position),false,out var center,out var forward);
        return Vector3.Dot(position-center,Vector3.Cross(Vector3.up,forward));
    }
    static float Score(AuthoredBonusWall altar)
    {
        string key=(altar.RolledStat??"").ToLowerInvariant();
        if(policy=="cautious"||policy=="campaign")return key.Contains("attpercent")?20:key.Contains("attackspeed")?15:key.Contains("boom")?20:key.Contains("hppercent")?12:key.Contains("missile")?10:3;
        if(policy=="health"||policy=="bypass")return key.Contains("hp")?10:key.Contains("att")?5:3;
        if(policy=="helper")return key.Contains("tung")||key.Contains("boom")?15:key.Contains("att")?7:3;
        return key.Contains("att")?10:key.Contains("missile")?7:3;
    }
    static void Capture(string label)
    {
        string path=folder+"/"+(shots++).ToString("D3")+"-"+(started?(Time.time-began).ToString("000.0"):"lobby")+"-"+label+".png";
        ScreenCapture.CaptureScreenshot(path);
    }
    static void Finish(string reason)
    {
        // The game's win panel appears after two real seconds. Preserve its
        // actual rendered result, not the empty interval before presentation.
        outcome=reason;endedAt=Time.time-began;finished=true;finishAt=EditorApplication.timeSinceStartup+2.6;
        File.WriteAllText(folder+"/run-verification.json",Json(new{chapter=chapter!=null?chapter.chapter:0,completed=chapter!=null&&chapter.Completed,unlocked=PlayerPrefs.GetInt("chapter_unlocked",1),fixedGameFps,width=Screen.width,height=Screen.height,physicsStep=Time.fixedDeltaTime,damageNotifications=player.GetComponent<PlayerDamageFeedback>()?.ShownHitCount}));
        Capture("end-"+reason);WriteSummary();
    }
    static void WriteSummary()=>File.WriteAllText(folder+"/summary.json",Json(new{scene,policy,profile,testSpeed,outcome,seconds=endedAt,health=player.currentHealth,attack=player.currentDamage,initialCoins,bank=MoneyScript.S.Coin,earnedCoins=MoneyScript.S.Coin-initialCoins,distance,hits,bonuses=selected.Count,encounters=encountered.Count,movementInputs=moves,shots,fatalCause=outcome=="death"?player.LastDamageCause.ToString():null,forks=highway?.forks.Select(f=>new{f.start,f.decided,f.bypass}).ToArray(),events}));
}
