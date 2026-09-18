#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exercises normal PlayerMove input, earned currency and the real purchase handler.
[InitializeOnLoad]
public static class Sr18ProgressionPlaytest
{
    const string ActiveKey="SR18.Progression.Active",ConfigKey="SR18.Progression.Config";
    [Serializable] class Config {public string folder;public int maxRuns,seed,attempt;public bool buy;public bool stopOnClear=true;public bool saveForBestUpgrade;}
    static Config config;
    static PlayerScript player;
    static CanvasScript canvas;
    static EnemyEventController[] enemies;
    static BonusWallChoicePair[] pairs;
    static ObstacleStats[] hazards;
    static EnemyEventController[][] formations;
    static bool started,recorded;
    static double readyAt,endedAt,nextSample,nextCapture;
    static float began,initialHp,initialAttack;
    static int initialCoin,spent,captures;
    static int lastProcessedFrame=-1,beganFrame,movementCalls;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    const float EnemyAvoidanceDistance=14f;
    static readonly MethodInfo Move=typeof(PlayerScript).GetMethod("PlayerMove",Flags);
    static readonly FieldInfo Origin=typeof(PlayerScript).GetField("routeLaneOrigin",Flags),Right=typeof(PlayerScript).GetField("routeRight",Flags);
    static Sr18ProgressionPlaytest(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
    public static object Begin(string label,int maxRuns=1,int seed=20260910,bool buy=true,bool resetProgress=true,bool stopOnClear=true,string snapshotPath=null,bool saveForBestUpgrade=false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        string backup = snapshotPath ?? "tmp/backups/sr18-presentation-progression-20260910/ui-playerprefs.tsv";
        if(!File.Exists(backup)||File.ReadAllLines(backup).Length<30)throw new InvalidOperationException("Preference snapshot required");
        config=new Config{folder="tmp/image-previews/sr18-presentation-progression-2026-09-10/"+label,maxRuns=maxRuns,seed=seed,buy=buy,stopOnClear=stopOnClear,saveForBestUpgrade=saveForBestUpgrade};
        if(Directory.Exists(config.folder))throw new InvalidOperationException("Choose new evidence label");
        Directory.CreateDirectory(config.folder);
        File.WriteAllText(config.folder+"/runs.csv","attempt,seconds,outcome,start_hp,start_attack,final_hp,kills,choices,earned_coin,spent_coin,bank,att_level,hp_level,movement_calls,game_frames\n");
        if(resetProgress)
        {
            for(int i=1;i<=9;i++)PlayerPrefs.DeleteKey("upgrade_lv_"+i);
            foreach(var type in Enum.GetNames(typeof(UpgradeStatManager.UpgradeType))){PlayerPrefs.DeleteKey("upgrade_stat_"+type);PlayerPrefs.DeleteKey("upgrade_stat_type_"+type);}
            PlayerPrefs.SetInt("coin",0);PlayerPrefs.SetInt("jewel",0);
        }
        foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))PlayerPrefs.SetString(CosmeticService.EquippedKey(slot),CosmeticTables.Rows.Single(r=>r.slot==slot&&r.isDefault).id);
        PlayerPrefs.Save();
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",3);
        lastProcessedFrame=-1;SaveConfig();SessionState.SetBool(ActiveKey,true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
        return new{config.folder,maxRuns,seed,buy,normalStats=true,normalLateral=true};
    }
    public static object Status()=>new{active=SessionState.GetBool(ActiveKey,false),config=config??LoadConfig(),started,recorded};
    public static void Stop(){SessionState.SetBool(ActiveKey,false);EditorApplication.isPaused=true;}
    static Config LoadConfig()=>JsonUtility.FromJson<Config>(SessionState.GetString(ConfigKey,"{}"));
    static void SaveConfig()=>SessionState.SetString(ConfigKey,JsonUtility.ToJson(config));
    static void Tick()
    {
        if(!SessionState.GetBool(ActiveKey,false)||EditorApplication.isCompiling||!EditorApplication.isPlaying||EditorApplication.isPaused)return;
        // Editor update can run several times per rendered game frame. PlayerMove
        // uses deltaTime, so repeated calls would multiply lateral movement.
        if(Time.frameCount==lastProcessedFrame)return;
        lastProcessedFrame=Time.frameCount;
        config??=LoadConfig();
        if(player==null){player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();if(player==null)return;canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();started=recorded=false;readyAt=EditorApplication.timeSinceStartup+1;return;}
        if(!started){if(EditorApplication.timeSinceStartup<readyAt||canvas==null)return;StartRun();return;}
        if(!TimeManager.isGameRunning||player.currentHealth<=0){FinishRun();return;}
        if(Time.time-began>400){File.AppendAllText(config.folder+"/conditions.txt","ERROR route exceeded400game seconds\n");Stop();return;}
        if(EditorApplication.timeSinceStartup>=nextSample){nextSample=EditorApplication.timeSinceStartup+1;File.AppendAllText(config.folder+"/timeline.csv",string.Join(",",config.attempt,Time.time-began,player.currentHealth,player.currentDamage,player.transform.position.x,player.transform.position.y,player.transform.position.z,player.canShoot,enemies.Count(e=>e.RuntimeState==EnemyEventRuntimeState.Dead),MoneyScript.S.Coin)+"\n");}
        if(EditorApplication.timeSinceStartup>=nextCapture){nextCapture=EditorApplication.timeSinceStartup+5;ScreenCapture.CaptureScreenshot(config.folder+$"/run-{config.attempt:D2}-{captures++:D3}.png");}
        Steer();
    }
    static void StartRun()
    {
        OpeningStoryUI.Instance?.Skip();
        var map=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        enemies=map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true);pairs=map.Find("Bonuses").GetComponentsInChildren<BonusWallChoicePair>(true);
        hazards=map.Find("Props").GetComponentsInChildren<ObstacleStats>(true).Where(o=>o.gameObject.activeInHierarchy&&o.enabled).ToArray();
        formations=enemies.Where(e=>e.name.EndsWith("_Right")).Select(e=>new[]{enemies.Single(l=>l.name==e.name.Substring(0,e.name.Length-6)),e}).ToArray();
        spent=config.attempt>0&&config.buy?BuyEarnedUpgrades():0;
        UnityEngine.Random.InitState(config.seed+config.attempt);
        // Initial scene rolls precede the harness seed. Re-roll through the real
        // run-reset API so repeated seed cohorts compare the same authored bonuses.
        map.GetComponent<EncounterPlacementController>().BeginNewRun();
        Time.timeScale=3;canvas.PlayerPressedStartButton();
        if(!TimeManager.isGameRunning)throw new InvalidOperationException("Start handler did not start gameplay");
        began=Time.time;beganFrame=Time.frameCount;movementCalls=0;initialHp=player.currentHealth;initialAttack=UpgradeStatManager.S.ApplyToBase(UpgradeStatManager.UpgradeType.ATT,player.DefaultAttackDamage);initialCoin=MoneyScript.S.Coin;config.attempt++;SaveConfig();started=true;nextSample=nextCapture=0;captures=0;
        File.AppendAllText(config.folder+"/conditions.txt",$"Run {config.attempt}: HP={initialHp} ATT={initialAttack} seed={config.seed+config.attempt-1} spent={spent} timeScale=3\n");
    }
    static void FinishRun()
    {
        if(!recorded)
        {
            recorded=true;endedAt=EditorApplication.timeSinceStartup;string outcome=player.currentHealth>0?"clear":"death";
            File.AppendAllText(config.folder+"/runs.csv",string.Join(",",config.attempt,(Time.time-began).ToString("F2",System.Globalization.CultureInfo.InvariantCulture),outcome,initialHp,initialAttack,player.currentHealth,enemies.Count(e=>e.RuntimeState==EnemyEventRuntimeState.Dead),pairs.Count(p=>p.Selected!=null),MoneyScript.S.Coin-initialCoin,spent,MoneyScript.S.Coin,PlayerPrefs.GetInt("upgrade_lv_1"),PlayerPrefs.GetInt("upgrade_lv_2"),movementCalls,Time.frameCount-beganFrame+1)+"\n");
            ScreenCapture.CaptureScreenshot(config.folder+$"/run-{config.attempt:D2}-end.png");
        }
        if(EditorApplication.timeSinceStartup-endedAt<1)return;
        if(config.attempt>=config.maxRuns||(player.currentHealth>0&&config.stopOnClear)){Stop();return;}
        if(player.currentHealth>0){SceneManager.LoadScene(SceneManager.GetActiveScene().name);return;}
        var defeat=UnityEngine.Object.FindFirstObjectByType<DefeatPresentation>();if(defeat!=null)defeat.ReturnToAltar();
    }
    static int BuyEarnedUpgrades()
    {
        int before=MoneyScript.S.Coin;
        var cards=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponentsInChildren<UpgradeUI>(true);
        for(int purchases=0;purchases<12;purchases++)
        {
            // The comparison cohort may save for the best value instead of spending
            // every small balance on a cheaper secondary upgrade.
            var candidates=cards.Where(c=>c.UpgradeId>=1&&c.UpgradeId<=3).Where(c=>UpgradeTables.TryGet(c.UpgradeId,PlayerPrefs.GetInt("upgrade_lv_"+c.UpgradeId)+1,out var row)&&(config.saveForBestUpgrade||row.price<=MoneyScript.S.Coin)).ToArray();
            if(candidates.Length==0)break;
            var best=candidates.OrderByDescending(c=>Value(c.UpgradeId)).First();if(!best.TryBuy())break;
        }
        return before-MoneyScript.S.Coin;
    }
    static float Value(int id)
    {
        int level=PlayerPrefs.GetInt("upgrade_lv_"+id);if(!UpgradeTables.TryGet(id,level+1,out var next))return 0;
        float previous=UpgradeTables.TryGet(id,level,out var current)?current.amount:0;
        return (next.amount-previous)/(id==1?Mathf.Max(1,player.DefaultAttackDamage+previous):id==2?Mathf.Max(1,player.MaxHealth):100+previous)*(id==2?.9f:1f)/Mathf.Max(1,next.price);
    }
    static void Steer()
    {
        if(player.IsWorldYawTurnActive)return;
        Vector2 laneRange=(Vector2)typeof(PlayerScript).GetField("xRange",Flags).GetValue(player);
        Vector3 forward=Vector3.ProjectOnPlane(player.transform.forward,Vector3.up).normalized,lateral=(Vector3)Right.GetValue(player),origin=(Vector3)Origin.GetValue(player);
        float desired=0,nearest=26;
        foreach(var pair in pairs)
        {
            if(pair==null||pair.Selected!=null||!pair.Left.gameObject.activeInHierarchy)continue;
            Vector3 delta=(pair.Left.transform.position+pair.Right.transform.position)*.5f-player.transform.position;float ahead=Vector3.Dot(delta,forward);
            if(ahead< -1||ahead>nearest||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,lateral))>5||Vector3.Dot(Vector3.ProjectOnPlane(pair.Left.transform.forward,Vector3.up).normalized,forward)>-.9f)continue;
            nearest=ahead;bool helper=(pair.Right.RolledStat??"").Contains("tung")||(pair.Right.RolledStat??"").Contains("boom");desired=Mathf.Clamp(Vector3.Dot((helper?pair.Right:pair.Left).transform.position-origin,lateral),laneRange.x,laneRange.y);
        }
        foreach(var pair in formations)
        {
            Vector3 delta=(pair[0].transform.position+pair[1].transform.position)*.5f-player.transform.position;float ahead=Vector3.Dot(delta,forward);
            if(ahead< -2.2f||ahead>nearest||Mathf.Abs(delta.y)>2||Mathf.Abs(Vector3.Dot(delta,lateral))>5||Vector3.Dot(pair[0].transform.forward,forward)<.9f)continue;
            nearest=ahead;
            bool roadChapter=SceneManager.GetActiveScene().name=="HighWay"||SceneManager.GetActiveScene().name=="RestStop";
            // Clear the inner enemy, beside the alternating open lane. Aiming at
            // the outer enemy needlessly required a full-road crossing to dodge.
            var aim=roadChapter?pair.OrderBy(e=>Mathf.Abs(Vector3.Dot(e.transform.position-origin,lateral))).First():pair[1].RuntimeState==EnemyEventRuntimeState.Dead?pair[1]:pair[0];
            desired=Mathf.Clamp(Vector3.Dot(aim.transform.position-origin,lateral),laneRange.x,laneRange.y);
        }
        if(nearest==26)
        {
            var target=enemies.Where(e=>e.RuntimeState!=EnemyEventRuntimeState.Dead&&Mathf.Abs(e.transform.position.y-player.transform.position.y)<2&&Mathf.Abs(Vector3.Dot(e.transform.position-player.transform.position,lateral))<5&&Vector3.Dot(e.transform.position-player.transform.position,forward)>0&&Vector3.Dot(e.transform.position-player.transform.position,forward)<38).OrderBy(e=>Vector3.Dot(e.transform.position-player.transform.position,forward)).FirstOrDefault();
            if(target!=null)desired=Mathf.Clamp(Vector3.Dot(target.transform.position-origin,lateral),laneRange.x,laneRange.y);
        }
        float currentLane=Vector3.Dot(player.transform.position-origin,lateral);
        foreach(var projectile in UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None))
        {
            var body=projectile.GetComponent<Rigidbody>();if(body==null||body.isKinematic)continue;
            Vector3 delta=projectile.transform.position-player.transform.position;float ahead=Vector3.Dot(delta,forward),offset=Vector3.Dot(projectile.transform.position-origin,lateral);
            if(ahead>0&&ahead<11&&Mathf.Abs(delta.y)<2.5f&&Mathf.Abs(offset-currentLane)<1f)
                desired=Mathf.Clamp(offset+(offset>=0?-1.5f:1.5f),laneRange.x,laneRange.y);
        }
        var playerCollider=player.GetComponent<Collider>();
        float playerHalfWidth=playerCollider==null?.4f:Mathf.Abs(lateral.x)*playerCollider.bounds.extents.x+Mathf.Abs(lateral.z)*playerCollider.bounds.extents.z;
        var candidates=hazards.Where(h=>h!=null&&h.enabled&&h.gameObject.activeInHierarchy).Select(h=>h.GetComponent<Collider>());
        if(SceneManager.GetActiveScene().name=="HighWay" || SceneManager.GetActiveScene().name=="RestStop")
        {
            // Give shots time to arrive on approach, then dodge a surviving
            // enemy in the final close-range window instead of ramming it.
            candidates=candidates.Concat(enemies.Where(e=>e.RuntimeState!=EnemyEventRuntimeState.Dead)
                .Where(e=>Vector3.Dot(e.transform.position-player.transform.position,forward)<EnemyAvoidanceDistance)
                .Where(e=>e.GetComponent<EnemyScript_space>().CurrentHealth>player.currentDamage)
                .Select(e=>e.GetComponent<Collider>()));
        }
        var nearby=candidates.Where(c=>c!=null&&c.enabled).Where(c=>
        {
            var delta=c.bounds.center-player.transform.position;float ahead=Vector3.Dot(delta,forward);
            float horizon=c.GetComponent<EnemyScript_space>()!=null?EnemyAvoidanceDistance:14;
            return ahead>=-1.5f&&ahead<=horizon&&Mathf.Abs(delta.y)<3&&Mathf.Abs(Vector3.Dot(delta,lateral))<Mathf.Max(Mathf.Abs(laneRange.x),Mathf.Abs(laneRange.y))+2;
        }).ToArray();
        if(nearby.Length>0)
        {
            // Evaluate all close obstacles together. Alternating extreme lanes for
            // individual roadside lamps missed the open central corridor.
            float Cost(float lane)
            {
                float cost=Mathf.Abs(lane-desired)*.15f+Mathf.Abs(lane-currentLane)*.08f;
                foreach(var c in nearby)
                {
                    float offset=Vector3.Dot(c.bounds.center-origin,lateral);
                    float halfWidth=Mathf.Abs(lateral.x)*c.bounds.extents.x+Mathf.Abs(lateral.z)*c.bounds.extents.z;
                    var movingEnemy=c.GetComponent<EnemyEventController>();
                    if(movingEnemy!=null&&(movingEnemy.RuntimeState==EnemyEventRuntimeState.MovingToTarget||movingEnemy.RuntimeState==EnemyEventRuntimeState.MovingToStart))halfWidth+=(SceneManager.GetActiveScene().name=="HighWay"||SceneManager.GetActiveScene().name=="RestStop")?Mathf.Min(.6f,movingEnemy.MoveSpeed*.3f):movingEnemy.MoveSpeed*.7f;
                    float overlap=halfWidth+playerHalfWidth+.13f-Mathf.Abs(lane-offset);
                    if(overlap>0)cost+=100+overlap*10;
                }
                return cost;
            }
            int samples=Mathf.CeilToInt((laneRange.y-laneRange.x)/.25f)+1;
            desired=Enumerable.Range(0,samples).Select(i=>Mathf.Lerp(laneRange.x,laneRange.y,i/(float)(samples-1))).OrderBy(Cost).First();
        }
        float error=desired-Vector3.Dot(player.transform.position-origin,lateral);
        if(Mathf.Abs(error)>.04f){Move.Invoke(player,new object[]{Mathf.Sign(error)*Mathf.Min(15,Mathf.Abs(error)*12)});movementCalls++;}
    }
}
#endif
