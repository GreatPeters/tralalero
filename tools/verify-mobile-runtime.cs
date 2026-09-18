using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerifyMobileRuntime
{
    public static object Main(string folder)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
        Directory.CreateDirectory(folder);
        var roots=SceneManager.GetActiveScene().GetRootGameObjects();
        var canvas=roots.Single(g=>g.name=="Canvas").GetComponent<CanvasScript>();
        var player=roots.SelectMany(g=>g.GetComponentsInChildren<PlayerScript>(true)).Single();
        if(TimeManager.isGameRunning||player.IsStationaryCombat||CanvasScript.isGameOver)throw new InvalidOperationException("A fresh lobby is required");
        var content=canvas.transform.Find("UI/Upgrade2/UpgradeViewport/GameObject");
        var cards=content.GetComponentsInChildren<UpgradeUI>(true);
        var lateral=cards.Single(c=>c.UpgradeId==10);
        var manager=UpgradeStatManager.S;var wallet=MoneyScript.S;
        var keys=new Dictionary<string,(bool exists,int integer,float number,bool isFloat)>();
        void Keep(string key,bool floating=false)=>keys[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key),PlayerPrefs.GetFloat(key),floating);
        foreach(string key in new[]{"coin","jewel","upgrade_lv_1","upgrade_lv_2","upgrade_lv_10","soundEnabled","vibrationEnabled"})Keep(key);
        foreach(string key in new[]{"soundVolume","moveSensitivity"})Keep(key,true);
        foreach(string name in Enum.GetNames(typeof(UpgradeStatManager.UpgradeType))){Keep("upgrade_stat_"+name,true);Keep("upgrade_stat_type_"+name);}
        File.WriteAllLines(Path.Combine(folder,"runtime-preferences-before.tsv"),keys.Select(k=>k.Key+"\t"+k.Value.exists+"\t"+k.Value.isFloat+"\t"+k.Value.integer+"\t"+k.Value.number.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
        int coins=wallet.Coin,jewels=wallet.Jewel;float factor=TimeManager.timeFactor;bool running=TimeManager.isGameRunning;
        var position=player.transform.position;var rotation=player.transform.rotation;float health=player.currentHealth;
        var routeFields=new[]{"routeLaneOrigin","routeRight","routeFrameInitialized"}.Select(n=>typeof(PlayerScript).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance)).ToArray();
        var routeValues=routeFields.Select(f=>f.GetValue(player)).ToArray();
        bool movement=player.movement;bool shopActive=content.parent.parent.gameObject.activeSelf;
        var checks=new List<string>();
        var lockRoot=new GameObject("Temporary movement verification owner");lockRoot.SetActive(false);
        var lockOwner=lockRoot.AddComponent<RestStopHoldout>();
        void Check(bool success,string label){if(!success)throw new InvalidOperationException(label);checks.Add(label);}
        void RestorePrefs()
        {
            foreach(var pair in keys)
            {
                if(!pair.Value.exists)PlayerPrefs.DeleteKey(pair.Key);
                else if(pair.Value.isFloat)PlayerPrefs.SetFloat(pair.Key,pair.Value.number);
                else PlayerPrefs.SetInt(pair.Key,pair.Value.integer);
            }
            PlayerPrefs.Save();
        }
        try
        {
            canvas.GetComponentInChildren<OpeningStoryUI>(true)?.Skip();
            content.parent.parent.gameObject.SetActive(true);
            PlayerPrefs.SetInt("upgrade_lv_10",0);manager.SyncFromPurchasedLevels();wallet.Coin=0;
            Check(!lateral.TryBuy()&&wallet.Coin==0&&PlayerPrefs.GetInt("upgrade_lv_10")==0,"insufficient funds never charges");
            wallet.Coin=100000;int total=0;
            for(int level=1;level<=10;level++)
            {
                UpgradeTables.TryGet(10,level,out var row);total+=row.price;
                Check(lateral.TryBuy()&&PlayerPrefs.GetInt("upgrade_lv_10")==level,"purchase level "+level);
                Check(Mathf.Abs(player.LateralSpeedMultiplier-(1+level*.05f))<.00001f,"movement percent level "+level);
            }
            Check(total==2275&&wallet.Coin==100000-total,"exact total upgrade cost");
            Check(!lateral.TryBuy()&&!lateral.TryBuy()&&wallet.Coin==100000-total,"maximum cannot charge again");
            manager.ApplyUpgrade(UpgradeStatManager.UpgradeType.LATERAL_SPEED,0,global::ValueType.Percent);
            manager.SyncFromPurchasedLevels();Check(Mathf.Abs(player.LateralSpeedMultiplier-1.5f)<.00001f,"saved level reloads fifty percent");
            float oldAttack=player.currentDamage,oldMax=player.MaxHealth;
            Check(cards.Single(c=>c.UpgradeId==1).TryBuy(),"attack purchase succeeds");
            Check(cards.Single(c=>c.UpgradeId==2).TryBuy(),"health purchase succeeds");
            var summary=content.parent.parent.GetComponentInChildren<UpgradeSummaryUI>(true);summary.RefreshNow();
            var data=new SerializedObject(summary);
            Check(((TMP_Text)data.FindProperty("attackText").objectReferenceValue).text==player.currentDamage.ToString("#,0.##")&&player.currentDamage>oldAttack,"header shows actual changed attack");
            Check(((TMP_Text)data.FindProperty("healthText").objectReferenceValue).text==player.MaxHealth.ToString("#,0.##")&&player.MaxHealth>oldMax,"header shows actual changed max health");
            var move=typeof(PlayerScript).GetMethod("PlayerMove",BindingFlags.NonPublic|BindingFlags.Instance);
            player.movement=true;TimeManager.timeFactor=1;
            player.ApplyContinuousRoutePose(position,Vector3.forward,0);
            manager.ApplyUpgrade(UpgradeStatManager.UpgradeType.LATERAL_SPEED,0,global::ValueType.Percent);
            move.Invoke(player,new object[]{10f});float baseline=Vector3.Distance(position,player.transform.position);
            player.ApplyContinuousRoutePose(position,Vector3.forward,0);
            manager.ApplyUpgrade(UpgradeStatManager.UpgradeType.LATERAL_SPEED,50,global::ValueType.Percent);
            move.Invoke(player,new object[]{10f});float upgraded=Vector3.Distance(position,player.transform.position);
            Check(baseline>0&&Mathf.Abs(upgraded/baseline-1.5f)<.01f,"actual movement grows fifty percent in the same frame");
            for(int i=0;i<150;i++)move.Invoke(player,new object[]{100000f});
            var range=(Vector2)typeof(PlayerScript).GetField("xRange",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player);
            Check(Mathf.Abs(player.transform.position.x-position.x)<=Mathf.Max(Mathf.Abs(range.x),Mathf.Abs(range.y))+.001f,"lane bounds remain enforced");
            player.ApplyContinuousRoutePose(position,Vector3.forward,0);TimeManager.timeFactor=0;move.Invoke(player,new object[]{500f});
            Check(Vector3.Distance(position,player.transform.position)<.0001f,"pause rejects lateral movement");
            TimeManager.timeFactor=1;player.SetStationaryCombat(lockOwner,true);move.Invoke(player,new object[]{500f});
            Check(Vector3.Distance(position,player.transform.position)<.0001f,"stationary combat rejects lateral movement");player.SetStationaryCombat(lockOwner,false);
            var audio=GameAudioService.Instance;Check(audio!=null&&audio.GetComponentsInChildren<AudioSource>().Length==14,"audio has exactly twelve effect voices and two loops");
            SettingsManager.Instance.SetSoundEnabled(false);Check(AudioListener.volume==0,"master mute works");
            File.WriteAllLines(Path.Combine(folder,"passed.txt"),checks);
            return new {passed=checks.Count,totalCost=total,baselineMovement=baseline,upgradedMovement=upgraded};
        }
        catch(Exception error){File.WriteAllText(Path.Combine(folder,"error.txt"),error.ToString());throw;}
        finally
        {
            player.SetStationaryCombat(lockOwner,false);UnityEngine.Object.Destroy(lockRoot);
            RestorePrefs();wallet.Coin=coins;wallet.Jewel=jewels;manager.SyncFromPurchasedLevels();player.RefreshUpgradeStats();
            player.ApplyContinuousRoutePose(position,rotation*Vector3.forward,0);player.transform.rotation=rotation;player.currentHealth=health;player.movement=movement;
            for(int i=0;i<routeFields.Length;i++)routeFields[i].SetValue(player,routeValues[i]);
            TimeManager.timeFactor=factor;TimeManager.isGameRunning=running;content.parent.parent.gameObject.SetActive(shopActive);
            RestorePrefs();SettingsManager.Instance.LoadSettings();
        }
    }
}
