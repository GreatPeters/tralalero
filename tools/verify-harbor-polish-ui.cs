if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var c=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var phase=SessionState.GetString("Harbor.PolishCapturePhase","first-pass");
var folder="C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-polish-2026-09-15/"+phase+"/"+scene.name;
System.IO.Directory.CreateDirectory(folder);var report=folder+"/checks.txt";System.IO.File.WriteAllText(report,"");
void Check(bool ok,string message){System.IO.File.AppendAllText(report,(ok?"PASS: ":"FAIL: ")+message+"\n");if(!ok)throw new Exception(message);}
System.Collections.IEnumerator Shot(string name)
{yield return new WaitForSecondsRealtime(.45f);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.2f);}
System.Collections.IEnumerator Verify()
{
    var story=c.GetComponentInChildren<OpeningStoryUI>(true);
    story.Open();yield return new WaitForSecondsRealtime(1.2f);
    var footer=(RectTransform)story.transform.Find("HarborContent/NextButton");Vector3 before=footer.position;
    story.Next();story.Next();yield return new WaitForSecondsRealtime(.65f);
    Check(story.CurrentPage==1,"Rapid Next does not issue overlapping seeks");
    Check(Vector3.Distance(before,footer.position)<.5f,"Video footer stays in place during seek");
    Check(story.movieDisplay.gameObject.activeInHierarchy&&story.IsMoviePlaying,"Prepared video displays a decoded frame");
    yield return Shot("01-story");story.Skip();
    yield return Shot("02-lobby");
    c.transform.Find("UI/Main/Bottom/Upgrade_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    var upgrades=c.transform.Find("UI/Upgrade2");upgrades.GetComponent<HarborUpgradeTabs>().chapterTab.onClick.Invoke();
    yield return Shot("03-chapter-upgrades");
    var chapter=upgrades.GetComponentsInChildren<ChapterUpgradeCardUI>().First(x=>x.chapter==1);
    Check(!chapter.lockMarker.activeSelf&&chapter.title.text.Contains("/ 5"),"Noryangjin upgrade unlocked and five-rank display");
    upgrades.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    c.transform.Find("UI/Top/Setting/Image").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    var settings=c.settingsMenuUI.GetComponent<HarborSettingsPanel>();
    Check(settings.gameObject.activeInHierarchy&&!settings.runActions.activeSelf,"Lobby settings has no run actions");
    c.PlayerPressedStartButton();Check(!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning,"Settings blocks the start gesture");
    bool oldSound=IndianOceanAssets.ShooterSurvival.SettingsManager.Instance.soundEnabled;
    settings.soundButton.onClick.Invoke();Check(IndianOceanAssets.ShooterSurvival.SettingsManager.Instance.soundEnabled!=oldSound,"Sound switch changes actual setting");settings.soundButton.onClick.Invoke();
    yield return Shot("04-lobby-settings");settings.Close();
    Check(!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning&&!c.scoreParent.activeInHierarchy,"Lobby settings close does not start gameplay or show legacy score");
    c.transform.Find("UI/Main/Bottom/Skin_Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    var shop=c.GetComponentInChildren<CosmeticShopUI>(true);shop.SelectSlot(CosmeticSlot.Hat);shop.SelectItem("hat_diver");
    yield return Shot("05-hat-shop");
    Check(shop.coinBalance.alignment==TMPro.TextAlignmentOptions.Center&&shop.jewelBalance.alignment==TMPro.TextAlignmentOptions.Center,"Shop balances centered");shop.Close();
    c.PlayerPressedStartButton();yield return new WaitForSecondsRealtime(.3f);
    Check(p.sharkAnim.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"Shark walks on start");
    c.PauseGame();yield return Shot("06-run-settings");
    Check(settings.runActions.activeInHierarchy,"Run settings contains resume and retry");settings.Close();yield return new WaitForSecondsRealtime(.3f);
    Check(IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning&&p.sharkAnim.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"Closing settings resumes shark locomotion");
    Check(!c.scoreParent.activeInHierarchy,"Resume keeps legacy score hidden");
    p.sharkAnim.SetTrigger("Die");p.sharkAnim.Update(.1f);p.ResetState();yield return new WaitForSecondsRealtime(.3f);
    Check(p.sharkAnim.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"Run reset clears death animation and restores walking");
    float health=p.currentHealth;p.currentHealth=p.MaxHealth*.5f;p.UpdateHealth();yield return Shot("07-hud-half-health");p.currentHealth=health;
    var fill=c.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.PlayerStatusHud>(true).transform.Find("HealthCard/HealthFill").GetComponent<UnityEngine.UI.Image>();
    Check(fill.sprite!=null&&Mathf.Abs(fill.fillAmount-.5f)<.01f,"Rectangular HUD fill represents half health");
    IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;
    IndianOceanAssets.ShooterSurvival.CoinPickup.Spawn(p.transform.position+p.transform.forward*7+p.transform.right*2,5);
    yield return Shot("08-coin");IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=1;
    var camera=Camera.main;Check(camera.GetComponent<StableGameplayCamera>()!=null&&!camera.transform.IsChildOf(p.transform),"Camera independent of animated player hierarchy");
    var initialCameraRotation=camera.transform.rotation;var initialPlayerRotation=p.transform.rotation;
    p.transform.rotation=Quaternion.Euler(15,p.transform.eulerAngles.y,0);yield return null;
    Check(Quaternion.Angle(initialCameraRotation,camera.transform.rotation)<.2f,"Slope pitch does not tilt the camera");p.transform.rotation=initialPlayerRotation;
    Check(Resources.Load<AudioClip>("Audio/Mobile/shot").length<.06f&&GameAudioService.Cooldown(GameSound.Shot)>=.12f,"Short pop replaces overlapping swish");
    c.PauseGame();settings.gameObject.SetActive(false);
    var defeat=c.gameOverUI.GetComponent<DefeatPresentation>();defeat.SetResult(42,125,"harbor-polish-preview");c.gameOverUI.SetActive(true);
    yield return Shot("09-defeat");Check(defeat.closeButton!=null,"Defeat has an upper-right close control");
    c.gameOverUI.SetActive(false);System.IO.File.AppendAllText(report,"COMPLETE\n");
}
c.StartCoroutine(Verify());return new{scheduled=true,folder};
