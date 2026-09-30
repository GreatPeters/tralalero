using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class NoryangjinInteriorV2Review
{
    const string Root="outputs/noryangjin-interior-v2-2026-09-28";
    const string Preview="tmp/image-previews/noryangjin-interior-v2-2026-09-28";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    public static object HealthReadouts()=>Object.FindObjectsByType<NoryangjinMerchant>(FindObjectsInactive.Include,FindObjectsSortMode.None).Take(3).Select(m=>new{m.name,position=m.transform.position.ToString(),canvases=m.GetComponentsInChildren<Canvas>(true).Select(c=>new{position=c.transform.position.ToString(),local=c.transform.localPosition.ToString(),scale=c.transform.localScale.ToString(),text=c.GetComponentInChildren<TMPro.TMP_Text>()?.transform.position.ToString()})}).ToArray();
    public static object Prepare()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        string snapshot=Root+"/before/playerprefs.tsv";
        if(!File.Exists(snapshot))
        {
            ChapterPlaytestPreferences.SnapshotAt(snapshot);
            File.WriteAllText(Root+"/before/start-scene.txt",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            File.WriteAllText(Root+"/before/stage.txt",SessionState.GetInt("NoryangjinMapTool.TestStartStage",0).ToString());
        }
        var selection=typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage");
        selection.GetMethod("SelectStage",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{1});
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);
        return new{ready=true};
    }
    public static object Restore()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
        var result=ChapterPlaytestPreferences.RestoreAt(Root+"/before/playerprefs.tsv");
        var selection=typeof(ChapterPlaytestPreferences).Assembly.GetType("NoryangjinMapToolTestStartStage").GetMethod("SelectStage",BindingFlags.Static|BindingFlags.NonPublic);
        selection.Invoke(null,new object[]{0});EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(File.ReadAllText(Root+"/before/start-scene.txt"));
        selection.Invoke(null,new object[]{int.Parse(File.ReadAllText(Root+"/before/stage.txt"))});
        return result;
    }
    public static object VerifyRestored()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var mismatches=new List<string>();int count=0;
        foreach(var line in File.ReadAllLines(Root+"/before/playerprefs.tsv"))
        {
            count++;var f=line.Split('\t');bool existed=bool.Parse(f[2]);
            if(PlayerPrefs.HasKey(f[0])!=existed){mismatches.Add(f[0]);continue;}if(!existed)continue;
            string current=f[1]=="int"?PlayerPrefs.GetInt(f[0]).ToString(System.Globalization.CultureInfo.InvariantCulture):f[1]=="float"?PlayerPrefs.GetFloat(f[0]).ToString("R",System.Globalization.CultureInfo.InvariantCulture):Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString(f[0])));
            if(current!=f[3])mismatches.Add(f[0]);
        }
        var result=new{count,mismatches,snapshotMatches=mismatches.Count==0,stageMatches=SessionState.GetInt("NoryangjinMapTool.TestStartStage",0)==int.Parse(File.ReadAllText(Root+"/before/stage.txt")),startSceneMatches=AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)==File.ReadAllText(Root+"/before/start-scene.txt"),scene=EditorSceneManager.GetActiveScene().path,dirty=EditorSceneManager.GetActiveScene().isDirty};
        File.WriteAllText(Root+"/restoration.json",Json(result));return result;
    }
    public static object Capture()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");
        string folder=Preview+"/native-"+DateTime.Now.ToString("HHmmss");Directory.CreateDirectory(folder);
        var p=Object.FindFirstObjectByType<PlayerScript>();var d=Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        var canvas=Object.FindFirstObjectByType<CanvasScript>();var cam=GameObject.Find("MapTool_Camera").GetComponent<Camera>();
        var hold=new GameObject("Visual review stationary anchor");
        var owner=p.GetType().GetField("stationaryCombatOwner",Hidden);var point=p.GetType().GetField("stationaryCombatPosition",Hidden);
        int stage=-1,frame=-1;double due=EditorApplication.timeSinceStartup+1.5;bool queued=false,started=false,awaitingShot=false;string file="";
        var records=new List<object>();
        string[] names={"aisle","turret","merchants","side-stalls"};
        Vector3[] at={new(124.3f,.22f,4),new(124.3f,.22f,-185),new(124.3f,.22f,3),new(124.3f,.22f,-50)};
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(!EditorApplication.isPlaying||p==null){EditorApplication.update-=tick;return;}
                if(Time.frameCount==frame)return;frame=Time.frameCount;
                if(EditorApplication.timeSinceStartup<due)return;
                if(awaitingShot)return;
                if(!started)
                {
                    Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();
                    if(!TimeManager.isGameRunning)canvas.PlayerPressedStartButton();
                    if(!TimeManager.isGameRunning){due=EditorApplication.timeSinceStartup+.5;return;}
                    started=true;Time.timeScale=1;
                    foreach(var weapon in Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None)){weapon.StopAllCoroutines();weapon.enabled=false;}
                    foreach(var bullet in Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None))bullet.gameObject.SetActive(false);
                    foreach(var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))harness.enabled=false;
                    var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.Close();tutorial.enabled=false;}
                    foreach(var ev in d.GetComponentsInChildren<NoryangjinRevampEvent>(true))ev.enabled=false;
                    typeof(NoryangjinMarketBranch).GetProperty("Selected").SetValue(d.Branch,true);
                    typeof(NoryangjinMarketBranch).GetProperty("Outside").SetValue(d.Branch,false);
                    due=EditorApplication.timeSinceStartup+.6;return;
                }
                if(queued)
                {
                    if(!File.Exists(file)){return;}
                    records.Add(new{name=names[stage],file,kind="native visual fixture; stationary player and disabled firing",position=p.transform.position.ToString(),camera=cam.transform.position.ToString(),fov=cam.fieldOfView});
                    queued=false;
                    if(stage==names.Length-1)
                    {
                        File.WriteAllText(folder+"/captures.json",Json(records));File.WriteAllText(Root+"/latest-capture.txt",folder);
                        EditorApplication.update-=tick;EditorApplication.isPaused=true;Time.timeScale=1;return;
                    }
                }
                if(!queued)
                {
                    stage++;
                    p.ApplyContinuousRoutePose(at[stage],Vector3.back,0);owner.SetValue(p,hold);point.SetValue(p,p.transform.position);
                    var stable=cam.GetComponent<StableGameplayCamera>();stable.yawRelativeOffset=new Vector3(.12f,5.8f,-17);stable.yawRelativeRotation=Quaternion.Euler(8,0,0);
                    if(stage==1)
                    {
                        var turret=d.GetComponentInChildren<NoryangjinTurretEvent>();
                        turret.turret.Arm();turret.turret.transform.position=p.transform.position+p.transform.forward*8;
                        turret.turret.restLocalWithMotion=turret.turret.transform.localPosition;
                    }
                    if(stage==2)
                    {
                        var rush=d.GetComponentsInChildren<NoryangjinRushEvent>().First(e=>e.name=="E1_MerchantRush");
                        typeof(NoryangjinRushEvent).GetMethod("OnTriggered",Hidden).Invoke(rush,null);
                    }
                    file=folder+"/"+names[stage]+".png";due=EditorApplication.timeSinceStartup+1.2;awaitingShot=true;
                    EditorApplication.CallbackFunction shot=null;
                    shot=()=>
                    {
                        if(!EditorApplication.isPlaying){EditorApplication.update-=shot;return;}
                        if(EditorApplication.timeSinceStartup<due)return;
                        ScreenCapture.CaptureScreenshot(file);queued=true;awaitingShot=false;EditorApplication.update-=shot;
                    };
                    EditorApplication.update+=shot;
                }
            }
            catch(Exception error){EditorApplication.update-=tick;File.WriteAllText(folder+"/error.txt",error.ToString());Time.timeScale=1;}
        };
        EditorApplication.update+=tick;return new{folder,scheduled=true};
    }
    public static object DeathFixture()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required after Capture");
        EditorApplication.isPaused=false;Time.timeScale=1;
        var d=Object.FindFirstObjectByType<NoryangjinRevampDirector>();var p=d.Player;
        var templates=d.GetComponentsInChildren<NoryangjinRushEvent>().First(e=>e.name=="E1_MerchantRush").templates;
        var actors=new List<GameObject>();
        for(int i=0;i<2;i++)
        {
            var go=Object.Instantiate(templates[i],p.transform.position+p.transform.forward*9+p.transform.right*(i==0?-1.1f:1.1f),Quaternion.LookRotation(p.transform.forward),d.transform);
            go.name="NativeDeathFixture"+i;go.transform.localScale=Vector3.one*1.69f;NoryangjinRushEvent.StripToCombat(go);
            go.AddComponent<NoryangjinMerchant>();go.SetActive(true);var enemy=go.GetComponent<EnemyScript_space>();enemy.ConfigureRewards(false,0);enemy.ApplyStat(0,100,EnemyTier.Normal);actors.Add(go);
        }
        string folder=Preview+"/native-death-"+DateTime.Now.ToString("HHmmss");Directory.CreateDirectory(folder);
        double start=EditorApplication.timeSinceStartup;bool dead=false;int sample=0;var evidence=new List<object>();
        float[] moments={.3f,1.0f,1.8f,2.9f,3.5f};
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}
            double elapsed=EditorApplication.timeSinceStartup-start;
            if(!dead&&elapsed>.6){foreach(var go in actors)go.GetComponent<EnemyScript_space>().EnemyDeath();dead=true;}
            if(sample<moments.Length&&elapsed>moments[sample])
            {
                bool allActive=actors.All(go=>go.activeSelf);
                evidence.Add(new{elapsed,allActive,deathSeconds=actors.Select(go=>go.GetComponent<NoryangjinMerchant>().DeathSeconds).ToArray()});
                ScreenCapture.CaptureScreenshot(folder+"/pose-"+sample+".png");sample++;
            }
            if(sample==moments.Length)
            {
                File.WriteAllText(folder+"/result.json",Json(evidence));File.WriteAllText(Root+"/native-death-folder.txt",folder);
                EditorApplication.update-=tick;EditorApplication.isPaused=true;
            }
        };
        EditorApplication.update+=tick;return new{folder,kind="native death lifecycle fixture"};
    }
}
