if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var chapter=canvas.GetComponent<ChapterProgression>();
var transition=chapter.transitionUI;
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-ui-live-2026-09-15/transitions";
var report="map-concepts/harbor-ui-live-2026-09-15/transition-"+scene.name+".txt";
System.IO.Directory.CreateDirectory(folder);
canvas.GetComponentInChildren<OpeningStoryUI>(true).Skip();
chapter.CompleteChapter();
if(!chapter.TryAdvanceAutomatically())throw new Exception("Chapter advance was not accepted");
System.IO.File.WriteAllText(report,"Destination: "+chapter.nextScene+"\n");
System.Collections.IEnumerator CheckMovie()
{
    float until=Time.realtimeSinceStartup+12;
    while(!transition.IsPresenting||!transition.player.isPlaying)
    {
        if(Time.realtimeSinceStartup>until){System.IO.File.AppendAllText(report,"FAIL: transition did not play\n");yield break;}
        yield return null;
    }
    yield return new WaitForSecondsRealtime(.5f);
    var content=transition.transform.Find("HarborContent");
    if(content.localScale!=Vector3.one)throw new Exception("Transition content scale invalid");
    string path=folder+"/"+scene.name+".png";ScreenCapture.CaptureScreenshot(path);
    yield return new WaitForSecondsRealtime(.35f);
    var image=new Texture2D(2,2);ImageConversion.LoadImage(image,System.IO.File.ReadAllBytes(path));var pixels=image.GetPixels32();UnityEngine.Object.Destroy(image);
    if(pixels.Count(p=>p.r>65||p.g>65||p.b>65)<pixels.Length/5)throw new Exception("Blank chapter video UI");
    System.IO.File.AppendAllText(report,"PASS: actual video and overlay rendered\n");
    if(scene.name=="HighWay")
    {transition.skipButton.onClick.Invoke();System.IO.File.AppendAllText(report,"Skip invoked through button\n");}
    else System.IO.File.AppendAllText(report,"Await natural completion\n");
}
canvas.StartCoroutine(CheckMovie());
return new{from=scene.name,to=chapter.nextScene,unlocked=ChapterUpgradeService.UnlockedChapter,clearJewels=chapter.ClearJewels};
