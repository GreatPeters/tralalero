using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

public static class VerifyBonusAmountPlay
{
    static string folder;
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required.");
        folder="tmp/image-previews/bonus-amount-fix-2026-09-22/"+DateTime.UtcNow.ToString("HHmmss");Directory.CreateDirectory(folder);
        var canvas=Object.FindFirstObjectByType<CanvasScript>();canvas.StartCoroutine(Guard(Run(canvas)));return folder;
    }
    static IEnumerator Guard(IEnumerator run)
    {
        while(true){object step;try{if(!run.MoveNext())break;step=run.Current;}catch(Exception error){File.WriteAllText(folder+"/error.txt",error.ToString());yield break;}yield return step;}
    }
    static IEnumerator Run(CanvasScript canvas)
    {
        OpeningStoryUI.Instance?.Skip();var tutorial=Object.FindFirstObjectByType<CoastalTutorialUI>();if(tutorial!=null){tutorial.enabled=false;tutorial.Close();}
        canvas.PlayerPressedStartButton();yield return null;
        var player=Object.FindFirstObjectByType<PlayerScript>();var playerBody=player.GetComponent<Collider>();
        var anchor=new GameObject("Bonus verification stationary anchor").AddComponent<RestStopHoldout>();anchor.enabled=false;anchor.police=Array.Empty<EnemyEventController>();
        player.SetStationaryCombat(anchor,true);player.canShoot=false;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Walls/New/wall_atk_normal.prefab");
        var cases=new[]{("Normal","att",12f),("Normal","hp",14f),("Unique","att",36f),("Unique","hp",54f)};
        var report=new List<object>();
        foreach(var item in cases)
        {
            yield return new WaitForSeconds(2.1f);
            var root=Object.Instantiate(source,player.transform.position+player.transform.forward*7,Quaternion.identity);root.AddComponent<RuntimeBonusWall>();
            var wall=root.GetComponentInChildren<WallScript>(true);yield return null;
            BonusTables.TryGet(item.Item1,item.Item2,out var row);
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            void Set(string key,object value)=>typeof(WallScript).GetField(key,flags).SetValue(wall,value);
            wall.isRandom=false;wall.rarity=item.Item1=="Unique"?Rarity.Unique:Rarity.Normal;
            BonusAltarRules.TryResolveBuffType(wall.rarity,item.Item2,out var buff);wall.buffType=buff;
            Set("hasSelectedBonusRow",true);Set("selectedBonusRow",row);Set("selectedDisplayRow",row);
            var random=UnityEngine.Random.state;int seed=0;
            for(;seed<10000;seed++){UnityEngine.Random.InitState(seed);if(BonusAltarRules.ResolveValue(row,UnityEngine.Random.value,player.originalDamage)==item.Item3)break;}
            if(seed==10000)throw new Exception("No seed in real workbook range");
            UnityEngine.Random.InitState(seed);wall.SetStats();UnityEngine.Random.state=random;wall.SetWallSprite();
            if(wall.CurrentBonusValue!=item.Item3||wall.CurrentBonusDisplayValue!=item.Item3)throw new Exception("Label/apply mismatch before contact");
            float before=item.Item2=="att"?player.ResolvedAttackDamage:player.currentHealth,maxBefore=player.MaxHealth;
            string label=wall.CurrentBonusDisplayText;string name=item.Item1+"-"+item.Item2+"-"+item.Item3;
            yield return new WaitForEndOfFrame();Capture(name+"-before");
            var collider=wall.GetComponent<Collider>();var body=root.AddComponent<Rigidbody>();body.isKinematic=true;
            body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            float started=Time.time;
            while(root.activeInHierarchy&&Time.time-started<4)
            {
                var delta=playerBody.bounds.center-collider.bounds.center;
                body.MovePosition(body.position+Vector3.ClampMagnitude(delta,8*Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }
            yield return null;float after=item.Item2=="att"?player.ResolvedAttackDamage:player.currentHealth;
            bool valid=!root.activeInHierarchy&&Mathf.Abs((after-before)-item.Item3)<.001f;
            if(item.Item2=="hp")valid&=Mathf.Abs(player.MaxHealth-maxBefore-item.Item3)<.001f;
            report.Add(new{item.Item1,item.Item2,label,before,after,maxBefore,maxAfter=player.MaxHealth,valid,contact="real trigger, moved native prefab; seeded roll in unmodified workbook range"});
            File.WriteAllText(folder+"/report.json",Json(report));if(!valid)throw new Exception("Physical pickup mismatch: "+name);
            yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();Capture(name+"-after");Object.Destroy(root);
        }
        player.SetStationaryCombat(anchor,false);Object.Destroy(anchor.gameObject);TimeManager.isGameRunning=false;
        File.WriteAllText(folder+"/complete.txt","Four actual collider pickups match displayed ATT/HP values; maximum HP also increases correctly.");
    }
    static void Capture(string name){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+"/"+name+".png",t.EncodeToPNG());Object.Destroy(t);}
    static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
}
