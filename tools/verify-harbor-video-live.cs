if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var canvas=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);
string folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-ui-live-2026-09-15/video-verification";
string report="map-concepts/harbor-ui-live-2026-09-15/video-verification.tsv";
System.IO.Directory.CreateDirectory(folder);
if(System.IO.File.Exists("map-concepts/harbor-ui-live-2026-09-15/video-complete.txt"))System.IO.File.Delete("map-concepts/harbor-ui-live-2026-09-15/video-complete.txt");
if(System.IO.File.Exists("map-concepts/harbor-ui-live-2026-09-15/video-error.txt"))System.IO.File.Delete("map-concepts/harbor-ui-live-2026-09-15/video-error.txt");
System.IO.File.WriteAllText(report,"event\tpage\ttime\tactive_segment\n");
void Check(bool passed,string message)
{
    if(!passed){System.IO.File.WriteAllText("map-concepts/harbor-ui-live-2026-09-15/video-error.txt",message);throw new Exception(message);}
}
void Record(string name)
{
    int active=Array.FindIndex(story.sceneIndicators,i=>i.sprite==story.activeSceneSprite);
    System.IO.File.AppendAllText(report,name+"\t"+story.CurrentPage+"\t"+story.video.time.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+"\t"+active+"\n");
}
System.Collections.IEnumerator WaitForMovie()
{
    float until=Time.realtimeSinceStartup+12;
    while(!story.IsMoviePlaying&&Time.realtimeSinceStartup<until)yield return null;
    Check(story.IsMoviePlaying,"Movie did not prepare/play");
    yield return new WaitForSecondsRealtime(.4f);
}
System.Collections.IEnumerator Verify()
{
    story.Open();yield return WaitForMovie();
    Check(Mathf.Abs(story.pageContent.transform.localScale.x-1)<.0001f,"Nested video content scale is not normalized");
    for(int page=0;page<OpeningStoryUI.MovieSceneCount;page++)
    {
        if(page>0){story.transform.Find("HarborContent/NextButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);}
        Check(story.CurrentPage==page,"Next did not select expected scene");
        Check(story.sceneIndicators.Count(i=>i.sprite==story.activeSceneSprite)==1&&story.sceneIndicators[page].sprite==story.activeSceneSprite,"Indicator did not follow current video scene");
        Check(story.movieDisplay.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode==UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent,"Movie fit mode changed");
        Record("scene-"+(page+1));ScreenCapture.CaptureScreenshot(folder+"/scene-"+(page+1)+".png");
        yield return new WaitForSecondsRealtime(.5f);
        var capture=new Texture2D(2,2);ImageConversion.LoadImage(capture,System.IO.File.ReadAllBytes(folder+"/scene-"+(page+1)+".png"));
        var pixels=capture.GetPixels32();UnityEngine.Object.Destroy(capture);
        Check(pixels.Count(p=>p.r>65||p.g>65||p.b>65)>pixels.Length/5,"Video UI screenshot is blank/dark for scene "+(page+1));
    }
    story.transform.Find("HarborContent/ReplayButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    yield return WaitForMovie();Check(story.CurrentPage==0,"Replay did not return to scene one");Record("replay");
    float until=Time.realtimeSinceStartup+13;
    while(story.CurrentPage==0&&Time.realtimeSinceStartup<until)yield return null;
    Check(story.CurrentPage==1&&story.sceneIndicators[1].sprite==story.activeSceneSprite,"Natural playback did not advance the indicator");Record("natural-boundary");
    story.transform.Find("HarborContent/SkipButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    Check(!story.gameObject.activeSelf&&story.video.targetTexture==null&&story.movieDisplay.texture==null,"Skip did not close/release movie");Record("skip");
    story.Open();yield return WaitForMovie();until=Time.realtimeSinceStartup+33;
    while(story.gameObject.activeSelf&&Time.realtimeSinceStartup<until)yield return null;
    Check(!story.gameObject.activeSelf&&story.video.targetTexture==null,"Natural movie completion did not close/release");Record("natural-completion");
    System.IO.File.WriteAllText("map-concepts/harbor-ui-live-2026-09-15/video-complete.txt","Passed: four Next states, Replay, natural scene transition, Skip and natural completion.");
}
canvas.StartCoroutine(Verify());
return "Video verification scheduled on the persistent Canvas host";
