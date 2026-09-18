if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("Isolated QA in Edit Mode required");
if(UnityEditor.EditorUserBuildSettings.activeBuildTarget!=UnityEditor.BuildTarget.Android)throw new System.InvalidOperationException("Switch QA to Android and wait for imports before building");
if(!UnityEngine.Resources.Load<IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsSettings>(IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsSettings.ResourcePath).useTestAds)throw new System.InvalidOperationException("Only test ads are authorized for this validation build");
const string folder="map-concepts/chapters-polish-2026-09-12/android-build/attempt-3";
const string output="Builds/Android/ChapterPolish-Test-20260913.apk";
if(System.IO.File.Exists(output))throw new System.InvalidOperationException("Preserve prior build");
System.IO.Directory.CreateDirectory(folder);
var architecture=UnityEditor.PlayerSettings.Android.targetArchitectures;
bool customSigning=UnityEditor.PlayerSettings.Android.useCustomKeystore;
UnityEditor.EditorApplication.delayCall+=()=>{try{
 System.IO.File.WriteAllText(folder+"/running.txt",System.DateTime.UtcNow.ToString("O"));
 UnityEditor.PlayerSettings.Android.targetArchitectures=UnityEditor.AndroidArchitecture.ARM64;
 UnityEditor.PlayerSettings.Android.useCustomKeystore=false;
 var options=new UnityEditor.BuildPlayerOptions{scenes=new[]{"Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity","Assets/ShooterSurvival/Scenes/Tools/HighWay.unity","Assets/ShooterSurvival/Scenes/Tools/RestStop.unity"},locationPathName=output,target=UnityEditor.BuildTarget.Android,options=UnityEditor.BuildOptions.Development|UnityEditor.BuildOptions.DetailedBuildReport};
 var result=UnityEditor.BuildPipeline.BuildPlayer(options);var summary=result.summary;
 var report=new{result=summary.result.ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings,seconds=summary.totalTime.TotalSeconds,bytes=summary.totalSize,output,testAds=true,architecture="ARM64",deviceTested=false};
 var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 System.IO.File.WriteAllText(folder+"/result.json",(string)serialize.Invoke(null,new object[]{report}));
}catch(System.Exception error){System.IO.File.WriteAllText(folder+"/error.txt",error.ToString());}
finally{
 UnityEditor.PlayerSettings.Android.targetArchitectures=architecture;
 UnityEditor.PlayerSettings.Android.useCustomKeystore=customSigning;
 UnityEditor.AssetDatabase.SaveAssets();
 System.IO.File.WriteAllText(folder+"/settings-restored.txt","QA architecture and signing restored; original project was not used for the build.");
}};
return "Android validation build scheduled once; monitor android-build/attempt-3/result.json or error.txt";
