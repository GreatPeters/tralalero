using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class AuditBossPrefix
{
    [Serializable]public sealed class Input{public Frame[] frames;}
    [Serializable]public sealed class Frame{public int run;public float[] position;public float maximumProjectileRange;}
    public static object Main(string path)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var route=UnityEngine.Object.FindFirstObjectByType<HighwayRoute>();
        var boss=route.GetComponent<HighwayEncounterLanes>().rows.Single(r=>r.left.name=="HWY_E23_TollgateChief");
        var json=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        var input=(Input)json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText(path),typeof(Input)});float minimumGap=float.PositiveInfinity;
        foreach(var frame in input.frames)
        {
            var position=new Vector3(frame.position[0],frame.position[1],frame.position[2]);
            float gap=boss.routeDistance-route.NearestDistance(position);minimumGap=Mathf.Min(minimumGap,gap);
            //100m is also larger than the boss activation and QA aiming windows.
            if(gap<=frame.maximumProjectileRange+100)throw new InvalidOperationException("Run may interact with changed boss: "+frame.run);
        }
        string report=$"{{\"passed\":true,\"runs\":{input.frames.Select(f=>f.run).Distinct().Count()},\"frames\":{input.frames.Length},\"minimumBossGap\":{minimumGap.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"extraMargin\":100}}";
        File.WriteAllText(path+".verified.json",report);return report;
    }
}
