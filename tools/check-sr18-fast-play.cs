if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var bindings=map.GetComponents<IndianOceanAssets.ShooterSurvival.EncounterPlacementController>();
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
int enemies=map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>().Length;
int hazards=map.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("SR18_L_G")).Sum(t=>t.GetComponentsInChildren<ObstacleStats>().Length);
if(bindings.Length!=1||bindings[0].AppliedCount!=74||enemies!=25||hazards!=42||player.currentHealth!=100||player.MaxHealth!=100)throw new System.InvalidOperationException("Fast Play state did not reset correctly");
return new{passed=true,applied=74,enemies,hazards,player.currentHealth,player.MaxHealth,fastPlayOptions=UnityEditor.EditorSettings.enterPlayModeOptions.ToString()};
