using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using IndianOceanAssets.ShooterSurvival;
public static class ReproShieldHazard{
 public static object Main(){
  if(!EditorApplication.isPlaying)throw new Exception("Play required");var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();var c=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
  c.PlayerPressedStartButton();if(!TimeManager.isGameRunning||!d.Running)throw new Exception("Actual start failed");d.GrantShield();var body=p.GetComponent<Rigidbody>();
  var before=new{hp=p.currentHealth,p.movement,p.canShoot,kinematic=body.isKinematic,shield=d.ShieldReady};p.DieFromHazard(true);
  var after=new{hp=p.currentHealth,p.movement,p.canShoot,kinematic=body.isKinematic,shield=d.ShieldReady};var result=new{before,after,aliveButFrozen=p.currentHealth>0&&!p.movement&&!p.canShoot&&body.isKinematic,gameOver=CanvasScript.isGameOver,fixture=true};
  var j=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");File.WriteAllText("outputs/chapter45-detailed-design-2026-10-03/shield-hazard-before.json",(string)j.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;
 }
}
