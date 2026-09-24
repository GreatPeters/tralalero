using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class RecordCampaignSegment
{
    public static string AtTime(string label,float chapterSeconds,float gameSeconds=12)
    {
        if(!EditorApplication.isPlaying||!TimeManager.isGameRunning)throw new InvalidOperationException("A live run is required");
        UnityEngine.Object.FindFirstObjectByType<CanvasScript>().StartCoroutine(AwaitTime(label,chapterSeconds,gameSeconds));
        return "Scheduled native capture at "+chapterSeconds;
    }
    static IEnumerator AwaitTime(string label,float seconds,float duration)
    {
        var chapter=UnityEngine.Object.FindFirstObjectByType<ChapterProgression>();
        while(chapter!=null&&chapter.Elapsed<seconds&&TimeManager.isGameRunning)yield return null;
        if(chapter!=null&&TimeManager.isGameRunning)Main(label,duration);
    }
    public static string Main(string label,float gameSeconds=12)
    {
        if(!EditorApplication.isPlaying||!TimeManager.isGameRunning)throw new InvalidOperationException("A live run is required");
        string folder="tmp/image-previews/campaign-balance-2026-09-23/segments/"+label;
        if(Directory.Exists(folder))throw new IOException("Preserve existing footage");Directory.CreateDirectory(folder);
        UnityEngine.Object.FindFirstObjectByType<CanvasScript>().StartCoroutine(Record(folder,gameSeconds));return folder;
    }
    static IEnumerator Record(string folder,float seconds)
    {
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var chapter=UnityEngine.Object.FindFirstObjectByType<ChapterProgression>();
        float start=Time.time;int frame=0;
        using(var log=new StreamWriter(folder+"/timeline.csv"))
        {
            log.WriteLine("game_time,chapter_time,hp,attack");
            while(player!=null&&Time.time-start<seconds)
            {
                yield return new WaitForEndOfFrame();
                var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+"/frame-"+frame++.ToString("D4")+".jpg",image.EncodeToJPG(87));UnityEngine.Object.Destroy(image);
                log.WriteLine($"{Time.time-start},{chapter.Elapsed},{player.currentHealth},{player.currentDamage}");
                if(!TimeManager.isGameRunning)break;
            }
        }
        File.WriteAllText(folder+"/capture.txt",$"{frame} native frames from the active earned-progression run. No gameplay/stat/camera changes. Cohort uses fixed30game-fps simulation; not a device FPS measurement.");
    }
}
