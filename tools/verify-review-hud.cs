using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class VerifyReviewHud
{
    public static string Main(string label="v1")
    {
        string folder="tmp/image-previews/review-fixes-20-runs-2026-09-22/hud-"+label;if(Directory.Exists(folder))throw new Exception("Fresh output required");Directory.CreateDirectory(folder);
        var c=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();c.StartCoroutine(Run(c,folder));return folder;
    }
    static IEnumerator Run(CanvasScript canvas,string folder)
    {
        OpeningStoryUI.Instance?.Skip();var tutorial=UnityEngine.Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.enabled=false;tutorial.Close();}
        canvas.PlayerPressedStartButton();yield return null;
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();p.movement=false;p.canShoot=false;
        var boss=UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None).First(e=>e.name.EndsWith("E25_T293_Enemy_Woman"));
        boss.transform.position=p.transform.position+p.transform.forward*18;Physics.SyncTransforms();
        yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();Capture(folder+"/threat.png");
        boss.transform.position=p.transform.position+p.transform.forward*.4f;Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(1.6f);yield return new WaitForEndOfFrame();Capture(folder+"/defeat.png");
        var d=canvas.gameOverUI.GetComponent<DefeatPresentation>();
        File.WriteAllText(folder+"/report.txt",$"health={p.currentHealth}; cause={p.LastDamageCause}; damage={p.LastDamageAmount}; fatal={p.LastDamageWasFatal}; panel={canvas.gameOverUI.activeInHierarchy}; text={d.causeText.text}; tip={d.adviceText.text}");
        if(p.currentHealth!=0||p.LastDamageCause!=PlayerDamageCause.EnemyContact||!canvas.gameOverUI.activeInHierarchy)throw new Exception("Physical defeat evidence failed");
    }
    static void Capture(string path){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
}
