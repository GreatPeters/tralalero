using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ChapterPlaytestPreferences
{
    public const string SnapshotPath="tmp/backups/two-chapter-2026-09-11/playerprefs.tsv";
    public static object Snapshot() => SnapshotAt(SnapshotPath);
    public static object SnapshotAt(string snapshotPath)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        if(File.Exists(snapshotPath))throw new InvalidOperationException("Preserve the existing user-state snapshot.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(snapshotPath)));
        var lines=new List<string>();
        void Add(string key,string kind)
        {string value=kind=="string"?Convert.ToBase64String(Encoding.UTF8.GetBytes(PlayerPrefs.GetString(key))):kind=="int"?PlayerPrefs.GetInt(key).ToString(CultureInfo.InvariantCulture):PlayerPrefs.GetFloat(key).ToString("R",CultureInfo.InvariantCulture);lines.Add(string.Join("\t",key,kind,PlayerPrefs.HasKey(key),value));}
        Add("coin","int");Add("jewel","int");Add("chapter_unlocked","int");
        Add("ads_last_reward_round","string");
        for(int chapter=1;chapter<=5;chapter++)Add("chapter_rewarded_"+chapter,"int");
        for(int i=1;i<=Enum.GetValues(typeof(UpgradeStatManager.UpgradeType)).Length;i++)Add("upgrade_lv_"+i,"int");
        foreach(var type in Enum.GetNames(typeof(UpgradeStatManager.UpgradeType))){Add("upgrade_stat_"+type,"float");Add("upgrade_stat_type_"+type,"int");}
        foreach(var row in CosmeticTables.Rows)Add(CosmeticService.OwnedKey(row.id),"int");
        foreach(CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))Add(CosmeticService.EquippedKey(slot),"string");
        File.WriteAllLines(snapshotPath,lines);
        string stateKey = snapshotPath == SnapshotPath ? "TwoChapter" : "ChapterTest." + snapshotPath;
        SessionState.SetBool(stateKey+".OriginalPower",SessionState.GetBool("NoryangjinMapTool.TestPower9999",false));
        SessionState.SetBool(stateKey+".OriginalLateral",SessionState.GetBool("NoryangjinMapTool.TestFastLateral",false));
        SessionState.SetFloat(stateKey+".OriginalSpeed",SessionState.GetFloat("NoryangjinMapTool.TestTimeScale",1));
        return new{saved=lines.Count,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel")};
    }
    public static object Restore() => RestoreAt(SnapshotPath);
    public static object RestoreAt(string snapshotPath)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        var lines=File.ReadAllLines(snapshotPath);if(lines.Length<30)throw new InvalidOperationException("Incomplete state snapshot.");
        foreach(var line in lines)
        {
            var p=line.Split('\t');
            if(!bool.Parse(p[2]))PlayerPrefs.DeleteKey(p[0]);
            else if(p[1]=="int")PlayerPrefs.SetInt(p[0],int.Parse(p[3],CultureInfo.InvariantCulture));
            else if(p[1]=="float")PlayerPrefs.SetFloat(p[0],float.Parse(p[3],CultureInfo.InvariantCulture));
            else PlayerPrefs.SetString(p[0],Encoding.UTF8.GetString(Convert.FromBase64String(p[3])));
        }
        PlayerPrefs.Save();CosmeticService.ReloadCatalog();
        string stateKey = snapshotPath == SnapshotPath ? "TwoChapter" : "ChapterTest." + snapshotPath;
        SessionState.SetBool("NoryangjinMapTool.TestPower9999",SessionState.GetBool(stateKey+".OriginalPower",false));
        SessionState.SetBool("NoryangjinMapTool.TestFastLateral",SessionState.GetBool(stateKey+".OriginalLateral",false));
        SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",SessionState.GetFloat(stateKey+".OriginalSpeed",1));
        SessionState.SetBool("SR18.Progression.Active",false);Time.timeScale=1;
        return new{restored=lines.Length,coin=PlayerPrefs.GetInt("coin"),jewel=PlayerPrefs.GetInt("jewel")};
    }
}
