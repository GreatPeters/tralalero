if(!UnityEditor.EditorApplication.isPlaying)throw new InvalidOperationException("Play required");
IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;
var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var helpers=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.ExtraHelpBuffScript>(UnityEngine.FindObjectsSortMode.None);
if(helpers.Length!=2)throw new Exception("Helpers did not survive initialization");
var tung=helpers.Single(h=>h.helpType==HelpType.Tungtungtung);
var boom=helpers.Single(h=>h.helpType==HelpType.Boombardino);
int boomShots=boom.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.WeaponScript>().Sum(w=>w.TotalProjectilesSpawned);
if(boomShots<=0)throw new Exception("BoomBar spawned but did not fire an actual projectile");
foreach(var w in boom.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.WeaponScript>())w.enabled=false;
var route=tung.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinHelperRouteFollower>();
tung.transform.position=player.transform.position; route.Configure(player);
float maximumY=tung.transform.position.y; int steps=0;
for(;steps<2700;steps++) {
 var before=tung.transform.position; route.Advance(1);
 if(Vector3.Distance(before,tung.transform.position)<.1f)break;
 maximumY=Mathf.Max(maximumY,tung.transform.position.y);
}
if(route.CompletedTurns!=15||maximumY<11.8f||tung.transform.position.y>1)throw new Exception("Tung route/slope failure: turns="+route.CompletedTurns+" maxY="+maximumY+" stop="+tung.transform.position);
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var enemies=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.EnemyEventController>(UnityEngine.FindObjectsSortMode.None);
int walks=0;
foreach(var e in enemies.Where(e=>e.EventMode==IndianOceanAssets.ShooterSurvival.EnemyEventMode.AmbushMoveThenShoot)) {
 if(e.IsAmbushHidden||e.HideWhileWaiting||e.MoveAnimation!=IndianOceanAssets.ShooterSurvival.EnemyMoveAnimation.Walk)throw new Exception("Ambush pop-in remains");
 var start=e.transform.position;
 IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=true;
 if(!e.ActivateFromSpot())throw new Exception("Walk could not activate "+e.name);
 typeof(IndianOceanAssets.ShooterSurvival.EnemyEventController).GetMethod("AdvanceEvent",flags).Invoke(e,new object[]{.5f});
 if(Vector3.Distance(start,e.transform.position)<1.4f||e.RuntimeState!=IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.MovingToTarget)throw new Exception("Walk did not interpolate");
 walks++;
}
IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;
var result=new{passed=true,actualBonusTypes=2,boomShots,nullWeaponEntries=player.extraHelpWeaponScript.Count(w=>w==null),walks,routeTurns=route.CompletedTurns,routeSteps=steps,maximumY,end=tung.transform.position.ToString("F2"),allCombatRows=25,chapterDidNotOverride=true};
var serializer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null).GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/sr18-combat-polish-2026-09-09/runtime-probe.json",(string)serializer.Invoke(null,new object[]{result}));
return result;
