using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VerifyNoryangjinRevampFix
{
    const string Root = "outputs/noryangjin-revamp-fix-2026-09-28";
    const string Preview = "tmp/image-previews/noryangjin-revamp-fix-2026-09-28";
    const string Scene = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18_Revamp.unity";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    public static object Prepare()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode required");
        string path = Root + "/before/playerprefs.tsv";
        if (!File.Exists(path))
        {
            ChapterPlaytestPreferences.SnapshotAt(path);
            File.WriteAllText(Root + "/before/start-scene.txt", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            File.WriteAllText(Root + "/before/start-stage.txt", SessionState.GetInt("NoryangjinMapTool.TestStartStage", 0).ToString());
        }
        var type = typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage");
        type.GetMethod("SelectStage", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] {1});
        SessionState.SetBool("NoryangjinMapTool.TestPower9999", false);
        SessionState.SetBool("NoryangjinMapTool.TestFastLateral", false);
        SessionState.SetFloat("NoryangjinMapTool.TestTimeScale", 1);
        return new { prepared = true, start = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), power9999 = false };
    }
    public static object Restore()
    {
        var result = ChapterPlaytestPreferences.RestoreAt(Root + "/before/playerprefs.tsv");
        var type = typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage");
        var select = type.GetMethod("SelectStage", BindingFlags.Static | BindingFlags.NonPublic);
        select.Invoke(null, new object[] {0});
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(File.ReadAllText(Root + "/before/start-scene.txt"));
        select.Invoke(null, new object[] {int.Parse(File.ReadAllText(Root + "/before/start-stage.txt"))});
        return result;
    }
    public static object VerifyRestored()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before verifying restoration");
        var mismatches=new List<string>();
        foreach(var line in File.ReadAllLines(Root+"/before/playerprefs.tsv"))
        {
            var fields=line.Split('\t');bool existed=bool.Parse(fields[2]);
            if(PlayerPrefs.HasKey(fields[0])!=existed){mismatches.Add(fields[0]);continue;}
            if(!existed)continue;
            string current=fields[1]=="int"?PlayerPrefs.GetInt(fields[0]).ToString(System.Globalization.CultureInfo.InvariantCulture)
                :fields[1]=="float"?PlayerPrefs.GetFloat(fields[0]).ToString("R",System.Globalization.CultureInfo.InvariantCulture)
                :Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(fields[0])));
            if(current!=fields[3])mismatches.Add(fields[0]);
        }
        var result=new{snapshotMatches=mismatches.Count==0,mismatches,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel"),
            attackLevel=PlayerPrefs.GetInt("upgrade_lv_1"),healthLevel=PlayerPrefs.GetInt("upgrade_lv_2"),speedLevel=PlayerPrefs.GetInt("upgrade_lv_3"),
            stageMatches=SessionState.GetInt("NoryangjinMapTool.TestStartStage",0)==int.Parse(File.ReadAllText(Root+"/before/start-stage.txt")),
            startSceneMatches=AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)==File.ReadAllText(Root+"/before/start-scene.txt"),
            scene=EditorSceneManager.GetActiveScene().path,dirty=EditorSceneManager.GetActiveScene().isDirty};
        File.WriteAllText(Root+"/restoration.json",Json(result));return result;
    }
    public static object PrepareGrowth()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        ChapterPlaytestPreferences.RestoreAt(Root + "/before/playerprefs.tsv");
        Prepare();
        PlayerPrefs.SetInt("upgrade_lv_1",20); PlayerPrefs.SetInt("upgrade_lv_2",12); PlayerPrefs.SetInt("upgrade_lv_3",10);
        PlayerPrefs.Save();
        return new {attackLevel=20,healthLevel=12,fireRateLevel=10,pinHealth=false,debug9999=false};
    }
    public static object PrepareClearGrowth()
    {
        PrepareGrowth();
        PlayerPrefs.SetInt("upgrade_lv_1",37); PlayerPrefs.SetInt("upgrade_lv_2",46); PlayerPrefs.SetInt("upgrade_lv_3",30);
        PlayerPrefs.Save(); return new {attackLevel=37,healthLevel=46,fireRateLevel=30,pinHealth=false,debug9999=false};
    }
    public static object Audit()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != Scene || EditorApplication.isPlaying) throw new Exception("Saved revamp edit scene required");
        var director = Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        var suppressor = director.GetComponent<NoryangjinRevampSuppressor>();
        var branch = director.GetComponentInChildren<NoryangjinMarketBranch>();
        var models = director.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("N0")).Select(t => t.name).Distinct().ToArray();
        var report = new { scene = scene.path, scene.isDirty, suppressed = suppressor.targets.Length,
            activeSuppressed = suppressor.targets.Count(t => t != null && t.activeSelf),
            ceilings = director.GetComponentsInChildren<Transform>().Count(t => t.name == "LowCeilingPanel"),
            branch = new { branch.length, branch.outsideOffset, branch.earliestChoice, branch.choiceSeconds }, models,
            events = director.GetComponentsInChildren<NoryangjinRevampEvent>(true).Select(e => new {e.name, type=e.GetType().Name, route=e.routeScope.ToString(),e.hazardSeconds}).ToArray() };
        File.WriteAllText(Root + "/scene-audit.json", Json(report)); return report;
    }
    public static object EditorViews()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode required");
        var cam = GameObject.Find("MapTool_Camera").GetComponent<Camera>();
        var p = cam.transform.position; var q = cam.transform.rotation; var previous = cam.targetTexture; var active = RenderTexture.active;
        string folder = Preview + "/edit-v2"; Directory.CreateDirectory(folder);
        var rt = new RenderTexture(540,1170,24);
        var spots = new[] {
            ("indoor",new Vector3(124.3f,0,8),Vector3.back,true),
            ("turret",new Vector3(124.3f,0,-174),Vector3.back,true),
            ("shutter",new Vector3(124.3f,0,-295),Vector3.back,true),
            ("outside",new Vector3(99.3f,0,-63),Vector3.back,false),
            ("cold",new Vector3(-67,0,-313),Vector3.forward,false),
            ("port",new Vector3(225.5f,0,-44),Vector3.forward,false),
            ("lighthouse",new Vector3(248,0,253),Vector3.forward,false)};
        try
        {
            foreach(var shot in spots)
            {
                var frame = Quaternion.LookRotation(shot.Item3);
                cam.transform.SetPositionAndRotation(shot.Item2 + frame * (shot.Item4 ? new Vector3(.12f,5.8f,-17) : new Vector3(.12f,12.55f,-19.27f)), frame * Quaternion.Euler(shot.Item4 ? 8 : 30,0,0));
                cam.targetTexture = rt; cam.aspect = 540f/1170; cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(540,1170,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,540,1170),0,0);tex.Apply();
                File.WriteAllBytes(folder+"/"+shot.Item1+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
            }
        }
        finally {cam.transform.SetPositionAndRotation(p,q);cam.targetTexture=previous;RenderTexture.active=active;cam.ResetAspect();Object.DestroyImmediate(rt);}
        return new {folder, count=spots.Length};
    }
    public static object Inside() => Begin("inside-" + DateTime.Now.ToString("HHmmss"),0);
    public static object InteriorV2Inside() => Begin("interior-v2-inside-" + DateTime.Now.ToString("HHmmss"),0);
    public static object InteriorV2Outside() => Begin("interior-v2-outside-" + DateTime.Now.ToString("HHmmss"),1);
    public static object DebugReviewInside() => Begin("debug-review-inside-" + DateTime.Now.ToString("HHmmss"),0,true);
    public static object DebugReviewOutside() => Begin("debug-review-outside-" + DateTime.Now.ToString("HHmmss"),1,true);
    public static object DebugFinalInside() => Begin("debug-final-inside-" + DateTime.Now.ToString("HHmmss"),0,true,3);
    public static object DebugFinalOutside() => Begin("debug-final-outside-" + DateTime.Now.ToString("HHmmss"),1,true,3);
    public static object Outside() => Begin("outside-" + DateTime.Now.ToString("HHmmss"),1);
    public static object CaptureInside() => CaptureBranch(0);
    public static object CaptureOutside() => CaptureBranch(1);
    static object CaptureBranch(int route)
    {
        var p=Object.FindFirstObjectByType<PlayerScript>();var d=Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        d.GetComponentInChildren<NoryangjinMarketBranch>().earliestChoice=0;
        var result=Begin("capture-final-"+(route==0?"inside-":"outside-")+DateTime.Now.ToString("HHmmss"),route);
        // ResetState runs inside Begin and restores the authored heading. Relocate only afterward.
        p.ApplyContinuousRoutePose(d.Branch.transform.position+Vector3.up*.12f,Vector3.back,0);
        var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();
        if(tutorial!=null){tutorial.Close();tutorial.enabled=false;} // Capture fixture only; authored tutorial stays enabled.
        string folder=File.ReadAllText(Root+"/active-run.txt");
        EditorApplication.CallbackFunction finish=null;
        finish=()=>
        {
            if(!EditorApplication.isPlaying||d==null){EditorApplication.update-=finish;return;}
            if(d.Branch.Complete||d.Player.currentHealth<=0)
            {
                File.WriteAllText(folder+"/section-result.json",Json(new{kind="branch-only capture fixture; relocated start",route,complete=d.Branch.Complete,health=p.currentHealth,timeline=d.Timeline.ToArray()}));
                EditorApplication.update-=finish;EditorApplication.isPaused=true;Time.timeScale=1;
            }
        };
        EditorApplication.update+=finish;return result;
    }
    public static object Status()
    {
        var d = Object.FindFirstObjectByType<NoryangjinRevampDirector>(); var cam = Camera.main;
        return new {playing=EditorApplication.isPlaying,paused=EditorApplication.isPaused,running=TimeManager.isGameRunning,
            time=d?.Elapsed,health=d?.Player?.currentHealth,position=d?.Player?.transform.position.ToString(),
            branch=d?.Branch?.Distance,selected=d?.Branch?.Selected,pending=d?.Branch?.choiceUI?.Pending,
            fov=cam?.fieldOfView,camera=cam?.transform.position.ToString(),debug9999=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false),timeline=d?.Timeline.ToArray()};
    }
    public static object InspectModels()
    {
        var root=GameObject.Find("NoryangjinRevamp");
        return root.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && (t.name.Contains("tank")||t.name.Contains("N03")||t.name.Contains("N04"))).Take(8)
            .Select(t=>new{name=t.name,source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject),scale=t.localScale.ToString(),bounds=Combined(t).size.ToString()}).ToArray();
    }
    static Bounds Combined(Transform t)
    {
        var list=t.GetComponentsInChildren<Renderer>(true);var bounds=new Bounds(t.position,Vector3.zero);
        foreach(var r in list)bounds.Encapsulate(r.bounds);return bounds;
    }
    public static object StartAfterOpening()
    {
        Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
        Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        return Status();
    }
    // Isolated real-component probes. These are mechanic fixtures, never difficulty evidence.
    public static object ProbeMechanics()
    {
        if (!EditorApplication.isPlaying || TimeManager.isGameRunning) throw new Exception("Fresh Play start required");
        Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
        Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton(); Time.timeScale=1;
        var p=Object.FindFirstObjectByType<PlayerScript>(); var d=Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        var fixture=new GameObject("Directed mechanic fixture");
        var owner=p.GetType().GetField("stationaryCombatOwner",Flags); var fixedPosition=p.GetType().GetField("stationaryCombatPosition",Flags);
        void Place(Vector3 position,Vector3 forward)
        {
            p.ApplyContinuousRoutePose(position,forward,0);owner.SetValue(p,fixture);fixedPosition.SetValue(p,p.transform.position);Physics.SyncTransforms();
        }
        Place(new Vector3(1000,.12f,1000),Vector3.forward);
        var results=new List<object>();int stage=0,selected=-1;double began=EditorApplication.timeSinceStartup;float hp=0;int wallet=0;
        EnemyScript_space merchant=null;NoryangjinWaveEvent wave=null;
        Action<int> choice=i=>selected=i;
        d.Branch.choiceUI.Chosen+=choice;
        d.Branch.choiceUI.OpenCustom("갈림길 기본 선택 검증","시장 안쪽","바깥 부두","5초 후 안쪽","수동 선택",d.Branch.indoorPicture,d.Branch.outdoorPicture,5);
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(!EditorApplication.isPlaying||p==null){EditorApplication.update-=tick;return;}
                double now=EditorApplication.timeSinceStartup;
                if(stage==0&&now-began>5.8)
                {
                    results.Add(new{name="five-second default selects indoor",pass=selected==0&&!d.Branch.choiceUI.Pending,selected});
                    d.Branch.choiceUI.Chosen-=choice; wallet=MoneyScript.S.Coin;hp=p.currentHealth;
                    var template=d.GetComponentInChildren<NoryangjinRushEvent>().templates[0];
                    var go=Object.Instantiate(template,p.transform.position,Quaternion.LookRotation(Vector3.back),fixture.transform);
                    NoryangjinRushEvent.StripToCombat(go);var behavior=go.AddComponent<NoryangjinMerchant>();behavior.coinContact=true;
                    go.SetActive(true);merchant=go.GetComponent<EnemyScript_space>();merchant.ConfigureRewards(false,0);merchant.ApplyStat(100,100,EnemyTier.Normal);
                    go.GetComponent<Collider>().enabled=true;Physics.SyncTransforms();stage=1;began=now;
                }
                else if(stage==1&&now-began>1.2)
                {
                    results.Add(new{name="merchant physical contact spends coins without HP damage",pass=merchant.IsDead&&MoneyScript.S.Coin==wallet-Mathf.Min(wallet,30)&&Mathf.Abs(p.currentHealth-hp)<.1f,coinsLost=wallet-MoneyScript.S.Coin,hpLost=hp-p.currentHealth});
                    hp=p.currentHealth;d.StartSpin(2,.1f,PlayerDamageCause.WaterJet);stage=2;began=now;
                }
                else if(stage==2&&now-began>2.6)
                {
                    results.Add(new{name="hose two seconds drains twenty percent",pass=Mathf.Abs((hp-p.currentHealth)-p.MaxHealth*.2f)<2&&!d.Spinning,damage=hp-p.currentHealth,expected=p.MaxHealth*.2f});
                    // Establish the actual indoor selection before probing route-scoped events.
                    Place(d.Branch.transform.position+Vector3.up*.12f,d.Branch.transform.forward);
                    d.Branch.Tick(0);d.Branch.Choose(0);
                    typeof(NoryangjinRevampDirector).GetProperty("QuietUntil").SetValue(d,0f);
                    var cat=d.GetComponentInChildren<NoryangjinCatEvent>();cat.ResetForRun();
                    Place(cat.transform.position-cat.transform.forward*10+Vector3.up*.12f,cat.transform.forward);cat.Tick(0);
                    cat.protectedStall.Damage(1);wallet=MoneyScript.S.Coin;
                    Place(cat.transform.position+cat.transform.forward*8+Vector3.up*.12f,cat.transform.forward);cat.Tick(.1f);
                    bool resolved=(bool)typeof(NoryangjinCatEvent).GetField("resolved",Flags).GetValue(cat);
                    bool follows=(bool)typeof(NoryangjinCatEvent).GetField("following",Flags).GetValue(cat);
                    results.Add(new{name="damaged shop does not trigger cat reward",pass=resolved&&!follows&&MoneyScript.S.Coin==wallet});
                    var shutter=d.GetComponentInChildren<NoryangjinShutterEvent>();
                    shutter.ResetForRun();shutter.walls=Array.Empty<NoryangjinBreakable>();
                    Place(shutter.transform.position-shutter.transform.forward*1.5f+Vector3.up*.12f,shutter.transform.forward);
                    shutter.Tick(0);shutter.Tick(20);
                    results.Add(new{name="expired shutter becomes a blocking breakable",pass=shutter.shutter.Alive&&shutter.shutter.BlocksNow});
                    hp=p.currentHealth;stage=3;began=now;
                }
                else if(stage==3&&now-began>1)
                {
                    results.Add(new{name="closed shutter drains real player health",pass=p.currentHealth<hp,damage=hp-p.currentHealth});
                    Place(new Vector3(1002,.12f,1000),Vector3.forward);
                    var root=new GameObject("Wave fixture");root.transform.SetParent(fixture.transform);root.transform.position=new Vector3(1000,0,1000);
                    wave=root.AddComponent<NoryangjinWaveEvent>();wave.halfWidth=2;wave.Bind(d);
                    var warning=new GameObject("Warning").transform;warning.SetParent(root.transform);
                    var water=new GameObject("Water").transform;water.SetParent(root.transform);
                    wave.waves=new[]{new NoryangjinWaveEvent.Wave{along=0,side=-1,length=18,warning=warning,water=water}};
                    wave.ResetForRun();hp=p.currentHealth;wave.Tick(0);wave.Tick(2.3f);
                    results.Add(new{name="safe half survives active wave",pass=p.currentHealth==hp});
                    Place(new Vector3(998,.12f,1000),Vector3.forward);wave.Tick(.01f);
                    results.Add(new{name="danger half dies from wave",pass=p.currentHealth<=0&&p.LastDamageCause==PlayerDamageCause.Wave});
                    File.WriteAllText(Root+"/directed-mechanics.json",Json(results));
                    owner.SetValue(p,null);EditorApplication.update-=tick;Time.timeScale=1;EditorApplication.isPaused=true;
                }
                if(now-began>20){throw new Exception("Directed probe stage timed out: "+stage);}
            }
            catch(Exception error)
            {
                EditorApplication.update-=tick;owner.SetValue(p,null);Time.timeScale=1;
                File.WriteAllText(Root+"/directed-mechanics-error.txt",error.ToString());
            }
        };
        EditorApplication.update+=tick;return new{started=true,kind="isolated native component and physics fixtures"};
    }
    public static object Begin(string label, int route, bool debugReview = false,float reviewTimeScale=1)
    {
        if (!EditorApplication.isPlaying || TimeManager.isGameRunning) throw new Exception("Play start screen required");
        var player = Object.FindFirstObjectByType<PlayerScript>();
        var director = Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        var progression = Object.FindFirstObjectByType<ChapterProgression>();
        var move = player.GetType().GetMethod("PlayerMove",Flags);
        var laneOrigin = player.GetType().GetField("routeLaneOrigin",Flags);
        var routeRight = player.GetType().GetField("routeRight",Flags);
        var waves = director.GetComponentsInChildren<NoryangjinWaveEvent>();
        var hoses = director.GetComponentsInChildren<NoryangjinHoseEvent>();
        var incidents = director.GetComponentsInChildren<NoryangjinMarketIncident>();
        var bonuses = Object.FindObjectsByType<BonusWallChoicePair>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var hazards = GameObject.Find("Noryangjin_MapTool/Props").transform.Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")).ToArray();
        string folder = Preview + "/play-" + label; Directory.CreateDirectory(folder);
        File.WriteAllText(folder+"/samples.csv","elapsed,hp,maxhp,x,y,z,branch,blocked\n");
        File.WriteAllText(folder+"/frames.csv","frame,elapsed,x,y,z,yaw\n");
        int frame=-1,seen=0,shots=0;float nextShot=0,nextSample=0,nextTargetScan=0;bool done=false,debugObserved=false;double stopAt=0;
        var aimTargets=Array.Empty<EnemyScript_space>();
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
            if(!EditorApplication.isPlaying||player==null){EditorApplication.update-=tick;return;}
            if(done){if(EditorApplication.timeSinceStartup>stopAt){EditorApplication.update-=tick;EditorApplication.isPaused=true;Time.timeScale=1;}return;}
            if(frame==Time.frameCount||EditorApplication.isPaused)return;frame=Time.frameCount;
            debugObserved |= SessionState.GetBool("NoryangjinMapTool.TestPower9999",false) || player.ResolvedAttackDamage >= 9999;
            if(director.Branch.choiceUI.Pending&&director.Branch.choiceUI.Remaining<3)director.Branch.choiceUI.Select(route);
            if(director.Elapsed>=nextSample)
            {
                nextSample+=.5f;var at=player.transform.position;
                File.AppendAllText(folder+"/samples.csv",$"{director.Elapsed:F2},{player.currentHealth:F1},{player.MaxHealth:F1},{at.x:F2},{at.y:F2},{at.z:F2},{director.Branch.Distance:F1},{director.Blocked}\n");
            }
            if(director.Elapsed>=nextShot||director.Timeline.Count>seen)
            {
                nextShot=director.Elapsed+(label.StartsWith("capture-final")?2:debugReview?4:8);seen=director.Timeline.Count;
                string frameName="frame-"+shots++.ToString("D3");
                ScreenCapture.CaptureScreenshot(folder+"/"+frameName+".png");
                var pose=player.transform.position;
                File.AppendAllText(folder+"/frames.csv",$"{frameName},{director.Elapsed:F2},{pose.x:F2},{pose.y:F2},{pose.z:F2},{player.transform.eulerAngles.y:F1}\n");
            }
            if(player.currentHealth<=0||progression.Completed||director.Elapsed>480)
            {
                done=true;stopAt=EditorApplication.timeSinceStartup+1;
                File.WriteAllText(folder+"/timeline.txt",string.Join("\n",director.Timeline));
                var result=new{route,elapsed=director.Elapsed,health=player.currentHealth,maximum=player.MaxHealth,attack=player.ResolvedAttackDamage,
                    completed=progression.Completed,cause=player.LastDamageCause.ToString(),branchSelected=director.Branch.Selected,branchComplete=director.Branch.Complete,
                    suppressedActive=director.GetComponent<NoryangjinRevampSuppressor>().targets.Count(t=>t!=null&&t.activeSelf),
                    debugObserved,fastLateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),requestedTimeScale=debugReview?reviewTimeScale:3,prefMode=debugObserved?"9999 override; mechanic/presentation coverage only":"saved upgrade levels; no health pin or healing",timeline=director.Timeline.ToArray()};
                File.WriteAllText(folder+"/result.json",Json(result));
                return;
            }
            if(!TimeManager.isGameRunning||player.IsWorldYawTurnActive)return;
            float desired=0,nearest=25;var origin=(Vector3)laneOrigin.GetValue(player);var right=(Vector3)routeRight.GetValue(player);
            var forward=Vector3.ProjectOnPlane(player.transform.forward,Vector3.up).normalized;
            foreach(var pair in bonuses)
            {
                if(pair==null||pair.Left==null||pair.Right==null||pair.Selected!=null||!pair.Left.gameObject.activeInHierarchy)continue;
                Vector3 delta=(pair.Left.transform.position+pair.Right.transform.position)*.5f-player.transform.position;float ahead=Vector3.Dot(delta,forward);
                if(ahead < -1 || ahead>nearest||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>5)continue;
                nearest=ahead;desired=Mathf.Clamp(Vector3.Dot(pair.Left.transform.position-origin,right),-1.8f,1.8f);
            }
            if(debugReview)
            {
                if(director.Elapsed>=nextTargetScan){aimTargets=Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None);nextTargetScan=director.Elapsed+.2f;}
                float best=30,currentLane=Vector3.Dot(player.transform.position-origin,right);
                foreach(var enemy in aimTargets)
                {
                    if(enemy==null||enemy.IsDead||!enemy.gameObject.activeInHierarchy)continue;
                    var delta=enemy.transform.position-player.transform.position;float ahead=Vector3.Dot(delta,forward),lane=Vector3.Dot(enemy.transform.position-origin,right);
                    if(ahead<1||ahead>26||Mathf.Abs(lane)>2.2f||Mathf.Abs(delta.y)>3)continue;
                    float priority=ahead+Mathf.Abs(lane-currentLane)*2;
                    if(priority<best){best=priority;desired=Mathf.Clamp(lane,-1.8f,1.8f);}
                }
            }
            float nearestHazard=16;
            foreach(var hazard in hazards)
            {
                if(hazard==null||!hazard.gameObject.activeInHierarchy)continue;
                var colliders=hazard.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.bounds.size.sqrMagnitude>.001f&&c.bounds.min.y<player.transform.position.y+3&&c.bounds.max.y>player.transform.position.y).ToArray();
                var hazardPoint=hazard.position;
                if(colliders.Length>0){var bounds=colliders[0].bounds;foreach(var c in colliders)bounds.Encapsulate(c.bounds);hazardPoint=bounds.center;}
                var delta=hazardPoint-player.transform.position;float ahead=Vector3.Dot(delta,forward);
                if(ahead< -2||ahead>15||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,right))>5)continue;
                if(ahead>=nearestHazard)continue;nearestHazard=ahead;
                desired=Vector3.Dot(hazardPoint-origin,right)>=0?-1.8f:1.8f;
            }
            foreach(var wave in waves)foreach(var band in wave.waves)
                if(band.warning!=null&&band.warning.gameObject.activeInHierarchy&&Mathf.Abs(wave.Lateral(player.transform.position))<5)desired=-band.side*1.8f;
            foreach(var hose in hoses)if(hose.Triggered&&Mathf.Abs(hose.Lateral(player.transform.position))<5)
                foreach(var jet in hose.jets){float gap=jet.along+hose.Ahead(player.transform.position);if(gap> -1&&gap<9&&jet.visual!=null&&jet.visual.gameObject.activeSelf)desired=-jet.side*1.8f;}
            foreach(var incident in incidents)
                if(incident.Triggered&&incident.Ahead(player.transform.position)>-8&&incident.Ahead(player.transform.position)<24&&Mathf.Abs(incident.Lateral(player.transform.position))<5)
                    desired=incident.lane<0?1.8f:-1.8f;
            float current=Vector3.Dot(player.transform.position-origin,right);float error=desired-current;
            if(Mathf.Abs(error)>.05f)move.Invoke(player,new object[]{Mathf.Sign(error)*Mathf.Min(15,Mathf.Abs(error)*12)});
            }
            catch(Exception error)
            {
                EditorApplication.update-=tick;
                File.WriteAllText(folder+"/harness-error.txt",error.ToString());
                Time.timeScale=1;
            }
        };
        EditorApplication.update+=tick;
        // Synchronize actual saved upgrade levels through the game's own path, never a health pin.
        UpgradeStatManager.S?.SyncFromPurchasedLevels();
        var overrides=typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestOverrides");
        overrides.GetMethod("Select",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{debugReview,debugReview});
        foreach(var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))harness.enabled=false;
        Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
        Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();Time.timeScale=debugReview?reviewTimeScale:3;
        File.WriteAllText(Root+"/active-run.txt",folder);
        return new{folder,route,health=player.currentHealth,maximum=player.MaxHealth,attack=player.ResolvedAttackDamage};
    }
}
