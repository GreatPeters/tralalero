if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
var canvas = UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var dim = canvas.buttons.transform.Find("LobbyDim").GetComponent<UnityEngine.UI.Image>();
var folder = "C:/Users/ljh/tralalero Shooter/tmp/image-previews/harbor-lobby-dim-2026-09-15";
System.IO.Directory.CreateDirectory(folder);
var report = folder + "/verification.txt";
System.IO.File.WriteAllText(report, "");
void Check(bool success, string message)
{
    System.IO.File.AppendAllText(report, (success ? "PASS: " : "FAIL: ") + message + "\n");
    if (!success) throw new Exception(message);
}
System.Collections.IEnumerator Shot(string filename)
{
    yield return new WaitForSecondsRealtime(.4f);
    Canvas.ForceUpdateCanvases();
    ScreenCapture.CaptureScreenshot(folder + "/" + filename + ".png");
    yield return new WaitForSecondsRealtime(.25f);
    Check(System.IO.File.Exists(folder + "/" + filename + ".png"), filename + " captured");
}
System.Collections.IEnumerator Verify()
{
    canvas.GetComponentInChildren<OpeningStoryUI>(true).Skip();
    canvas.GetComponentInChildren<CosmeticShopUI>(true).Close();
    Check(dim.gameObject.activeInHierarchy && dim.enabled, "Dim visible in lobby");
    Check(dim.transform.GetSiblingIndex() == 0 && dim.transform.parent == canvas.buttons.transform, "Dim behind all lobby controls");
    Check(!dim.raycastTarget && Mathf.Approximately(dim.color.a, .20f), "20% black, input transparent");
    dim.enabled = false;
    yield return Shot("lobby-before");
    dim.enabled = true;
    yield return Shot("lobby-20-percent");
    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = new Vector2(Screen.width * .5f, Screen.height * .4f) };
    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
    Check(!hits.Any(hit => hit.gameObject == dim.gameObject), "Dim does not capture start gesture");
    canvas.PlayerPressedStartButton();
    Check(IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning && !dim.gameObject.activeInHierarchy, "Dim hidden when game starts");
    yield return Shot("gameplay-no-dim");
    Check(canvas.pauseButton.activeInHierarchy, "Pause button remains available");
    System.IO.File.AppendAllText(report, "COMPLETE\n");
}
canvas.StartCoroutine(Verify());
return new { scheduled = true, folder };
