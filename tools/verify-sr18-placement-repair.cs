if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/sr18-placement-repair-2026-09-15/final";System.IO.Directory.CreateDirectory(folder);
var report=folder+"/verification.txt";System.IO.File.WriteAllText(report,"");
void Check(bool success,string message){System.IO.File.AppendAllText(report,(success?"PASS: ":"FAIL: ")+message+"\n");if(!success)throw new Exception(message);}
System.Collections.IEnumerator Shot(string name)
{yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.2f);}
System.Collections.IEnumerator Verify()
{
    yield return new WaitForSecondsRealtime(.4f);
    canvas.GetComponentInChildren<OpeningStoryUI>(true).Skip();canvas.PlayerPressedStartButton();
    IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;player.canShoot=false;
    var controller=map.GetComponent<IndianOceanAssets.ShooterSurvival.EncounterPlacementController>();
    int expected=EncounterPlacementTables.Rows.Count(row=>row.scene==scene.name);
    Check(controller.AppliedCount==expected&&expected>0,"Complete workbook placement applied without fallback");
    controller.BeginNewRun();controller.BeginNewRun();Check(controller.AppliedCount==expected,"Repeated run preparation succeeds");
    var enemies=map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>(true);
    var spots=map.Find("Props").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>(true);
    foreach(var enemy in enemies){var matches=spots.Where(s=>s.Targets.Contains(enemy)).ToArray();Check(matches.Length==1&&matches[0].Targets.Length==1,"Unique binding: "+enemy.name);}
    foreach(var suffix in new[]{"","_Right"})
    {
        var spot=map.Find("Props/SR18_L_E01_Activation"+suffix).GetComponent<IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot>();
        Check(spot.ActivateTargets(),"First pair activates independently: "+suffix);
        Check(!spot.GetComponent<BoxCollider>().enabled,"Consumed spot disabled");
    }
    IndianOceanAssets.ShooterSurvival.EnemyEventController.ResetAllForNewRun();IndianOceanAssets.ShooterSurvival.EnemyEventActivationSpot.ResetAllForNewRun();
    Check(map.Find("Props/SR18_L_E01_Activation").GetComponent<BoxCollider>().enabled,"Activation spots reset for retry");
    var gantry=map.Find("Props").Cast<Transform>().Single(t=>t.name.Contains("Harbor_lane_signal_gantry"));
    var camera=Camera.main;var occlusion=camera.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinCameraOcclusion>();
    void Place(Vector3 position)
    {position.y=.12f;player.transform.SetPositionAndRotation(position,Quaternion.identity);var body=player.GetComponent<Rigidbody>();body.position=position;body.rotation=Quaternion.identity;}
    Place(gantry.position-Vector3.forward*4.5f);yield return new WaitForSecondsRealtime(.3f);
    occlusion.enabled=false;yield return Shot("01-gantry-visible-reference");
    occlusion.enabled=true;occlusion.RefreshVisibility();Check(gantry.GetComponent<Renderer>().forceRenderingOff,"Grounded gantry hides when it blocks the gameplay view");
    yield return Shot("02-clear-enemy-view");
    Place(gantry.position+Vector3.forward*26);yield return new WaitForSecondsRealtime(.3f);occlusion.RefreshVisibility();
    Check(!gantry.GetComponent<Renderer>().forceRenderingOff,"Gantry restores after passing");
    Check(gantry.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Decorative gantry cannot block movement");
    System.IO.File.AppendAllText(report,$"COMPLETE: {enemies.Length} enemies, {spots.Length} spots, {expected} placement rows\n");
}
canvas.StartCoroutine(Verify());return "scheduled";
