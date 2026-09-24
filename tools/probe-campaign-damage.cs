using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;

public static class ProbeCampaignDamage
{
    public static string Main()
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new InvalidOperationException("Fresh HighWay Play lobby required");
        const string folder="tmp/image-previews/campaign-balance-2026-09-23/physical-prop-damage";
        if(Directory.Exists(folder))throw new IOException("Preserve prior evidence");Directory.CreateDirectory(folder);
        PlayerPrefs.SetInt("TutorialDone",1);OpeningStoryUI.Instance?.Skip();
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
        canvas.StartCoroutine(Capture(canvas,folder));return folder;
    }
    static IEnumerator Capture(CanvasScript canvas,string folder)
    {
        yield return null;canvas.PlayerPressedStartButton();Time.timeScale=1;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var anchor=new GameObject("Damage probe anchor").AddComponent<RestStopHoldout>();
        anchor.police=Array.Empty<EnemyEventController>();
        player.SetStationaryCombat(anchor,true);player.canShoot=false;
        var hazard=UnityEngine.Object.FindObjectsByType<HighwayHazard>(FindObjectsSortMode.None).First(h=>h.GetComponent<ObstacleStats>().obstaclePattern==ObstaclePattern.HighwayRoadblock);
        float before=player.currentHealth;int beforeHits=player.GetComponent<PlayerDamageFeedback>().ShownHitCount;
        hazard.transform.position=player.transform.position;Physics.SyncTransforms();
        float deadline=Time.realtimeSinceStartup+3;
        while(player.currentHealth>=before&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        yield return new WaitForSecondsRealtime(.18f);
        ScreenCapture.CaptureScreenshot(folder+"/roadblock-hit.png");
        File.WriteAllText(folder+"/result.json","{\"physicalContact\":true,\"fixture\":\"real roadblock relocated to stationary ordinary-health player; not a traversal run\",\"before\":"+before+",\"after\":"+player.currentHealth+",\"cause\":\""+player.LastDamageCause+"\",\"notifications\":"+(player.GetComponent<PlayerDamageFeedback>().ShownHitCount-beforeHits)+",\"visible\":\""+player.GetComponent<PlayerDamageFeedback>().VisibleDamageText+"\"}");
        yield return new WaitForEndOfFrame();EditorApplication.isPaused=true;
    }
}
