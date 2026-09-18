if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Clean Edit Mode required");
const string folder="tmp/analytics-flow-20260912";System.IO.Directory.CreateDirectory(folder);System.IO.File.Copy("ProjectSettings/ProjectSettings.asset",folder+"/ProjectSettings-before.asset",false);
var architectures=UnityEditor.PlayerSettings.Android.targetArchitectures;bool signing=UnityEditor.PlayerSettings.Android.useCustomKeystore;
System.IO.File.WriteAllText(folder+"/build-settings-before.json","{\"architectures\":"+(int)architectures+",\"customKeystore\":"+signing.ToString().ToLowerInvariant()+"}");
try{
 UnityEditor.PlayerSettings.Android.targetArchitectures=UnityEditor.AndroidArchitecture.X86_64;UnityEditor.PlayerSettings.Android.useCustomKeystore=false;
 var options=new UnityEditor.BuildPlayerOptions{scenes=new[]{"Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity"},locationPathName="Builds/Android/AnalyticsSmoke-20260912.apk",target=UnityEditor.BuildTarget.Android,options=UnityEditor.BuildOptions.Development};
 var report=UnityEditor.BuildPipeline.BuildPlayer(options);var summary=report.summary;
 var json="{\"result\":\""+summary.result+"\",\"errors\":"+summary.totalErrors+",\"warnings\":"+summary.totalWarnings+",\"seconds\":"+summary.totalTime.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"bytes\":"+summary.totalSize+"}";
 System.IO.File.WriteAllText(folder+"/build-report.json",json);return json;
}finally{UnityEditor.PlayerSettings.Android.targetArchitectures=architectures;UnityEditor.PlayerSettings.Android.useCustomKeystore=signing;UnityEditor.AssetDatabase.SaveAssets();System.IO.File.WriteAllText(folder+"/build-settings-restored.json","{\"architectures\":"+(int)UnityEditor.PlayerSettings.Android.targetArchitectures+",\"customKeystore\":"+UnityEditor.PlayerSettings.Android.useCustomKeystore.ToString().ToLowerInvariant()+"}");}
