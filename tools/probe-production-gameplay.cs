using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class ProbeProductionGameplay
{
    public static string Main(string label,string focus="",float seconds=20)
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPaused||TimeManager.isGameRunning)throw new Exception("Fresh unpaused Play Mode lobby required");
        string folder="tmp/image-previews/reststop-scene-integration-2026-09-25/runtime-"+label;if(Directory.Exists(folder))throw new Exception("Preserve previous evidence");Directory.CreateDirectory(folder);
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas,folder,focus,seconds));return folder;
    }
    static IEnumerator Run(CanvasScript canvas,string folder,string focus,float seconds)
    {
        var errors=new List<string>();Application.LogCallback onLog=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};Application.logMessageReceived+=onLog;
        Time.timeScale=1;OpeningStoryUI.Instance?.Skip();canvas.PlayerPressedStartButton();yield return null;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var startPosition=player.transform.position;
        float initialHp=player.currentHealth;var speed=typeof(PlayerScript).GetField("currentForwardMoveSpeed",BindingFlags.NonPublic|BindingFlags.Instance);
        GameObject actor=null;Transform held=null;Animator animator=null;Vector3 initialHand=Vector3.zero;Transform hand=null;
        if(focus.Length>0)
        {
            var sources=UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var template=sources.First(e=>e.GetComponentsInChildren<Animator>(true).Any(a=>AssetDatabase.GetAssetPath(a.runtimeAnimatorController).Contains("/"+focus+"/")));
            actor=UnityEngine.Object.Instantiate(template.gameObject,player.transform.position+player.transform.forward*14,player.transform.rotation);actor.name="Production firing probe "+focus;actor.SetActive(true);
            foreach(var enemy in sources)enemy.gameObject.SetActive(false);
            foreach(var hazard in UnityEngine.Object.FindObjectsByType<HighwayHazard>(FindObjectsSortMode.None))hazard.enabled=false;
            var membership=actor.GetComponent<HighwayEncounterMember>();if(membership!=null)UnityEngine.Object.Destroy(membership);
            var enemyProbe=actor.GetComponent<EnemyScript_space>();enemyProbe.ApplyStat(100,10000,EnemyTier.Normal);
            held=new SerializedObject(enemyProbe).FindProperty("heldProjectile").objectReferenceValue as Transform;
            if(held==null){File.WriteAllText(folder+"/failed.txt","Missing controlled projectile");Application.logMessageReceived-=onLog;yield break;}
            var events=actor.GetComponent<EnemyEventController>();events.EventMode=EnemyEventMode.Shoot;events.ActivateFromSpot();
            animator=actor.GetComponentInChildren<Animator>();hand=actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Hand.R");initialHand=hand.position;
        }
        float start=Time.realtimeSinceStartup,startGame=Time.time,nextCapture=0,nextSample=0,maxHandMotion=0;int frames=0,shotsSeen=0;float firstShot=-1,releasePhase=-1;
        var seen=new HashSet<int>();
        using(var log=new StreamWriter(folder+"/timeline.csv"))
        {
            log.WriteLine("wallSeconds,gameSeconds,health,x,y,z,shots,animationPhase");
            try
            {
                while(Time.realtimeSinceStartup-start<seconds)
                {
                    if(focus.Length>0){speed.SetValue(player,0f);player.canShoot=false;}
                    yield return null;
                    float elapsed=Time.realtimeSinceStartup-start;
                    if(hand!=null)maxHandMotion=Mathf.Max(maxHandMotion,Vector3.Distance(hand.position,initialHand));
                    if(held!=null)foreach(var shot in UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(p=>p.name==held.name+"_Shot"))
                    {
                        if(!seen.Add(shot.GetInstanceID()))continue;shotsSeen++;
                        if(firstShot<0){firstShot=elapsed;releasePhase=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;}
                    }
                    if(elapsed>=nextSample){log.WriteLine(string.Join(",",elapsed,Time.time-startGame,player.currentHealth,player.transform.position.x,player.transform.position.y,player.transform.position.z,shotsSeen,animator!=null?animator.GetCurrentAnimatorStateInfo(0).normalizedTime:0));nextSample+=.2f;}
                    if(elapsed>=nextCapture){Capture(Camera.main,folder+"/frame-"+frames++.ToString("D4")+".jpg");nextCapture+=focus.Length>0?.1f:1;}
                    if(player.currentHealth<=0&&elapsed>5)break;
                }
                var report=new{scene=SceneManager.GetActiveScene().name,focus,controlled=focus.Length>0,frames,wallSeconds=Time.realtimeSinceStartup-start,gameSeconds=Time.time-startGame,initialHp,finalHp=player.currentHealth,distanceMoved=Vector3.Distance(startPosition,player.transform.position),shotsSeen,firstShot,releasePhase,maxHandMotion,errors,success=errors.Count==0&&(focus.Length==0?Vector3.Distance(startPosition,player.transform.position)>1:shotsSeen>0&&maxHandMotion>.05f&&player.currentHealth<initialHp)};
                File.WriteAllText(folder+"/result.json",Json(report));
            }
            finally{Application.logMessageReceived-=onLog;Time.timeScale=1;EditorApplication.isPaused=true;}
        }
    }
    static void Capture(Camera camera,string path)
    {
        if(camera==null)return;var target=new RenderTexture(1280,720,24);target.Create();var original=camera.targetTexture;var prior=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToJPG(88));UnityEngine.Object.Destroy(image);}
        finally{camera.targetTexture=original;RenderTexture.active=prior;target.Release();UnityEngine.Object.Destroy(target);}
    }
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
}
