if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var c=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var props=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Props");
var ship=props.Find("SR18_Polish_Ship_2").GetComponent<ObstacleStats>();
var station=props.Cast<Transform>().Single(t=>t.name.StartsWith("SR18_L_G21_"));
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-polish-2026-09-15/final/boat-final";System.IO.Directory.CreateDirectory(folder);
System.Collections.IEnumerator Capture()
{
    c.GetComponentInChildren<OpeningStoryUI>(true).Skip();c.settingsMenuUI.SetActive(false);c.pauseMenuUI.SetActive(false);c.gameOverUI.SetActive(false);
    if(!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)c.PlayerPressedStartButton();
    foreach(var enemy in UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.EnemyScript_space>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
    p.canShoot=false;IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;
    foreach(float distance in new[]{20f,12f})
    {
        var position=station.position-station.forward*distance;position.y=.12f;
        p.transform.SetPositionAndRotation(position,station.rotation);var body=p.GetComponent<Rigidbody>();body.position=position;body.rotation=station.rotation;
        yield return new WaitForSecondsRealtime(.6f);
        ScreenCapture.CaptureScreenshot(folder+"/boat-"+distance+"m.png");yield return new WaitForSecondsRealtime(.2f);
    }
    bool fired=(bool)typeof(ObstacleStats).GetField("hasFired",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(ship);
    System.IO.File.WriteAllText(folder+"/boat-check.txt",$"Active coastal ship: {ship.name}\nPosition: {ship.transform.position}\nTriggered cannon: {fired}\nPaddle boat: {props.Find("SR18_L_G08_T097_Oldman")!=null}\n");
    if(!fired)throw new Exception("Coastal cannon did not trigger");
}
c.StartCoroutine(Capture());return "scheduled";
