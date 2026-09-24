using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;

public static class ProbeRestStopPassages
{
    public static string Main(string label,float steering)
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning||UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="RestStop")throw new InvalidOperationException("Fresh RestStop lobby required");
        string folder="tmp/image-previews/later-chapters-30-2026-09-23/"+label;
        if(Directory.Exists(folder))throw new IOException("Preserve existing contact evidence");Directory.CreateDirectory(folder);
        UnityEngine.Object.FindFirstObjectByType<CanvasScript>().StartCoroutine(Run(folder,steering));return folder;
    }
    static IEnumerator Run(string folder,float steering)
    {
        Time.timeScale=1;Time.captureDeltaTime=1f/30;PlayerPrefs.SetInt("TutorialDone",1);OpeningStoryUI.Instance?.Skip();
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.PlayerPressedStartButton();
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();player.canShoot=false;
        var held=typeof(EnemyScript_space).GetField("heldProjectile",BindingFlags.Instance|BindingFlags.NonPublic);
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None))held.SetValue(enemy,null);
        var row=UnityEngine.Object.FindObjectsByType<HighwayEncounterRow>(FindObjectsSortMode.None).OrderBy(r=>(r.transform.position-player.transform.position).sqrMagnitude).First();
        float startingHp=player.currentHealth,leftHp=row.left.CurrentHealth,rightHp=row.right.CurrentHealth,start=Time.time;
        canvas.StartCoroutine(Drive(player,steering,start+16));int frame=0;float endAt=float.PositiveInfinity;
        using(var log=new StreamWriter(folder+"/timeline.csv"))
        {
            log.WriteLine("time,hp,x,z,lane");
            while(Time.time-start<16&&Time.time<endAt)
            {
                yield return new WaitForEndOfFrame();
                var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+"/frame-"+frame++.ToString("D4")+".jpg",image.EncodeToJPG(85));UnityEngine.Object.Destroy(image);
                float lane=Vector3.Dot(player.transform.position-row.transform.position,row.transform.right);
                log.WriteLine(FormattableString.Invariant($"{Time.time-start},{player.currentHealth},{player.transform.position.x},{player.transform.position.z},{lane}"));
                if(player.currentHealth<=0&&float.IsPositiveInfinity(endAt))endAt=Time.time+.5f;
            }
        }
        var result=new{fixture="real first-row approach; outgoing fire suppressed only to isolate contact",steering,startingHp,leftHp,rightHp,finalHp=player.currentHealth,cause=player.LastDamageCause.ToString(),finalLane=Vector3.Dot(player.transform.position-row.transform.position,row.transform.right),frames=frame,normalMovement=true,healthOverride=false};
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result});
        File.WriteAllText(folder+"/result.json",json);Time.captureDeltaTime=0;EditorApplication.isPaused=true;
    }
    static IEnumerator Drive(PlayerScript player,float steering,float until)
    {
        var move=typeof(PlayerScript).GetMethod("PlayerMove",BindingFlags.Instance|BindingFlags.NonPublic);
        while(TimeManager.isGameRunning&&player.currentHealth>0&&Time.time<until)
        {move.Invoke(player,new object[]{steering});yield return new WaitForFixedUpdate();}
    }
}
