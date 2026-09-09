if(!UnityEditor.EditorApplication.isPlaying) throw new InvalidOperationException("Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
if(IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning) throw new InvalidOperationException("Before Tap to Play required");
player.enabled=false;
foreach(var root in scene.GetRootGameObjects()) {
 foreach(var w in root.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.WeaponScript>(true))w.enabled=false;
 foreach(var c in root.GetComponentsInChildren<Collider>(true)) if(c is not MeshCollider)c.enabled=false;
}
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var binding=map.GetComponent<IndianOceanAssets.ShooterSurvival.EncounterPlacementController>();
binding.BeginNewRun();
var rows=EncounterPlacementTables.Rows.Where(r=>r.kind=="적 배치").ToArray();
foreach(var row in rows) {
 var enemy=map.Find("Enemies").Find(row.id).GetComponent<IndianOceanAssets.ShooterSurvival.EnemyScript_space>();
 if((float)enemy.GetType().GetField("_damage",flags).GetValue(enemy)!=row.damage || (float)enemy.GetType().GetField("_health",flags).GetValue(enemy)!=row.health || (EnemyTier)enemy.GetType().GetField("enemyTier",flags).GetValue(enemy)!=row.tier)throw new Exception("Workbook combat mismatch "+row.id);
}
UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.ChapterEnemyStatController>().ApplyStats();
var last=map.Find("Enemies").Find(rows.Last().id).GetComponent<IndianOceanAssets.ShooterSurvival.EnemyScript_space>();
if((float)last.GetType().GetField("_health",flags).GetValue(last)!=rows.Last().health)throw new Exception("Chapter overwrote placement stats");
if(player.currentHealth!=100||player.MaxHealth!=100)throw new Exception("Health defaults");
var pair=map.Find("Bonuses").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.BonusWallChoicePair>().OrderBy(p=>p.name).First();
var contact=typeof(IndianOceanAssets.ShooterSurvival.WallScript).GetMethod("OnTriggerEnter",flags);
foreach(var type in new[]{IndianOceanAssets.ShooterSurvival.BuffType.tungtung_rare,IndianOceanAssets.ShooterSurvival.BuffType.boombar_rare}) {
 pair.PrepareForRun(IndianOceanAssets.ShooterSurvival.Rarity.Rare,true);
 var wall=pair.Left.Wall; wall.buffType=type; wall.buffSFX=null;
 player.lastWallTouchTime=-1000;
 IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=true;
 contact.Invoke(wall,new object[]{player.GetComponent<Collider>()});
}
var helpers=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.ExtraHelpBuffScript>(UnityEngine.FindObjectsSortMode.None);
if(helpers.Length!=2||player.extraHelpCount!=2||player.extraHelpWeaponScript.Any(w=>w==null))throw new Exception("Bonus helper spawn/count/weapon failure");
foreach(var helper in helpers)foreach(var c in helper.GetComponentsInChildren<Collider>(true))c.enabled=false;
return new{started=true,applied=binding.AppliedCount,combatRows=rows.Length,helpers=helpers.Select(h=>new{name=h.name,type=h.helpType.ToString()}).ToArray(),buckets=map.Find("Props").GetComponentsInChildren<ObstacleStats>().Count(o=>o.obstaclePattern==ObstaclePattern.Bucket),player.currentHealth,player.MaxHealth};
