if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var ambush=map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>().Where(e=>(int)e.EventMode==6).ToArray();
var held=typeof(IndianOceanAssets.ShooterSurvival.EnemyScript_space).GetField("heldProjectile",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
if(ambush.Any(e=>!((Transform)held.GetValue(e.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyScript_space>())).IsChildOf(e.transform)))throw new System.InvalidOperationException("Projectile released while paused");
IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=true;
return "Pause passed; wind-ups resumed";
