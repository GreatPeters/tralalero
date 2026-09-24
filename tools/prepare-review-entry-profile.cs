using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PrepareReviewEntryProfile
{
    public static string Main(string profile,float speed=3)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        const string folder="tmp/review-fixes-20-runs-2026-09-22";
        if(!File.Exists(folder+"/before-prefs.tsv"))throw new Exception("Preference backup required");
        int[] levels=profile switch
        {
            "current"=>new[]{15,8,1},"harbor-growth"=>new[]{20,12,10},
            "highway-entry"=>new[]{27,32,20},"highway-ready"=>new[]{32,36,30},
            "reststop-entry"=>new[]{37,46,25},"reststop-ready"=>new[]{42,50,30},
            _=>throw new ArgumentException("Unknown profile")
        };
        var info=new List<string>();long total=0;
        for(int id=1;id<=3;id++)
        {
            if(!UpgradeTables.TryGet(id,levels[id-1],out var row))throw new Exception("Unsupported purchased level");
            long cost=0;for(int level=1;level<=levels[id-1];level++){if(!UpgradeTables.TryGet(id,level,out var step))throw new Exception("Missing price row");cost+=step.price;}
            total+=cost;info.Add($"id={id}, level={levels[id-1]}, amount={row.amount}, type={row.valueType}, cumulativeCoinCost={cost}");
            PlayerPrefs.SetInt("upgrade_lv_"+id,levels[id-1]);
        }
        PlayerPrefs.Save();SessionState.SetFloat("NoryangjinMapTool.TestTimeScale",speed);SessionState.SetBool("NoryangjinMapTool.TestPower9999",false);SessionState.SetBool("NoryangjinMapTool.TestFastLateral",false);
        File.WriteAllLines(folder+"/profile-"+profile+".txt",new[]{"Temporary reproducible purchased-level save; not a claim about natural campaign progression.","profile="+profile,"totalCoinCost="+total,"testSpeed="+speed}.Concat(info));
        return profile+" "+string.Join(" / ",info)+"; totalCoinCost="+total;
    }
}
