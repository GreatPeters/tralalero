using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class RecordReadabilityPreflight
{
    public static string Main(string label="harbor")
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        string folder="tmp/image-previews/review-fixes-20-runs-2026-09-22/preflight-"+label;if(Directory.Exists(folder))throw new Exception("Fresh label required");Directory.CreateDirectory(folder);
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas,folder));return folder;
    }
    static IEnumerator Run(CanvasScript canvas,string folder)
    {
        OpeningStoryUI.Instance?.Skip();canvas.PlayerPressedStartButton();float began=Time.time,next=0;int frame=0;
        var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var camera=Camera.main;var occlusion=camera.GetComponent<NoryangjinCameraOcclusion>();
        var gantry=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).FirstOrDefault(r=>r.name.Contains("Harbor_lane_signal_gantry"));
        using(var log=new StreamWriter(folder+"/occlusion.csv"))
        {
            log.WriteLine("time,opacity,hidden,faded,health");
            while(Time.time-began<22&&p.currentHealth>0)
            {
                yield return new WaitForEndOfFrame();float t=Time.time-began;
                if(t>=next){next=t+.25f;var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+"/frame-"+frame++.ToString("D3")+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);log.WriteLine($"{t},{(gantry!=null?occlusion.SceneryOpacity(gantry):1)},{occlusion.HiddenCount},{occlusion.FadedCount},{p.currentHealth}");}
            }
        }
        TimeManager.isGameRunning=false;EditorApplication.isPaused=true;File.WriteAllText(folder+"/complete.txt","Native 1x preflight; ordinary movement/stats.");
    }
}
