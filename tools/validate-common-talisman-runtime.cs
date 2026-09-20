using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;

public static class ValidateCommonTalismanRuntime
{
    static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static string report;
    public static object Main()
    {
        report="map-concepts/talisman-polish-2026-09-20/runtime-"+SceneManager.GetActiveScene().name+".txt";
        Directory.CreateDirectory(Path.GetDirectoryName(report));File.WriteAllText(report,"");
        var canvas=UnityEngine.Object.FindFirstObjectByType<CanvasScript>();
        canvas.StartCoroutine(Guard(Run(canvas)));
        return report;
    }
    static IEnumerator Guard(IEnumerator run)
    {
        while(true){object step;try{if(!run.MoveNext())break;step=run.Current;}catch(Exception error){File.AppendAllText(report,"FAIL "+error+"\n");yield break;}yield return step;}
    }
    static void Check(bool pass,string message){File.AppendAllText(report,(pass?"PASS ":"FAIL ")+message+"\n");if(!pass)throw new Exception(message);}
    static int Effects()=>UnityEngine.Object.FindObjectsByType<BonusTalismanPickup>(FindObjectsSortMode.None).Length;
    static IEnumerator Run(CanvasScript canvas)
    {
        var story=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(FindObjectsInactive.Include);if(story!=null)story.Skip();
        canvas.PlayerPressedStartButton();
        Time.timeScale=1;TimeManager.isGameRunning=true;TimeManager.timeFactor=0;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var collider=player.GetComponent<Collider>();
        Check(collider!=null,"Player collider exists");
        var weapon=player.GetComponent<WeaponManager>().currentWeapon.GetComponentInChildren<WeaponScript>();
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Walls/New/wall_atk_normal.prefab");
        foreach(var type in new[]{BuffType.att_normmal,BuffType.attPer_normal,BuffType.hp_normal,BuffType.hpPer_normal,BuffType.attackSpeed_normal,BuffType.missileDistance_normal,BuffType.missileAdd_unique,BuffType.tungtung_rare,BuffType.boombar_rare})
        {
            var root=UnityEngine.Object.Instantiate(source,player.transform.position+player.transform.forward*5,Quaternion.identity);
            root.AddComponent<RuntimeBonusWall>();
            var wall=root.GetComponentInChildren<WallScript>(true);
            yield return null;
            wall.isRandom=false;wall.buffType=type;
            wall.rarity=type==BuffType.tungtung_rare||type==BuffType.boombar_rare?Rarity.Rare:type==BuffType.missileAdd_unique?Rarity.Unique:Rarity.Normal;
            wall.SetStats();wall.SetWallSprite();
            player.currentHealth=Mathf.Max(100,player.currentHealth);
            player.lastWallTouchTime=Time.time-3;
            float health=player.currentHealth,damage=weapon.damage,rate=weapon.fireRate;int bullets=weapon.bulletCount;
            int helpers=UnityEngine.Object.FindObjectsByType<ExtraHelpBuffScript>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            float value=wall.CurrentBonusValue;
            Check(value>0,type+" has a valid configured bonus value");
            typeof(WallScript).GetMethod("OnTriggerEnter",Private).Invoke(wall,new object[]{collider});
            var effect=UnityEngine.Object.FindFirstObjectByType<BonusTalismanPickup>();
            Check(effect!=null && effect.transform.parent==null,type+" spawned detached effect");
            Check(!wall.gameObject.activeInHierarchy,type+" original disabled after claim");
            Check(!wall.GetComponent<BonusTalismanPresentation>().PlayPickup(player),type+" duplicate feedback rejected");
            if(type==BuffType.att_normmal)Check(Mathf.Abs(weapon.damage-(damage+value))<.01f,"Flat attack applied exactly once");
            if(type==BuffType.attPer_normal)Check(Mathf.Abs(weapon.damage-damage*(1+value*.01f))<.01f,"Percent attack applied exactly once");
            if(type==BuffType.hp_normal||type==BuffType.hpPer_normal)Check(player.currentHealth>health,"Health bonus preserved");
            if(type==BuffType.attackSpeed_normal)Check(weapon.fireRate>rate,"Fire rate bonus preserved");
            if(type==BuffType.missileAdd_unique)Check(weapon.bulletCount==bullets+(int)value,"Extra projectile bonus preserved");
            if(type==BuffType.tungtung_rare||type==BuffType.boombar_rare){yield return null;Check(UnityEngine.Object.FindObjectsByType<ExtraHelpBuffScript>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length>helpers,"Helper summon preserved: "+type);}
            yield return new WaitForSeconds(BonusTalismanPickup.Duration+.15f);
            Check(Effects()==0,type+" effect cleaned up after duration");
            UnityEngine.Object.Destroy(root);
        }
        var enemy=UnityEngine.Object.FindObjectsByType<EnemyScript_space>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
        var dropped=(GameObject)typeof(EnemyScript_space).GetMethod("SpawnBonusAltar",Private).Invoke(enemy,null);
        dropped.transform.position=player.transform.position+player.transform.forward*5;
        yield return null;
        var droppedWall=dropped.GetComponentInChildren<WallScript>(true);
        Check(dropped.GetComponent<RuntimeBonusWall>()!=null,"Actual enemy drop has runtime cleanup marker");
        Check(droppedWall.GetComponent<BonusTalismanPresentation>().Visual!=null,"Actual enemy drop uses talisman");
        player.lastWallTouchTime=Time.time-3;
        typeof(WallScript).GetMethod("OnTriggerEnter",Private).Invoke(droppedWall,new object[]{collider});
        Check(Effects()==1 && !dropped.activeSelf,"Actual enemy drop claim emits one effect and hides root");
        yield return new WaitForSeconds(BonusTalismanPickup.Duration+.15f);
        Check(Effects()==0,"Actual enemy drop effect cleans up");
        UnityEngine.Object.Destroy(dropped);
        // Reactivation and reset cleanup use real Play Mode callbacks.
        var again=UnityEngine.Object.Instantiate(source,player.transform.position+player.transform.forward*4,Quaternion.identity);
        yield return null;
        var againWall=again.GetComponentInChildren<WallScript>(true);againWall.SetWallSprite();
        var presentation=againWall.GetComponent<BonusTalismanPresentation>();
        Check(presentation.PlayPickup(player),"First presentation accepted");
        player.ResetState();yield return null;yield return null;
        Check(Effects()==0,"Same-frame player reset cancels old pickup effect");
        again.SetActive(false);again.SetActive(true);yield return null;
        Check(presentation.PlayPickup(player),"Re-enabled instance accepts next run pickup");
        TimeManager.isGameRunning=false;yield return null;yield return null;
        Check(Effects()==0,"Run exit cancels all transient effects");
        UnityEngine.Object.Destroy(again);
        File.AppendAllText(report,"COMPLETE\n");
    }
}
