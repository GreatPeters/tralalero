// Runtime authoring probe only: no player shots, enemy contact, kills, coins or saves.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var controllers=map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>();
var spots=map.Find("Props").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>();
var before=controllers.ToDictionary(c=>c,c=>c.transform.position);var results=new System.Collections.Generic.List<object>();
bool oldRunning=IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning,oldPlayerEnabled=player.enabled;
var weapons=player.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.WeaponScript>(true).ToDictionary(w=>w,w=>w.enabled);
var bodies=controllers.SelectMany(c=>c.GetComponentsInChildren<Collider>(true)).ToDictionary(c=>c,c=>c.enabled);
var advance=typeof(IndianOceanAssets.ShooterSurvival.EnemyEventController).GetMethod("AdvanceEvent",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
try{
 player.enabled=false;foreach(var w in weapons)w.Key.enabled=false;foreach(var c in bodies)c.Key.enabled=false;
 IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=true;
 foreach(var spot in spots){
  spot.SendMessage("OnTriggerEnter",player.GetComponent<Collider>(),SendMessageOptions.RequireReceiver);
  bool consumed=!spot.GetComponent<BoxCollider>().enabled;
  if(!consumed||spot.Targets.Any(c=>c.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting))throw new System.InvalidOperationException("Activation failed "+spot.name);
  if(spot.ActivateTargets())throw new System.InvalidOperationException("Repeated activation accepted "+spot.name);
  results.Add(new{name=spot.name,targets=spot.Targets.Length,consumed,states=spot.Targets.Select(c=>c.RuntimeState.ToString()).ToArray()});
 }
 int moved=0;foreach(var c in controllers.Where(c=>c.HasUsableTarget)){
  advance.Invoke(c,new object[]{.5f});if(Vector3.Distance(before[c],c.transform.position)<.1f)throw new System.InvalidOperationException("Target movement did not advance "+c.name);moved++;
 }
 IndianOceanAssets.ShooterSurvival.EnemyEventController.ResetAllForNewRun();IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot.ResetAllForNewRun();
 if(controllers.Any(c=>c.RuntimeState!=IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting||Vector3.Distance(before[c],c.transform.position)>.001f))throw new System.InvalidOperationException("Enemy reset failed");
 if(spots.Any(s=>!s.GetComponent<BoxCollider>().enabled))throw new System.InvalidOperationException("Spot reset failed");
 var asm=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=asm.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 var report=new{status="passed",scope="Real Play Mode; root-collider trigger callback, state transitions and reset; not a full balance run",spots=spots.Length,enemies=controllers.Length,movingEnemies=moved,results};
 System.IO.File.WriteAllText("map-concepts/sr18-encounters-applied-2026-09-06/runtime-probe.json",(string)serialize.Invoke(null,new object[]{report}));
 return new{status="passed",spots=spots.Length,enemies=controllers.Length,movingEnemies=moved,resetPassed=true};
}finally{
 IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=oldRunning;player.enabled=oldPlayerEnabled;foreach(var w in weapons)if(w.Key!=null)w.Key.enabled=w.Value;foreach(var c in bodies)if(c.Key!=null)c.Key.enabled=c.Value;
}
