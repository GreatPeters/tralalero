using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class RestStopKoreanPlaytest
{
    public static object HighwayChapter2Prepare() => HighwayChapter2Playtest.Prepare();
    public static object HighwayChapter2Begin() => HighwayChapter2Playtest.Begin();
    public static object HighwayChapter2Restore() => HighwayChapter2Playtest.Restore();
    public static object HighwayChapter2PrepareJam() => HighwayChapter2Playtest.PrepareJam();
    public static object HighwayChapter2BeginJam() => HighwayChapter2Playtest.BeginJam();
    public static object HighwayChapter2BeginCash() => HighwayChapter2Playtest.BeginCash();
    public static object HighwayChapter2BeginOpenCash() => HighwayChapter2Playtest.BeginOpenCash();
    public static object HighwayChapter2BeginFollowup() => HighwayChapter2Playtest.BeginFollowup();
    public static object HighwayChapter2BeginVisual() => HighwayChapter2Playtest.BeginVisual();
    public static object HighwayChapter2BeginNoFire() => HighwayChapter2Playtest.BeginNoFire();
    public static object HighwayChapter2CompareOriginal() => HighwayChapter2Playtest.CompareOriginal();
    public static object HighwayChapter2RestoreOriginal() => HighwayChapter2Playtest.RestoreOriginal();
    const string Record="outputs/meshy-reststop-2026-09-25";
    static readonly Vector3[] Points={new(0,0,-420),new(0,0,0),new(240,0,0),new(240,0,440),new(-100,0,440),new(-100,0,780),new(220,0,780)};
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    public static string Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Record+"/prefs/playerprefs.tsv"))throw new InvalidOperationException("Fresh snapshot and Edit Mode required");
        for(int i=1;i<=9;i++)PlayerPrefs.SetInt("upgrade_lv_"+i,i==1?37:i==2?46:i==3?30:0);
        foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))PlayerPrefs.SetString(CosmeticService.EquippedKey(slot),CosmeticTables.Rows.Single(r=>r.slot==slot&&r.isDefault).id);
        PlayerPrefs.Save();SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);
        return "ATT37 HP46 attack-speed30; fresh preferences preserved";
    }
    static void Sample(float d,out Vector3 p,out Vector3 f)
    {
        for(int i=0;i<Points.Length-1;i++){var delta=Points[i+1]-Points[i];float length=delta.magnitude;if(d<=length||i==Points.Length-2){f=delta.normalized;p=Points[i]+f*Mathf.Clamp(d,0,length);return;}d-=length;}throw new InvalidOperationException();
    }
    // 2026-09-26 traffic clearance check: coverage bot on HighWay's left-centre lane (potholes now sit at ±0.8/0).
    public static string HwTraffic0926()=>Begin("hw-traffic-0926",0,110,-1.9f,true);
    public static string RsTraffic0926()=>Begin("rs-traffic-0926",0,100,-1.9f,true);
    public static string Interior0926()=>Begin("interior-0926-"+System.DateTime.Now.ToString("HHmmss"),945,24,-1.9f,true);
    public static string Baseline0926()=>Begin("baseline-0926-"+SceneManager.GetActiveScene().name+"-"+System.DateTime.Now.ToString("HHmmss"),0,420,2.2f,true);
    public static string BaselineLate0926()=>Begin("baseline-late-0926-"+System.DateTime.Now.ToString("HHmmss"),1000,160,2.2f,true);
    public static string Difficulty0926()=>Begin("difficulty-0926-"+System.DateTime.Now.ToString("HHmmss"),0,420,-1.2f,true);
    public static string DeerRun0926()=>Begin("deer-0926-"+SceneManager.GetActiveScene().name,0,150,-1.9f,true);
    public static string Begin(string label,float startDistance,float seconds,float lane,bool endurance=false)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
        if(TimeManager.isGameRunning)throw new InvalidOperationException("A run is already active; begin from a fresh lobby");
        string folder=Record+"/live/"+label;if(Directory.Exists(folder))throw new InvalidOperationException("Preserve previous test label");Directory.CreateDirectory(folder);
        var player=Object.FindFirstObjectByType<PlayerScript>();var canvas=Object.FindFirstObjectByType<CanvasScript>();var route=Object.FindFirstObjectByType<HighwayRoute>();var holdout=Object.FindFirstObjectByType<RestStopHoldout>();
        if(startDistance>0&&route!=null)throw new InvalidOperationException("Highway must traverse from start");
        OpeningStoryUI.Instance?.Skip();UnityEngine.Random.InitState(20260925);canvas.PlayerPressedStartButton();if(!TimeManager.isGameRunning)throw new InvalidOperationException("Start failed");
        if(startDistance>0){Sample(startDistance,out var p,out var f);player.ApplyContinuousRoutePose(p+Vector3.up*.12f,f,0);}
        Time.timeScale=1;TimeManager.timeFactor=1;
        // Route-coverage runs (endurance) only: instant-death hazards stay visible but lose contact, so a fixed-lane bot reaches every section.
        if(endurance)foreach(var hz in Object.FindObjectsByType<HighwayHazard>(FindObjectsSortMode.None)){hz.enabled=false;foreach(var c in hz.GetComponentsInChildren<Collider>())c.enabled=false;}
        if(endurance)foreach(var onc in Object.FindObjectsByType<HighwayOncomingTraffic>(FindObjectsSortMode.None))onc.enabled=false;
        if(endurance)foreach(var hole in Object.FindObjectsByType<RoadPotholeHazard>(FindObjectsSortMode.None)){hole.enabled=false;foreach(var c in hole.GetComponentsInChildren<Collider>())c.enabled=false;}
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var move=typeof(PlayerScript).GetMethod("PlayerMove",flags);var origin=typeof(PlayerScript).GetField("routeLaneOrigin",flags);var right=typeof(PlayerScript).GetField("routeRight",flags);
        var camera=Camera.main;var cameraPosition=camera.transform.localPosition;var cameraRotation=camera.transform.localRotation;float fov=camera.fieldOfView;
        var visual=player.transform.Find("Original");var banner=Object.FindFirstObjectByType<RestStopHoldoutBanner>(FindObjectsInactive.Include);bool bannerSeen=false;string bannerTitle="";var quadrants=new HashSet<int>();float maxDrift=0,maxCameraPosition=0,maxCameraAngle=0,maxFov=0;
        float began=Time.time,nextSample=0,nextShot=1,damageTaken=0;int frame=-1,moves=0,hits=0;bool pinned=false;bool done=false;double stopAt=0;string scene=SceneManager.GetActiveScene().name;
        File.WriteAllText(folder+"/timeline.csv","time,distance,health,x,y,z,holdout,holdout_time,police,camera_position_error,camera_angle_error,drift\n");
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}
                if(done){if(EditorApplication.timeSinceStartup>stopAt){EditorApplication.update-=tick;EditorApplication.isPaused=true;Time.timeScale=1;TimeManager.timeFactor=1;}return;}
                if(EditorApplication.isPaused||frame==Time.frameCount)return;frame=Time.frameCount;float elapsed=Time.time-began;
                // Endurance keeps the shark alive; every drop below the pin is damage a real run would have taken.
                if(endurance&&pinned&&player.currentHealth<100000){damageTaken+=100000-player.currentHealth;hits++;}
                if(endurance&&player.currentHealth>0){player.currentHealth=Mathf.Max(player.currentHealth,100000);pinned=true;}
                if(holdout!=null&&holdout.Active&&banner!=null){var b=banner.transform.Find("HoldoutBanner");if(b!=null&&b.gameObject.activeInHierarchy){bannerSeen=true;bannerTitle=b.Find("Title").GetComponent<TMPro.TMP_Text>().text;}}
                if(holdout!=null&&holdout.Active)
                {
                    // Simulated player: turn (at the enforced 90 deg/s) toward the closest approaching officer.
                    Transform nearest=null;float bestDistance=float.MaxValue;
                    foreach(var cop in holdout.police){if(cop==null||!cop.gameObject.activeInHierarchy)continue;var ec=cop.GetComponent<EnemyScript_space>();if(ec!=null&&ec.IsDead)continue;float dd=(cop.transform.position-holdout.LockedPosition).sqrMagnitude;if(dd<bestDistance){bestDistance=dd;nearest=cop.transform;}}
                    if(nearest!=null){var want=Vector3.ProjectOnPlane(nearest.position-holdout.LockedPosition,Vector3.up);float turn=Vector3.SignedAngle(holdout.AimDirection,want,Vector3.up);holdout.RotateAim(Mathf.Clamp(turn/8f,-1,1),Time.deltaTime*TimeManager.timeFactor);}
                    maxDrift=Mathf.Max(maxDrift,Vector3.Distance(player.transform.position,holdout.LockedPosition));
                    maxCameraPosition=Mathf.Max(maxCameraPosition,Vector3.Distance(cameraPosition,camera.transform.localPosition));maxCameraAngle=Mathf.Max(maxCameraAngle,Quaternion.Angle(cameraRotation,camera.transform.localRotation));maxFov=Mathf.Max(maxFov,Mathf.Abs(fov-camera.fieldOfView));
                    var aim=holdout.AimDirection;if(aim.sqrMagnitude>.01f)quadrants.Add(Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg,360)/90)%4);
                }
                if(elapsed>=nextShot){nextShot+=endurance?6:5;ScreenCapture.CaptureScreenshot(folder+"/frame-"+elapsed.ToString("000")+".png");}
                if(elapsed>=nextSample){nextSample+=.5f;File.AppendAllText(folder+"/timeline.csv",string.Join(",",elapsed,route?.Distance??-1,player.currentHealth,player.transform.position.x,player.transform.position.y,player.transform.position.z,holdout?.Active??false,holdout?.Elapsed??0,holdout?.Spawned??0,maxCameraPosition,maxCameraAngle,maxDrift)+"\n");}
                bool finished=elapsed>=seconds||player.currentHealth<=0||!TimeManager.isGameRunning||(holdout!=null&&holdout.Completed&&Vector3.Dot(player.transform.position-holdout.center.position,holdout.center.forward)>holdout.exitShutter.localPosition.z+12);
                if(finished)
                {
                    var traffic=Object.FindFirstObjectByType<HighwayOncomingTraffic>();
                    var result=new{scene,label,elapsed,startDistance,endurance,attackLevel=37,healthLevel=46,health=player.currentHealth,distance=route?.Distance??-1,forks=route?.forks.Select(f=>new{f.decided,f.bypass,f.rewarded}).ToArray(),trafficLaunched=traffic?.Launched??0,holdoutCompleted=holdout?.Completed??false,holdoutSeconds=holdout?.Elapsed??0,spawned=holdout?.Spawned??0,doors=holdout?.SpawnedByDoor,spawnError=holdout?.MaximumSpawnPositionError??0,maxDrift,maxCameraPosition,maxCameraAngle,maxFov,aimQuadrants=quadrants.OrderBy(q=>q).ToArray(),movementLocked=player.IsStationaryCombat,cameraRestoreError=holdout!=null&&holdout.Completed?Vector3.Distance(cameraPosition,camera.transform.localPosition):-1,moves,bannerSeen,bannerTitle};
                    File.WriteAllText(folder+"/result.json",Json(result));File.WriteAllText(folder+"/damage.json",Json(new{damageTaken,hits,maxHealth=player.MaxHealth,ratio=damageTaken/Mathf.Max(1,player.MaxHealth),distance=route?.Distance??-1,elapsed=Time.time-began}));ScreenCapture.CaptureScreenshot(folder+"/end.png");done=true;stopAt=EditorApplication.timeSinceStartup+.7;return;
                }
                var r=(Vector3)right.GetValue(player);var o=(Vector3)origin.GetValue(player);float current=Vector3.Dot(player.transform.position-o,r);move.Invoke(player,new object[]{Mathf.Clamp((lane-current)*125,-90,90)});moves++;
            }
            catch(Exception error){File.WriteAllText(folder+"/error.txt",error.ToString());EditorApplication.update-=tick;EditorApplication.isPaused=true;Time.timeScale=1;TimeManager.timeFactor=1;}
        };
        EditorApplication.update+=tick;return folder;
    }
}

// Chapter2 verification deliberately does not call the legacy endurance/coverage path above.
public static class HighwayChapter2Playtest
{
    const string Root="outputs/highway-combat-revision-2026-09-27/toll-visual-v1";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly MethodInfo Move=typeof(PlayerScript).GetMethod("PlayerMove",Private);
    static readonly FieldInfo LaneOrigin=typeof(PlayerScript).GetField("routeLaneOrigin",Private),LaneRight=typeof(PlayerScript).GetField("routeRight",Private);
    static readonly FieldInfo Smoothness=typeof(PlayerScript).GetField("movementSmoothness",Private);
    static int run,frame=-1,shot,lastChains,lastPopupCount;
    static int firstRun,maxRun=4;
    static bool cashOnly, followupPair, visualOnly, noFire;
    static HighwayVehicleEnemy focusCar;
    static bool active,started,ended;
    static double readyAt,endedAt;
    static float began,nextSample,initialHp,initialAttack,popupStarted;
    static HighwayChapter2Controller chapter;
    static ChapterProgression progression;
    static PlayerScript player;
    static readonly List<object> damage=new();
    static readonly HashSet<int> seen=new();
    static readonly Dictionary<int,float> priorDistances=new();
    static readonly Dictionary<int,float> observedSpeeds=new();
    static int wrongDirection,overlapFrames,fullWallFrames,smokeOverflow,hiddenBodyFrames;
    static float farthestVisibleVehicle;
    static float maximumRushSpeed,postRushSpeed;
    static int rushStateErrors,labelWithoutBody,warningComponents;
    static int damageWithoutVehicleOverlap;
    static int initialProjectileCount;
    static readonly Dictionary<int,MeshRenderer[]> vehicleRenderers=new();
    static float minimumGap;
    static string Label=>run==0?"first-accident":new[]{"jam-hipass","jam-cash","open-hipass","open-cash"}[run-1];
    static string Folder=>Root+"/"+Label;
    static readonly float[] Stations={12,30,65,110,205,260,405,460,490,520,550,650,900,1195,1250,1358,1418,1500,1605,1680,1780,1888,1922,2040,2310};
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    static void Write(string path,object value)=>File.WriteAllText(path,Json(value));
    static float S(string key)=>HighwayChapter2Data.Value(key);
    static string RegistryPath=>"Software\\Unity\\UnityEditor\\"+PlayerSettings.companyName+"\\"+PlayerSettings.productName;
    static string EncodeRegistryValue(Microsoft.Win32.RegistryValueKind kind,object value)
    {
        byte[] bytes;
        if(kind==Microsoft.Win32.RegistryValueKind.DWord)bytes=BitConverter.GetBytes(unchecked((int)Convert.ToInt64(value)));
        else if(kind==Microsoft.Win32.RegistryValueKind.QWord)bytes=BitConverter.GetBytes(value is ulong unsigned?unchecked((long)unsigned):Convert.ToInt64(value));
        else if(value is byte[] raw)bytes=raw;
        else if(value is string[] multiple)bytes=System.Text.Encoding.Unicode.GetBytes(string.Join("\0",multiple)+"\0");
        else bytes=System.Text.Encoding.Unicode.GetBytes(Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture)??"");
        return Convert.ToBase64String(bytes);
    }
    static string RegistrySnapshot()
    {
        using var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath,false);
        if(key==null)throw new Exception("PlayerPrefs registry folder is unavailable: "+RegistryPath);
        return string.Join("\n",key.GetValueNames().OrderBy(n=>n,StringComparer.Ordinal).Select(n=>
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(n))+"\t"+(int)key.GetValueKind(n)+"\t"+
            EncodeRegistryValue(key.GetValueKind(n),key.GetValue(n,null,Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames))));
    }
    public static object CompareOriginal()
    {
        string baseline="outputs/highway-combat-revision-2026-09-27/play-v1/registry-before.txt";
        return new{registryByteIdentical=File.ReadAllText(baseline,System.Text.Encoding.UTF8)==RegistrySnapshot(),coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel")};
    }
    static void RestoreRegistry(string snapshot)
    {
        using var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath,true);
        if(key==null)throw new Exception("PlayerPrefs registry folder is unavailable");
        var rows=snapshot.Split('\n').Where(s=>s.Length>0).Select(s=>s.Split('\t')).ToArray();
        var names=new HashSet<string>(rows.Select(r=>System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(r[0]))),StringComparer.Ordinal);
        foreach(string name in key.GetValueNames())
            if(!names.Contains(name))
            {
                var match=System.Text.RegularExpressions.Regex.Match(name,@"^(.*)_h\d+$");
                if(match.Success)PlayerPrefs.DeleteKey(match.Groups[1].Value);
            }
        PlayerPrefs.Save();
        foreach(string name in key.GetValueNames())if(!names.Contains(name))key.DeleteValue(name,false);
        foreach(var row in rows)
        {
            string name=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(row[0]));var kind=(Microsoft.Win32.RegistryValueKind)int.Parse(row[1]);byte[] bytes=Convert.FromBase64String(row[2]);object value;
            if(kind==Microsoft.Win32.RegistryValueKind.DWord)value=BitConverter.ToInt32(bytes,0);
            else if(kind==Microsoft.Win32.RegistryValueKind.QWord)value=BitConverter.ToInt64(bytes,0);
            else if(kind==Microsoft.Win32.RegistryValueKind.MultiString)value=System.Text.Encoding.Unicode.GetString(bytes).TrimEnd('\0').Split('\0');
            else if(kind==Microsoft.Win32.RegistryValueKind.String||kind==Microsoft.Win32.RegistryValueKind.ExpandString)value=System.Text.Encoding.Unicode.GetString(bytes);
            else value=bytes;
            key.SetValue(name,value,kind);
        }
        key.Flush();
    }
    static void SelectStage(int value)
    {
        var type=typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage");
        type.GetMethod("SelectStage",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{value});
    }
    static void Stats(bool entry)
    {
        for(int i=1;i<=Enum.GetValues(typeof(UpgradeStatManager.UpgradeType)).Length;i++)PlayerPrefs.SetInt("upgrade_lv_"+i,
            i==1?HighwayChapter2Data.Count(entry?"entryAttLevel":"clearAttLevel"):
            i==2?HighwayChapter2Data.Count(entry?"entryHpLevel":"clearHpLevel"):
            i==3?HighwayChapter2Data.Count(entry?"entrySpeedLevel":"clearSpeedLevel"):0);
        PlayerPrefs.SetInt("TutorialDone",1);PlayerPrefs.Save();
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",1);
    }
    public static object Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().name!="HighWay"||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean HighWay Edit Mode required");
        if(Directory.Exists(Root)&&Directory.EnumerateFileSystemEntries(Root).Any())throw new Exception("Preserve earlier cohort; use a fresh output folder");
        Directory.CreateDirectory(Root);PlayerPrefs.Save();
        File.WriteAllText(Root+"/registry-before.txt",RegistrySnapshot(),new System.Text.UTF8Encoding(false));
        File.WriteAllText(Root+"/registry-path.txt",RegistryPath);
        var result=ChapterPlaytestPreferences.SnapshotAt(Root+"/before-prefs.tsv");
        var extras=new[]{"TutorialDone"}.Concat(Enumerable.Range(1,5).SelectMany(c=>new[]{ChapterUpgradeService.LevelKey(c),ChapterUpgradeService.OwnedKey(c)}));
        File.WriteAllLines(Root+"/extra-prefs.tsv",extras.Select(k=>k+"\t"+PlayerPrefs.HasKey(k)+"\t"+PlayerPrefs.GetInt(k)));
        File.WriteAllText(Root+"/start-stage.txt",SessionState.GetInt("NoryangjinMapTool.TestStartStage",0).ToString());
        File.WriteAllText(Root+"/start-scene.txt",EditorSceneManager.playModeStartScene==null?"":AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SelectStage(2);Stats(true);
        Write(Root+"/conditions.json",new{entryLevels=new[]{S("entryAttLevel"),S("entryHpLevel"),S("entrySpeedLevel")},
            clearLevels=new[]{S("clearAttLevel"),S("clearHpLevel"),S("clearSpeedLevel")},test9999=false,fastLateral=false,timeScale=1,
            invulnerability=false,healthPin=false,teleport=false,collisions=true,driver="normal PlayerMove input once per rendered frame; popup Select uses the production button handler"});
        return result;
    }
    public static object Begin()
    {
        if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="HighWay"||!File.Exists(Root+"/registry-before.txt")||Directory.Exists(Root+"/first-accident"))throw new Exception("Fresh prepared Highway Play Mode required");
        active=true;cashOnly=followupPair=false;firstRun=run=0;maxRun=4;started=ended=false;player=null;frame=-1;EditorApplication.update+=Tick;return new{active};
    }
    public static object BeginFollowup(){var result=Begin();followupPair=true;return result;}
    public static object BeginVisual(){BeginJam();visualOnly=true;maxRun=run;return new{active,scope="Native visual review through750m; not a full clear"};}
    public static object BeginNoFire(){BeginOpenCash();noFire=true;maxRun=run;return new{active,scope="No-fire weaving attempt; expected to fail before clearing"};}
    public static object PrepareJam(){var result=Prepare();Stats(false);return result;}
    public static object BeginJam()
    {
        if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="HighWay"||!File.Exists(Root+"/registry-before.txt")||Directory.Exists(Root+"/jam-hipass"))throw new Exception("Fresh prepared Highway Play Mode required");
        active=true;cashOnly=false;firstRun=run=1;maxRun=2;started=ended=false;player=null;frame=-1;EditorApplication.update+=Tick;return new{active,runs=2};
    }
    public static object BeginCash()
    {
        if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="HighWay"||!File.Exists(Root+"/registry-before.txt")||Directory.Exists(Root+"/jam-cash"))throw new Exception("Fresh prepared Highway Play Mode required");
        active=true;cashOnly=true;firstRun=run=2;maxRun=4;started=ended=false;player=null;frame=-1;EditorApplication.update+=Tick;return new{active,runs=2};
    }
    public static object BeginOpenCash()
    {
        if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="HighWay"||!File.Exists(Root+"/registry-before.txt")||Directory.Exists(Root+"/open-cash"))throw new Exception("Fresh prepared Highway Play Mode required");
        active=true;cashOnly=false;firstRun=run=4;maxRun=4;started=ended=false;player=null;frame=-1;EditorApplication.update+=Tick;return new{active,runs=1};
    }
    public static object Stop()
    {
        active=false;EditorApplication.update-=Tick;return new{stopped=true};
    }
    static void Tick()
    {
        if(!active||!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.isCompiling||frame==Time.frameCount)return;
        frame=Time.frameCount;
        try
        {
            if(ended)
            {
                if(EditorApplication.timeSinceStartup-endedAt<2)return;
                if(run>=maxRun)
                {
                    active=false;EditorApplication.update-=Tick;ScreenCapture.CaptureScreenshot(Folder+"/result-panel.png");
                    Write(Root+"/complete.json",new{finished=true,normalRoutes=visualOnly?0:cashOnly||followupPair?2:maxRun-Mathf.Max(1,firstRun)+1,firstAccident=firstRun==0?1:0,visualOnly});EditorApplication.delayCall+=()=>EditorApplication.isPaused=true;return;
                }
                player.DamageTaken-=Damage;run=followupPair&&run==1?4:run+(cashOnly?2:1);Stats(false);player=null;started=ended=false;SceneManager.LoadScene("HighWay");return;
            }
            if(player==null)
            {
                if(SceneManager.GetActiveScene().name!="HighWay")throw new Exception("Unexpected scene");
                player=Object.FindFirstObjectByType<PlayerScript>();chapter=Object.FindFirstObjectByType<HighwayChapter2Controller>();progression=Object.FindFirstObjectByType<ChapterProgression>();
                if(player==null||chapter==null||progression==null)return;readyAt=EditorApplication.timeSinceStartup+1.5;return;
            }
            if(!started)
            {
                if(EditorApplication.timeSinceStartup<readyAt)return;
                OpeningStoryUI.Instance?.Skip();UnityEngine.Random.InitState(202609270+run);
                if(noFire){player.canShoot=false;foreach(var weapon in player.GetComponentsInChildren<WeaponScript>()){weapon.CancelInvoke("ShootBullet");weapon.enabled=false;}}
                progression.nextScene="";Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
                if(!TimeManager.isGameRunning)return;
                if(noFire)player.canShoot=false;
                var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.Close();tutorial.enabled=false;}
                foreach(var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))harness.enabled=false;
                Time.timeScale=1;TimeManager.timeFactor=1;began=Time.time;initialHp=player.currentHealth;initialAttack=player.ResolvedAttackDamage;
                if(initialAttack>=9999||initialHp>=9999)throw new Exception("Unexpected test-power stats");
                shot=lastChains=lastPopupCount=0;nextSample=0;damage.Clear();seen.Clear();priorDistances.Clear();observedSpeeds.Clear();
                wrongDirection=overlapFrames=fullWallFrames=smokeOverflow=hiddenBodyFrames=rushStateErrors=labelWithoutBody=warningComponents=damageWithoutVehicleOverlap=0;farthestVisibleVehicle=maximumRushSpeed=postRushSpeed=0;focusCar=null;vehicleRenderers.Clear();minimumGap=float.PositiveInfinity;player.DamageTaken+=Damage;
                initialProjectileCount=player.GetComponentsInChildren<WeaponScript>().Sum(w=>w.TotalProjectilesSpawned);
                Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/timeline.csv","time,distance,hp,attack,lane,branch,hazard,cars\n");
                started=true;return;
            }
            float elapsed=Time.time-began, pd=chapter.Route.Distance;
            var cars=chapter.Vehicles.Where(v=>v.Active&&!v.Dead).ToArray();
            if(noFire){player.canShoot=false;foreach(var weapon in player.GetComponentsInChildren<WeaponScript>()){weapon.CancelInvoke("ShootBullet");weapon.enabled=false;}}
            foreach(var car in cars)
            {
                seen.Add(car.GetInstanceID());chapter.Route.Sample(car.Distance,chapter.UsesBranch(car),out _,out var forward);
                if(!vehicleRenderers.TryGetValue(car.GetInstanceID(),out var carRenderers))vehicleRenderers[car.GetInstanceID()]=carRenderers=car.body.GetComponentsInChildren<MeshRenderer>(true);
                if(carRenderers.Any(r=>r.forceRenderingOff))hiddenBodyFrames++;
                else farthestVisibleVehicle=Mathf.Max(farthestVisibleVehicle,car.Distance-pd);
                if(car.HealthLabelVisible&&(!car.body.gameObject.activeInHierarchy||carRenderers.All(r=>r.forceRenderingOff)||Vector3.Distance(car.transform.position,player.transform.position)>S("healthLabelDistance")+2))labelWithoutBody++;
                if(car.speed>0&&Vector3.Dot(car.transform.forward,forward)>-.85f)wrongDirection++;
                if(priorDistances.TryGetValue(car.GetInstanceID(),out float old))
                {
                    if(car.Distance>old+.01f)wrongDirection++;
                    observedSpeeds[car.GetInstanceID()]=Mathf.Clamp((old-car.Distance)/Mathf.Max(.001f,Time.deltaTime),0,car.speed);
                }
                priorDistances[car.GetInstanceID()]=car.Distance;
            }
            for(int i=0;i<cars.Length;i++)for(int j=i+1;j<cars.Length;j++)
            {
                if(chapter.UsesBranch(cars[i])!=chapter.UsesBranch(cars[j]))continue;
                var a=cars[i].GetComponent<BoxCollider>();var b=cars[j].GetComponent<BoxCollider>();
                if(a.enabled&&b.enabled&&Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out float penetration)&&penetration>.08f)
                {overlapFrames++;if(overlapFrames<=10)File.AppendAllText(Folder+"/overlap-samples.jsonl",Json(new{time=Time.time-began,a=cars[i].name,b=cars[j].name,firstDistance=cars[i].Distance,secondDistance=cars[j].Distance,penetration})+"\n");}
                if(Mathf.Abs(cars[i].Lane-cars[j].Lane)<1)minimumGap=Mathf.Min(minimumGap,Mathf.Abs(cars[i].Distance-cars[j].Distance)-cars[i].HalfLength-cars[j].HalfLength);
            }
            var close=cars.Where(c=>c.speed>0&&Mathf.Abs(c.Distance-pd)<c.HalfLength+S("playerRadius")).Select(c=>new Vector2(c.Lane,c.HalfWidth)).ToArray();
            if(close.Length>0&&!HighwayChapter2Rules.LeavesPassage(close[0].x,close[0].y,close.Skip(1).ToArray(),S("laneWidth"),S("playerRadius")))fullWallFrames++;
            if(chapter.feedback.ActiveExplosions>HighwayChapter2Data.Count("fxMaximum"))smokeOverflow++;
            if(chapter.RushActive)maximumRushSpeed=Mathf.Max(maximumRushSpeed,player.ForwardMoveSpeed);
            if(chapter.RoadChoice==1&&pd>S("mergeAt")+3){postRushSpeed=player.ForwardMoveSpeed;if(chapter.ui.RushShown||Mathf.Abs(postRushSpeed-S("runSpeed"))>.01f)rushStateErrors++;}
            if(chapter.ui.Pending)
            {
                if(chapter.ui.PopupCount!=lastPopupCount){lastPopupCount=chapter.ui.PopupCount;popupStarted=Time.realtimeSinceStartup;ScreenCapture.CaptureScreenshot(Folder+"/popup-"+lastPopupCount+".png");}
                if(Time.realtimeSinceStartup-popupStarted>=1.2f)chapter.ui.Select(chapter.ui.TollChoice?(run-1)%2:run>=3?1:0);
            }
            if(chapter.TankerChains>lastChains){lastChains=chapter.TankerChains;ScreenCapture.CaptureScreenshot(Folder+"/tanker-chain-"+lastChains+".png");}
            if(shot<Stations.Length&&pd>=Stations[shot]){ScreenCapture.CaptureScreenshot(Folder+"/d-"+((int)Stations[shot]).ToString("0000")+".png");shot++;}
            float lane=Vector3.Dot(player.transform.position-(Vector3)LaneOrigin.GetValue(player),(Vector3)LaneRight.GetValue(player));
            if(elapsed>=nextSample)
            {
                nextSample=elapsed+.5f;File.AppendAllText(Folder+"/timeline.csv",string.Join(",",elapsed,pd,player.currentHealth,player.ResolvedAttackDamage,lane,chapter.Route.OnBypass,chapter.PrimaryHazard,cars.Length)+"\n");
                warningComponents=Mathf.Max(warningComponents,chapter.GetComponentsInChildren<EnemyContactWarning>(true).Length);
                Write(Root+"/status.json",new{run,label=Label,elapsed,distance=pd,hp=player.currentHealth,attack=player.ResolvedAttackDamage,
                    seen=seen.Count,wrongDirection,overlapFrames,fullWallFrames,hiddenBodyFrames,farthestVisibleVehicle,chapter.RoadChoice,chapter.ExitChoice,chapter.LogsReleased,chapter.RandomBonuses,chapter.VehicleProjectileHits,speed=player.ForwardMoveSpeed,rush=chapter.ui.RushShown});
            }
            bool firstPassed=run==0&&pd>=S("crash1")+45;
            bool visualDone=visualOnly&&pd>=750;
            if(firstPassed||visualDone||player.currentHealth<=0||progression.Completed||!TimeManager.isGameRunning||elapsed>350)
            {
                var first=chapter.Vehicles.Where(v=>v.group==1&&v.accidentFront).Select(v=>new{v.kind,dead=v.Dead,hp=v.Combat.CurrentHealth}).ToArray();
                string outcome=firstPassed?"first-accident-passed":visualDone?"visual-range-covered":progression.Completed?"clear":player.currentHealth<=0?"death":"timeout";
                Write(Folder+"/result.json",new{run,label=Label,outcome,elapsed,distance=pd,initialHp,initialAttack,health=player.currentHealth,attack=player.ResolvedAttackDamage,
                    damage,seenCars=seen.Count,wrongDirection,overlapFrames,mandatoryWallFrames=fullWallFrames,minimumGap,smokeOverflow,hiddenBodyFrames,farthestVisibleVehicle,noFire,chapter.VehicleProjectileHits,initialProjectileCount,projectilesSpawned=player.GetComponentsInChildren<WeaponScript>().Sum(w=>w.TotalProjectilesSpawned)-initialProjectileCount,maximumRushSpeed,postRushSpeed,rushStateErrors,labelWithoutBody,warningComponents,damageWithoutVehicleOverlap,
                    chapter.RoadChoice,chapter.ExitChoice,chapter.GrantedBonus,chapter.LogsReleased,chapter.TankerChains,chapter.RandomBonuses,
                    popupCount=chapter.ui.PopupCount,chapter.ui.TimeoutCount,first,events=chapter.Timeline.ToArray()});
                ScreenCapture.CaptureScreenshot(Folder+"/end.png");ended=true;endedAt=EditorApplication.timeSinceStartup;return;
            }
            Steer(cars);
        }
        catch(Exception e){File.WriteAllText(Root+"/error.txt",e.ToString());active=false;EditorApplication.update-=Tick;EditorApplication.isPaused=true;}
    }
    static void Damage(float amount,PlayerDamageCause cause)
    {
        var body=player.GetComponent<Collider>();var contacts=chapter.Vehicles.Where(v=>v.Active&&!v.Dead&&v.ContactCollider!=null&&v.ContactCollider.enabled&&v.ContactCollider.bounds.Intersects(body.bounds)).Select(v=>v.name).ToArray();
        if((cause==PlayerDamageCause.EnemyContact||cause==PlayerDamageCause.Traffic)&&contacts.Length==0)damageWithoutVehicleOverlap++;
        damage.Add(new{time=Time.time-began,distance=chapter.Route.Distance,amount,cause=cause.ToString(),hp=player.currentHealth,contacts});
    }
    static void Steer(HighwayVehicleEnemy[] cars)
    {
        if(chapter.ui.Pending)return;
        float pd=chapter.Route.Distance;
        var origin=(Vector3)LaneOrigin.GetValue(player);var side=(Vector3)LaneRight.GetValue(player);
        float current=Vector3.Dot(player.transform.position-origin,side);
        if(chapter.ExitChoice<0&&pd>=S("tollAt")-S("popupAhead")&&pd<S("tollAt"))
        {float exitLane=(run-1)%2==0?-S("laneWidth"):S("laneWidth");Move.Invoke(player,new object[]{Mathf.Clamp((exitLane-current)*125,-90,90)});return;}
        if(!noFire&&(chapter.PrimaryHazard==HighwayPrimaryHazard.None||chapter.PrimaryHazard==HighwayPrimaryHazard.Swarm))
        {
            float nearestRow=float.PositiveInfinity;HighwayVehicleEnemy[] row=null;
            foreach(var group in chapter.Vehicles.Where(v=>v.Spawned&&(v.group>=100||v.group==6)).GroupBy(v=>v.group))
            {
                var anchor=group.FirstOrDefault(v=>v.Active&&!v.Dead);if(anchor==null||chapter.UsesBranch(anchor)!=chapter.Route.OnBypass)continue;
                float shift=anchor.Distance-anchor.station;
                // Include already-destroyed front members: continuing shots can open the next row
                // before we reach it. Selecting only living cars drove the bot out of that gap.
                var ahead=group.Where(v=>v.station+shift+v.HalfLength+1>pd&&v.station+shift-pd<100).ToArray();if(ahead.Length==0)continue;
                float distance=ahead.Min(v=>v.station+shift);if(distance>=nearestRow)continue;
                nearestRow=distance;row=ahead.Where(v=>v.station+shift-distance<10).ToArray();
            }
            if(row!=null)
            {
                var alive=row.Where(v=>v.Active&&!v.Dead).ToArray();
                var gaps=new[]{-S("laneWidth"),0,S("laneWidth")}.Where(lane=>alive.All(v=>Mathf.Abs(lane-v.Lane)>=v.HalfWidth+S("playerRadius")+.05f)).OrderBy(lane=>Mathf.Abs(lane-current)).ToArray();
                focusCar=alive.OrderBy(v=>v.Combat.CurrentHealth).ThenBy(v=>Mathf.Abs(v.Lane-current)).FirstOrDefault();
                float lane=gaps.Length>0?gaps[0]:focusCar.Lane;
                Move.Invoke(player,new object[]{Mathf.Clamp((lane-current)*125,-90,90)});return;
            }
        }
        var target=cars.Where(v=>chapter.UsesBranch(v)==chapter.Route.OnBypass&&v.Distance-pd>0&&v.Distance-pd<60)
            .OrderBy(v=>v.accidentFront&&v.kind==HighwayVehicleKind.Taxi?0:v.kind==HighwayVehicleKind.Tanker?1:2).ThenBy(v=>v.Distance-pd).FirstOrDefault();
        float desired=target!=null?target.Lane:0;
        bool openCorridor=false;
        HighwayVehicleEnemy gateTarget=null;
        for(int group=1;group<=4;group++)
        {
            float station=S("crash"+group);
            if(pd<station-65||pd>station+S("crashTail"))continue;
            var front=chapter.Vehicles.Where(v=>v.group==group&&v.accidentFront&&
                (v.routeChoice==HighwayVehicleRoute.Common||v.routeChoice==HighwayVehicleRoute.Jam&&chapter.RoadChoice==0)).ToArray();
            var weak=front.FirstOrDefault(v=>v.kind==HighwayVehicleKind.Taxi);
            if(weak==null)continue;
            desired=-S("laneWidth");openCorridor=true;gateTarget=weak;target=weak;break;
        }
        float Cost(float lane)
        {
            float cost=Mathf.Abs(lane-desired)*(openCorridor?5000:.3f)+Mathf.Abs(lane-current)*.4f;
            float maxStep=90*player.moveSensitivity/Mathf.Max(.001f,player.moveSensitivity_Devision)*player.LateralSpeedMultiplier;
            float travelRate=maxStep*Mathf.Clamp01(Time.deltaTime*(float)Smoothness.GetValue(player)*TimeManager.timeFactor)/Mathf.Max(.001f,Time.deltaTime)*.9f;
            float worstRisk=0;
            foreach(var car in cars)
            {
                if(chapter.UsesBranch(car)!=chapter.Route.OnBypass)continue;
                float ahead=car.Distance-pd;
                if(ahead< -car.HalfLength-2||ahead>65)continue;
                // Editor callbacks can sample before the current game update. Use the configured
                // closing-speed bound instead of mistaking a zero-delta sample for a parked car.
                float carSpeed=car.speed;
                float meeting=Mathf.Max(0,ahead-car.HalfLength-S("playerRadius"))/Mathf.Max(1,player.ForwardMoveSpeed+carSpeed);
                float rate=player.GetComponentsInChildren<WeaponScript>().Where(w=>w.bulletKind==BulletKind.Water).Select(w=>w.fireRate).DefaultIfEmpty(1).First();
                float shots=Mathf.Ceil(car.Combat.CurrentHealth/Mathf.Max(1,player.ResolvedAttackDamage));
                float range=BulletScript.BaseMissileSpeed*BulletScript.CurrentMissileDuration;
                float gap=Mathf.Max(0,ahead-car.HalfLength);
                float killTime=Mathf.Max(0,shots-1)/Mathf.Max(.1f,rate)+Mathf.Max(0,gap-range)/Mathf.Max(1,player.ForwardMoveSpeed+carSpeed)
                    +Mathf.Min(gap,range)/Mathf.Max(1,BulletScript.BaseMissileSpeed+carSpeed)+.35f;
                bool shootable=!noFire&&car==target&&Mathf.Abs(lane-car.Lane)<.3f&&killTime+.2f<meeting;
                if(shootable||car==gateTarget)continue;
                for(float future=0;future<=2.6f;future+=.13f)
                {
                    float playerD=Mathf.Min(chapter.Route.length,pd+player.ForwardMoveSpeed*future),carD=Mathf.Max(0,car.Distance-carSpeed*future);
                    bool playerBranch=chapter.Route.forks.Any(f=>f.decided&&f.bypass&&playerD>=f.start&&playerD<=f.end);
                    bool carBranch=car.routeChoice==HighwayVehicleRoute.Open||car.routeChoice==HighwayVehicleRoute.Common&&chapter.ExitChoice==1&&carD>=S("tollAt");
                    chapter.Route.Sample(playerD,playerBranch,out var pp,out var pf);
                    chapter.Route.Sample(carD,carBranch,out var cp,out var cf);
                    pp+=Vector3.Cross(Vector3.up,pf)*Mathf.MoveTowards(current,lane,travelRate*future);
                    cp+=Vector3.Cross(Vector3.up,cf)*car.Lane;
                    var delta=pp-cp;
                    float along=Vector3.Dot(delta,cf),lateral=Vector3.Dot(delta,Vector3.Cross(Vector3.up,cf));
                    if(Mathf.Abs(along)<car.HalfLength+S("playerRadius")+.35f&&Mathf.Abs(lateral)<car.HalfWidth+S("playerRadius")+.3f)
                    {worstRisk=Mathf.Max(worstRisk,10000/(future+.15f));break;}
                }
            }
            cost+=worstRisk;
            if(!chapter.Route.OnBypass)
            {
                if(chapter.hole!=null&&pd>S("holeAt")-36&&pd<S("holeAt")+5&&Mathf.Abs(lane-HighwayChapter2Data.Lane(S("holeLane")))<2)cost+=1000000;
                if(pd>S("workAt")-36&&pd<S("workAt")+55&&Mathf.Abs(lane-HighwayChapter2Data.Lane(S("workLane")))<2.3f)cost+=30000;
                var log=chapter.singleLog;
                if(log!=null&&log.gameObject.activeInHierarchy)
                {float ahead=Vector3.Dot(log.position-player.transform.position,player.transform.forward);float ll=Vector3.Dot(log.position-origin,side);if(ahead> -3&&ahead<40&&Mathf.Abs(lane-ll)<S("logRadius")+.8f)cost+=25000;}
                var deer=chapter.deer!=null?chapter.deer.deer:null;
                if(deer!=null&&deer.gameObject.activeInHierarchy)
                {float ahead=Vector3.Dot(deer.position-player.transform.position,player.transform.forward);float dl=Vector3.Dot(deer.position-origin,side);if(ahead> -3&&ahead<35&&Mathf.Abs(lane-dl)<2.4f)cost+=20000;}
                foreach(var cone in chapter.thrownCones)
                    if(cone.gameObject.activeInHierarchy&&Vector3.Distance(cone.position,player.transform.position)<16&&Mathf.Abs(lane-Vector3.Dot(cone.position-origin,side))<1.8f)cost+=15000;
            }
            return cost;
        }
        desired=new[]{-S("laneWidth"),0,S("laneWidth")}.OrderBy(Cost).First();
        Move.Invoke(player,new object[]{Mathf.Clamp((desired-current)*125,-90,90)});
    }
    public static object Restore()=>RestoreFrom(Root,Root+"/restored.json");
    public static object RestoreOriginal()=>RestoreFrom("outputs/highway-combat-revision-2026-09-27/play-v1","outputs/highway-combat-revision-2026-09-27/original-restored.json");
    static object RestoreFrom(string folder,string receipt)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        Stop();var restored=ChapterPlaytestPreferences.RestoreAt(folder+"/before-prefs.tsv");
        foreach(string line in File.ReadAllLines(folder+"/extra-prefs.tsv"))
        {var p=line.Split('\t');if(bool.Parse(p[1]))PlayerPrefs.SetInt(p[0],int.Parse(p[2]));else PlayerPrefs.DeleteKey(p[0]);}
        PlayerPrefs.Save();SelectStage(int.Parse(File.ReadAllText(folder+"/start-stage.txt")));
        string scene=File.ReadAllText(folder+"/start-scene.txt");
        EditorSceneManager.playModeStartScene=scene.Length==0?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(scene);
        string before=File.ReadAllText(folder+"/registry-before.txt",System.Text.Encoding.UTF8);RestoreRegistry(before);
        string after=RegistrySnapshot();File.WriteAllText(folder+"/registry-restored.txt",after,new System.Text.UTF8Encoding(false));
        bool identical=File.ReadAllBytes(folder+"/registry-before.txt").SequenceEqual(File.ReadAllBytes(folder+"/registry-restored.txt"));
        if(!identical)throw new Exception("PlayerPrefs byte comparison failed");
        var result=new{restored,registryByteIdentical=identical,startScene=AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            sceneClean=!SceneManager.GetActiveScene().isDirty,timeScale=Time.timeScale};
        Write(receipt,result);return result;
    }
}
