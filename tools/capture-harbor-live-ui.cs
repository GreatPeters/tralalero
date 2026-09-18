if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var root=canvas.transform;
var phase=SessionState.GetString("Harbor.LiveCapturePhase","cohort");
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-ui-live-2026-09-15/"+phase+"/"+scene.name;
var report="map-concepts/harbor-ui-live-2026-09-15/"+phase+"-"+scene.name+".txt";
System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllText(report,"");
var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);
var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);
var upgrade=root.Find("UI/Upgrade2");
void Check(bool condition,string message)
{if(!condition){System.IO.File.AppendAllText(report,"FAIL: "+message+"\n");throw new Exception(message);}}
System.Collections.IEnumerator Shot(string name)
{
    yield return new WaitForSecondsRealtime(.65f);
    Canvas.ForceUpdateCanvases();
    ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");
    yield return new WaitForSecondsRealtime(.3f);
    var image=new Texture2D(2,2);ImageConversion.LoadImage(image,System.IO.File.ReadAllBytes(folder+"/"+name+".png"));
    var pixels=image.GetPixels32();UnityEngine.Object.Destroy(image);
    Check(pixels.Count(p=>p.r>65||p.g>65||p.b>65)>pixels.Length/5,"Blank screenshot: "+name);
    System.IO.File.AppendAllText(report,"PASS: "+name+"\n");
}
System.Collections.IEnumerator Capture()
{
    story.Skip();shop.Close();
    if(upgrade.gameObject.activeSelf)upgrade.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    yield return Shot("01-lobby");
    root.Find("UI/Main/Bottom/Upgrade_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    yield return Shot("02-permanent");
    Check(upgrade.GetComponentsInChildren<UpgradeUI>().Length==10,"Ten permanent upgrades not reachable");
    var scroll=upgrade.Find("UpgradeViewport").GetComponent<UnityEngine.UI.ScrollRect>();scroll.verticalNormalizedPosition=0;
    yield return Shot("03-permanent-last");
    var last=(RectTransform)upgrade.GetComponentsInChildren<UpgradeUI>().Single(u=>u.UpgradeId==10).transform;
    var lastCorners=new Vector3[4];var viewportCorners=new Vector3[4];last.GetWorldCorners(lastCorners);scroll.viewport.GetWorldCorners(viewportCorners);
    Check(lastCorners[0].y>=viewportCorners[0].y-2&&lastCorners[1].y<=viewportCorners[1].y+2,"Final upgrade clipped or unreachable");
    upgrade.GetComponent<HarborUpgradeTabs>().chapterTab.onClick.Invoke();
    yield return Shot("04-chapters");
    Check(upgrade.GetComponentsInChildren<ChapterUpgradeCardUI>().Length==3,"Chapter cards missing");
    upgrade.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    Check(!upgrade.gameObject.activeSelf,"Upgrade back failed");
    root.Find("UI/Main/Bottom/Skin_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    shop.SelectSlot(CosmeticSlot.Shoes);shop.SelectItem("shoes_ruby");
    yield return Shot("05-shoes");Check(shop.preview.Texture!=null,"Shoe preview not rendered");
    shop.SelectSlot(CosmeticSlot.Skin);shop.SelectItem("skin_coral");yield return Shot("06-skins");
    shop.SelectSlot(CosmeticSlot.Hat);shop.SelectItem("hat_cap");yield return Shot("07-hats");
    shop.Close();
    root.Find("UI/Top/Setting/Image").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    yield return Shot("08-lobby-settings");
    var setting=root.Find("UI/Setting");setting.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name=="Close").onClick.Invoke();
    Check(!setting.gameObject.activeSelf,"Settings close failed");
    var rays=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=new Vector2(Screen.width*.5f,Screen.height*.4f)};
    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,rays);
    Check(!rays.Any(r=>r.gameObject.GetComponentInParent<UnityEngine.UI.Button>()!=null),"Start gesture surface covered by a button");
    System.IO.File.AppendAllText(report,"PASS: unobstructed horizontal-start area\nCOMPLETE\n");
}
canvas.StartCoroutine(Capture());
return new{scheduled=true,scene=scene.name,folder};
