var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity")throw new System.InvalidOperationException("SR18 required");
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
string backup=null;
if(!UnityEditor.EditorApplication.isPlaying){string folder="tmp/backups/sr18-data-work-2026-09-07/"+System.DateTime.Now.ToString("HHmmss");System.IO.Directory.CreateDirectory(folder);backup=folder+"/live-before.unity";UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,backup,true);}
return new{scene=scene.path,scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,backup,player.originalHealth,player.currentHealth,player.MaxHealth,gameRunning=IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning,canvas=scene.GetRootGameObjects().Where(g=>g.name=="Canvas").Select(g=>g.activeSelf).ToArray(),healthLabels=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)).Where(t=>t.name=="HealthValue").Select(t=>t.text).ToArray()};
