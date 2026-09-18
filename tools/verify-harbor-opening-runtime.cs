if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18")throw new Exception("SR18 required");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
string folder=System.IO.Path.GetFullPath("tmp/image-previews/harbor-opening-refinement-2026-09-16/opening-runtime-final");System.IO.Directory.CreateDirectory(folder);string report=folder+"/checks.txt";System.IO.File.WriteAllText(report,"");
var left=map.Find("Enemies/SR18_L_E01_T005_Enemy_OldMan").GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>();var right=map.Find("Enemies/SR18_L_E01_T005_Enemy_OldMan_Right").GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventController>();
var speed=typeof(IndianOceanAssets.ShooterSurvival.PlayerScript).GetField("currentForwardMoveSpeed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);float savedSpeed=(float)speed.GetValue(player);player.canShoot=false;
void Check(bool ok,string message){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
System.Collections.IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.2f);}
System.Collections.IEnumerator Run(){
 yield return Shot("01-workshop-signal");
 Check(left.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting&&right.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting,"Both enemies wait before approach");
 float until=Time.realtimeSinceStartup+75,minY=player.transform.position.y;IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=1;
 while(left.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting&&Time.realtimeSinceStartup<until&&!IndianOceanAssets.ShooterSurvival.CanvasScript.isGameOver){minY=Mathf.Min(minY,player.transform.position.y);yield return null;}
 speed.SetValue(player,0f);
 Check(!IndianOceanAssets.ShooterSurvival.CanvasScript.isGameOver&&minY>-.2f,"Natural extended-pier traversal and first turn stay on floor; minY="+minY);
 Check(left.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.MovingToTarget&&right.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting,"First enemy walks while second still waits; player="+player.transform.position);
 yield return Shot("02-first-approach");yield return new WaitForSecondsRealtime(1.3f);
 Check(left.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Attacking&&right.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Waiting,"Walker arrives before the second enemy attacks");
 var gate=map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>(true).Single(s=>s.Targets.Contains(right));
 player.ApplyContinuousRoutePose(new Vector3(-10.7f,.12f,gate.transform.position.z),Vector3.forward,0);Physics.SyncTransforms();yield return new WaitForSecondsRealtime(.2f);
 Check(right.RuntimeState==IndianOceanAssets.ShooterSurvival.EnemyEventRuntimeState.Attacking,"Actual player collider entering second gate starts its attack");yield return Shot("03-second-response");
 var altar=map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.AuthoredBonusWall>(true).Single(a=>a.name=="SR18_L_B01_T009");
 player.ApplyContinuousRoutePose(new Vector3(-10.7f,.12f,altar.transform.position.z-11),Vector3.forward,0);Physics.SyncTransforms();yield return new WaitForSecondsRealtime(.5f);yield return Shot("04-bonus-approach");
 Check(altar.GetComponent<IndianOceanAssets.ShooterSurvival.BonusRewardCue>().nearbyGlow.activeSelf,"Bonus glow enabled at approach distance");
 var box=altar.Wall.GetComponent<Collider>();var center=box.bounds.center;float health=player.currentHealth,attack=player.ResolvedAttackDamage;string reward=altar.Wall.CurrentBonusAlias+" "+altar.Wall.CurrentBonusDisplayValue;
 player.ApplyContinuousRoutePose(new Vector3(center.x,.12f,center.z-3),Vector3.forward,0);Physics.SyncTransforms();speed.SetValue(player,2f);until=Time.realtimeSinceStartup+5;
 while(altar.ChoicePair.Selected==null&&Time.realtimeSinceStartup<until)yield return null;
 speed.SetValue(player,0f);Check(altar.ChoicePair.Selected==altar&&!altar.gameObject.activeSelf,"Walking into the bonus claims it and removes chosen altar");
 Check(!altar.ChoicePair.Right.gameObject.activeSelf,"The unselected reward is removed");
 var bursts=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Select(p=>p.transform.root).Distinct().Where(t=>t.name.StartsWith("BonusPickupBurst")).ToArray();Check(bursts.Length==1,"Exactly one pickup burst on successful claim");
 System.IO.File.AppendAllText(report,"Reward="+reward+" health "+health+" -> "+player.currentHealth+" attack "+attack+" -> "+player.ResolvedAttackDamage+"\n");yield return Shot("05-bonus-collected");
 IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;speed.SetValue(player,savedSpeed);System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Run());return folder;
