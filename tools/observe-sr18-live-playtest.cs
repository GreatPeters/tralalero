if(!UnityEditor.EditorApplication.isPlaying)throw new InvalidOperationException("Play required");
const string key="SR18.LivePlaytest.20260909";
string folder=UnityEditor.SessionState.GetString(key+".folder","");if(folder=="")throw new Exception("Missing snapshot");
string filename=folder+"/timeline-"+DateTime.Now.ToString("HHmmss")+".jsonl";
var serializer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null).GetMethod("SerializeObject",new[]{typeof(object)});
double next=0; double began=UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction observe=null;
observe=()=>{
 if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=observe;return;}
 double now=UnityEditor.EditorApplication.timeSinceStartup;if(now<next)return;next=now+1;
 var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();if(p==null)return;
 var es=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.EnemyEventController>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None);
 var bonus=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.BonusWallChoicePair>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None);
 var info=new{t=now-began,gameTime=Time.time,run=IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning,hp=p.currentHealth,max=p.MaxHealth,attack=p.currentDamage,canShoot=p.canShoot,pos=new[]{p.transform.position.x,p.transform.position.y,p.transform.position.z},yaw=p.transform.eulerAngles.y,turning=p.IsWorldYawTurnActive,dead=es.Count(e=>e.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Dead),enemyEvents=es.Where(e=>e.RuntimeState!=IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting).Select(e=>new{id=e.name,state=e.RuntimeState.ToString(),pos=new[]{e.transform.position.x,e.transform.position.y,e.transform.position.z}}).ToArray(),choices=bonus.Where(b=>b.Selected!=null).Select(b=>new{id=b.name,selected=b.Selected.name}).ToArray(),helpers=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.ExtraHelpBuffScript>(UnityEngine.FindObjectsSortMode.None).Select(h=>new{type=h.helpType.ToString(),hp=h.currentHealth,pos=new[]{h.transform.position.x,h.transform.position.y,h.transform.position.z}}).ToArray(),coin=MoneyScript.S!=null?MoneyScript.S.Coin:0};
 System.IO.File.AppendAllText(filename,(string)serializer.Invoke(null,new object[]{info})+"\n");
};
UnityEditor.EditorApplication.update+=observe;
return new{recording=filename,playerMovementUnchanged=true,healthUnchanged=true};
