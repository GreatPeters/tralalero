using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

public static class ProbeHighwayPlayLifecycle
{
    const string Root="outputs/highway-three-lane-rebuild-2026-09-26/play-v5";
    static object Record(string name,object value)
    {
        var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
        File.WriteAllText(Root+"/"+name+".json",json);return value;
    }
    public static object Arm()
    {
        if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="HighWay")throw new Exception("HighWay Play Mode required");
        OncomingLaneTraffic.Suppress(5000);
        return Record("restart-armed",new{time=Time.time,suppressed=OncomingLaneTraffic.Suppressed,domainReloadDisabled=EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload)});
    }
    public static object Check()
    {
        if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="HighWay")throw new Exception("Fresh HighWay Play Mode required");
        var traffic=Object.FindObjectsByType<OncomingLaneTraffic>(FindObjectsSortMode.None);
        int harnesses=Object.FindObjectsByType<CombatHarness>(FindObjectsSortMode.None).Length;
        if(OncomingLaneTraffic.Suppressed||OncomingLaneTraffic.Blockers.Count!=0||traffic.Length!=1||harnesses!=1)throw new Exception("Fresh-session state failed");
        return Record("restart-verified",new{time=Time.time,suppressed=OncomingLaneTraffic.Suppressed,blockers=OncomingLaneTraffic.Blockers.Count,traffic=traffic.Length,harnesses});
    }
    public static object CheckEdit()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        int harnesses=Object.FindObjectsByType<CombatHarness>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        if(harnesses!=0)throw new Exception("Runtime harness remains after exit");
        return Record("exit-verified",new{harnesses,scene=SceneManager.GetActiveScene().name,dirty=SceneManager.GetActiveScene().isDirty,timeScale=Time.timeScale});
    }
}
