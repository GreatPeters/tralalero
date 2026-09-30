using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class NoryangjinDebugReview
{
    const string Root="outputs/noryangjin-debug-review-2026-09-28";
    static void Select(string type,string method,params object[] args)=>typeof(ChapterPlaytestPreferences).Assembly.GetType(type).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    public static object Prepare()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        Directory.CreateDirectory(Root+"/before");
        if(!File.Exists(Root+"/before/playerprefs.tsv"))ChapterPlaytestPreferences.SnapshotAt(Root+"/before/playerprefs.tsv");
        Select("NoryangjinMapToolTestStartStage","SelectStage",1);
        Select("NoryangjinMapToolTestOverrides","Select",true,true);
        Select("NoryangjinMapToolTestSpeed","SelectTimeScale",1f);
        return State();
    }
    public static object Finish()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        ChapterPlaytestPreferences.RestoreAt(Root+"/before/playerprefs.tsv");
        Prepare();
        string json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{State()});
        File.WriteAllText(Root+"/final-state.json",json);return State();
    }
    public static object VerifyPreservedPrefs()
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
        var result=new{count,mismatches,preserved=mismatches.Count==0,settings=State()};
        string json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result});File.WriteAllText(Root+"/restoration.json",json);return result;
    }
    public static object State()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        return new{playing=EditorApplication.isPlaying,paused=EditorApplication.isPaused,power9999=SessionState.GetBool("NoryangjinMapTool.TestPower9999",false),fastLateral=SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false),stage=SessionState.GetInt("NoryangjinMapTool.TestStartStage",0),timeScale=Time.timeScale,health=p?.currentHealth,maxHealth=p?.MaxHealth,attack=p?.ResolvedAttackDamage,sensitivity=p?.moveSensitivity_Devision,scene=EditorSceneManager.GetActiveScene().path,dirty=EditorSceneManager.GetActiveScene().isDirty};
    }
    public static object CombatGeometry()
    {
        var enemies=UnityEngine.Object.FindObjectsByType<NoryangjinMerchant>(FindObjectsSortMode.None);
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        return new{player=p.transform.position.ToString(),weapons=p.GetComponentsInChildren<WeaponScript>().Select(w=>new{w.name,pos=w.transform.position.ToString(),w.damage}),bullets=UnityEngine.Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None).Take(10).Select(b=>new{b.name,pos=b.transform.position.ToString()}),enemies=enemies.Where(e=>e.name.Contains("D1_")).Select(e=>new{e.name,pos=e.transform.position.ToString(),bounds=e.GetComponent<Collider>().bounds.ToString(),health=e.GetComponent<EnemyScript_space>().CurrentHealth})};
    }
    public static object NearbyHazards()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        return GameObject.Find("Noryangjin_MapTool/Props").transform.Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")&&Vector3.Distance(t.position,p.transform.position)<30).Select(t=>new{t.name,active=t.gameObject.activeInHierarchy,at=t.position.ToString(),colliders=t.GetComponentsInChildren<Collider>(true).Select(c=>new{c.name,c.enabled,bounds=c.bounds.ToString()})}).ToArray();
    }
    public static object WatchAuction()
    {
        var d=UnityEngine.Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        string path=Root+"/auction-"+DateTime.Now.ToString("HHmmss")+".csv";
        File.WriteAllText(path,"elapsed,playerHealth,centerEnemyHealth,activeBidders,power9999,fastLateral\n");float next=0;
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            if(!EditorApplication.isPlaying||d==null){EditorApplication.update-=tick;return;}
            if(d.Elapsed<248||d.Elapsed<next)return;next=d.Elapsed+.2f;
            var bidders=d.GetComponentsInChildren<NoryangjinMerchant>(true).Where(e=>e.name.StartsWith("D1_BidderWall_")).ToArray();
            var center=bidders.OrderBy(e=>Mathf.Abs(e.transform.position.z-203.5f)).FirstOrDefault();
            File.AppendAllText(path,$"{d.Elapsed:F2},{d.Player.currentHealth:F1},{(center!=null?center.GetComponent<EnemyScript_space>().CurrentHealth:-1):F1},{bidders.Count(e=>e.gameObject.activeSelf)},{SessionState.GetBool("NoryangjinMapTool.TestPower9999",false)},{SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false)}\n");
            if(d.Elapsed>270||d.Player.currentHealth<=0)EditorApplication.update-=tick;
        };
        EditorApplication.update+=tick;return new{path};
    }
    public static object ProbePresentation()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Fresh Play required");
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var d=UnityEngine.Object.FindFirstObjectByType<NoryangjinRevampDirector>();
        UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>()?.Skip();UnityEngine.Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();Time.timeScale=1;
        foreach(var h in UnityEngine.Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))h.enabled=false;
        foreach(var w in UnityEngine.Object.FindObjectsByType<WeaponScript>(FindObjectsSortMode.None)){w.StopAllCoroutines();w.CancelInvoke();w.enabled=false;}
        foreach(var bullet in UnityEngine.Object.FindObjectsByType<BulletScript>(FindObjectsSortMode.None))bullet.gameObject.SetActive(false);
        foreach(var ev in d.GetComponentsInChildren<NoryangjinRevampEvent>())ev.enabled=false;
        typeof(NoryangjinMarketBranch).GetProperty("Selected").SetValue(d.Branch,true);
        var owner=p.GetType().GetField("stationaryCombatOwner",BindingFlags.Instance|BindingFlags.NonPublic);var point=p.GetType().GetField("stationaryCombatPosition",BindingFlags.Instance|BindingFlags.NonPublic);
        var hold=new GameObject("ReviewFixtureAnchor");owner.SetValue(p,hold);
        void Move(Vector3 at){p.ApplyContinuousRoutePose(at,Vector3.back,0);point.SetValue(p,p.transform.position);}
        Move(new Vector3(124.3f,.22f,-315));
        var shutter=d.GetComponentInChildren<NoryangjinShutterEvent>();
        var hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        var trigger=typeof(NoryangjinShutterEvent).GetMethod("OnTriggered",hidden);var tickMethod=typeof(NoryangjinShutterEvent).GetMethod("OnTick",hidden);var remaining=typeof(NoryangjinShutterEvent).GetField("remaining",hidden);
        trigger.Invoke(shutter,null);remaining.SetValue(shutter,1f);tickMethod.Invoke(shutter,new object[]{0f});
        string folder="tmp/image-previews/noryangjin-debug-review-2026-09-28/probe-"+DateTime.Now.ToString("HHmmss");Directory.CreateDirectory(folder);
        var results=new List<object>();double due=EditorApplication.timeSinceStartup+1;int step=0,coinsBefore=0,expectedCoins=0;float hp=0;
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            try
            {
                if(!EditorApplication.isPlaying||p==null){EditorApplication.update-=tick;return;}
                if(EditorApplication.timeSinceStartup<due)return;
                if(step==0)
                {
                    float bottom=shutter.shutterPanel.GetComponent<Renderer>().bounds.min.y;
                    results.Add(new{check="positive timer clears shark",passed=bottom>=3.6f,bottom});ScreenCapture.CaptureScreenshot(folder+"/shutter-open.png");
                }
                else if(step==1)
                {
                    remaining.SetValue(shutter,.001f);tickMethod.Invoke(shutter,new object[]{.01f});tickMethod.Invoke(shutter,new object[]{.5f});
                }
                else if(step==2)
                {
                    float bottom=shutter.shutterPanel.GetComponent<Renderer>().bounds.min.y;
                    results.Add(new{check="expired shutter closes and arms",passed=bottom<.2f&&shutter.shutter.Armed,bottom,armed=shutter.shutter.Armed});ScreenCapture.CaptureScreenshot(folder+"/shutter-closed.png");
                }
                else if(step==3)
                {
                    Move(shutter.shutter.transform.position-shutter.transform.forward*2.8f+Vector3.up*.22f);hp=p.currentHealth;
                }
                else if(step==4)
                {
                    results.Add(new{check="closed shutter blocks and drains",passed=d.Blocked&&p.currentHealth<hp,blocked=d.Blocked,damage=hp-p.currentHealth});shutter.shutter.Damage(shutter.shutter.Health);
                }
                else if(step==5)
                {
                    results.Add(new{check="broken shutter releases route",passed=!shutter.shutter.Alive&&!d.Blocked,blocked=d.Blocked});
                    var go=new GameObject("HighCargoRewardFixture");go.transform.position=p.transform.position+p.transform.forward;var block=go.AddComponent<NoryangjinBreakable>();block.showHealth=false;block.coins=0;block.rewardsPerPiece=true;
                    var cargo=new GameObject("HighCargo").transform;cargo.SetParent(go.transform,false);cargo.localPosition=Vector3.up*4;block.shedPieces=new[]{cargo};block.Bind(d);block.Arm();
                    coinsBefore=MoneyScript.S.Coin;expectedCoins=CoinDropUtility.ApplyCoinBonus(1);block.Damage(block.Health);
                    var coin=UnityEngine.Object.FindObjectsByType<CoinPickup>(FindObjectsSortMode.None).OrderBy(c=>Vector3.Distance(c.transform.position,p.transform.position)).FirstOrDefault();
                    results.Add(new{check="high cargo coin reachable",passed=coin!=null&&Mathf.Abs(coin.transform.position.y-p.transform.position.y)<2.5f,height=coin!=null?coin.transform.position.y:-100});
                }
                else
                {
                    int received=MoneyScript.S.Coin-coinsBefore;results.Add(new{check="coin amount preserved on collection",passed=received==expectedCoins,received,expectedCoins});
                    var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{results});
                    File.WriteAllText(Root+"/native-probe.json",json);File.WriteAllText(Root+"/probe-folder.txt",folder);EditorApplication.update-=tick;EditorApplication.isPaused=true;return;
                }
                step++;due=EditorApplication.timeSinceStartup+.65;
            }
            catch(Exception error){EditorApplication.update-=tick;File.WriteAllText(Root+"/probe-error.txt",error.ToString());EditorApplication.isPaused=true;}
        };
        EditorApplication.update+=tick;return new{folder,scheduled=true};
    }
}
