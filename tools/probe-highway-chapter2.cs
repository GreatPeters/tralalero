using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class ProbeHighwayChapter2
{
    static double contractAt;
    static int contractStage;
    static HighwayChapter2Controller contractChapter;
    static PlayerScript contractPlayer;
    static int coinsBefore;
    static readonly System.Collections.Generic.List<string> assertions=new();
    static void Assert(bool condition,string name){if(!condition)throw new Exception(name);assertions.Add(name);}
    static void SetBonus(HighwayUniqueBonus value)
        =>typeof(HighwayChapter2Controller).GetProperty("GrantedBonus").SetValue(contractChapter,(HighwayUniqueBonus?)value);
    public static object FreshContractsScene()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        StopAllDrivers();SceneManager.LoadScene("HighWay");return new{loading=true};
    }
    public static object BeginContracts()
    {
        contractChapter=Object.FindFirstObjectByType<HighwayChapter2Controller>();
        contractPlayer=Object.FindFirstObjectByType<PlayerScript>();
        if(contractChapter==null||!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new Exception("Fresh HighWay lobby required");
        OpeningStoryUI.Instance?.Skip();Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();
        var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.Close();tutorial.enabled=false;}
        Object.FindFirstObjectByType<ChapterProgression>().nextScene="";
        assertions.Clear();contractStage=0;contractAt=EditorApplication.timeSinceStartup;
        typeof(HighwayChapter2Controller).GetMethod("OpenChoice",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(contractChapter,new object[]{false});
        Assert(Mathf.Approximately(Time.timeScale,.2f),"Popup slows to0.2");
        EditorApplication.update+=ContractTick;
        return new{running=true,scope="Directed integration probes, not additional route clears"};
    }
    static void ContractTick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPaused)return;
        try
        {
            double elapsed=EditorApplication.timeSinceStartup-contractAt;
            if(contractStage==0)
            {
                if(elapsed<5.4)return;
                Assert(contractChapter.RoadChoice==0&&contractChapter.ui.TimeoutCount==1,"Five-second timeout selects jam");
                Assert(Mathf.Approximately(Time.timeScale,1),"Popup restores prior clock");
                var group=contractChapter.Vehicles.Where(v=>v.group==4).ToArray();
                foreach(var v in group){v.ResetForRun();v.Activate(false,1);v.Place(contractChapter.Route,false);}
                var tanker=group.Single(v=>v.kind==HighwayVehicleKind.Tanker);
                tanker.Combat.ReceiveVehicleDamage(tanker.Combat.CurrentHealth);
                Assert(group.All(v=>v.Dead),"Tanker destroys every adjacent accident/queue car");
                Assert(group.All(v=>!v.GetComponent<Collider>().enabled),"Destroyed lanes lose collision immediately");
                Assert(contractChapter.feedback.ActiveExplosions<=3,"Explosion pool limit");
                contractStage=1;contractAt=EditorApplication.timeSinceStartup;return;
            }
            if(contractStage==1)
            {
                if(elapsed<1.1)return;
                Assert(contractChapter.feedback.ActiveExplosions==0,"Explosion smoke clears within one second");
                var targets=contractChapter.Vehicles.Where(v=>v.group==4&&v.kind!=HighwayVehicleKind.Accident).ToArray();
                var source=targets.First(v=>v.accidentFront);
                var centre=targets.First(v=>v.kind==HighwayVehicleKind.Tanker);
                var side=targets.First(v=>v.kind==HighwayVehicleKind.Box);
                foreach(var v in new[]{source,centre,side})
                {v.ResetForRun();v.Distance=contractChapter.Route.Distance+100;v.Activate(false,1);v.Place(contractChapter.Route,false);v.Combat.ApplyStat(0,1000,EnemyTier.Normal);}
                var shot=new GameObject("Projectile contract fixture").AddComponent<BulletScript>();
                try
                {
                    shot.SetDirection(Vector3.forward,contractPlayer,40);shot.ProjectileKind=BulletKind.Water;shot.enabled=false;
                    SetBonus(HighwayUniqueBonus.ChainMissile);source.ProjectileHit(source,shot);
                    Assert(Mathf.Abs(centre.Combat.CurrentHealth-970)<.01f&&Mathf.Abs(side.Combat.CurrentHealth-970)<.01f,"Chain upgrade works with the actual default projectile");
                    side.Distance+=15;side.Place(contractChapter.Route,false);side.Combat.ApplyStat(0,1000,EnemyTier.Normal);
                    SetBonus(HighwayUniqueBonus.Shatter);source.ProjectileHit(source,shot);
                    Assert(side.Combat.CurrentHealth<1000,"Shatter reaches beyond the ordinary explosion radius");
                }
                finally{Object.Destroy(shot.gameObject);}
                SetBonus(HighwayUniqueBonus.GoldenShield);
                typeof(HighwayChapter2Controller).GetField("shieldReady",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(contractChapter,true);
                Assert(!contractChapter.ConsumeShield(contractPlayer.MaxHealth,PlayerDamageCause.Hole),"Potholes stay fatal despite shield");
                float hp=contractPlayer.currentHealth,hit=contractPlayer.MaxHealth*.25f;
                Assert(contractPlayer.ApplyDamage(hit,PlayerDamageCause.Traffic)==0&&contractPlayer.currentHealth==hp,"Golden shield blocks one large hit");
                Assert(Mathf.Abs(contractPlayer.ApplyDamage(hit,PlayerDamageCause.Traffic)-hit)<.01f,"Shield is consumed once");
                contractPlayer.ApplyDamage(contractPlayer.currentHealth-contractPlayer.MaxHealth*.05f,PlayerDamageCause.Other);
                typeof(HighwayChapter2Controller).GetField("shieldReady",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(contractChapter,true);
                source.Combat.ApplyStat(0,1000,EnemyTier.Normal);float lowHp=contractPlayer.currentHealth;
                typeof(EnemyScript_space).GetMethod("ResolvePlayerContact",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(source.Combat,null);
                Assert(contractPlayer.currentHealth==lowHp&&source.Dead,"Low-health shield tests full incoming car damage, not the clipped HP loss");
                SetBonus(HighwayUniqueBonus.CoinMagnet);coinsBefore=PlayerPrefs.GetInt("coin");
                CoinPickup.Spawn(contractPlayer.transform.position+contractPlayer.transform.right*5,7);
                contractStage=2;contractAt=EditorApplication.timeSinceStartup;return;
            }
            if(contractStage==2)
            {
                if(elapsed<1)return;
                Assert(PlayerPrefs.GetInt("coin")>=coinsBefore+7,"Coin magnet collects beyond the normal radius");
                var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)new{passed=true,scope="Directed probes, not route clears",assertions}});
                System.IO.File.WriteAllText("outputs/highway-chapter2-2026-09-27/contracts.json",json);
                EditorApplication.update-=ContractTick;TimeManager.isGameRunning=false;EditorApplication.isPaused=true;
            }
        }
        catch(Exception e)
        {
            System.IO.File.WriteAllText("outputs/highway-chapter2-2026-09-27/contracts-error.txt",e.ToString());
            EditorApplication.update-=ContractTick;EditorApplication.isPaused=true;
        }
    }
    public static object PlayerShape()
    {
        var p=Object.FindFirstObjectByType<PlayerScript>();
        return new { scale=p.transform.lossyScale.ToString(), colliders=p.GetComponentsInChildren<Collider>().Select(c=>new{c.name,type=c.GetType().Name,size=c.bounds.size.ToString(),centre=c.bounds.center.ToString()}).ToArray()};
    }
    public static object Weapons()
    {
        var player=Object.FindFirstObjectByType<PlayerScript>();
        return player.GetComponentsInChildren<WeaponScript>(true).Select(w=>new{w.name,w.bulletKind,w.damage,w.fireRate,active=w.gameObject.activeInHierarchy}).ToArray();
    }
    public static object Swarm()
    {
        var c=Object.FindFirstObjectByType<HighwayChapter2Controller>();
        return c.Vehicles.Where(v=>v.group==6).Select(v=>new{v.name,v.kind,v.station,v.Lane,v.HalfWidth,v.HalfLength,v.Spawned,v.Active,v.Distance,dead=v.Dead}).ToArray();
    }
    public static object ContactState()
    {
        var c=Object.FindFirstObjectByType<HighwayChapter2Controller>();var p=c.Player;
        var capsule=p.GetComponent<Collider>();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var origin=(Vector3)typeof(PlayerScript).GetField("routeLaneOrigin",flags).GetValue(p);var right=(Vector3)typeof(PlayerScript).GetField("routeRight",flags).GetValue(p);
        return new{d=c.Route.Distance,p=c.Player.transform.position.ToString(),origin=origin.ToString(),right=right.ToString(),lane=Vector3.Dot(p.transform.position-origin,right),
            smooth=typeof(PlayerScript).GetField("movementSmoothness",flags).GetValue(p),p.moveSensitivity,p.moveSensitivity_Devision,p.LateralSpeedMultiplier,
            cars=c.Vehicles.Where(v=>v.Active&&Mathf.Abs(v.Distance-c.Route.Distance)<80).Select(v=>
            {
                var col=v.GetComponent<BoxCollider>();bool hit=Physics.ComputePenetration(capsule,capsule.transform.position,capsule.transform.rotation,col,col.transform.position,col.transform.rotation,out var direction,out float depth);
                return new{v.name,v.Distance,v.Lane,v.HalfLength,v.HalfWidth,v.speed,centre=col.center.ToString(),world=v.transform.position.ToString(),hit,depth,
                    relative=Vector3.Dot(v.transform.position-origin,right),branch=c.UsesBranch(v)};
            }).ToArray()};
    }
    public static object StopAllDrivers()
    {
        int stopped=0;
        foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type=assembly.GetType("HighwayChapter2Playtest");if(type==null)continue;
            var active=type.GetField("active",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            if(active==null||!(bool)active.GetValue(null))continue;
            type.GetMethod("Stop").Invoke(null,null);stopped++;
        }
        return new{stopped};
    }
    public static object VehicleFacing()
    {
        var c=Object.FindFirstObjectByType<HighwayChapter2Controller>();
        object Describe(Transform t)=>new {t.name,rotation=t.eulerAngles.ToString(),children=t.GetComponentsInChildren<Transform>(true)
            .Where(x=>x.name.Contains("MRS_")||x.name=="Orientation"||x.name=="Model")
            .Select(x=>new{x.name,local=x.localEulerAngles.ToString(),world=x.eulerAngles.ToString()}).ToArray()};
        return new {cars=c.Vehicles.Where(v=>v.kind==HighwayVehicleKind.OneTon).Take(1).Select(v=>Describe(v.transform)).ToArray(),
            opposite=Describe(c.oppositeCars[0])};
    }
    public static object SceneryCorridors()
    {
        var route=Object.FindFirstObjectByType<HighwayRoute>();
        var scenery=route.transform.Find("HighwayChapter2_20260927/Korean city scenery");
        var groups=scenery.Cast<Transform>().Where(t=>t.name!="Landscape base").Select(t=>
        {
            var rs=t.GetComponentsInChildren<Renderer>();var b=rs.Length>0?rs[0].bounds:new Bounds(t.position,Vector3.zero);
            foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return new{t,b,rs};
        }).ToArray();
        var failures=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<float>>();
        for(float d=0;d<=route.length;d+=8)
            foreach(bool branch in new[]{false,true})
            {
                if(branch&&!route.forks.Any(fork=>d>=fork.start&&d<=fork.end))continue;
                route.Sample(d,branch,out var p,out var f);var right=Vector3.Cross(Vector3.up,f);
                foreach(float lane in new[]{-4.4f,0,4.4f})
                    foreach(var g in groups)
                        if(g.b.Contains(p+right*lane+Vector3.up*1.5f)&&g.rs.Any(r=>r.localBounds.Contains(r.transform.InverseTransformPoint(p+right*lane+Vector3.up*1.5f))))
                        {
                            if(!failures.ContainsKey(g.t.name))failures[g.t.name]=new System.Collections.Generic.List<float>();
                            failures[g.t.name].Add(d);
                        }
            }
        var result=new{groups=groups.Length,overlaps=failures.Select(x=>new{name=x.Key,min=x.Value.Min(),max=x.Value.Max(),samples=x.Value.Count}).ToArray()};
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)result});
        System.IO.File.WriteAllText("outputs/highway-chapter2-2026-09-27/scenery-corridors.json",json);return result;
    }
    public static object TraceDamage()
    {
        var c=Object.FindFirstObjectByType<HighwayChapter2Controller>();
        var p=Object.FindFirstObjectByType<PlayerScript>();
        p.DamageTaken+=(amount,cause)=>
        {
            var value=new{d=c.Route.Distance,player=p.transform.position.ToString(),lane=RouteFrame.Lane(p.transform.position,c.Route.Distance),amount,cause,
                cars=c.Vehicles.Where(v=>v.Active&&!v.Dead).Select(v=>new{v.name,v.kind,v.Distance,v.Lane,v.HalfWidth,v.HalfLength,v.speed,hp=v.Combat.CurrentHealth}).ToArray()};
            var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)value});
            System.IO.File.AppendAllText("outputs/highway-chapter2-2026-09-27/play-v2/damage-cars.jsonl",json+"\n");
        };
        return new{tracing=true};
    }
}
