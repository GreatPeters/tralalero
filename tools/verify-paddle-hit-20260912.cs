if(!UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isPaused)throw new System.InvalidOperationException("Unpaused Play Mode required");
OpeningStoryUI.Instance?.Skip();
var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var customizer=player.GetComponentInChildren<PlayerCosmeticCustomizer>(true);var model=customizer.modelRoot;
var obstacle=UnityEngine.Object.FindObjectsByType<ObstacleStats>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None).First(o=>o.obstaclePattern==ObstaclePattern.Oldman);
var paddle=obstacle.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.SimpleProjectile>(true).Single(p=>p.name=="Paddle");
var collider=player.GetComponents<UnityEngine.Collider>().First(c=>c.CompareTag("Player"));
bool wasEnabled=player.enabled,wasRunning=IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning;float hp=player.currentHealth;
player.enabled=false;IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=true;UnityEngine.Time.timeScale=1;
obstacle.gameObject.SetActive(true);paddle.gameObject.SetActive(true);paddle.SetFlightActive(true);
var position=player.transform.position;var rotation=player.transform.rotation;var modelRotation=model.localRotation;
var onHit=typeof(IndianOceanAssets.ShooterSurvival.SimpleProjectile).GetMethod("OnTriggerEnter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
onHit.Invoke(paddle,new object[]{collider});float after=player.currentHealth;onHit.Invoke(paddle,new object[]{collider});
if(UnityEngine.Mathf.Abs(hp-after-paddle.damage)>.001f||player.currentHealth!=after)throw new System.InvalidOperationException("Paddle damage/re-entry gate failed");
double start=UnityEditor.EditorApplication.timeSinceStartup;bool observedSpin=false;UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 try{
  double seconds=UnityEditor.EditorApplication.timeSinceStartup-start;
  if(seconds<2.1)observedSpin|=UnityEngine.Quaternion.Angle(model.localRotation,modelRotation)>10;
  if(seconds<2.6)return;
  if(UnityEngine.Vector3.Distance(position,player.transform.position)>.001f||UnityEngine.Quaternion.Angle(rotation,player.transform.rotation)>.001f)throw new System.InvalidOperationException("Paddle altered player root");
  if(!observedSpin||UnityEngine.Quaternion.Angle(modelRotation,model.localRotation)>.01f)throw new System.InvalidOperationException("Model did not spin and restore");
  System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/paddle-hit-verified.json","{\"rootPosePreserved\":true,\"modelSpunAndRestored\":true,\"singleDamage\":"+paddle.damage.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"repeatContactIgnored\":true,\"controlledStationaryProbe\":true}");
 }catch(System.Exception error){System.IO.File.WriteAllText("map-concepts/skins-reststop-2026-09-12/paddle-hit-error.txt",error.ToString());}
 finally{if(UnityEditor.EditorApplication.timeSinceStartup-start>=2.6){UnityEditor.EditorApplication.update-=tick;player.enabled=wasEnabled;player.currentHealth=hp;player.UpdateHealth();IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=wasRunning;}}
};UnityEditor.EditorApplication.update+=tick;return "Paddle hit probe started";
