using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class RecordTalismanPlay
{
    public static object Main()
    {
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
        string folder="tmp/image-previews/talisman-polish-2026-09-20/video/"+SceneManager.GetActiveScene().name;
        Directory.CreateDirectory(folder);canvas.StartCoroutine(Record(canvas,folder));return folder;
    }
    static IEnumerator Record(CanvasScript canvas,string folder)
    {
        var story=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);if(story!=null)story.Skip();
        canvas.PlayerPressedStartButton();yield return null;
        var tutorial=canvas.GetComponent<CoastalTutorialUI>();if(tutorial!=null&&tutorial.panel!=null)tutorial.panel.gameObject.SetActive(false);
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        foreach(var weapon in player.GetComponentsInChildren<WeaponScript>())weapon.enabled=false;
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Walls/New/wall_attPer_normal.prefab");
        var root=UnityEngine.Object.Instantiate(source,player.transform.position+player.transform.forward*14,Quaternion.identity);
        root.AddComponent<RuntimeBonusWall>();
        var wall=root.GetComponentInChildren<WallScript>(true);yield return null;wall.SetWallSprite();wall.ReactivateLifetimeObject();
        int claimedFrame=-1,effectFrames=0;float oldCapture=Time.captureDeltaTime;float oldScale=Time.timeScale;
        var target=RenderTexture.GetTemporary(540,1170,0,RenderTextureFormat.ARGB32);
        var pixels=new Texture2D(540,1170,TextureFormat.RGB24,false);
        Time.captureDeltaTime=1f/30;Time.timeScale=1;
        try
        {
            for(int frame=0;frame<120;frame++)
            {
                yield return new WaitForEndOfFrame();
                if(!root.activeInHierarchy&&claimedFrame<0)claimedFrame=frame;
                if(UnityEngine.Object.FindFirstObjectByType<BonusTalismanPickup>()!=null)effectFrames++;
                var screenshot=ScreenCapture.CaptureScreenshotAsTexture();
                Graphics.Blit(screenshot,target);var previous=RenderTexture.active;RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,540,1170),0,0);pixels.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(folder+"/frame-"+frame.ToString("D3")+".png",pixels.EncodeToPNG());
                UnityEngine.Object.Destroy(screenshot);
            }
        }
        finally
        {
            Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;
            RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(root);
        }
        Time.timeScale=0;
        File.WriteAllText(folder+"/record.json","{\"fps\":30,\"frames\":120,\"claimedFrame\":"+claimedFrame+",\"effectFrames\":"+effectFrames+",\"physicalTrigger\":true}");
        if(claimedFrame<0||effectFrames<15)Debug.LogError("Recorded pickup did not complete visibly; inspect "+folder);
    }
}
