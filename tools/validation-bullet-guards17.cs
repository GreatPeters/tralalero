using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class ValidationBulletGuards17 {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Field(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static void Property(object o,string n,object v)=>Field(o,"<"+n+">k__BackingField",v);
 static object Invoke(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
 static string Json(object x)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 public static object Main(string folder){
  if(!EditorApplication.isPlaying||Directory.Exists(folder))throw new Exception("Fresh Play/output required");Directory.CreateDirectory(folder);
  var d=Object.FindFirstObjectByType<Chapter45Director>();var p=d.Player;Object.FindFirstObjectByType<CanvasScript>().PlayerPressedStartButton();p.canShoot=false;
  var pool=Object.FindFirstObjectByType<BulletPooler>();var originalTargets=d.targets;var root=new GameObject("QA17 temporary guard fixture");var t=root.AddComponent<Chapter45Target>();var col=root.AddComponent<BoxCollider>();col.isTrigger=true;col.size=Vector3.one*3;t.hitCollider=col;t.health=40;t.healthKey="QA17_NoSetting_Target";t.floor=d.CurrentFloor;t.distance=d.Distance;t.activationLead=32;t.targetRenderers=Array.Empty<Renderer>();root.transform.position=p.transform.position+Vector3.forward*12;
  var checks=new List<object>();var errors=new List<string>();float factor=TimeManager.timeFactor;bool gameRunning=TimeManager.isGameRunning;bool running=d.Running;var originalRiding=typeof(Chapter45Director).GetField("riding",F).GetValue(d);var fixtureLift=root.AddComponent<Chapter45Lift>();
  try {
   d.targets=new[]{t};
   foreach(BulletKind kind in new[]{BulletKind.Water,BulletKind.Bomb})foreach(string test in new[]{"inside","outside","disabled","inactive","not-admitted","ineligible","wrong-deck","paused","game-stopped","director-stopped","transfer","expired","no-chapter","shield","zero-damage"}){
    root.SetActive(true);t.choiceIndex=-1;t.floor=d.CurrentFloor;TimeManager.timeFactor=factor;TimeManager.isGameRunning=gameRunning;Property(d,"Running",running);Field(d,"riding",null);t.ResetForRun(d);t.Tick(0);Property(t,"Shielded",false);
    var q=(Queue<GameObject>)typeof(BulletPooler).GetField(kind==BulletKind.Water?"poolWater":"poolBomb",F).GetValue(pool);int count=q.Count;
    var bulletRoot=pool.Get(kind,root.transform);if(bulletRoot==null)throw new Exception("Missing authored pool kind "+kind);bulletRoot.transform.SetParent(null,true);bulletRoot.transform.position=col.bounds.center;
    var bullet=bulletRoot.GetComponentInChildren<BulletScript>(true);Field(bullet,"bulletPooler",pool);bullet.SetDirection(Vector3.forward,test=="no-chapter"?null:p,test=="zero-damage"?0:4);bullet.ProjectileKind=kind;
    if(test=="outside")bulletRoot.transform.position=col.bounds.max+Vector3.one;
    if(test=="disabled")col.enabled=false;
    if(test=="inactive")root.SetActive(false);
    if(test=="not-admitted")Field(t,"admitted",false);
    if(test=="ineligible")t.choiceIndex=int.MaxValue;
    if(test=="wrong-deck")Field(bullet,"launchDeck",d.CurrentFloor+1);
    if(test=="paused")TimeManager.timeFactor=0;
    if(test=="game-stopped")TimeManager.isGameRunning=false;
    if(test=="director-stopped")Property(d,"Running",false);
    if(test=="transfer")Field(d,"riding",fixtureLift);
    if(test=="expired")Field(bullet,"elapsedDuration",BulletScript.CurrentMissileDuration);
    if(test=="shield")Property(t,"Shielded",true);
    Physics.SyncTransforms();float before=t.Health;Invoke(bullet,"FixedUpdate");float after=t.Health;bool returned=!bulletRoot.activeSelf;int countAfter=q.Count;
    bool damageExpected=test=="inside";bool returnExpected=damageExpected||test=="shield"||test=="zero-damage"||test=="expired";
    bool ok=Mathf.Abs(after-(before-(damageExpected?4:0)))<.001f&&returned==returnExpected&&countAfter==count-(returnExpected?0:1);
    float afterSecond=after;int secondCount=countAfter;if(returnExpected){Invoke(bullet,"FixedUpdate");Invoke(bullet,"OnTriggerEnter",col);afterSecond=t.Health;secondCount=q.Count;ok&=Mathf.Abs(afterSecond-after)<.001f&&secondCount==countAfter;}
    checks.Add(new{ok,kind=kind.ToString(),test,before,after,returned,poolBefore=count,poolAfter=countAfter,poolAfterDuplicate=secondCount,healthAfterDuplicate=afterSecond});
    if(bulletRoot.activeSelf)pool.Return(bulletRoot);
   }
  }catch(Exception e){errors.Add(e.ToString());}
  finally{d.targets=originalTargets;TimeManager.timeFactor=factor;TimeManager.isGameRunning=gameRunning;Property(d,"Running",running);Field(d,"riding",originalRiding);Object.Destroy(root);}
  var data=new{directedFixture=true,ordinaryPlaythrough=false,manualFixedUpdate=true,manualDuplicateTrigger=true,authoredWaterAndBombPools=true,temporaryTarget=true,productSaved=false,checks,errors};File.WriteAllText(Path.Combine(folder,"result.json"),Json(data));return data;
 }
}


