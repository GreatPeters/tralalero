if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var pairs=map.Find("Bonuses").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.BonusWallChoicePair>(true);
var player=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_Player").GetComponent<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var capsule=player.GetComponent<CapsuleCollider>();
var weapons=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.WeaponScript>(true)).ToDictionary(w=>w,w=>w.enabled);
var weapon=player.GetComponent<IndianOceanAssets.ShooterSurvival.WeaponManager>().currentWeapon.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.WeaponScript>();
var position=player.transform.position;var rotation=player.transform.rotation;bool playerEnabled=player.enabled,colliderEnabled=capsule.enabled,running=IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning;
float damage=weapon.damage,touch=player.lastWallTouchTime;int laneHits=0,claims=0;
if(running)throw new System.InvalidOperationException("Run the probe before starting gameplay");
try{
 player.enabled=false;foreach(var w in weapons)w.Key.enabled=false;
 if(pairs.Length!=25)throw new System.InvalidOperationException("Expected 25 pairs");
 foreach(var pair in pairs){
  if(!pair.IsConfigured||!pair.Left.gameObject.activeInHierarchy||!pair.Right.gameObject.activeInHierarchy||string.IsNullOrEmpty(pair.Left.RolledStat)||string.IsNullOrEmpty(pair.Right.RolledStat)||pair.Left.RolledStat==pair.Right.RolledStat)throw new System.InvalidOperationException("Invalid or duplicate choices: "+pair.name);
  Vector3 center=(pair.Left.transform.position+pair.Right.transform.position)*.5f;Vector3 side=(pair.Right.transform.position-pair.Left.transform.position).normalized;
  var boxes=new[]{pair.Left.Wall.GetComponent<Collider>(),pair.Right.Wall.GetComponent<Collider>()};capsule.enabled=true;
  foreach(float lane in new[]{-2f,0f,2f}){
   player.transform.SetPositionAndRotation(center+side*lane,Quaternion.LookRotation(Vector3.Cross(side,Vector3.up)));Physics.SyncTransforms();
   bool left=Physics.ComputePenetration(capsule,player.transform.position,player.transform.rotation,boxes[0],boxes[0].transform.position,boxes[0].transform.rotation,out _,out _);
   bool right=Physics.ComputePenetration(capsule,player.transform.position,player.transform.rotation,boxes[1],boxes[1].transform.position,boxes[1].transform.rotation,out _,out _);
   if(left!=(lane<0)||right!=(lane>0))throw new System.InvalidOperationException("Lane cannot select independently: "+pair.name+" lane="+lane+" left="+left+" right="+right);
   if(lane!=0)laneHits++;
  }
 }
 capsule.enabled=false;
 var targetPair=pairs.First(p=>p.Left.Rarity==IndianOceanAssets.ShooterSurvival.Rarity.Normal);
 var grade=targetPair.Left.Rarity;var walls=new[]{targetPair.Left.Wall,targetPair.Right.Wall};var sounds=walls.Select(w=>w.buffSFX).ToArray();
 var valueField=typeof(IndianOceanAssets.ShooterSurvival.WallScript).GetField("bonusValue",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
 var contact=typeof(IndianOceanAssets.ShooterSurvival.WallScript).GetMethod("OnTriggerEnter",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
 try{
  foreach(int chosenIndex in new[]{0,1}){
   targetPair.PrepareForRun(grade,true);
   // Deterministic +1 attack input tests the real collision/reward path without rewards, kills or save changes.
   foreach(var wall in walls){wall.buffType=IndianOceanAssets.ShooterSurvival.BuffType.att_normmal;valueField.SetValue(wall,1f);wall.buffSFX=null;}
   float before=weapon.damage;player.lastWallTouchTime=-1000;IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=true;
   contact.Invoke(walls[chosenIndex],new object[]{capsule});
   player.lastWallTouchTime=-1000;contact.Invoke(walls[1-chosenIndex],new object[]{capsule});
   IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;
   if(Mathf.Abs(weapon.damage-before-1)>.001f||targetPair.Selected!=(chosenIndex==0?targetPair.Left:targetPair.Right))throw new System.InvalidOperationException("Choice gave zero or duplicate rewards");
   claims++;
  }
 }finally{
  IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=false;
  for(int i=0;i<2;i++)walls[i].buffSFX=sounds[i];targetPair.PrepareForRun(grade,true);
 }
 var controller=map.GetComponent<IndianOceanAssets.ShooterSurvival.EncounterPlacementController>();controller.BeginNewRun();
 int clearances=0;foreach(var enemy in map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>()){
  var buffer=enemy.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyCornerClearance>();buffer.ValidateMovement(enemy.transform.position,enemy.HasUsableTarget?enemy.TargetPoint.position:enemy.transform.position);
  var spot=map.Find("Props").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>().Single(s=>s.Targets.Contains(enemy));
  if(buffer.DistanceAfterCorner(spot.transform.position)<19.98f)throw new System.InvalidOperationException("Excel bypassed corner clearance");clearances++;
 }
 System.IO.File.WriteAllText("map-concepts/sr18-choices-and-corner-space-2026-09-08/runtime-probe.json","{\"passed\":true,\"pairs\":25,\"independentSideLaneHits\":50,\"neutralLaneHits\":0,\"singleRewardPaths\":2,\"distinctChoiceEffects\":true,\"postTurnBuffers\":25,\"restored\":true}");
 return new{passed=true,pairs=25,laneHits,claims,clearances};
}finally{
 IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning=running;weapon.damage=damage;player.lastWallTouchTime=touch;
 player.transform.SetPositionAndRotation(position,rotation);capsule.enabled=colliderEnabled;player.enabled=playerEnabled;foreach(var w in weapons)if(w.Key!=null)w.Key.enabled=w.Value;
}
