if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
OpeningStoryUI.Instance?.Skip();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
string folder="tmp/image-previews/chapter-ui-themes-2026-09-17/"+scene.name;System.IO.Directory.CreateDirectory(folder);string report=folder+"/checks.txt";
string expected=scene.name=="HighWay"?"HighwayPlaque":"RestStopPlaque";var title=canvas.transform.Find("UI/Main/Center/ChapterTitle").GetComponent<UnityEngine.UI.Image>();
void Check(bool value,string message){System.IO.File.AppendAllText(report,(value?"PASS ":"FAIL ")+message+"\n");if(!value)throw new Exception(message);}
System.Collections.IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.35f);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.4f);}
System.Collections.IEnumerator Capture(){
 System.IO.File.WriteAllText(report,"");Check(title.sprite.name==expected,"Chapter plaque is "+expected);yield return Shot("01-lobby");
 canvas.transform.Find("UI/Top/Setting/Image").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>().Close();
 Check(title.sprite.name==expected&&title.gameObject.activeInHierarchy,"Chapter theme retained after closing settings");
 canvas.transform.Find("UI/Main/Bottom/Upgrade_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);canvas.transform.Find("UI/Upgrade2/Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
 Check(title.sprite.name==expected&&title.gameObject.activeInHierarchy,"Chapter theme retained after returning from upgrades");yield return Shot("02-returned-lobby");
 Check(canvas.GetComponentInChildren<HarborSwipeHint>(true).finger.gameObject.activeInHierarchy,"Independent hand hint retained");System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Capture());return folder;
