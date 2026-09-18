if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var canvas=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var progression=canvas.GetComponent<ChapterProgression>();
string folder=System.IO.Path.GetFullPath("tmp/image-previews/coastal-enamel-ui-2026-09-16/final/"+canvas.gameObject.scene.name);
System.IO.Directory.CreateDirectory(folder);
System.Collections.IEnumerator Verify(){
 canvas.gameOverUI.SetActive(false);canvas.youWinUI.SetActive(false);canvas.settingsMenuUI.SetActive(false);
 var ui=progression.transitionUI;
 if(progression.nextChapterMovie==null)yield break;
 canvas.StartCoroutine(ui.Present(progression.nextChapterMovie,progression.nextChapterTitle,progression.nextChapterCaption,25));
 yield return new WaitForSecondsRealtime(1.5f);
 if(!ui.IsPresenting||ui.display.texture==null||ui.player.frame<0)throw new Exception("Transition did not render actual video");
 ScreenCapture.CaptureScreenshot(folder+"/17-chapter-video.png");
 yield return new WaitForSecondsRealtime(.3f);ui.skipButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
 if(ui.IsPresenting||ui.display.texture!=null)throw new Exception("Transition skip did not release playback");
 System.IO.File.WriteAllText(folder+"/transition-check.txt","PASS actual chapter movie, skip and resource release");
}
canvas.StartCoroutine(Verify());return folder;
