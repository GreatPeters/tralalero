using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using IndianOceanAssets.ShooterSurvival;
public static class VerifyFatalHazard {
 public static object Main(string mode){
  if(!EditorApplication.isPlaying)throw new Exception("Play required");
  var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();var c=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
  c.PlayerPressedStartButton();if(!TimeManager.isGameRunning||!d.Running)throw new Exception("Actual start failed");d.GrantShield();var rb=p.GetComponent<Rigidbody>();bool initialKinematic=rb.isKinematic;
  float before=p.currentHealth;bool pauseProtected=true;
  if(mode=="normal")p.DieFromHazard(true);
  else {
   p.ApplyRunHealthBonus(5000,false);before=p.currentHealth;
   var go=new GameObject("Fatal contact verification fixture");var h=go.AddComponent<Chapter45Hazard>();h.manualOnly=true;h.floor=d.CurrentFloor;h.fatalContact=true;h.fallIntoHole=mode=="hole";h.damageCause=PlayerDamageCause.Traffic;h.ResetForRun(d);h.Trigger();h.Tick(1.6f);
   if(!h.Active)throw new Exception("Warning did not enter active contact phase");
   TimeManager.isGameRunning=false;h.Hit(p);pauseProtected=p.currentHealth==before&&h.Contacts==0;TimeManager.isGameRunning=true;
   h.Hit(p);if(h.Contacts!=1)throw new Exception("Contact was not recorded exactly once");h.Hit(p);if(h.Contacts!=1)throw new Exception("Repeated contact accepted");
  }
  bool valid=mode=="normal" ? p.currentHealth==before&&p.movement&&p.canShoot&&rb.isKinematic==initialKinematic&&!d.ShieldReady : p.currentHealth==0&&!p.movement&&!p.canShoot&&rb.isKinematic&&p.LastDamageWasFatal&&pauseProtected;
  var result=new{mode,passed=valid,before,hp=p.currentHealth,p.movement,p.canShoot,kinematic=rb.isKinematic,shield=d.ShieldReady,pauseProtected,cause=p.LastDamageCause.ToString(),p.LastDamageWasFatal,fixture=true};
  var j=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");File.WriteAllText("outputs/chapter45-detailed-design-2026-10-03/hazard-after-"+mode+".json",(string)j.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));
  if(!valid)throw new Exception("Hazard verification failed: "+mode);return result;
 }
}
