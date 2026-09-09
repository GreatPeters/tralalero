if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var controller=map.GetComponent<IndianOceanAssets.ShooterSurvival.EncounterPlacementController>();
if(controller==null||controller.AppliedCount!=74)throw new System.InvalidOperationException("Workbook settings did not reach all 74 placements");
var rows=EncounterPlacementTables.Rows.Where(r=>r.scene==scene.name).ToArray();
foreach(var r in rows){
 var parent=map.Find(r.kind=="적 배치"?"Enemies":r.kind=="보너스 배치"?"Bonuses":"Props");var t=parent.Find(r.id);
 if(t.gameObject.activeSelf!=r.enabled)throw new System.InvalidOperationException("Enabled mismatch "+r.id);
 if(r.kind=="적 배치"){
 var e=t.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>();if(e.EventMode!=r.mode||Mathf.Abs(e.MoveSpeed-r.moveSpeed)>.001f)throw new System.InvalidOperationException("Enemy settings mismatch "+r.id);
 }else if(r.kind=="보너스 배치"){
 if(t.GetComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>().Rarity!=r.rarity)throw new System.InvalidOperationException("Bonus settings mismatch "+r.id);
 }else{
 var s=t.GetComponentsInChildren<ObstacleStats>().Single();float value=r.pattern==ObstaclePattern.Bucket?s.bucketAttachSeconds:s.value;
 if(Mathf.Abs(value-r.effectValue)>.001f||s.GetComponent<BoxCollider>().bounds.size.sqrMagnitude<.1f)throw new System.InvalidOperationException("Gimmick settings/physics mismatch "+r.id);
 }
}
var clone=typeof(object).GetMethod("MemberwiseClone",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
var variants=rows.Select(r=>(EncounterPlacementRow)clone.Invoke(r,null)).ToArray();
var enemyRow=variants.First(r=>r.kind=="적 배치"&&r.mode==IndianOceanAssets.ShooterSurvival.EnemyEventMode.PatrolBetweenStartAndTarget);enemyRow.moveSpeed=4.25f;
var bonusRow=variants.First(r=>r.kind=="보너스 배치");bonusRow.rarity=IndianOceanAssets.ShooterSurvival.Rarity.Unique;
var bucketRow=variants.First(r=>r.kind=="기믹 배치"&&r.pattern==ObstaclePattern.Bucket);bucketRow.effectValue=1.5f;
var hiddenRow=variants.First(r=>r.kind=="기믹 배치"&&r.pattern==ObstaclePattern.Light);hiddenRow.enabled=false;
var disabledEnemy=variants.First(r=>r.kind=="적 배치");disabledEnemy.enabled=false;
try{
 controller.BeginNewRun(variants);
 if(map.Find("Enemies").Find(enemyRow.id).GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>().MoveSpeed!=4.25f||
 map.Find("Bonuses").Find(bonusRow.id).GetComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>().Rarity!=IndianOceanAssets.ShooterSurvival.Rarity.Unique||
 map.Find("Props").Find(bucketRow.id).GetComponent<ObstacleStats>().bucketAttachSeconds!=1.5f||map.Find("Props").Find(hiddenRow.id).gameObject.activeSelf)
 throw new System.InvalidOperationException("Representative runtime overrides did not apply");
 if(!map.Find("Bonuses").Find(bonusRow.id).GetComponent<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>().Wall.buffType.ToString().EndsWith("_unique"))throw new System.InvalidOperationException("Rarity changed but the rolled bonus retained its old grade");
 if(IndianOceanAssets.ShooterSurvival.ChapterEnemyProgression.CollectEncounterEnemies(scene).Count!=24)throw new System.InvalidOperationException("Disabled enemy still affects growth count");
}finally{
 controller.BeginNewRun(rows);
}
var bad=rows.Select(r=>(EncounterPlacementRow)clone.Invoke(r,null)).ToArray();bad[bad.Length-1].id="intentionally_missing_test_id";
bool rejected=false;
try{controller.BeginNewRun(bad);}catch(System.IO.InvalidDataException){rejected=true;}
if(!rejected||map.Find("Enemies").Find(enemyRow.id).GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>().MoveSpeed!=2.5f)throw new System.InvalidOperationException("Preflight rejection/restore failed");
controller.BeginNewRun(rows);
System.IO.File.WriteAllText("tmp/sr18-data-work-2026-09-07/runtime-settings-probe.json","{\"passed\":true,\"applied\":74,\"enemySpeedOverride\":true,\"bonusRarityOverride\":true,\"bucketDurationOverride\":true,\"disabledGimmick\":true,\"disabledEnemyExcludedFromGrowth\":true,\"badIdRejectedBeforeMutation\":true,\"restored\":true}");
return new{passed=true,applied=74,representativeOverrides=5,badIdRejected=true,restored=true};
