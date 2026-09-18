if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);
string folder="tmp/image-previews/harbor-faithful-art-2026-09-17/"+SessionState.GetString("FaithfulUI.Phase","candidate-1")+"/"+scene.name;System.IO.Directory.CreateDirectory(folder);string report=folder+"/checks.txt";System.IO.File.WriteAllText(report,"");
void Check(bool value,string label){System.IO.File.AppendAllText(report,(value?"PASS ":"FAIL ")+label+"\n");if(!value)throw new Exception(label);}
System.Collections.IEnumerator Ready(){float end=Time.realtimeSinceStartup+12;while((story.IsSeeking||!story.IsMoviePlaying||story.video.frame<0)&&Time.realtimeSinceStartup<end)yield return null;Check(!story.IsSeeking&&story.IsMoviePlaying&&story.video.frame>=0,"Real movie ready");}
System.Collections.IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.3f);}
System.Collections.IEnumerator Capture(){
 story.Open();yield return Ready();story.video.time=3;yield return new WaitForSecondsRealtime(.3f);yield return Shot("01-story-first");Check(!story.previousButton.interactable,"Previous disabled on first scene");
 story.Next();yield return Ready();story.Next();yield return Ready();story.Previous();yield return Ready();Check(story.CurrentPage==1,"Previous reaches preceding movie scene");
 Check(story.sceneIndicators[1].sprite==story.activeSceneSprite&&story.sceneIndicators[0].sprite==story.inactiveSceneSprite,"Only current chapter tab highlighted");yield return Shot("02-story-previous");story.Skip();yield return Shot("03-lobby");
 var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);shop.Open();yield return Shot("04-shop-binding");shop.Close();yield return Shot("05-hand-return");
 Check(canvas.GetComponentInChildren<HarborSwipeHint>(true).finger.gameObject.activeInHierarchy,"Independent hand restored after shop close");
 System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Capture());return folder;
