using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;

public static class VerifyHighwayProjectilePose
{
    public static string Main(string name,string label="final",string assetFolder="Assets/ShooterSurvival/Prefabs/Highway/Enemies",string evidenceRoot="tmp/image-previews/highway-enemy-rebuild-2026-09-23",float delayOverride=-1)
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new Exception("Fresh lobby required");
        string folder=evidenceRoot+"/projectile-"+label+"/"+name;if(Directory.Exists(folder))throw new Exception("Preserve evidence");Directory.CreateDirectory(folder);
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas,name,folder,assetFolder,delayOverride));return folder;
    }
    static IEnumerator Run(CanvasScript canvas,string name,string folder,string assetFolder,float delayOverride)
    {
        Time.timeScale=1;Time.captureDeltaTime=1f/30;OpeningStoryUI.Instance?.Skip();canvas.PlayerPressedStartButton();
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();player.canShoot=false;
        var speed=typeof(PlayerScript).GetField("currentForwardMoveSpeed",BindingFlags.NonPublic|BindingFlags.Instance);speed.SetValue(player,0f);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(assetFolder+"/"+name+".prefab");var actor=UnityEngine.Object.Instantiate(source,player.transform.position+player.transform.forward*16,player.transform.rotation);actor.name="Probe_"+name;
        var enemy=actor.GetComponent<EnemyScript_space>();enemy.ApplyStat(100,10000,EnemyTier.Normal);var events=actor.GetComponent<EnemyEventController>();events.EventMode=EnemyEventMode.Shoot;
        if(delayOverride>=0)enemy.ConfigureTriggeredFire(delayOverride,10.2f);
        var data=new SerializedObject(enemy);var held=(Transform)data.FindProperty("heldProjectile").objectReferenceValue;var muzzle=(Transform)data.FindProperty("throwPoint").objectReferenceValue;float delay=data.FindProperty("throwReleaseDelay").floatValue;
        yield return null;float begin=Time.time;events.ActivateFromSpot();int frame=0;GameObject shot=null;float firstTime=-1,positionError=-1,rotationError=-1,releasePhase=-1;
        using(var log=new StreamWriter(folder+"/timeline.csv"))
        {
            log.WriteLine("time,health,shotCount,handY,shotY");
            while(Time.time-begin<4)
            {
                speed.SetValue(player,0f);player.canShoot=false;yield return new WaitForEndOfFrame();
                var shots=UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(p=>p.name==held.name+"_Shot").ToArray();
                if(shot==null&&shots.Length>0)
                {shot=shots[0].gameObject;firstTime=Time.time-begin;positionError=Vector3.Distance(shot.transform.position,muzzle.position);rotationError=Quaternion.Angle(shot.transform.rotation,held.rotation);releasePhase=actor.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;}
                var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+"/frame-"+frame++.ToString("D4")+".jpg",texture.EncodeToJPG(85));UnityEngine.Object.Destroy(texture);
                log.WriteLine($"{Time.time-begin},{player.currentHealth},{shots.Length},{held.position.y},{(shot!=null?shot.transform.position.y:0)}");
            }
        }
        Time.captureDeltaTime=0;
        File.WriteAllText(folder+"/result.txt",$"Controlled firing probe: actor HP10000/attack100; player movement/shooting held for inspection. No scene saved.\nreleaseDelay={delay}, firstShot={firstTime}, phase={releasePhase}, positionError={positionError}, rotationError={rotationError}, finalPlayerHP={player.currentHealth}, cause={player.LastDamageCause}");
        EditorApplication.isPaused=true;
    }
}
