var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var enemies = UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.EnemyScript_space>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None).OrderBy(e=>e.name).ToArray();
var data = enemies.Select(e => {
 var ev=e.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>();
 var a=e.GetComponentInChildren<UnityEngine.Animator>(true);
 var cl=e.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyCornerClearance>();
 return new {id=e.name,damage=typeof(IndianOceanAssets.ShooterSurvival.EnemyScript_space).GetField("_damage",flags).GetValue(e),health=typeof(IndianOceanAssets.ShooterSurvival.EnemyScript_space).GetField("_health",flags).GetValue(e),tier=ForwardEnemyTierResolver.ResolveOrFallback(e.name,EnemyTier.Normal).ToString(),position=e.transform.position.ToString("F3"),mode=ev.EventMode.ToString(),target=ev.HasUsableTarget?ev.TargetPoint.position.ToString("F3"):"",visual=a.transform.localEulerAngles.ToString(),cornerDistance=cl!=null?cl.DistanceAfterCorner(ev.HasUsableTarget?ev.TargetPoint.position:e.transform.position):0};
}).ToArray();
var serializer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null).GetMethod("SerializeObject",new[]{typeof(object)});
var json=(string)serializer.Invoke(null,new object[]{data});
System.IO.Directory.CreateDirectory("tmp/sr18-polish-2026-09-09");
System.IO.File.WriteAllText("tmp/sr18-polish-2026-09-09/before-enemies.json",json);
return new {playing=UnityEditor.EditorApplication.isPlaying,dirty=scene.isDirty,enemies=data,spawners=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.NoryangjinUpgradeExtraHelpSpawner>(UnityEngine.FindObjectsSortMode.None).Select(s=>new{name=s.name,configured=s.IsConfigured}).ToArray()};
