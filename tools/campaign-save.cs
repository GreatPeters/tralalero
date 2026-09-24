using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public static class CampaignSave
{
    const string DefaultFolder="tmp/campaign-balance-2026-09-23";
    public static object Capture(string folder)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||File.Exists(folder+"/before-prefs.tsv"))throw new InvalidOperationException("Fresh Edit Mode snapshot required");
        var result=ChapterPlaytestPreferences.SnapshotAt(folder+"/before-prefs.tsv");
        var keys=new[]{"TutorialDone"}.Concat(Enumerable.Range(1,5).SelectMany(c=>new[]{ChapterUpgradeService.LevelKey(c),ChapterUpgradeService.OwnedKey(c)}));
        File.WriteAllLines(folder+"/extra-prefs.tsv",keys.Select(k=>$"{k}\t{PlayerPrefs.HasKey(k)}\t{PlayerPrefs.GetInt(k)}"));
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");var window=Resources.FindObjectsOfTypeAll(type).First();
        var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var size=(Vector2)type.GetProperty("targetSize",flags).GetValue(window);
        File.WriteAllText(folder+"/gameview-before.txt",$"{type.GetProperty("selectedSizeIndex",flags).GetValue(window)}\t{size.x}\t{size.y}");
        File.WriteAllText(folder+"/gameview-maximized-before.txt",((EditorWindow)window).maximized.ToString());
        File.WriteAllText(folder+"/render-cap.txt",Application.targetFrameRate.ToString());
        File.WriteAllText(folder+"/scene-before.txt",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        return result;
    }
    public static object Reset(string folder=DefaultFolder)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(folder+"/before-prefs.tsv") || !File.Exists(folder+"/extra-prefs.tsv")) throw new InvalidOperationException("Edit Mode and complete original preference snapshot required");
        for(int id=1;id<=10;id++)PlayerPrefs.DeleteKey("upgrade_lv_"+id);
        foreach(var type in Enum.GetNames(typeof(UpgradeStatManager.UpgradeType))){PlayerPrefs.DeleteKey("upgrade_stat_"+type);PlayerPrefs.DeleteKey("upgrade_stat_type_"+type);}
        for(int chapter=1;chapter<=5;chapter++){PlayerPrefs.DeleteKey(ChapterUpgradeService.LevelKey(chapter));PlayerPrefs.DeleteKey(ChapterUpgradeService.OwnedKey(chapter));PlayerPrefs.DeleteKey("chapter_rewarded_"+chapter);}
        foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))PlayerPrefs.SetString(CosmeticService.EquippedKey(slot),CosmeticTables.Rows.Single(r=>r.slot==slot&&r.isDefault).id);
        PlayerPrefs.SetInt("coin",0);PlayerPrefs.SetInt("jewel",0);PlayerPrefs.SetInt("chapter_unlocked",1);PlayerPrefs.SetInt("TutorialDone",1);PlayerPrefs.Save();
        SessionState.SetBool("SR18.Progression.Active",false);SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",3);
        return new{reset=true};
    }
    public static object Buy(string path)
    {
        if(!EditorApplication.isPlaying || TimeManager.isGameRunning)throw new InvalidOperationException("Play Mode lobby required");
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();
        var cards=UnityEngine.Object.FindObjectsByType<UpgradeUI>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var purchases=new System.Collections.Generic.List<object>();
        for(int n=0;n<12;n++)
        {
            float Value(int id){int level=PlayerPrefs.GetInt("upgrade_lv_"+id);if(!UpgradeTables.TryGet(id,level+1,out var next))return 0;float previous=UpgradeTables.TryGet(id,level,out var row)?row.amount:0;return (next.amount-previous)/(id==1?player.DefaultAttackDamage+previous:id==2?player.MaxHealth:100+previous)*(id==2?.9f:1f)/Mathf.Max(1,next.price);}
            var best=cards.Where(c=>c.UpgradeId>=1&&c.UpgradeId<=3&&UpgradeTables.TryGet(c.UpgradeId,PlayerPrefs.GetInt("upgrade_lv_"+c.UpgradeId)+1,out _)).OrderByDescending(c=>Value(c.UpgradeId)).FirstOrDefault();
            if(best==null)break;
            int id=best.UpgradeId,nextLevel=PlayerPrefs.GetInt("upgrade_lv_"+id)+1;var row=UpgradeTables.Get(id,nextLevel);int before=MoneyScript.S.Coin;
            if(!best.TryBuy())break;
            purchases.Add(new{id,level=nextLevel,price=row.price,amount=row.amount,before,after=MoneyScript.S.Coin});
        }
        var result=new{purchases,bank=MoneyScript.S.Coin,levels=Enumerable.Range(1,10).Select(i=>PlayerPrefs.GetInt("upgrade_lv_"+i)).ToArray(),hp=player.MaxHealth};
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result});File.WriteAllText(path,json);return result;
    }
    public static object Restore(string folder=DefaultFolder)
    {
        var result=ChapterPlaytestPreferences.RestoreAt(folder+"/before-prefs.tsv");
        Time.captureDeltaTime=0;
        if(File.Exists(folder+"/render-cap.txt"))Application.targetFrameRate=int.Parse(File.ReadAllText(folder+"/render-cap.txt"));
        foreach(var line in File.ReadAllLines(folder+"/extra-prefs.tsv")){var p=line.Split('\t');if(bool.Parse(p[1]))PlayerPrefs.SetInt(p[0],int.Parse(p[2]));else PlayerPrefs.DeleteKey(p[0]);}PlayerPrefs.Save();return result;
    }
}
