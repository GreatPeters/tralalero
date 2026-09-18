if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
string folder=System.IO.Path.GetFullPath("tmp/image-previews/harbor-opening-refinement-2026-09-16/"+SessionState.GetString("HarborRefinement.Phase","first-pass")+"/"+scene.name);System.IO.Directory.CreateDirectory(folder);
string report=folder+"/checks.txt";System.IO.File.WriteAllText(report,"");
void Check(bool ok,string message){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
System.Collections.IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.4f);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.25f);}
System.Collections.IEnumerator Ready(OpeningStoryUI story){float until=Time.realtimeSinceStartup+12;while((story.IsSeeking||!story.IsMoviePlaying||story.video.frame<0)&&Time.realtimeSinceStartup<until)yield return null;Check(!story.IsSeeking&&story.IsMoviePlaying&&story.video.frame>=0,"Video prepared and target frame rendered");}
System.Collections.IEnumerator Capture(){
 var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);story.Open();yield return Ready(story);
 Check(story.previousButton!=null&&!story.previousButton.interactable,"Previous disabled at first scene");story.Next();yield return Ready(story);story.Next();yield return Ready(story);
 int page=story.CurrentPage;story.previousButton.onClick.Invoke();yield return Ready(story);Check(story.CurrentPage==page-1,"Previous goes to previous scene rather than restart");yield return Shot("01-video-previous");story.Skip();
 yield return Shot("02-lobby");
 canvas.transform.Find("UI/Top/Setting/Image").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return Shot("03-settings");canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>().Close();
 canvas.transform.Find("UI/Main/Bottom/Upgrade_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return Shot("04-upgrades");var upgrades=canvas.transform.Find("UI/Upgrade2");upgrades.GetComponent<HarborUpgradeTabs>().ShowChapters();yield return Shot("05-chapters");upgrades.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
 var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);shop.Open();yield return Shot("06-shop");shop.Close();
 canvas.PlayerPressedStartButton();canvas.GetComponent<CoastalTutorialUI>().Close();yield return Shot("07-start-greeting");
 var tutorial=canvas.GetComponent<CoastalTutorialUI>();tutorial.ShowTopic(0);yield return Shot("08-small-tutorial");tutorial.Close();canvas.PauseGame();yield return Shot("09-pause");canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>().Close();
 IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;
 Check(canvas.GetComponentsInChildren<TMPro.TMP_Text>(true).All(t=>t.font!=null),"No missing UI fonts");
 if(scene.name=="Noryangjin_MapTool_Mode_SR18"){
  var merchant=UnityEngine.Object.FindFirstObjectByType<HarborMerchantGreeting>();Check(merchant!=null&&merchant.HasGreeted,"Merchant greets on departure");
  var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
  Check(!map.Find("Props/SR18_L_G01_T013_Bucket").gameObject.activeSelf,"First bucket cluster disabled by workbook");
  var enemies=map.Find("Enemies").GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.EnemyEventController>(true);
  var left=enemies.Single(e=>e.name=="SR18_L_E01_T005_Enemy_OldMan");var right=enemies.Single(e=>e.name=="SR18_L_E01_T005_Enemy_OldMan_Right");
  Check(Vector3.Distance(left.transform.position,right.transform.position)>4,"Opening enemies staggered spatially");
 }
 System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Capture());return folder;
