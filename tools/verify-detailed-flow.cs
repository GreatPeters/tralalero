using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using IndianOceanAssets.ShooterSurvival;
public static class VerifyDetailedFlow {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly List<string> checks=new();
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add(message);}
 static void Set(object obj,string property,object value)=>obj.GetType().GetField("<"+property+">k__BackingField",Private).SetValue(obj,value);
 static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Private).Invoke(obj,args);
 public static object Main(int branch){
  if(!EditorApplication.isPlaying)throw new Exception("Play required");checks.Clear();
  var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var c=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();c.PlayerPressedStartButton();
  Check(d.Running&&TimeManager.isGameRunning,"actual start button starts run");
  var actor=d.encounters.SelectMany(e=>e.actors).First(e=>e!=null);actor.gameObject.SetActive(true);actor.ApplyStat(2,12,EnemyTier.Normal);typeof(EnemyScript_space).GetField("playerScript",Private).SetValue(actor,p);
  var deck=actor.GetComponent<Chapter45Deck>();int originalFloor=d.CurrentFloor;float hp=p.currentHealth,enemyHP=actor.CurrentHealth;int coins=MoneyScript.S.Coin;
  TimeManager.isGameRunning=false;Call(actor,"ResolvePlayerContact");Check(p.currentHealth==hp&&actor.CurrentHealth==enemyHP&&MoneyScript.S.Coin==coins,"paused body contact changes neither HP nor wallet");TimeManager.isGameRunning=true;
  deck.floor=999;Call(actor,"ResolvePlayerContact");Check(p.currentHealth==hp&&actor.CurrentHealth==enemyHP&&MoneyScript.S.Coin==coins,"other-floor body contact changes neither HP nor wallet");deck.floor=originalFloor;
  var shotGo=new GameObject("Flow verification projectile");shotGo.transform.position=p.transform.position;var shot=shotGo.AddComponent<SimpleProjectile>();shot.Launch(Vector3.forward,1,5,8,actor.transform);Check(shot.CanDamage(p),"same-floor projectile admitted");
  TimeManager.isGameRunning=false;Check(!shot.CanDamage(p),"paused projectile rejected");TimeManager.isGameRunning=true;
  var fixture=new GameObject("Flow verification fixture");var nodes=new[]{1,2,3,4,8,5,6,7};
  d.route.segments=nodes.Select((f,i)=>new Chapter45Route.Segment{start=new Vector3(4000,i*8,0),end=new Vector3(4000,i*8,10),length=10,floor=f,label=f==8?"B1":f+"F"}).ToArray();
  foreach(var e in d.encounters)e.Retire();d.encounters=Array.Empty<Chapter45Encounter>();d.targets=Array.Empty<Chapter45Target>();d.hazards=Array.Empty<Chapter45Hazard>();d.goals=Array.Empty<Chapter45Goal>();
  var choice=fixture.AddComponent<Chapter45Choice>();choice.kind=Chapter45Choice.ChoiceKind.FloorRoute;choice.floor=4;choice.distance=31;choice.endDistance=40;choice.leftName="B1";choice.rightName="5F";
  var future=new GameObject("Unvisited branch reward").AddComponent<Chapter45Choice>();future.transform.SetParent(fixture.transform);future.kind=Chapter45Choice.ChoiceKind.ShieldOrAttack;future.floor=6;future.distance=61;future.rewardDistance=62;
  d.choices=new[]{choice,future};var connections=new List<Chapter45Lift>();
  void Lift(int from,int to,int pick=-1){var go=new GameObject("Fixture link "+from+" to "+to);go.transform.SetParent(fixture.transform);var l=go.AddComponent<Chapter45Lift>();l.afterSegment=from;l.destinationSegment=to;l.duration=1;l.exactDuration=true;l.choiceIndex=pick<0?-1:0;l.requiredChoice=pick;connections.Add(l);}
  Lift(0,1);connections[0].minimumFloorSeconds=30;Lift(1,2);Lift(2,3);Lift(3,4,0);Lift(3,5,1);Lift(4,7,0);Lift(5,6,1);Lift(6,7,1);d.lifts=connections.ToArray();d.BeginRun();
  Check(!shot.CanDamage(p),"projectile from previous floor rejected");
  Set(d,"FloorElapsed",29.9f);Call(d,"Advance",100f);Check(!d.IsTransferring&&d.CurrentFloor==1,"1F cannot leave before active30 seconds");
  TimeManager.isGameRunning=false;float clock=d.FloorElapsed;Call(d,"Update");Call(d,"Advance",100f);Check(d.FloorElapsed==clock&&!d.IsTransferring,"pause does not advance floor clock or open gate");TimeManager.isGameRunning=true;
  Set(d,"FloorElapsed",30f);Call(d,"Advance",100f);Check(d.IsTransferring,"1F lift opens at30 without kill requirement");
  deck.floor=1;Call(actor,"ResolvePlayerContact");Check(p.currentHealth==hp&&actor.CurrentHealth==enemyHP&&MoneyScript.S.Coin==coins,"transfer body contact changes neither HP nor wallet");
  var visits=new List<int>{1};Call(d,"Advance",2f);visits.Add(d.CurrentFloor);Check(d.CurrentFloor==2&&d.Distance==10&&d.FloorElapsed==0,"explicit arrival rebases distance and floor clock");
  Check(!choice.Selected&&!future.Selected,"future floor choices not committed by distance");
  for(int guard=0;guard<10&&d.CurrentFloor!=7;guard++){
   if(d.CurrentFloor==4)choice.Commit(branch==0?-2:2);
   Call(d,"Advance",100f);Check(d.IsTransferring,"authored eligible connection boards from "+d.CurrentFloor);Call(d,"Advance",2f);visits.Add(d.CurrentFloor);
  }
  int[] expected=branch==0?new[]{1,2,3,4,8,7}:new[]{1,2,3,4,5,6,7};Check(visits.SequenceEqual(expected),"selected branch visits exact floors: "+string.Join(",",visits));
  if(branch==0)Check(!future.Selected&&!future.Rewarded,"B1 skip does not claim6F reward");
  Check(choice.Rewarded&&MoneyScript.S.Coin==coins,"destination choice carries no implicit coin or heal reward");
  var hgo=new GameObject("Cinema contract fixture");var hold=hgo.AddComponent<RestStopHoldout>();hold.chapter45Owner=d;hold.requiredFloor=6;hold.choiceIndex=0;hold.requiredChoice=1;hold.police=Array.Empty<EnemyEventController>();hold.entranceSignals=Array.Empty<Renderer>();hold.entrances=Array.Empty<Transform>();hold.center=hgo.transform;hold.center.position=p.transform.position;hold.BeginRun();
  Call(hold,"Update");Check(!hold.Active&&!hold.OnChapterFloor,"cinema does not start on7F with matchingXZ");Check(hold.Duration==30,"cinema duration is exactly30 independent ofCh3 workbook");
  Set(d,"CurrentSegment",6);Set(choice,"Selection",1);hold.center.position=p.transform.position;Call(hold,"Update");Check(hold.Active,"cinema admits only chosen6F");
  Set(hold,"Elapsed",29.99f);TimeManager.isGameRunning=false;Call(hold,"Update");Check(hold.Elapsed==29.99f&&!hold.Completed,"cinema pause preserves countdown");TimeManager.isGameRunning=true;
  Set(hold,"Elapsed",29.99999f);Call(hold,"Update");Check(hold.Completed&&!hold.Active&&hold.Elapsed==30,"cinema reaches30 and releases stationary control");
  hold.BeginRun();Check(!hold.Completed&&!hold.Active&&hold.Elapsed==0,"cinema retry clears elapsed and completion");
  var result=new{branch,checks=checks.ToArray(),visits=visits.ToArray(),fixture=true,usesDirectedClockAndRouteState=true,ordinaryPlaythrough=false};
  var json=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");File.WriteAllText("outputs/chapter45-detailed-design-2026-10-03/flow-contract-"+branch+".json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;
 }
}
