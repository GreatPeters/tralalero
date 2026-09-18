using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

public static class ActualRetryProbe
{
    public static object Begin()
    {
        const string output="tmp/combat-feedback-2026-09-14/actual-retry.txt";
        if(File.Exists(output))throw new InvalidOperationException("Preserve earlier test");
        OpeningStoryUI.Instance?.Skip();
        var player=Object.FindFirstObjectByType<PlayerScript>();var canvas=Object.FindFirstObjectByType<CanvasScript>();
        canvas.PlayerPressedStartButton();
        if(!TimeManager.isGameRunning)throw new InvalidOperationException("Start blocked");
        float health=player.MaxHealth;var weapon=player.GetComponentInChildren<WeaponScript>();
        float damage=weapon.damage,rate=weapon.fireRate,duration=BulletScript.CurrentMissileDuration;int count=weapon.bulletCount;
        player.ApplyRunHealthBonus(200,false);weapon.damage+=100;weapon.fireRate+=3;weapon.bulletCount+=3;
        BulletScript.AddMissileDurationPercent(100);player.DieFromHazard(true);
        double began=EditorApplication.timeSinceStartup;bool continued=false;int frame=-1;
        EditorApplication.CallbackFunction tick=null;
        tick=()=>{
            if(!EditorApplication.isPlaying){EditorApplication.update-=tick;return;}
            if(frame==Time.frameCount)return;frame=Time.frameCount;
            double elapsed=EditorApplication.timeSinceStartup-began;
            if(!continued&&elapsed>1.2){canvas.gameOverUI.GetComponent<DefeatPresentation>().ReturnToAltar();continued=true;}
            if(continued&&player==null)
            {
                var fresh=Object.FindFirstObjectByType<PlayerScript>();if(fresh==null)return;
                var freshWeapon=fresh.GetComponentInChildren<WeaponScript>();
                bool ok=Mathf.Approximately(fresh.MaxHealth,health)&&Mathf.Approximately(freshWeapon.damage,damage)&&Mathf.Approximately(freshWeapon.fireRate,rate)&&freshWeapon.bulletCount==count&&Mathf.Approximately(BulletScript.CurrentMissileDuration,duration);
                File.WriteAllText(output,"Actual defeat Continue -> scene reload: "+ok+"; HP="+fresh.MaxHealth+"; ATT="+freshWeapon.damage+"; rate="+freshWeapon.fireRate+"; count="+freshWeapon.bulletCount+"; duration="+BulletScript.CurrentMissileDuration);
                EditorApplication.update-=tick;
            }
            if(elapsed>20){EditorApplication.update-=tick;File.WriteAllText(output,"FAILED: retry did not complete within 20 seconds");}
        };
        EditorApplication.update+=tick;return "Waiting for actual retry";
    }
}
