if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var folder=System.IO.Path.GetFullPath("tmp/image-previews/coastal-enamel-ui-2026-09-16/"+SessionState.GetString("Coastal.Phase","first-pass")+"/"+scene.name);
System.IO.Directory.CreateDirectory(folder);
var report=folder+"/checks.txt";System.IO.File.WriteAllText(report,"");
void Check(bool ok,string name){System.IO.File.AppendAllText(report,(ok?"PASS ":"FAIL ")+name+"\n");if(!ok)throw new Exception(name);}
System.Collections.IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.45f);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.25f);}
System.Collections.IEnumerator Capture(){
 var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);story.Open();yield return new WaitForSecondsRealtime(1.3f);
 yield return Shot("01-story");Check(story.movieDisplay.texture!=null&&story.sceneIndicators.Length==4,"Real video and four scene indicators");story.Skip();
 yield return Shot("02-lobby");
 var root=canvas.transform;root.Find("UI/Main/Bottom/Upgrade_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
 var upgrades=root.Find("UI/Upgrade2");yield return Shot("03-upgrades");
 Check(upgrades.GetComponentsInChildren<UpgradeUI>().Length==10,"Ten regular upgrades reachable");
 var scroll=upgrades.Find("UpgradeViewport").GetComponent<UnityEngine.UI.ScrollRect>();scroll.verticalNormalizedPosition=0;
 yield return Shot("04-upgrades-last");
 var last=upgrades.GetComponentsInChildren<UpgradeUI>().Single(x=>x.UpgradeId==10).GetComponent<RectTransform>();
 var corners=new Vector3[4];var view=new Vector3[4];last.GetWorldCorners(corners);scroll.viewport.GetWorldCorners(view);
 Check(corners[0].y>=view[0].y-2&&corners[1].y<=view[1].y+2,"Last upgrade fully inside scroll viewport");
 upgrades.GetComponent<HarborUpgradeTabs>().ShowChapters();yield return Shot("05-chapter-upgrades");
 Check(upgrades.GetComponentsInChildren<ChapterUpgradeCardUI>().All(x=>x.rankPips.Length==5),"Chapter rank pips bound");
 Check(ChapterUpgradeService.Catalog.entries.All(x=>x.attackPercent==5&&x.healthPercent==5),"Every chapter grants5percent per rank");
 upgrades.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(!upgrades.gameObject.activeSelf,"Upgrade close");
 var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);shop.Open();yield return Shot("06-skins");
 Check(shop.preview.Texture!=null,"Actual cosmetic preview RenderTexture");
 shop.SelectItem("skin_coral");yield return Shot("07-skin-selected");
 if(MoneyScript.S.Jewel<250&&!CosmeticService.Current.Owns("skin_coral")){
  int coins=MoneyScript.S.Coin,gems=MoneyScript.S.Jewel;shop.actionButton.onClick.Invoke();yield return Shot("08-insufficient-gems");
  Check(shop.messagePanel.gameObject.activeSelf&&MoneyScript.S.Coin==coins&&MoneyScript.S.Jewel==gems,"Insufficient funds shown without debit");shop.messagePanel.Close();
 }
 shop.SelectSlot(CosmeticSlot.Shoes);yield return Shot("09-shoes");shop.SelectSlot(CosmeticSlot.Hat);yield return Shot("10-hats");
 shop.Close();root.Find("UI/Top/Setting/Image").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return Shot("11-lobby-settings");
 var settings=canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>();Check(!settings.runActions.activeSelf,"Lobby settings has no resume intent");settings.Close();
 canvas.PlayerPressedStartButton();canvas.GetComponent<CoastalTutorialUI>().Close();yield return Shot("12-hud");canvas.PauseGame();yield return Shot("13-run-settings");
 Check(settings.runActions.activeSelf,"Pause contains resume and retry");settings.Close();
 var tutorial=canvas.GetComponent<CoastalTutorialUI>();for(int i=0;i<4;i++){tutorial.ShowTopic(i);yield return Shot("14-tutorial-"+i);tutorial.Close();}
 canvas.PauseGame();settings.gameObject.SetActive(false);
 var defeat=canvas.gameOverUI.GetComponent<DefeatPresentation>();defeat.SetResult(42,125,"coastal-ui-visual-only");canvas.gameOverUI.SetActive(true);yield return Shot("15-result");canvas.gameOverUI.SetActive(false);
 canvas.youWinUI.SetActive(true);yield return Shot("16-victory");canvas.youWinUI.SetActive(false);
 Check(canvas.GetComponentsInChildren<TMPro.TMP_Text>(true).All(t=>t.font!=null),"All labels have fonts");
 System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Capture());return folder;
