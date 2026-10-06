using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Actual ordinary-stat gameplay. Reflection invokes the same PlayerMove method
// as touch/keyboard; it never writes position, health, damage or enemy state.
public static class Chapters45Validation12
{
    const string Session = "Chapter45.Validation12.";
    const string Selector = "NoryangjinMapTool.TestStartStage";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Move = typeof(PlayerScript).GetMethod("PlayerMove", Private);
    static readonly FieldInfo Origin = typeof(PlayerScript).GetField("routeLaneOrigin", Private);
    static readonly FieldInfo Right = typeof(PlayerScript).GetField("routeRight", Private);
    static readonly FieldInfo Range = typeof(PlayerScript).GetField("xRange", Private);
    static readonly FieldInfo ShotDirection = typeof(BulletScript).GetField("direction", Private);
    static readonly FieldInfo ShotDeck = typeof(BulletScript).GetField("launchDeck", Private);
    static readonly FieldInfo ShotHeight = typeof(BulletScript).GetField("launchFloorHeight", Private);
    static readonly FieldInfo ShotRetired = typeof(BulletScript).GetField("chapterRetired", Private);
    static readonly FieldInfo RideElapsed = typeof(Chapter45Director).GetField("rideElapsed", Private);
    static readonly FieldInfo RideDuration = typeof(Chapter45Director).GetField("rideDuration", Private);
    static WeaponScript[] playerWeapons;
    static PlayerScript player;
    static Chapter45Director director;
    static ChapterProgression chapter;
    static CanvasScript canvas;
    static string folder, scene;
    static int routeChoice, moves, shots, frame, timelineCursor, initialCoins, initialJewels;
    static float nextSample, nextShot, initialHealth, initialAttack, lastHealth, lastAttack, began;
    static double readyAt, wallStarted, endedAt;
    static bool active, started, finished;
    static string outcome;
    static readonly List<object> damage = new();
    static readonly List<string> images = new();
    static readonly List<string> errors = new();
    static readonly List<string> animationWarnings = new();
    static readonly Dictionary<int, float> openingLanes = new();
    static readonly Dictionary<int, EnemyScript_space> enemyCommitments = new();
    static readonly Dictionary<int, float> avoidanceLanes = new();
    static string inputPolicy;
    static float desiredLane;
    static readonly HashSet<int> liftMidCaptured = new();
    static readonly HashSet<int> liftTurnCaptured = new();
    static string hudObserved, hudRecorded;
    static float hudChangedAt;
    static int hudChanges, hudCaptures;

    [Serializable] sealed class HudState { public bool present,panelVisible,titleVisible,detailVisible;public string title,detail; }
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    static void Write(string file,object value)=>File.WriteAllText(Path.Combine(folder,file),Json(value));
    static object StartSuiteRoute(string routeFolder,string routeScene,int choice)
    {
        folder=routeFolder;scene=routeScene;routeChoice=choice;Directory.CreateDirectory(folder);
        Write("conditions.json",new{singlePlaySession=true,statOverrides=false,timeScale=1,scene,routeChoice,teleport=false,forcedWin=false,capturePolicy="Limited native start/end and close actor samples",qaSceneSelectionAfterChapter5=true});
        player = Object.FindFirstObjectByType<PlayerScript>(); director = Object.FindFirstObjectByType<Chapter45Director>();
        chapter = Object.FindFirstObjectByType<ChapterProgression>(); canvas = Object.FindFirstObjectByType<CanvasScript>();
        var overlay = Object.FindFirstObjectByType<CombatHarness>();
        if (overlay != null) typeof(CombatHarness).GetField("overlayVisible", Private)?.SetValue(overlay, false);
        if (player == null || director == null || chapter == null || canvas == null || TimeManager.isGameRunning) throw new InvalidOperationException("Fresh chapter lobby and all runtime components required.");
        playerWeapons = player.GetComponentsInChildren<WeaponScript>(true);
        active = true; started = finished = false; outcome = "running"; frame = -1; moves = shots = timelineCursor = 0;
        damage.Clear(); images.Clear(); errors.Clear(); animationWarnings.Clear(); openingLanes.Clear(); enemyCommitments.Clear(); avoidanceLanes.Clear();
        liftMidCaptured.Clear(); liftTurnCaptured.Clear(); hudObserved = hudRecorded = null; hudChangedAt = 0; hudChanges = hudCaptures = 0;
        inputPolicy = "starting"; desiredLane = 0;
        nextSample = nextShot = 0; readyAt = EditorApplication.timeSinceStartup + 1.5; wallStarted = EditorApplication.timeSinceStartup;
        Time.timeScale = 1; Time.captureDeltaTime = 0;
        if (Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(o => o.gameObject.activeInHierarchy)) throw new InvalidOperationException("Saved chapter opening root must be inactive; QA does not skip it.");
        File.WriteAllText(Path.Combine(folder, "timeline.jsonl"), ""); File.WriteAllText(Path.Combine(folder, "director-timeline.txt"), "");
        Application.logMessageReceived += OnLog; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Write("status.json", new { prepared = true, active, started, finished, scene, routeChoice, folder });
        return new { active, scene, routeChoice, folder };
    }
    static void OnLog(string message, string stack, LogType kind)
    {
        if (kind == LogType.Warning && (message.IndexOf("Animator", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("animation", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            animationWarnings.Add(message);
            File.AppendAllText(Path.Combine(folder, "animation-warnings.txt"), message + "\n" + stack + "\n");
        }
        if (kind != LogType.Error && kind != LogType.Exception && kind != LogType.Assert) return;
        errors.Add(kind + ": " + message);
        File.AppendAllText(Path.Combine(folder, "errors.txt"), kind + ": " + message + "\n" + stack + "\n");
    }
    static void Tick()
    {
        if (!active) return;
        if (!EditorApplication.isPlaying) { Detach(); return; }
        if (EditorApplication.isCompiling || EditorApplication.isPaused || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (finished)
            {
                ObserveHud();
                if (EditorApplication.timeSinceStartup - endedAt < 3) return;
                Capture("result-or-transition");
                Write("capture-status.json", new { requested = images.ToArray(), written = images.Where(File.Exists).ToArray(), width = Screen.width, height = Screen.height,
                    hudChanges, hudCaptures, hudCaptureLimit = 100, liftMidCaptured = liftMidCaptured.OrderBy(i => i).ToArray() });
                Detach(); return;
            }
            if (EditorApplication.timeSinceStartup - wallStarted >= 600) { Finish("wall-clock-limit"); return; }
            if (player == null || director == null) { Finish("unexpected-scene-unload"); return; }
            if (!started)
            {
                if (EditorApplication.timeSinceStartup < readyAt) return;
                canvas.PlayerPressedStartButton();
                if (!TimeManager.isGameRunning) { readyAt = EditorApplication.timeSinceStartup + 1; return; }
                if (Mathf.Abs(Time.timeScale - 1) > .001f || Mathf.Abs(TimeManager.timeFactor - 1) > .001f) throw new InvalidOperationException("Ordinary 1x run failed: unexpected time override.");
                started = true; began = Time.time; initialHealth = lastHealth = player.currentHealth; initialAttack = lastAttack = player.ResolvedAttackDamage;
                initialCoins = MoneyScript.S != null ? MoneyScript.S.Coin : 0; initialJewels = MoneyScript.S != null ? MoneyScript.S.Jewel : 0;
                Write("start.json", new { scene, routeChoice, health = initialHealth, maxHealth = player.MaxHealth, attack = initialAttack, speed = player.ForwardMoveSpeed, routeSpeed = director.RouteSpeed,
                    fireRate = player.DefaultFireRate, missileSpeed = BulletScript.BaseMissileSpeed, missileSeconds = BulletScript.CurrentMissileDuration,
                    coin = initialCoins, jewel = initialJewels, statOverrides = false, timeScale = Time.timeScale, timeFactor = TimeManager.timeFactor,
                    levels = Enumerable.Range(1, Enum.GetValues(typeof(UpgradeStatManager.UpgradeType)).Length).Select(i => new { id = i, level = PlayerPrefs.GetInt("upgrade_lv_" + i) }).ToArray() });
                Capture("start"); canvas.StartCoroutine(Drive());
            }
            float elapsed = Time.time - began;
            ObserveHud();
            while (timelineCursor < director.Timeline.Count)
            {
                string entry = director.Timeline[timelineCursor++]; File.AppendAllText(Path.Combine(folder, "director-timeline.txt"), entry + "\n");
                if (entry.Contains("lift board") || entry.Contains("lift land") || entry.Contains("choice committed") || entry.Contains("choice reward") || entry.Contains("captain phase") || entry.Contains("target destroyed") || entry.Contains("physically")) Capture("event-" + timelineCursor);
            }
            if (player.currentHealth < lastHealth - .01f)
            {
                damage.Add(new { t = elapsed, distance = director.Distance, floor = director.CurrentFloor, before = lastHealth, after = player.currentHealth, cause = player.LastDamageCause.ToString() });
                Capture("damage-" + damage.Count);
            }
            lastHealth = player.currentHealth; lastAttack = player.ResolvedAttackDamage;
            for (int index = 0; index < director.choices.Length; index++)
            {
                var choice = director.choices[index];
                if (!choice.Selected && director.Distance >= choice.distance - 26 && director.Distance < choice.distance - 20 && !images.Any(x => x.Contains("choice-preview-" + index))) Capture("choice-preview-" + index);
            }
            if (elapsed >= nextShot) { nextShot = elapsed + 35; Capture("route"); }
            if (director.IsTransferring && !liftMidCaptured.Contains(director.LiftCount))
            {
                float rideElapsed = (float)RideElapsed.GetValue(director), rideDuration = (float)RideDuration.GetValue(director);
                if (rideElapsed >= 3 && rideElapsed < rideDuration)
                {
                    liftMidCaptured.Add(director.LiftCount); Capture("lift-mid-" + director.LiftCount);
                    File.AppendAllText(Path.Combine(folder, "lift-captures.jsonl"), Json(new { liftIndex = director.LiftCount,
                        directorElapsed = director.Elapsed, rideElapsed, rideDuration, requestedRideElapsed = 3,
                        delayPastRequested = rideElapsed - 3, boardElapsed = director.Elapsed - rideElapsed }) + "\n");
                }
            }
            if (director.IsTransferring && !liftTurnCaptured.Contains(director.LiftCount))
            {
                float rideElapsed = (float)RideElapsed.GetValue(director), rideDuration = (float)RideDuration.GetValue(director);
                if (rideElapsed >= 5.1f && rideElapsed < rideDuration)
                {
                    liftTurnCaptured.Add(director.LiftCount); Capture("lift-turn-" + director.LiftCount);
                    File.AppendAllText(Path.Combine(folder, "lift-turn-captures.jsonl"), Json(new { liftIndex = director.LiftCount,
                        directorElapsed = director.Elapsed, rideElapsed, rideDuration, requestedRideElapsed = 5.1f,
                        playerForward = V(player.transform.forward), position = V(player.transform.position) }) + "\n");
                }
            }
            if (elapsed >= nextSample)
            {
                nextSample = elapsed + .5f;
                var state = new { scene, routeChoice, active, started, finished, outcome, seconds = elapsed, distance = director.Distance, length = director.route.Length,
                    floor = director.CurrentFloor, position = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z }, lane = director.Lane,
                    hp = player.currentHealth, attack = player.ResolvedAttackDamage, running = TimeManager.isGameRunning, lift = director.IsTransferring, lifts = director.LiftCount,
                    hud = ReadHud(),
                    choice = director.choices.Select(c => new { c.name, c.Selected, c.Selection, c.Rewarded }).ToArray(),
                    targets = director.targets.Where(t => t.floor == director.CurrentFloor && t.Eligible).Select(t => new { t.name, t.Health, t.Open, t.Phase, t.Shielded }).ToArray(),
                    activeEnemies = director.encounters.SelectMany(e => e.actors).Count(e => e != null && e.gameObject.activeInHierarchy && !e.IsDead),
                    playerForward = V(player.transform.forward),
                    weapons = playerWeapons.Where(w => w != null && w.gameObject.activeInHierarchy).Select(w => new { w.name, w.TotalProjectilesSpawned,
                        spawn = V(w.LastProjectileSpawnPosition), mouth = V(w.LastVisibleMouthPositionAtSpawn), w.fireRate, w.damage, w.bulletCount }).ToArray(),
                    audience = player.HoldoutAim == null ? null : new { player.HoldoutAim.AimYaw, player.HoldoutAim.Elapsed, player.HoldoutAim.Spawned, player.HoldoutAim.Killed, actors=player.HoldoutAim.police.Where(a=>a!=null && a.gameObject.activeInHierarchy).Select(a=>new{a.name,position=V(a.transform.position),hp=a.GetComponent<EnemyScript_space>().CurrentHealth,state=a.RuntimeState.ToString(),retiredOnContact=a.GetComponent<Chapter45AudienceContact>()?.RetiredOnContact ?? false}).ToArray() },
                    nearbyActors = director.encounters.SelectMany(e => e.actors).Where(e => e != null && e.gameObject.activeInHierarchy && Vector3.Distance(e.transform.position, player.transform.position) < 55).Select(ActorSample).ToArray(),
                    projectiles = Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled).Take(30).Select(b => new { b.name,
                        position = V(b.transform.position), rootPosition = V(b.transform.root.position), direction = V((Vector3)ShotDirection.GetValue(b)),
                        deck = (int)ShotDeck.GetValue(b), launchHeight = (float)ShotHeight.GetValue(b), retired = (bool)ShotRetired.GetValue(b), b.LaunchDamage, b.HasDamagePayload }).ToArray(),
                    moves, inputPolicy, desiredLane, errors = errors.Count, render = new { UnityStats.batches, UnityStats.setPassCalls, UnityStats.triangles, frameMs = Time.unscaledDeltaTime * 1000 } };
                File.AppendAllText(Path.Combine(folder, "timeline.jsonl"), Json(state) + "\n"); Write("status.json", state);
            }
            if (player.currentHealth <= 0) { Finish("death"); return; }
            if (chapter.Completed || director.GoalClaimed) { Finish("clear"); return; }
            if (CanvasScript.isGameOver) { Finish("game-over-without-clear"); return; }
            if (elapsed >= 600) Finish("game-time-limit");
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(folder, "harness-error.txt"), error.ToString()); Finish("harness-error"); }
    }
    // QA observation layer only. Decisions consume observations delayed by350ms;
    // no health, shot-budget, future spawn, hazard safeLane or private attack target.
    sealed class Seen { public float at,lane,ahead,width,pixels,propPixels;public int id;public string name,kind; }
    sealed class Observation { public float at; public Seen[] items; }
    static readonly Queue<Observation> observations=new();
    static float observeAt,decideAt,heldLane,heldYaw;
    static readonly HashSet<string> seenCaptures=new();
    static Camera GameCamera()=>Camera.main;
    static bool ScreenBounds(Renderer[] renderers,out Bounds bounds,out float pixels){
        bounds=default;pixels=0;var rr=renderers.Where(r=>r!=null&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rr.Length==0||GameCamera()==null)return false;bounds=rr[0].bounds;foreach(var r in rr.Skip(1))bounds.Encapsulate(r.bounds);
        var cam=GameCamera();float left=2,right=-1,low=2,high=-1;bool front=false;
        for(int i=0;i<8;i++){var p=cam.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(p.z<=0)continue;front=true;left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);low=Mathf.Min(low,p.y);high=Mathf.Max(high,p.y);}
        pixels=Mathf.Max(0,Mathf.Min(1,high)-Mathf.Max(0,low))*Screen.height;
        return front&&right>.02f&&left<.98f&&high>.02f&&low<.94f&&pixels>=12;
    }
    static Seen[] ObserveVisible(Vector3 origin,Vector3 right){
        var result=new List<Seen>();Vector3 forward=Vector3.ProjectOnPlane(player.transform.forward,Vector3.up).normalized;
        foreach(var encounter in director.encounters){
            if(!encounter.Activated||encounter.Complete||encounter.floor!=director.CurrentFloor||!encounter.Eligible)continue;
            foreach(var actor in encounter.actors){
                if(actor==null||!actor.gameObject.activeInHierarchy||actor.IsDead)continue;
                var animator=actor.GetComponentInChildren<Animator>();var mesh=animator!=null?animator.GetComponentsInChildren<Renderer>():actor.GetComponentsInChildren<Renderer>();
                if(!ScreenBounds(mesh,out var b,out float px))continue;
                float ahead=Vector3.Dot(actor.transform.position-player.transform.position,forward);if(ahead<0||ahead>32)continue;
                var role=actor.GetComponent<Chapter45RoleAction>();float pp=0;if(role?.heldProp!=null)ScreenBounds(role.heldProp.GetComponentsInChildren<Renderer>(),out var pb,out pp);
                result.Add(new Seen{at=director.Elapsed,id=actor.GetInstanceID(),name=actor.name,kind=role==null?"actor":role.role.ToString(),lane=Vector3.Dot(actor.transform.position-origin,right),ahead=ahead,width=b.size.x,pixels=px,propPixels=pp});
                if(role?.warningMarker!=null&&role.warningMarker.gameObject.activeInHierarchy&&ScreenBounds(role.warningMarker.GetComponentsInChildren<Renderer>(),out var wb,out var wp)){
                    var aheadMarker=Vector3.Dot(role.warningMarker.position-player.transform.position,forward);
                    if(aheadMarker>=-3&&aheadMarker<14)result.Add(new Seen{at=director.Elapsed,id=role.GetInstanceID(),name=role.name,kind="windup",lane=Vector3.Dot(wb.center-origin,right),ahead=aheadMarker,width=Mathf.Abs(right.x)*wb.size.x+Mathf.Abs(right.z)*wb.size.z,pixels=wp});
                }
            }
        }
        foreach(var target in director.targets){
            if(target.floor!=director.CurrentFloor||!target.Eligible||target.Open||target.hitCollider==null||!target.hitCollider.enabled)continue;
            // Presentation meshes can be children even when the legacy renderer list is empty.
            // Observe actual enabled geometry, excluding the health-label text.
            var panelMeshes=target.targetRenderers.Concat(target.GetComponentsInChildren<Renderer>()).Where(r=>r!=null&&r.GetComponent<TMPro.TMP_Text>()==null).Distinct().ToArray();
            if(!ScreenBounds(panelMeshes,out var bounds,out var px))continue;
            var position=target.hitCollider.bounds.center;float ahead=Vector3.Dot(position-player.transform.position,forward);if(ahead<0||ahead>32)continue;
            result.Add(new Seen{at=director.Elapsed,id=target.GetInstanceID(),name=target.name,kind="visible panel",lane=Vector3.Dot(position-origin,right),ahead=ahead,width=bounds.size.x,pixels=px});
        }
        foreach(var h in director.hazards){
            if(h.floor!=director.CurrentFloor||h.footprint==null||!h.footprint.activeInHierarchy)continue;
            if(ScreenBounds(h.footprint.GetComponentsInChildren<Renderer>(),out var b,out float px))result.Add(new Seen{at=director.Elapsed,id=h.GetInstanceID(),name=h.name,kind="hazard footprint",lane=Vector3.Dot(b.center-origin,right),ahead=Vector3.Dot(b.center-player.transform.position,forward),width=Mathf.Abs(right.x)*b.size.x+Mathf.Abs(right.z)*b.size.z,pixels=px});
        }
        if(player.IsStationaryCombat&&player.HoldoutAim!=null){
            foreach(var a in player.HoldoutAim.police){
                if(a==null||!a.gameObject.activeInHierarchy||a.RuntimeState==EnemyEventRuntimeState.Dead)continue;
                var animator=a.GetComponentInChildren<Animator>();if(!ScreenBounds(animator!=null?animator.GetComponentsInChildren<Renderer>():a.GetComponentsInChildren<Renderer>(),out var b,out var px))continue;
                var delta=a.transform.position-player.transform.position;result.Add(new Seen{at=director.Elapsed,id=a.GetInstanceID(),name=a.name,kind="audience",lane=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,ahead=delta.magnitude,width=b.size.x,pixels=px});
            }
        }
        return result.ToArray();
    }
    static IEnumerator Drive(){
        observations.Clear();seenCaptures.Clear();observeAt=decideAt=0;heldLane=0;heldYaw=0;
        File.WriteAllText(Path.Combine(folder,"reactive-policy.json"),Json(new{reactionSeconds=.35f,decisionPeriod=.1f,inputCap=15,actorViewDistance=32,visiblePixelMinimum=12,noHealthLookahead=true,noSafeLaneRead=true,noFutureSpawnRead=true,teleports=false,limits="Bounded reactive automated input, not human play or a fun certification. Renderer frustum projection is not an occlusion proof; inspect native screenshots."}));
        while(active&&!finished&&player!=null&&director!=null){
            yield return new WaitForFixedUpdate();
            if(!TimeManager.isGameRunning||director.IsTransferring||player.currentHealth<=0||player.IsWorldYawTurnActive){observations.Clear();continue;}
            var origin=(Vector3)Origin.GetValue(player);var right=(Vector3)Right.GetValue(player);var range=(Vector2)Range.GetValue(player);
            if(director.Elapsed>=observeAt){
                observeAt=director.Elapsed+.1f;var seen=ObserveVisible(origin,right);observations.Enqueue(new Observation{at=director.Elapsed,items=seen});
                File.AppendAllText(Path.Combine(folder,"visible-observations.jsonl"),Json(new{at=director.Elapsed,floor=director.CurrentFloor,distance=director.Distance,hp=player.currentHealth,lane=director.Lane,seen})+"\n");
                foreach(var item in seen.Where(x=>(x.kind=="PhoneShopper"&&x.ahead<15)||(x.kind=="audience"&&x.ahead<10)||x.kind=="windup")){
                    string key=item.kind+"-"+(int)(item.ahead/5);if(seenCaptures.Add(key))Capture("readability-"+item.kind+"-"+(int)item.ahead);
                }
            }
            // Preserve age even for empty observations by enqueuing a sentinel.
            Seen[] matured=null;float maturedAt=0;
            while(observations.Count>0){var first=observations.Peek();if(director.Elapsed-first.at<.35f)break;var value=observations.Dequeue();matured=value.items;maturedAt=value.at;}
            if(matured!=null&&director.Elapsed>=decideAt){
                decideAt=director.Elapsed+.1f;
                if(player.IsStationaryCombat){var target=matured.Where(x=>x.kind=="audience").OrderBy(x=>x.ahead).FirstOrDefault();if(target!=null)heldYaw=target.lane;inputPolicy="visible audience delayed aim";}
                else{
                    var threats=matured.Where(x=>x.kind=="windup"||x.kind=="hazard footprint").ToArray();
                    var target=matured.Where(x=>x.kind!="windup"&&x.kind!="hazard footprint"&&x.kind!="audience").OrderBy(x=>x.ahead).ThenBy(x=>Mathf.Abs(x.lane-director.Lane)).FirstOrDefault();
                    if(threats.Length>0){
                        float best=float.NegativeInfinity;
                        foreach(float candidate in new[]{range.x+.65f,range.x*.5f,0,range.y*.5f,range.y-.65f}){
                            float clearance=threats.Min(x=>Mathf.Abs(candidate-x.lane)-x.width*.5f);
                            float score=Mathf.Min(clearance,2.5f)-Mathf.Abs(candidate-director.Lane)*.10f;
                            if(score>best){best=score;heldLane=candidate;}
                        }
                        inputPolicy="delayed visible warning dodge";
                    }else if(target!=null){heldLane=target.lane;inputPolicy="delayed visible target aim";}
                    else{heldLane=0;inputPolicy="no visible target";}
                }
                File.AppendAllText(Path.Combine(folder,"reactive-input.jsonl"),Json(new{at=director.Elapsed,observationAt=maturedAt,actualDelay=director.Elapsed-maturedAt,heldLane,heldYaw,inputPolicy})+"\n");
            }
            if(player.IsStationaryCombat){var hold=player.HoldoutAim;if(hold!=null&&hold.Active)hold.RotateAim(Mathf.Clamp(Mathf.DeltaAngle(hold.AimYaw,heldYaw)/12f,-1,1),Time.fixedDeltaTime*TimeManager.timeFactor);continue;}
            // Choice text and final shoe are explicit navigation decisions, not combat prediction.
            foreach(var choice in director.choices)if(!choice.Selected&&choice.OnCurrentFloor&&director.Distance>=choice.distance-20&&director.Distance<=choice.distance&&(choice.kind!=Chapter45Choice.ChoiceKind.FloorRoute||director.CurrentFloorEncountersComplete))heldLane=routeChoice==0?-3:3;
            foreach(var goal in director.goals.Where(g=>g.floor==director.CurrentFloor&&!g.Claimed))if(Vector3.Distance(goal.transform.position,player.transform.position)<15)heldLane=Vector3.Dot(goal.transform.position-origin,right);
            desiredLane=Mathf.Clamp(heldLane,range.x,range.y);float error=desiredLane-Vector3.Dot(player.transform.position-origin,right);
            if(Mathf.Abs(error)>.15f){Move.Invoke(player,new object[]{Mathf.Clamp(error*6,-15,15)});moves++;}
        }
    }

    static float FindClearLane(Chapter45Encounter encounter, Vector3 origin, Vector3 right, Vector2 range)
    {
        float low = Chapter45Director.ConstrainLane(player, range.x + .6f);
        float high = Chapter45Director.ConstrainLane(player, range.y - .6f);
        var occupied = encounter.actors.Where(e => e != null && e.gameObject.activeInHierarchy && !e.IsDead)
            .Select(e => Vector3.Dot(e.transform.position - origin, right)).OrderBy(x => x).ToArray();
        var candidates = new List<float> { Mathf.Clamp(0, low, high), low, high };
        for (int i = 1; i < occupied.Length; i++) candidates.Add(Mathf.Clamp((occupied[i - 1] + occupied[i]) * .5f, low, high));
        float best = candidates[0], score = float.NegativeInfinity;
        foreach (float lane in candidates)
        {
            float clearance = occupied.Length == 0 ? 100 : occupied.Min(x => Mathf.Abs(x - lane));
            // Prefer generous clearance; equal gaps favor less steering.
            float candidateScore = clearance - Mathf.Abs(lane - director.Lane) * .025f;
            if (candidateScore > score) { best = lane; score = candidateScore; }
        }
        return best;
    }
    static HudState ReadHud()
    {
        var hud = director != null ? director.hud : null;
        bool TextVisible(TMPro.TMP_Text text) => text != null && text.isActiveAndEnabled
            && text.gameObject.activeInHierarchy && text.color.a > .01f && text.canvasRenderer.GetAlpha() > .01f && !text.canvasRenderer.cull;
        return new HudState { present = hud != null,
            panelVisible = hud != null && hud.panel != null && hud.panel.activeInHierarchy,
            title = hud != null && hud.title != null ? hud.title.text : null,
            detail = hud != null && hud.description != null ? hud.description.text : null,
            titleVisible = hud != null && TextVisible(hud.title), detailVisible = hud != null && TextVisible(hud.description) };
    }
    static void ObserveHud()
    {
        var hud = ReadHud(); string signature = Json(hud);
        if (signature != hudObserved) { hudObserved = signature; hudChangedAt = Time.unscaledTime; return; }
        if (signature == hudRecorded || Time.unscaledTime - hudChangedAt < .1f) return;
        hudRecorded = signature; hudChanges++;
        // One screenshot per stable state change; no .5s sampling duplicates.
        // Cap screenshots if a product bug causes repeated flicker, retaining all
        // stable changes in the timeline and explicitly reporting suppression.
        bool capture = hudCaptures < 100;
        File.AppendAllText(Path.Combine(folder, "hud-timeline.jsonl"), Json(new { change = hudChanges,
            seconds = started ? Time.time - began : 0, directorElapsed = director != null ? director.Elapsed : -1,
            distance = director != null ? director.Distance : -1, hud, measurement=MeasureHud(), captureRequested = capture,
            observation = "Native active/enabled/alpha/culling flags; screenshot is separate visual evidence." }) + "\n");
        if (capture) { hudCaptures++; Capture("hud-change-" + hudChanges); }
    }
    static object MeasureHud(){if(director==null||director.hud==null)return null;return new[]{director.hud.title,director.hud.description}.Where(t=>t!=null&&t.gameObject.activeInHierarchy).Select(t=>{t.ForceMeshUpdate();var ch=t.textInfo.characterInfo.Take(t.textInfo.characterCount).Where(c=>c.isVisible).ToArray();var r=t.rectTransform.rect;return new{text=t.text,overflow=t.isTextOverflowing,outside=ch.Any(c=>c.bottomLeft.x<r.xMin-1||c.topRight.x>r.xMax+1||c.bottomLeft.y<r.yMin-1||c.topRight.y>r.yMax+1),missing=(t.text??"").Where(c=>!char.IsWhiteSpace(c)&&!t.font.HasCharacter(c,true,false)).Distinct().Select(c=>c.ToString()).ToArray()};}).ToArray();}
    static void Capture(string label)
    {
        if(label!="start"&&!label.StartsWith("end-")&&!label.StartsWith("lift-mid-")&&!label.StartsWith("hud-change-")&&label!="readability-audience-4"&&label!="result-or-transition")return;
        string path = Path.Combine(folder, (shots++).ToString("D3") + "-" + (started ? (Time.time - began).ToString("000.0", CultureInfo.InvariantCulture) : "lobby") + "-" + label + ".png");
        images.Add(path); ScreenCapture.CaptureScreenshot(path);
    }
    static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    static object ActorSample(EnemyScript_space actor)
    {
        var collider = actor.GetComponent<Collider>();
        var renderers = actor.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(actor.transform.position, Vector3.zero);
        if (renderers.Length > 0) { bounds = renderers[0].bounds; for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds); }
        var delta = actor.transform.position - player.transform.position;
        var role = actor.GetComponent<Chapter45RoleAction>();
        var animator = actor.GetComponentInChildren<Animator>(true);
        var animationState = animator != null && animator.isInitialized ? animator.GetCurrentAnimatorStateInfo(0) : default;
        string stateName = new[]{"idle","walk","run","attack_once","attack_loop","die"}.FirstOrDefault(n => animationState.IsName(n));
        return new { actor.name, actor.CurrentHealth, actor.AttackDamage, actor.IsDead, position = V(actor.transform.position), forward = V(actor.transform.forward),
            ahead = Vector3.Dot(delta, player.transform.forward), lane = Vector3.Dot(actor.transform.position - (Vector3)Origin.GetValue(player), (Vector3)Right.GetValue(player)),
            colliderEnabled = collider != null && collider.enabled, colliderMin = collider != null ? V(collider.bounds.min) : null, colliderMax = collider != null ? V(collider.bounds.max) : null,
            role = role != null ? role.role.ToString() : null, rolePhase = role != null ? role.Phase.ToString() : null,
            actions = role != null ? role.Actions : 0, contacts = role != null ? role.Contacts : 0,
            controller = animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : null,
            animatorEnabled = animator != null && animator.enabled, stateName, normalizedTime = animationState.normalizedTime,
            handProp = role != null && role.heldProp != null ? V(role.heldProp.position) : null,
            modelMin = V(bounds.min), modelMax = V(bounds.max) };
    }
    static void Finish(string reason)
    {
        if (finished) return;
        finished = true; outcome = reason; endedAt = EditorApplication.timeSinceStartup;
        Capture("end-" + reason);
        Write("summary.json", new { scene, routeChoice, outcome, finished, seconds = started ? Time.time - began : 0, wallSeconds = EditorApplication.timeSinceStartup - wallStarted,
            initialHealth, initialAttack, hp = player != null ? player.currentHealth : -1, attack = player != null ? player.ResolvedAttackDamage : -1,
            initialCoins, initialJewels, coin = MoneyScript.S != null ? MoneyScript.S.Coin : -1, jewel = MoneyScript.S != null ? MoneyScript.S.Jewel : -1,
            completed = chapter != null && chapter.Completed, claimed = director != null && director.GoalClaimed, distance = director != null ? director.Distance : -1,
            lifts = director != null ? director.LiftCount : -1, lastLiftSeconds = director != null ? director.LastLiftSeconds : -1,
            choices = director != null ? director.choices.Select(c => new { c.name, c.Selected, c.Selection, c.Rewarded, c.ResolvedCoins }).ToArray() : null,
            targets = director != null ? director.targets.Select(t => new { t.name, t.Health, t.Open, t.Phase }).ToArray() : null,
            encounters = director != null ? director.encounters.Select(e => new { e.name, e.Activated, e.Complete, e.Eligible }).ToArray() : null,
            damage, moves, errors, animationWarnings, nativeImages = images.ToArray(), statOverrides = false, teleports = false, forcedWins = false,
            hudChanges, hudCaptures, hudCaptureLimit = 100, liftMidCaptured = liftMidCaptured.OrderBy(i => i).ToArray(),
            limits = "Automated input policy; this is not a human fun certification or device performance measurement." });
        Write("status.json", new { finished, outcome, scene, routeChoice, folder });
    }
    static void Detach() { active = false; EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog; }
    static string suiteRoot;
    static int suiteIndex,suitePhase,suiteFrame=-1,suiteFailed;
    static double suiteAt,suiteStarted,nextResource;
    static bool suiteActive,duplicateChecked,flatPaused,liftPaused,cinemaPaused;
    static int pausePhase;static double pauseAt;static string pauseLabel;static float pauseClock,pauseLift,pauseHoldout;static Vector3 pausePlayer;
    static Dictionary<int,Vector3> pausePositions;
    static readonly List<object> suiteChecks=new();static readonly List<string> suiteErrors=new();
    static readonly string[] suiteScenes={"Jamsil","ShoeTower","Jamsil","ShoeTower"};
    static readonly int[] suiteChoices={0,0,1,1};
    static int startRewarded;static float expectedClearJewels;static float lastPauseWall;
    static void SuiteCheck(bool pass,string label,object evidence=null){suiteChecks.Add(new{pass,label,run=suiteIndex,evidence});if(!pass)suiteFailed++;File.WriteAllText(Path.Combine(suiteRoot,"checks.json"),Json(suiteChecks));}
    static void SuiteLog(string m,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)suiteErrors.Add(m+"\n"+stack);}
    public static object BeginSuite(string root){
        if(!EditorApplication.isPlaying||suiteActive)throw new Exception("Fresh Play required");
        if(!File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(root)),"original-game-settings.json")))throw new Exception("Capture exact game settings first");
        suiteRoot=Path.GetFullPath(root);Directory.CreateDirectory(suiteRoot);if(File.Exists(Path.Combine(suiteRoot,"suite-start.json")))throw new Exception("Preserve prior suite");
        SessionState.EraseString("Chapter45.Validation12.Abort");suiteIndex=suitePhase=suiteFailed=pausePhase=0;suiteFrame=-1;suiteChecks.Clear();suiteErrors.Clear();suiteActive=true;suiteAt=suiteStarted=EditorApplication.timeSinceStartup;nextResource=0;
        Application.logMessageReceived+=SuiteLog;EditorApplication.update+=SuiteTick;
        File.WriteAllText(Path.Combine(suiteRoot,"suite-start.json"),Json(new{utc=DateTime.UtcNow.ToString("o"),scenes=suiteScenes,choices=suiteChoices,singlePlaySession=true,automaticChapter4To5=true,qaSceneSelectionAfterChapter5=true,statOverrides=false,timeScale=1,explicitGC=false,explicitUnloadUnusedAssets=false,savePolicy="Exact six named integer game preferences captured before Play; engine metadata read-only"}));
        return new{started=true,suiteRoot};
    }
    static void SuiteTick(){
        if(!suiteActive)return;EditorApplication.QueuePlayerLoopUpdate();string abort=SessionState.GetString("Chapter45.Validation12.Abort","");if(abort!=""){EndSuite("aborted: "+abort);return;}
        if(!EditorApplication.isPlaying){EndSuite("editor-stopped");return;}
        if(suiteFrame==Time.frameCount)return;suiteFrame=Time.frameCount;
        try{
            double now=EditorApplication.timeSinceStartup,age=now-suiteAt;
            if(now-suiteStarted>2100)throw new TimeoutException("Suite wall limit");
            if(now>=nextResource){nextResource=now+10;File.AppendAllText(Path.Combine(suiteRoot,"memory-timeseries.jsonl"),Json(new{utc=DateTime.UtcNow.ToString("o"),run=suiteIndex,phase=suitePhase,scene=SceneManager.GetActiveScene().name,memory=ValidationMetrics12.Memory(),frameMs=Time.unscaledDeltaTime*1000,UnityStats.batches,UnityStats.setPassCalls})+"\n");}
            if(suitePhase==0){
                if(SceneManager.GetActiveScene().name!=suiteScenes[suiteIndex]){if(age>45)throw new Exception("Expected chapter scene "+suiteScenes[suiteIndex]);return;}
                if(age<5)return;
                UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale=UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.Locales.Single(l=>l.Identifier.Code=="en-US");
                File.WriteAllText(Path.Combine(suiteRoot,$"{suiteIndex:00}-lobby.json"),Json(ValidationMetrics12.Snapshot("lobby",suiteIndex)));
                var fresh=Object.FindFirstObjectByType<Chapter45Director>();var cp=Object.FindFirstObjectByType<ChapterProgression>();var hold=Object.FindObjectsByType<RestStopHoldout>(FindObjectsSortMode.None).FirstOrDefault(h=>h.chapter45Owner==fresh);
                SuiteCheck(!TimeManager.isGameRunning&&!CanvasScript.isGameOver&&fresh!=null&&!fresh.Running&&!fresh.IsTransferring&&fresh.Elapsed==0&&fresh.LiftCount==0&&cp!=null&&!cp.Completed&&cp.Elapsed==0,"Fresh scene has no running clock, old completion or lift state");
                SuiteCheck(ValidationMetrics12.ActiveProjectiles()==0,"Fresh lobby contains no active prior projectile",new{active=ValidationMetrics12.ActiveProjectiles()});
                SuiteCheck(hold==null||!hold.Active&&!hold.Completed&&hold.Elapsed==0&&hold.Spawned==0&&hold.Killed==0,"Fresh cinema counters are reset");
                var stale=ValidationMetrics12.Delegates().Where(r=>r.destroyedTarget).ToArray();SuiteCheck(stale.Length==0,"Observed event subscriptions have no destroyed Unity target",new{stale});
                StartSuiteRoute(Path.Combine(suiteRoot,$"run-{suiteIndex:00}-{suiteScenes[suiteIndex]}-{suiteChoices[suiteIndex]}"),suiteScenes[suiteIndex],suiteChoices[suiteIndex]);
                startRewarded=PlayerPrefs.GetInt("chapter_rewarded_"+chapter.chapter,0);string key=(startRewarded==0?"firstClearJewels_":"replayClearJewels_")+ChapterSceneKey.Resolve(scene);expectedClearJewels=EnvironmentVariableTables.TryGetFloat(key,out float reward)?Mathf.Floor(Mathf.Clamp(reward,0,100)):0;
                duplicateChecked=flatPaused=liftPaused=cinemaPaused=false;pausePhase=0;suitePhase=1;suiteAt=now;return;
            }
            if(suitePhase==1){
                if(started&&!finished)PauseInterventions(now);
                if(finished&&!duplicateChecked){
                    duplicateChecked=true;SuiteCheck(outcome=="clear","Actual bounded-input route clears",new{scene,routeChoice,outcome});
                    if(outcome!="clear"){EndSuite("route-failed");return;}
                    int jewel=MoneyScript.S.Jewel,coin=MoneyScript.S.Coin;chapter.CompleteChapter();chapter.CompleteChapter();canvas.YouWin();
                    SuiteCheck(MoneyScript.S.Jewel==jewel&&MoneyScript.S.Coin==coin,"Repeated completion callbacks cannot pay twice",new{jewel,coin,afterJewel=MoneyScript.S.Jewel,afterCoin=MoneyScript.S.Coin});
                    SuiteCheck(jewel-initialJewels==expectedClearJewels&&chapter.ClearJewels==expectedClearJewels,"Persistent first/replay reward flag selects exactly one authored payment",new{startRewarded,expectedClearJewels,actual=jewel-initialJewels,chapter.ClearJewels});
                    File.WriteAllText(Path.Combine(suiteRoot,$"{suiteIndex:00}-complete.json"),Json(ValidationMetrics12.Snapshot("complete",suiteIndex)));
                }
                if(active)return;
                if(!finished)throw new Exception("Route detached without result");
                SuiteCheck(flatPaused,"Ordinary gameplay pause/resume exercised");if(scene=="ShoeTower")SuiteCheck(liftPaused,"Pause/resume during actual lift exercised");if(scene=="ShoeTower"&&routeChoice==1)SuiteCheck(cinemaPaused,"Pause/resume during actual cinema survival exercised");
                player=null;director=null;chapter=null;canvas=null;playerWeapons=null;enemyCommitments.Clear();openingLanes.Clear();avoidanceLanes.Clear();observations.Clear();
                suiteIndex++;suiteAt=now;
                if(suiteIndex>=suiteScenes.Length){suitePhase=2;return;}
                if(suiteScenes[suiteIndex]=="Jamsil"){TimeManager.isGameRunning=false;TimeManager.timeFactor=1;SceneManager.LoadScene("Jamsil");}
                suitePhase=0;return;
            }
            if(suitePhase==2&&age>=5){File.WriteAllText(Path.Combine(suiteRoot,"final-idle.json"),Json(ValidationMetrics12.Snapshot("last-result-screen",suiteIndex)));EndSuite(suiteFailed==0&&suiteErrors.Count==0?"passed":"failed");}
        }catch(Exception e){suiteErrors.Add(e.ToString());EndSuite("harness-error");}
    }
    static Dictionary<int,Vector3> LivePositions(){
        var transforms=Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None).Where(x=>x.gameObject.activeInHierarchy&&!x.IsDead).Select(x=>x.transform)
          .Concat(Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None).Where(x=>x.gameObject.activeInHierarchy).Select(x=>x.transform))
          .Concat(Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(x=>x.gameObject.activeInHierarchy).Select(x=>x.transform));
        return transforms.Distinct().ToDictionary(x=>x.GetInstanceID(),x=>x.position);
    }
    static void PauseInterventions(double now){
        if(pausePhase==0){
            if(!flatPaused&&director.Elapsed>10&&!director.IsTransferring&&player.HoldoutAim==null){flatPaused=true;pauseLabel="ordinary";}
            else if(!liftPaused&&director.IsTransferring&&(float)RideElapsed.GetValue(director)>2){liftPaused=true;pauseLabel="lift";}
            else if(!cinemaPaused&&player.HoldoutAim!=null&&player.HoldoutAim.Active&&player.HoldoutAim.Elapsed>4){cinemaPaused=true;pauseLabel="cinema";}
            else return;
            canvas.PauseGame();pauseAt=now;pausePhase=1;return;
        }
        if(pausePhase==1){if(now-pauseAt<.15)return;pauseClock=director.Elapsed;pauseLift=(float)RideElapsed.GetValue(director);pauseHoldout=player.HoldoutAim!=null?player.HoldoutAim.Elapsed:0;pausePlayer=player.transform.position;pausePositions=LivePositions();pauseAt=now;pausePhase=2;return;}
        if(now-pauseAt<.7)return;var after=LivePositions();float drift=pausePositions.Where(x=>after.ContainsKey(x.Key)).Select(x=>Vector3.Distance(x.Value,after[x.Key])).DefaultIfEmpty(0).Max();float hold=player.HoldoutAim!=null?player.HoldoutAim.Elapsed:0;
        SuiteCheck(director.Elapsed==pauseClock&&(float)RideElapsed.GetValue(director)==pauseLift&&hold==pauseHoldout&&Vector3.Distance(player.transform.position,pausePlayer)<.001f&&drift<.001f,"Pause freezes game/lift/holdout clocks and active combat positions: "+pauseLabel,new{drift,playerDrift=Vector3.Distance(player.transform.position,pausePlayer),clockDrift=director.Elapsed-pauseClock,liftDrift=(float)RideElapsed.GetValue(director)-pauseLift,holdoutDrift=hold-pauseHoldout,beforeActive=pausePositions.Count,afterActive=after.Count});
        canvas.ResumeGame();pausePhase=0;
    }
    static void EndSuite(string reason){suiteActive=false;Detach();EditorApplication.update-=SuiteTick;Application.logMessageReceived-=SuiteLog;File.WriteAllText(Path.Combine(suiteRoot,"suite-summary.json"),Json(new{utc=DateTime.UtcNow.ToString("o"),outcome=reason,completedRoutes=suiteIndex,wallSeconds=EditorApplication.timeSinceStartup-suiteStarted,passed=suiteChecks.Count-suiteFailed,failed=suiteFailed,errors=suiteErrors,checks=suiteChecks,singlePlaySession=true,explicitGC=false,explicitUnloadUnusedAssets=false,limits="Editor with QA observer overhead; native/managed memory counts are not device measurements or proof of arbitrary-duration leak freedom. Runtime delegates observed on known services; arbitrary captured object graphs are not exhaustively traced."}));}

    public static object ProbeCompile()=>new{compiled=true};
    public static object RequestAbort(string reason){SessionState.SetString("Chapter45.Validation12.Abort",reason);return new{queued=true,reason};}
}

public static class ValidationMetrics12 {
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
  return new{utc=DateTime.UtcNow.ToString("o"),label,run,scene=liveScene.name,handle=liveScene.handle,memory,totalGameObjects=gos.Length,activeGameObjects=gos.Count(x=>x.activeInHierarchy),byScene=gos.GroupBy(x=>x.scene.name).Select(g=>new{scene=g.Key,count=g.Count(),active=g.Count(x=>x.activeInHierarchy)}).ToArray(),persistent,objectCounts=counts,nonAssetObjectCounts=nonAssetObjects,listeners=Delegates(),enemies=new{total=enemies.Length,active=enemies.Count(x=>x.gameObject.activeInHierarchy),aliveActive=enemies.Count(x=>x.gameObject.activeInHierarchy&&!x.IsDead)},bullets=new{total=bullets.Length,active=bullets.Count(x=>x.gameObject.activeInHierarchy)},roleProjectiles=new{total=simple.Length,active=simple.Count(x=>x.gameObject.activeInHierarchy)},director=d==null?null:new{d.Running,d.Elapsed,d.FloorElapsed,d.Distance,d.CurrentFloor,d.IsTransferring,d.LiftCount,d.GoalClaimed,encounters=d.encounters.Select(e=>new{e.name,e.Activated,e.Complete}).ToArray(),choices=d.choices.Select(c=>new{c.name,c.Selected,c.Selection,c.Rewarded}).ToArray()},holdouts=h.Select(x=>new{x.name,x.Active,x.Completed,x.Elapsed,x.Spawned,x.Killed}).ToArray(),player=p==null?null:new{p.currentHealth,p.IsStationaryCombat},renderer=new{UnityStats.batches,UnityStats.setPassCalls,UnityStats.triangles,frameMs=Time.unscaledDeltaTime*1000}};
 }
}
