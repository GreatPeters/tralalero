var canvas=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);canvas.GetComponentInChildren<OpeningStoryUI>(true).Skip();
string folder="tmp/image-previews/harbor-ui-all-2026-09-15/9-shop-fidelity-2026-09-17/"+SessionState.GetString("ShopFidelity.Phase","final-2")+"/Noryangjin_MapTool_Mode_SR18/";
System.Collections.IEnumerator Capture(){
 shop.Open();shop.SelectSlot(CosmeticSlot.Hat);shop.SelectItem("hat_tophat");yield return new WaitForSecondsRealtime(.5f);
 ScreenCapture.CaptureScreenshot(folder+"08-tall-hat-preview.png");yield return new WaitForSecondsRealtime(.2f);
 shop.SelectSlot(CosmeticSlot.Skin);yield return new WaitForSecondsRealtime(.2f);shop.itemsRoot.GetComponentInParent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=0;yield return new WaitForSecondsRealtime(.4f);
 ScreenCapture.CaptureScreenshot(folder+"09-skins-bottom.png");yield return new WaitForSecondsRealtime(.2f);
 shop.Close();System.IO.File.WriteAllText(folder+"fit-checks.txt","COMPLETE\n");
}
canvas.StartCoroutine(Capture());return folder;
