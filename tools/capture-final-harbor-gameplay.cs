if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
OpeningStoryUI.Instance?.Skip();canvas.PlayerPressedStartButton();canvas.GetComponent<CoastalTutorialUI>().Close();
string dir="tmp/image-previews/harbor-opening-refinement-2026-09-16/final-gameplay";System.IO.Directory.CreateDirectory(dir);
System.Collections.IEnumerator Capture(){yield return new WaitForSecondsRealtime(.7f);IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;yield return new WaitForSecondsRealtime(.1f);ScreenCapture.CaptureScreenshot(dir+"/"+scene.name+".png");yield return new WaitForSecondsRealtime(.2f);System.IO.File.WriteAllText(dir+"/"+scene.name+".txt","Captured real start camera after final world typography; frame="+Time.frameCount);}
canvas.StartCoroutine(Capture());return dir;
