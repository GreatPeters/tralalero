var canvas=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
string path=System.IO.Path.GetFullPath("tmp/image-previews/coastal-enamel-ui-2026-09-16/final/Noryangjin_MapTool_Mode_SR18/20-victory-final.png");
System.Collections.IEnumerator Capture(){
 yield return new WaitForSecondsRealtime(.5f);canvas.GetComponentInChildren<OpeningStoryUI>(true).Skip();canvas.buttons.SetActive(false);
 canvas.GetComponent<ChapterProgression>().CompleteChapter();canvas.youWinUI.SetActive(true);
 yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(path);
}
canvas.StartCoroutine(Capture());return path;
