using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class S22OcclusionParts
{
 public static object Main(){var c=UnityEngine.Object.FindFirstObjectByType<NoryangjinCameraOcclusion>();var p=UnityEngine.Object.FindFirstObjectByType<PlayerScript>();var m=c.GetType().GetMethod("CacheTraversedRoads",BindingFlags.NonPublic|BindingFlags.Instance);var result=new List<object>();foreach(var mode in new[]{"roads","all"}){var v=new List<double>();for(int i=0;i<23;i++){var s=System.Diagnostics.Stopwatch.StartNew();if(mode=="roads")m.Invoke(c,new object[]{p.transform.forward});else c.RefreshVisibility(.016f);s.Stop();if(i>2)v.Add(s.Elapsed.TotalMilliseconds);}result.Add(new{mode,mean=v.Average(),median=v.OrderBy(x=>x).ElementAt(10)});}var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)result});File.WriteAllText("outputs/s22-polish-2026-10-01/occlusion-parts-before.json",json);return result;}
}
