if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
player.enabled=false;foreach(var w in player.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.WeaponScript>(true))w.enabled=false;
foreach(var c in player.GetComponentsInChildren<Collider>(true))c.enabled=false;
var enemies=map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>();
var spots=map.Find("Props").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>();
var ambush=enemies.Where(e=>(int)e.EventMode==6).ToArray();
if(ambush.Length!=5||ambush.Any(e=>!e.IsAmbushHidden||e.GetComponent<Collider>().enabled))throw new System.InvalidOperationException("Ambush waiting visibility/collision failed");
Physics.SyncTransforms();
var hazards=map.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")).Select(t=>t.GetComponentsInChildren<ObstacleStats>().Single()).ToArray();
if(hazards.Length!=24||hazards.Any(h=>h.GetComponent<BoxCollider>().bounds.size.sqrMagnitude<.1f))throw new System.InvalidOperationException("Missing runtime hazard collider");
UnityEngine.Time.timeScale=1;
IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=true;IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=1;
var first=spots.First(s=>(int)s.Targets[0].EventMode==6);player.transform.position=first.transform.position;
int moves=0;var advance=typeof(IndianOceanAssets.ShooterSurvival.EnemyEventController).GetMethod("AdvanceEvent",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
foreach(var spot in spots){
 spot.SendMessage("OnTriggerEnter",player.GetComponent<Collider>(),SendMessageOptions.RequireReceiver);
 if(spot.GetComponent<BoxCollider>().enabled||spot.Targets[0].RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting||spot.ActivateTargets())throw new System.InvalidOperationException("Spot failed "+spot.name);
 var e=spot.Targets[0];
 if(e.HasUsableTarget){advance.Invoke(e,new object[]{.7f});moves++;}
}
if(ambush.Any(e=>e.IsAmbushHidden||e.RuntimeState!=IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Attacking))throw new System.InvalidOperationException("Ambush move-to-shoot failed");
// Freeze during the wind-up. The finish probe checks pause, resumes and inspects released projectiles.
IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;
return new{spots=spots.Length,moving=moves,ambush=ambush.Length,hazards=hazards.Length,phase="paused-before-release"};
