if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
string folder=System.IO.Path.GetFullPath("tmp/image-previews/coastal-enamel-ui-2026-09-16/final/"+scene.name+(Screen.height==1920?"-16x9":""));System.IO.Directory.CreateDirectory(folder);
string report=folder+"/last-cards.txt";System.IO.File.WriteAllText(report,"");
void Check(UnityEngine.UI.ScrollRect scroll,RectTransform last,string title){
 var card=new Vector3[4];var view=new Vector3[4];last.GetWorldCorners(card);scroll.viewport.GetWorldCorners(view);
 bool ok=card[0].y>=view[0].y-2&&card[1].y<=view[1].y+2;
 System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+title+"\n");if(!ok)throw new Exception(title);
}
System.Collections.IEnumerator Verify(){
 canvas.youWinUI.SetActive(false);canvas.gameOverUI.SetActive(false);canvas.settingsMenuUI.SetActive(false);canvas.transform.Find("UI").gameObject.SetActive(true);
 var upgrade=canvas.transform.Find("UI/Upgrade2");canvas.transform.Find("UI/Main/Bottom/Upgrade_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();upgrade.GetComponent<HarborUpgradeTabs>().ShowChapters();
 var chapterScroll=upgrade.Find("ChapterUpgrades/Viewport").GetComponent<UnityEngine.UI.ScrollRect>();chapterScroll.verticalNormalizedPosition=0;
 yield return new WaitForSecondsRealtime(.4f);Check(chapterScroll,upgrade.GetComponentsInChildren<ChapterUpgradeCardUI>().Single(x=>x.chapter==3).GetComponent<RectTransform>(),"Last chapter card reachable");
 upgrade.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);shop.Open();
 foreach(CosmeticSlot slot in System.Enum.GetValues(typeof(CosmeticSlot))){shop.SelectSlot(slot);yield return new WaitForSecondsRealtime(.3f);var scroll=shop.itemsRoot.GetComponentInParent<UnityEngine.UI.ScrollRect>();scroll.verticalNormalizedPosition=0;yield return new WaitForSecondsRealtime(.3f);var last=shop.itemsRoot.Cast<Transform>().Last(t=>t.gameObject.activeSelf);Check(scroll,(RectTransform)last,"Last "+slot+" item reachable");}
 ScreenCapture.CaptureScreenshot(folder+"/18-last-items.png");shop.Close();
 var progress=canvas.GetComponent<ChapterProgression>();progress.CompleteChapter();
 if(progress.clearRewardText==null||progress.ClearJewels>0&&!progress.clearRewardText.text.Contains(progress.ClearJewels.ToString()))throw new Exception("Clear reward label missing");
 canvas.youWinUI.SetActive(true);yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot(folder+"/19-victory-reward.png");
 System.IO.File.AppendAllText(report,"PASS live chapter-clear reward label\nCOMPLETE\n");
}
canvas.StartCoroutine(Verify());return folder;
