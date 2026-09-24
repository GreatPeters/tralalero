using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class RecordHighwayRebuild
{
    public static string Main(string label="left-v1",float steering=-15,float seconds=18,bool isolateEnemyContact=false)
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new Exception("Fresh Play Mode lobby required");
        string path="tmp/image-previews/highway-enemy-rebuild-2026-09-23/"+label;if(Directory.Exists(path))throw new Exception("Preserve evidence");Directory.CreateDirectory(path);
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas,path,steering,seconds,isolateEnemyContact));return path;
    }
    static IEnumerator Run(CanvasScript canvas,string path,float steering,float seconds,bool isolateEnemyContact)
    {
        Time.timeScale=1;Time.captureDeltaTime=1f/30;OpeningStoryUI.Instance?.Skip();canvas.PlayerPressedStartButton();
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var route=UnityEngine.Object.FindFirstObjectByType<HighwayRoute>();
        if(isolateEnemyContact)
        {
            var traffic=UnityEngine.Object.FindFirstObjectByType<HighwayOncomingTraffic>();traffic.enabled=false;
            foreach(var car in traffic.cars)if(car!=null)car.gameObject.SetActive(false);
            foreach(var line in traffic.warnings)if(line!=null)line.gameObject.SetActive(false);
        }
        var move=typeof(PlayerScript).GetMethod("PlayerMove",BindingFlags.Instance|BindingFlags.NonPublic);float start=Time.time;int frame=0;float deadAt=-1;
        var first=UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsSortMode.None).Where(e=>e.name.StartsWith("HWY_E01_")).ToArray();
        using(var log=new StreamWriter(path+"/timeline.csv"))
        {
            log.WriteLine("time,health,distance,x,z,enemy0hp,enemy1hp,animation,phase");
            while(Time.time-start<seconds)
            {
                if(TimeManager.isGameRunning&&player.currentHealth>0)move.Invoke(player,new object[]{steering});
                yield return new WaitForEndOfFrame();float t=Time.time-start;
                var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path+"/frame-"+frame++.ToString("D4")+".jpg",texture.EncodeToJPG(85));UnityEngine.Object.Destroy(texture);
                var a=first[0].GetComponentInChildren<Animator>();var state=a.GetCurrentAnimatorStateInfo(0);
                log.WriteLine($"{t},{player.currentHealth},{route.Distance},{player.transform.position.x},{player.transform.position.z},{first[0].CurrentHealth},{first[1].CurrentHealth},{state.shortNameHash},{state.normalizedTime}");
                if(player.currentHealth<=0&&deadAt<0)deadAt=Time.time;
                if(deadAt>=0&&Time.time-deadAt>1.3f)break;
            }
        }
        Time.captureDeltaTime=0;File.WriteAllText(path+"/complete.txt",$"Native ordinary controls, no health/damage override. Steering {steering}, final HP {player.currentHealth}, cause {player.LastDamageCause}. Traffic suppressed for isolated enemy-contact probe: {isolateEnemyContact}. Fixed 30fps simulation capture; no FPS performance claim.");EditorApplication.isPaused=true;
    }
}
