using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

public static class ProbeHighwayCombat
{
    public static object CaptureTollLayout()
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new Exception("Fresh lobby required");
        OpeningStoryUI.Instance?.Skip();Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        foreach(var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))harness.enabled=false;
        var chapter=HighwayChapter2Controller.Active;var player=chapter.Player;var camera=Camera.main;
        foreach(var car in chapter.Vehicles)car.Retire();typeof(HighwayChapter2Controller).GetProperty("Running").SetValue(chapter,false);
        foreach(var weapon in player.GetComponentsInChildren<WeaponScript>()){weapon.CancelInvoke("ShootBullet");weapon.enabled=false;}player.canShoot=false;player.enabled=false;
        var oldPosition=player.transform.position;var oldForward=player.transform.forward;var offset=camera.transform.position-oldPosition;var oldRotation=camera.transform.rotation;
        chapter.Route.Sample(1888,false,out var p,out var f);var turn=Quaternion.FromToRotation(oldForward,f);player.ApplyContinuousRoutePose(p+Vector3.up*.12f,f,0);typeof(HighwayRoute).GetProperty("Distance").SetValue(chapter.Route,1888f);
        var stable=camera.GetComponent<StableGameplayCamera>();if(stable!=null)stable.enabled=false;
        camera.transform.SetPositionAndRotation(player.transform.position+turn*offset,turn*oldRotation);
        EditorApplication.delayCall+=()=>ScreenCapture.CaptureScreenshot("outputs/highway-combat-revision-2026-09-27/toll-final.png");
        return new{scope="Directed native layout capture after support-only cosmetic fix; not a route clear",station=1888};
    }
    public static object CaptureTollFrame()
    {
        foreach(var harness in Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None))harness.enabled=false;
        ScreenCapture.CaptureScreenshot("outputs/highway-combat-revision-2026-09-27/toll-final.png");return new{scope="Same layout capture, debug overlay hidden"};
    }
    public static object VerifyWorkbookTestsDirect()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GameDataWorkbookTests")).First(t=>t!=null);var instance=Activator.CreateInstance(type);var results=new System.Collections.Generic.List<object>();int passed=0,failed=0;
        foreach(var method in type.GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly))
        {
            var attributes=method.GetCustomAttributes(false);var cases=attributes.Where(a=>a.GetType().Name=="TestCaseAttribute").ToArray();
            if(cases.Length==0&&!attributes.Any(a=>a.GetType().Name=="TestAttribute"))continue;
            var inputs=cases.Length==0?new[]{Array.Empty<object>()}:cases.Select(a=>(object[])a.GetType().GetProperty("Arguments").GetValue(a)).ToArray();
            foreach(var args in inputs)
            {
                try{method.Invoke(instance,args);passed++;results.Add(new{name=method.Name,passed=true});}
                catch(Exception e){failed++;results.Add(new{name=method.Name,passed=false,error=(e.InnerException??e).ToString()});}
            }
        }
        var result=new{mode="Direct invocation of the existing NUnit test methods after async runner stalled",total=passed+failed,passed,failed,results};
        File.WriteAllText("outputs/highway-combat-revision-2026-09-27/tests/GameDataWorkbookTests-direct.json",Json(result));return result;
    }
    static HighwayChapter2Controller testChapter;
    static PlayerScript testPlayer;
    static HighwayVehicleEnemy testCar;
    static float initialHp;
    static int stage,damageEvents;
    static double began;
    static readonly System.Collections.Generic.List<string> assertions=new();
    const System.Reflection.BindingFlags Private=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    static void Assert(bool value,string label){if(!value)throw new Exception(label);assertions.Add(label);}
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    public static object BeginContracts()
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new Exception("Fresh HighWay Play lobby required");
        testChapter=Object.FindFirstObjectByType<HighwayChapter2Controller>();testPlayer=Object.FindFirstObjectByType<PlayerScript>();
        testPlayer.canShoot=false;
        foreach(var weapon in testPlayer.GetComponentsInChildren<WeaponScript>()){weapon.CancelInvoke("ShootBullet");weapon.enabled=false;}
        OpeningStoryUI.Instance?.Skip();Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        testPlayer.canShoot=false;
        Object.FindFirstObjectByType<ChapterProgression>().nextScene="";
        foreach(var weapon in testPlayer.GetComponentsInChildren<WeaponScript>()){weapon.CancelInvoke("ShootBullet");weapon.enabled=false;}
        testPlayer.enabled=false;typeof(HighwayChapter2Controller).GetProperty("Running").SetValue(testChapter,false);
        foreach(var car in testChapter.Vehicles)car.Retire();
        testCar=testChapter.Vehicles.First(v=>v.kind==HighwayVehicleKind.Sedan);
        testCar.ResetForRun();testCar.Activate(false,1);testCar.Combat.ApplyStat(0,123,EnemyTier.Normal);
        var right=Vector3.Cross(Vector3.up,testPlayer.transform.forward);
        testCar.transform.SetPositionAndRotation(testPlayer.transform.position+right*(testCar.HalfWidth+2),Quaternion.LookRotation(-testPlayer.transform.forward));
        initialHp=testPlayer.currentHealth;damageEvents=0;testPlayer.DamageTaken+=RecordDamage;assertions.Clear();stage=0;began=EditorApplication.timeSinceStartup;Physics.SyncTransforms();EditorApplication.update+=ContractTick;
        return new{scope="Directed collision/visibility/pickup/lifecycle checks, not route play",running=true};
    }
    static void RecordDamage(float amount,PlayerDamageCause cause){damageEvents++;}
    static void ContractTick()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=ContractTick;return;}
        try
        {
            if(EditorApplication.timeSinceStartup-began<.35)return;
            if(stage==0)
            {
                Assert(Mathf.Approximately(initialHp,testPlayer.currentHealth)&&damageEvents==0,"Adjacent non-contact car causes no damage");
                testCar.transform.position=testPlayer.transform.position+testPlayer.transform.forward*2;Physics.SyncTransforms();stage=1;began=EditorApplication.timeSinceStartup;return;
            }
            Assert(Mathf.Abs(initialHp-testPlayer.currentHealth-123)<.01f&&damageEvents==1,"Actual contact applies exactly123HP once");
            Time.timeScale=0;
            testCar.ResetForRun();testCar.Activate(false,1);testCar.Combat.ApplyStat(0,1000,EnemyTier.Normal);
            testCar.transform.position=testPlayer.transform.position+testPlayer.transform.forward*24;Physics.SyncTransforms();
            var late=typeof(HighwayVehicleEnemy).GetMethod("LateUpdate",Private);late.Invoke(testCar,null);
            Assert(testCar.HealthLabelVisible,"Recognizable visible car shows its health");
            var camera=Camera.main;var renderer=(MeshRenderer)typeof(HighwayVehicleEnemy).GetField("mainRenderer",Private).GetValue(testCar);
            var target=renderer.bounds.center+Vector3.up*renderer.bounds.extents.y*.6f;
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                blocker.name="Visibility test wall";blocker.transform.SetPositionAndRotation(Vector3.Lerp(camera.transform.position,target,.6f),Quaternion.LookRotation(target-camera.transform.position));blocker.transform.localScale=new Vector3(12,12,1);Physics.SyncTransforms();late.Invoke(testCar,null);
                Assert(!testCar.HealthLabelVisible,"Occluded car cannot show floating health");
            }
            finally{Object.DestroyImmediate(blocker);}
            testCar.transform.position=testPlayer.transform.position-testPlayer.transform.forward*80;Physics.SyncTransforms();late.Invoke(testCar,null);
            Assert(!testCar.HealthLabelVisible,"Off-screen car cannot show health");
            testCar.transform.position=testPlayer.transform.position+testPlayer.transform.forward*8;Physics.SyncTransforms();
            var warning=testCar.gameObject.AddComponent<EnemyContactWarning>();typeof(EnemyContactWarning).GetMethod("LateUpdate",Private).Invoke(warning,null);
            Assert(typeof(EnemyContactWarning).GetField("text",Private).GetValue(warning)==null,"Legacy predictive damage warning stays absent on vehicles");
            Object.DestroyImmediate(warning);
            foreach(int side in new[]{0,1})
            {
                testChapter.Route.BeginRun();testChapter.BeginRun();typeof(HighwayRoute).GetProperty("Distance").SetValue(testChapter.Route,HighwayChapter2Data.Value("tollAt")-HighwayChapter2Data.Value("tollPickupLead"));
                testPlayer.transform.position=Vector3.zero;Physics.SyncTransforms();
                Assert(!testChapter.TryCollectExit(side,testPlayer),"Distant exit reward cannot be collected "+side);
                testChapter.Route.Sample(testChapter.Route.Distance,false,out var p,out var f);testPlayer.transform.position=p+Vector3.Cross(Vector3.up,f)*(side==0?-2.2f:2.2f)+Vector3.up*.12f;Physics.SyncTransforms();
                Assert(testChapter.TryCollectExit(side,testPlayer),"Physical exit pickup succeeds "+side);
                Assert(testChapter.GrantedBonus==(side==0?testChapter.LeftBonus:testChapter.RightBonus)&&testChapter.ui.PopupCount==0,"Exit grants chosen reward without popup "+side);
                Assert(!testChapter.TryCollectExit(1-side,testPlayer),"Second exit reward cannot be collected "+side);
            }
            testChapter.Route.BeginRun();testChapter.BeginRun();typeof(HighwayChapter2Controller).GetProperty("RoadChoice").SetValue(testChapter,1);typeof(HighwayRoute).GetProperty("Distance").SetValue(testChapter.Route,700f);
            Assert(Mathf.Abs(testPlayer.ForwardMoveSpeed-11.7f)<.01f,"Open route multiplies player speed by1.5");
            testChapter.ui.SetRush(true);testChapter.enabled=false;
            Assert(!testChapter.ui.RushShown&&Mathf.Abs(testPlayer.ForwardMoveSpeed-7.8f)<.01f,"Disabling the chapter restores speed and removes speed lines");
            Finish(null);
        }
        catch(Exception error){Finish(error);}
    }
    static void Finish(Exception error)
    {
        EditorApplication.update-=ContractTick;if(testPlayer!=null)testPlayer.DamageTaken-=RecordDamage;Time.timeScale=1;
        File.WriteAllText("outputs/highway-combat-revision-2026-09-27/contracts.json",Json(new{passed=error==null,assertions,error=error?.ToString(),scope="Directed fixtures; not additional route clears"}));EditorApplication.isPaused=true;
    }
    public static object EarlierCaptures()
    {
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;int updated=0;
        foreach(var type in AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("HighwayChapter2Playtest")).Where(t=>t!=null))
        {
            if(!(bool)type.GetField("active",flags).GetValue(null))continue;
            var stations=(float[])type.GetField("Stations",flags).GetValue(null);
            for(int i=0;i<stations.Length;i++){if(stations[i]==1440)stations[i]=1418;if(stations[i]==1960)stations[i]=1922;}updated++;
        }
        return new{drivers=updated,scope="Capture schedule only; no game state change"};
    }
    public static object DriverState()
    {
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
        var types=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("HighwayChapter2Playtest")).Where(t=>t!=null).ToArray();
        var type=types.LastOrDefault(t=>(bool)t.GetField("active",flags).GetValue(null));if(type==null)return new{driver="inactive"};
        var focus=type.GetField("focusCar",flags).GetValue(null) as HighwayVehicleEnemy;var chapter=HighwayChapter2Controller.Active;var player=chapter.Player;
        return new{distance=chapter.Route.Distance,lane=RouteFrame.Lane(player.transform.position,chapter.Route.Distance),focus=focus==null?null:new{focus.name,focus.group,focus.Distance,focus.station,focus.Lane,focus.Active,focus.Dead,hp=focus.Combat.CurrentHealth},
            near=chapter.Vehicles.Where(v=>v.Active&&!v.Dead&&Mathf.Abs(v.Distance-chapter.Route.Distance)<60).Select(v=>new{v.name,v.group,v.Distance,v.Lane,hp=v.Combat.CurrentHealth}).ToArray()};
    }
    public static object Runtime()
    {
        var chapter=HighwayChapter2Controller.Active;var player=chapter.Player;
        return new{distance=chapter.Route.Distance,player.currentHealth,player.MaxHealth,speed=player.ForwardMoveSpeed,chapter.RushActive,rushShown=chapter.ui.RushShown,chapter.ExitChoice,chapter.VehicleProjectileHits,
            weapons=player.GetComponentsInChildren<WeaponScript>().Select(w=>new{w.fireRate,w.damage,w.bulletCount,kind=w.bulletKind.ToString()}).ToArray(),range=BulletScript.BaseMissileSpeed*BulletScript.CurrentMissileDuration,
            cars=chapter.Vehicles.Where(v=>v.Active&&!v.Dead&&Mathf.Abs(v.Distance-chapter.Route.Distance)<100).Select(v=>new{v.name,v.group,v.Distance,v.Lane,hp=v.Combat.CurrentHealth,v.HealthLabelVisible}).ToArray()};
    }
    public static object Inspect()
    {
        var chapter=Object.FindFirstObjectByType<HighwayChapter2Controller>();var car=chapter.GetComponentsInChildren<HighwayVehicleEnemy>(true).First();var number=car.healthNumber;var material=number.fontSharedMaterial;
        return new{fontMaterial=material.name,path=AssetDatabase.GetAssetPath(material),shader=material.shader.name,hasDepth=material.HasProperty("_ZTestMode"),properties=Enumerable.Range(0,material.shader.GetPropertyCount()).Select(material.shader.GetPropertyName).ToArray(),canvas=number.canvas.renderMode.ToString(),rootCanvas=number.canvas.rootCanvas.renderMode.ToString(),overrideSort=number.canvas.overrideSorting,source=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Chapter2_20260927/VehicleNumber.mat").name};
    }
}
