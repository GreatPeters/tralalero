using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class VerifyMobileCombatPlay
{
    public static string Main(string folder)
    {
        if(!EditorApplication.isPlaying||Directory.Exists(folder))throw new InvalidOperationException("Play Mode and fresh output folder required.");
        Directory.CreateDirectory(folder);var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Run(canvas,folder));return folder;
    }
    static IEnumerator Run(CanvasScript canvas,string folder)
    {
        UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include)?.Skip();canvas.PlayerPressedStartButton();yield return null;
        var tutorial=UnityEngine.Object.FindFirstObjectByType<CoastalTutorialUI>(FindObjectsInactive.Include);if(tutorial!=null){tutorial.enabled=false;tutorial.Close();}
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();player.canShoot=false;
        foreach(var weapon in UnityEngine.Object.FindObjectsByType<WeaponScript>(FindObjectsInactive.Include,FindObjectsSortMode.None)){weapon.StopAllCoroutines();weapon.enabled=false;}
        var all=UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var guard=all.First(e=>e.name.Contains("Guard")&&e.gameObject.activeInHierarchy);
        var fat=all.First(e=>e.name.Contains("FatMan")&&e.gameObject.activeInHierarchy);
        foreach(var enemy in all)enemy.gameObject.SetActive(false);
        foreach(var hazard in UnityEngine.Object.FindObjectsByType<ObstacleStats>(FindObjectsSortMode.None))hazard.gameObject.SetActive(false);
        foreach(var wall in UnityEngine.Object.FindObjectsByType<WallScript>(FindObjectsSortMode.None))wall.GetComponentInParent<BonusWallLifetimeRoot>()?.gameObject.SetActive(false);
        foreach(var spot in UnityEngine.Object.FindObjectsByType<NoryangjinTurnSpot>(FindObjectsSortMode.None))foreach(var collider in spot.GetComponents<Collider>())collider.enabled=false;
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        float oldCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/30;
        var reports=new List<object>();
        try
        {
            foreach(string kind in new[]{"guard","guard-owner-disabled","fatman-once"})
            {
                foreach(var shot in UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(s=>s.name.EndsWith("_Shot")))UnityEngine.Object.Destroy(shot.gameObject);
                guard.gameObject.SetActive(false);fat.gameObject.SetActive(false);player.enabled=true;player.ResetState();player.currentHealth=player.MaxHealth;player.canShoot=false;
                foreach(var weapon in player.GetComponentsInChildren<WeaponScript>(true)){weapon.StopAllCoroutines();weapon.enabled=false;}
                var enemy=kind.StartsWith("guard")?guard:fat;var row=EncounterPlacementTables.Rows.First(r=>r.id==enemy.name);
                Vector3 direction=enemy.AuthoredThrowDirection,position=enemy.transform.position+direction*16;position.y=enemy.transform.position.y;
                player.ApplyContinuousRoutePose(position,-direction,0);player.enabled=false;player.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
                typeof(PlayerScript).GetField("currentForwardMoveSpeed",flags).SetValue(player,8.4f);
                enemy.gameObject.SetActive(true);enemy.ApplyStat(row.damage,row.health,row.tier);
                TimeManager.isGameRunning=true;TimeManager.timeFactor=1;
                bool triggered=kind.StartsWith("guard")?enemy.GetComponent<EnemyEventController>().ActivateFromSpot():true;
                var ids=new HashSet<int>();int firstShot=-1,hitFrame=-1,damageEvents=0;float initial=player.currentHealth,last=initial,maxTravel=0;bool survives=false;
                Vector3 shotStart=Vector3.zero;SimpleProjectile current=null;
                for(int frame=0;frame<240;frame++)
                {
                    yield return new WaitForEndOfFrame();
                    foreach(var shot in UnityEngine.Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None).Where(s=>s.name.EndsWith("_Shot")))
                    {
                        if(ids.Add(shot.GetInstanceID())&&firstShot<0){firstShot=frame;shotStart=shot.transform.position;current=shot;}
                        maxTravel=Mathf.Max(maxTravel,Vector3.Distance(shot.transform.position,shotStart));
                    }
                    if(kind=="guard-owner-disabled"&&firstShot>=0&&frame==firstShot+2)enemy.gameObject.SetActive(false);
                    if(kind=="guard-owner-disabled"&&firstShot>=0&&frame==firstShot+5)survives=current!=null&&current.gameObject.activeInHierarchy;
                    if(player.currentHealth<last-.001f){damageEvents++;if(hitFrame<0)hitFrame=frame;}
                    last=player.currentHealth;
                    if(kind!="guard-owner-disabled")Capture(folder+"/"+kind+"-"+frame.ToString("D3")+".png");
                }
                reports.Add(new{kind,triggered,shots=ids.Count,firstShot,hitFrame,damageEvents,attack=row.damage,initial,final=player.currentHealth,maxTravel,survives,canFireAgain=enemy.CanBeginTriggeredFire});Write(folder,reports);
            }
        }
        finally{Time.captureDeltaTime=oldCapture;TimeManager.isGameRunning=false;player.enabled=true;}
    }
    static void Capture(string path){var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);}
    static void Write(string folder,object report)
    {
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{report});File.WriteAllText(folder+"/report.json",json);
    }
}
