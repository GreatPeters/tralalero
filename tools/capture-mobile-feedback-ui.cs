using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class CaptureMobileFeedbackUI
{
    public static string Main(string folder)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required.");
        if(Directory.Exists(folder))throw new IOException("Preserve prior captures.");Directory.CreateDirectory(folder);
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas,folder));return folder;
    }
    static IEnumerator Run(CanvasScript canvas,string folder)
    {
        UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include)?.Skip();
        yield return null;yield return new WaitForEndOfFrame();Capture(folder+"/lobby.png");
        var upgrades=canvas.transform.Find("UI/Upgrade2");upgrades.gameObject.SetActive(true);
        yield return null;yield return new WaitForEndOfFrame();Capture(folder+"/upgrades.png");upgrades.gameObject.SetActive(false);
        canvas.PlayerPressedStartButton();yield return null;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();player.canShoot=false;
        foreach(var weapon in player.GetComponentsInChildren<WeaponScript>(true)){weapon.StopAllCoroutines();weapon.enabled=false;}
        var tutorial=UnityEngine.Object.FindFirstObjectByType<CoastalTutorialUI>(FindObjectsInactive.Include);tutorial.ShowTopic(0);tutorial.enabled=false;
        yield return new WaitForEndOfFrame();Capture(folder+"/tutorial.png");tutorial.Close();
        var source=Resources.Load<BonusTalismanVisual>(BonusTalismanPresentation.ResourcePath);
        var previews=new GameObject[2];
        for(int i=0;i<2;i++)
        {
            var art=UnityEngine.Object.Instantiate(source);previews[i]=art.gameObject;
            art.transform.position=player.transform.position+player.transform.forward*9+player.transform.right*(i==0?-1.4f:1.4f)+Vector3.up*2;
            art.RememberRestPose();
            art.SetContent(Resources.Load<Sprite>("WallBonusIcons/"+(i==0?"WallBonus_Health":"WallBonus_Attack")),i==0?"체력 +14%":"공격력 +20",i==0?new Color(.05f,.85f,.65f):new Color(1,.65f,.1f));
        }
        yield return new WaitForEndOfFrame();Capture(folder+"/talisman.png");
        foreach(var go in previews)UnityEngine.Object.Destroy(go);
        TimeManager.isGameRunning=false;
        File.WriteAllText(folder+"/complete.txt","Captured actual runtime lobby, upgrade UI, tutorial and native talisman artwork. The final two talismans are art previews, not reward transactions.");
    }
    static void Capture(string path)
    {
        var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.Destroy(image);
    }
}
