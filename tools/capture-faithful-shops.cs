if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);if(story.gameObject.activeInHierarchy)story.Skip();
var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);var upgrade=canvas.transform.Find("UI/Upgrade2");
string folder="tmp/image-previews/harbor-ui-all-2026-09-15/9-shop-fidelity-2026-09-17/"+SessionState.GetString("ShopFidelity.Phase","candidate-1")+"/"+scene.name;
System.IO.Directory.CreateDirectory(folder);string report=folder+"/checks.txt";System.IO.File.WriteAllText(report,"");
System.Collections.IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.6f);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.25f);}
System.Collections.IEnumerator Capture(){
 shop.Open();shop.SelectSlot(CosmeticSlot.Skin);yield return Shot("01-skins");
 shop.SelectItem("skin_coral");yield return Shot("02-skin-preview");
 shop.SelectSlot(CosmeticSlot.Shoes);yield return Shot("03-shoes");
 shop.SelectSlot(CosmeticSlot.Hat);yield return Shot("04-hats");
 shop.Close();upgrade.gameObject.SetActive(true);var tabs=upgrade.GetComponent<HarborUpgradeTabs>();tabs.ShowPermanent();yield return Shot("05-permanent");
 var scroll=upgrade.Find("UpgradeViewport").GetComponent<UnityEngine.UI.ScrollRect>();scroll.verticalNormalizedPosition=0;yield return Shot("06-permanent-bottom");scroll.verticalNormalizedPosition=1;
 tabs.ShowChapters();yield return Shot("07-chapters");
 upgrade.gameObject.SetActive(false);System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Capture());return folder;
