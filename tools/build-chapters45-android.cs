using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class Chapter45AndroidBuild
{
 [Serializable] sealed class Result{public string result,output;public string[] scenes;public int errors,warnings;public double seconds;public long bytes;public bool development;}
 public static object Main(bool development=false,string revision=""){
  if(revision.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'))throw new Exception("Revision must contain letters, digits or hyphens only");
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer||EditorUtility.scriptCompilationFailed)throw new Exception("Clean idle editor required");
  if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new Exception("Android target required");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  string label=(development?"development":"release")+(string.IsNullOrEmpty(revision)?"":"-"+revision),folder="outputs/chapters45-2026-10-02/build-"+label,output="Builds/Android/TralaleroShooter-20261002-Chapters45-"+label+".apk";
  if(Directory.Exists(folder)||File.Exists(output))throw new Exception("Preserve previous build");
  string[] scenes={"Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18_Revamp.unity","Assets/ShooterSurvival/Scenes/Tools/HighWay.unity","Assets/ShooterSurvival/Scenes/Tools/RestStop.unity","Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity","Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity"};
  Directory.CreateDirectory(folder);Directory.CreateDirectory(Path.GetDirectoryName(output));
  bool signing=PlayerSettings.Android.useCustomKeystore,bundle=EditorUserBuildSettings.buildAppBundle,export=EditorUserBuildSettings.exportAsGoogleAndroidProject,dev=EditorUserBuildSettings.development;var arch=PlayerSettings.Android.targetArchitectures;
  File.Copy("ProjectSettings/ProjectSettings.asset",folder+"/ProjectSettings.before.asset");
  EditorApplication.CallbackFunction run=null;run=()=>{EditorApplication.update-=run;try{
   File.WriteAllText(folder+"/running.txt",DateTime.UtcNow.ToString("O"));PlayerSettings.Android.useCustomKeystore=false;PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.exportAsGoogleAndroidProject=false;EditorUserBuildSettings.development=development;
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=output,target=BuildTarget.Android,options=BuildOptions.DetailedBuildReport|(development?BuildOptions.Development:BuildOptions.None)});var s=report.summary;
   File.WriteAllText(folder+"/result.json",JsonUtility.ToJson(new Result{result=s.result.ToString(),output=output,scenes=scenes,errors=(int)s.totalErrors,warnings=(int)s.totalWarnings,seconds=s.totalTime.TotalSeconds,bytes=File.Exists(output)?new FileInfo(output).Length:0,development=development},true));
   File.WriteAllLines(folder+"/errors.txt",report.steps.SelectMany(x=>x.messages).Where(x=>x.type==LogType.Error||x.type==LogType.Exception).Select(x=>x.content));
  }catch(Exception ex){File.WriteAllText(folder+"/error.txt",ex.ToString());}finally{PlayerSettings.Android.useCustomKeystore=signing;PlayerSettings.Android.targetArchitectures=arch;EditorUserBuildSettings.buildAppBundle=bundle;EditorUserBuildSettings.exportAsGoogleAndroidProject=export;EditorUserBuildSettings.development=dev;AssetDatabase.SaveAssets();File.WriteAllText(folder+"/restored.txt",$"signing={signing}; architecture={arch}; bundle={bundle}; export={export}; development={dev}");}};EditorApplication.update+=run;return new{scheduled=true,output,folder,scenes};
 }
}
