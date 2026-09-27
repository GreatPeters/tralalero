using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class RestStopKoreanPlaytest
{
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
