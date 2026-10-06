using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class Chapters45BuildEvidence {
 public static object Main(string label="release"){
  if(label.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'))throw new Exception("Invalid build evidence label");
  var method=typeof(BuildReport).GetMethod("GetLatestReport",BindingFlags.Static|BindingFlags.Public);
  var report=method?.Invoke(null,null) as BuildReport;
  if(report==null)report=Resources.FindObjectsOfTypeAll<BuildReport>().OrderByDescending(r=>r.summary.buildStartedAt).FirstOrDefault();
  string folder="outputs/chapters45-2026-10-02/build-"+label;
  var warnings=report!=null?report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning).Select(m=>m.content).ToArray():Array.Empty<string>();
  File.WriteAllLines(folder+"/warnings.txt",warnings);
  var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
  return new { reportAvailable=report!=null, recordedWarnings=warnings.Length, uniqueWarnings=warnings.Distinct().Count(),
   playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,buildActive=BuildPipeline.isBuildingPlayer,compileFailed=EditorUtility.scriptCompilationFailed,
   scenes=scenes.Select(s=>new{s.path,s.isDirty}).ToArray(),enabledBuildScenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray() };
 }
}
