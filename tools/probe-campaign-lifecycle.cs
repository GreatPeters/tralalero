using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using IndianOceanAssets.ShooterSurvival;

public static class ProbeCampaignLifecycle
{
    public static string Main(string mode)
    {
        if(!EditorApplication.isPlaying||TimeManager.isGameRunning)throw new InvalidOperationException("Fresh lobby required");
        string folder="tmp/image-previews/campaign-balance-2026-09-23/lifecycle-"+mode;
        if(Directory.Exists(folder))throw new IOException("Preserve prior probe");Directory.CreateDirectory(folder);
        UnityEngine.Object.FindFirstObjectByType<CanvasScript>().StartCoroutine(Run(mode,folder));return folder;
    }
    static IEnumerator Run(string mode,string folder)
    {
        Time.timeScale=1;Time.captureDeltaTime=0;PlayerPrefs.SetInt("TutorialDone",1);OpeningStoryUI.Instance?.Skip();
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();canvas.PlayerPressedStartButton();
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        typeof(PlayerScript).GetField("currentForwardMoveSpeed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,0f);player.canShoot=false;
        yield return null;
        if(mode.StartsWith("burst-heal",StringComparison.Ordinal))
        {
            float before=player.currentHealth,capacity=player.MaxHealth;
            player.ApplyDamage(100,PlayerDamageCause.Crate);player.ApplyDamage(25,PlayerDamageCause.EnemyProjectile);
            yield return new WaitForSecondsRealtime(.18f);yield return new WaitForEndOfFrame();
            var feedback=player.GetComponent<PlayerDamageFeedback>();string shown=feedback.VisibleDamageText;int count=feedback.ShownHitCount;
            var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+"/combined-damage.png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
            float afterBurst=player.currentHealth;player.ApplyDamage(player.currentHealth,PlayerDamageCause.Cannon);
            canvas.YouWin();float healed=player.Heal(100);player.ApplyRunHealthBonus(100,false);
            bool completed=UnityEngine.Object.FindFirstObjectByType<ChapterProgression>().Completed;
            File.WriteAllText(folder+"/result.json",$"{{\"fixture\":\"direct damage transaction probe, ordinary starting HP\",\"before\":{before},\"afterBurst\":{afterBurst},\"notice\":\"{shown}\",\"notifications\":{count},\"postFatalHP\":{player.currentHealth},\"postFatalMaxHP\":{player.MaxHealth},\"originalMaxHP\":{capacity},\"healedAfterDeath\":{healed},\"wonAfterDeath\":{completed.ToString().ToLowerInvariant()}}}");
        }
        else if(mode=="equal-contact")
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Highway/Enemies/ConeMechanic.prefab");
            var actor=UnityEngine.Object.Instantiate(prefab,player.transform.position+player.transform.forward*12,player.transform.rotation);
            yield return null;
            var enemy=actor.GetComponent<EnemyScript_space>();var events=actor.GetComponent<EnemyEventController>();events.EventMode=EnemyEventMode.AttackOnce;events.MoveSpeed=0;
            enemy.ConfigureRewards(false,0);enemy.ApplyStat(100,player.currentHealth,EnemyTier.Normal);float hp=player.currentHealth;
            actor.transform.position=player.transform.position;Physics.SyncTransforms();
            float deadline=Time.realtimeSinceStartup+2;
            while(player.currentHealth>0&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            File.WriteAllText(folder+"/result.json",$"{{\"fixture\":\"equal HP enemy relocated for real collider contact\",\"startingHPBoth\":{hp},\"playerHP\":{player.currentHealth},\"enemyHP\":{enemy.CurrentHealth},\"enemyState\":\"{events.RuntimeState}\",\"cause\":\"{player.LastDamageCause}\"}}");
        }
        else throw new ArgumentException(mode);
        yield return new WaitForEndOfFrame();EditorApplication.isPaused=true;
    }
}
