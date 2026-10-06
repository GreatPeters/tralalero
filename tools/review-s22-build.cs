using System;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class S22BuildReview
{
 public static object Main(){var r=BuildReport.GetLatestReport();var warnings=r.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning).GroupBy(m=>m.content).Select(g=>new{count=g.Count(),message=g.Key}).ToArray();var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)warnings});File.WriteAllText("outputs/s22-polish-2026-10-01/build-release/warnings.json",json);return new{uniqueWarnings=warnings.Length,warnings};}
}
