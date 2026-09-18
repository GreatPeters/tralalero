if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var c=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-polish-2026-09-15/final/"+scene.name;System.IO.Directory.CreateDirectory(folder);
System.Collections.IEnumerator Verify()
{
    yield return new WaitForSecondsRealtime(.3f);c.GetComponentInChildren<OpeningStoryUI>(true).Skip();
    c.PlayerPressedStartButton();IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;yield return new WaitForSecondsRealtime(.2f);
    c.PauseGame();var settings=c.settingsMenuUI.GetComponent<HarborSettingsPanel>();
    if(!settings.runActions.activeInHierarchy)throw new Exception("Run actions missing");
    yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(folder+"/settings.png");yield return new WaitForSecondsRealtime(.2f);
    settings.Close();IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;yield return new WaitForSecondsRealtime(.2f);
    var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
    if(!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning||!p.sharkAnim.GetCurrentAnimatorStateInfo(0).IsName("Walk")||c.scoreParent.activeInHierarchy)throw new Exception("Settings resume regression");
    if(c.pauseButton.transform.Find("Gear").GetComponent<UnityEngine.UI.Image>().sprite==null)throw new Exception("Settings icon missing");
    ScreenCapture.CaptureScreenshot(folder+"/hud.png");yield return new WaitForSecondsRealtime(.2f);
    System.IO.File.WriteAllText(folder+"/checks.txt","PASS: shared settings and run actions\nPASS: resume restores Walk and keeps legacy score hidden\nPASS: actual gear sprite is connected\n");
}
c.StartCoroutine(Verify());return "scheduled";
