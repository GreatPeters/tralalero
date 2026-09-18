if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();var progression=canvas.GetComponent<ChapterProgression>();if(progression.nextChapterMovie==null)return "No next-chapter movie in this scene";
var ui=progression.transitionUI;string folder="tmp/image-previews/harbor-faithful-art-2026-09-17/transitions";System.IO.Directory.CreateDirectory(folder);string report=folder+"/"+canvas.gameObject.scene.name+".txt";
System.Collections.IEnumerator Run(){
 var routine=canvas.StartCoroutine(ui.Present(progression.nextChapterMovie,progression.nextChapterTitle,progression.nextChapterCaption));float end=Time.realtimeSinceStartup+12;
 while((!ui.IsPresenting||ui.player.frame<0||!ui.player.isPlaying)&&Time.realtimeSinceStartup<end)yield return null;
 if(!ui.IsPresenting||ui.player.frame<0)throw new Exception("Transition did not render");yield return new WaitForSecondsRealtime(.8f);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(folder+"/"+canvas.gameObject.scene.name+".png");yield return new WaitForSecondsRealtime(.3f);ui.Skip();yield return routine;
 if(ui.IsPresenting||ui.display.texture!=null)throw new Exception("Transition leaked its video target");System.IO.File.WriteAllText(report,"PASS actual chapter movie rendered; Skip released render target. No progression/reward mutation.\n");
}
canvas.StartCoroutine(Run());return folder;
