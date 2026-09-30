using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildMobileIcon20260930
{
    [Serializable] private class Result
    {
        public string result, output;
        public string[] scenes;
        public int errors, warnings;
        public double seconds;
        public long bytes;
    }
    public static object Main()
    {
        const string folder = "tmp/mobile-icon-build-20260930/build-retry1";
        const string output = "Builds/Android/TralaleroShooter-20260930-AppIcon.apk";
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer || EditorUtility.scriptCompilationFailed)
            throw new InvalidOperationException("Idle editor without compile errors required.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Android target required.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Preserve unsaved scene before build.");
        if (Directory.Exists(folder) || File.Exists(output)) throw new InvalidOperationException("Preserve prior build.");
        string[] scenes = {
            "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18_Revamp.unity",
            "Assets/ShooterSurvival/Scenes/Tools/HighWay.unity",
            "Assets/ShooterSurvival/Scenes/Tools/RestStop.unity",
            "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity"
        };
        if(scenes.Any(p=>!File.Exists(p))) throw new InvalidOperationException("Missing scene.");
        var ads=Resources.Load<IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsSettings>(IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsSettings.ResourcePath);
        if(ads==null || !ads.useTestAds) throw new InvalidOperationException("Existing test ads required.");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        bool signing=PlayerSettings.Android.useCustomKeystore;
        bool bundle=EditorUserBuildSettings.buildAppBundle, export=EditorUserBuildSettings.exportAsGoogleAndroidProject, dev=EditorUserBuildSettings.development;
        var arch=PlayerSettings.Android.targetArchitectures;
        File.Copy("ProjectSettings/ProjectSettings.asset",folder+"/ProjectSettings.before-build.asset",false);
        EditorApplication.CallbackFunction run=null;
        run=()=>{
            EditorApplication.update-=run;
            try {
                File.WriteAllText(folder+"/running.txt",DateTime.UtcNow.ToString("O"));
                PlayerSettings.Android.useCustomKeystore=false;
                PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
                EditorUserBuildSettings.buildAppBundle=false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
                EditorUserBuildSettings.development=false;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=scenes,locationPathName=output,target=BuildTarget.Android,options=BuildOptions.DetailedBuildReport | BuildOptions.CleanBuildCache});
                var s=report.summary;
                File.WriteAllText(folder+"/result.json",JsonUtility.ToJson(new Result {result=s.result.ToString(),output=output,scenes=scenes,errors=(int)s.totalErrors,warnings=(int)s.totalWarnings,seconds=s.totalTime.TotalSeconds,bytes=File.Exists(output)?new FileInfo(output).Length:0},true));
                var failures=report.steps.SelectMany(x=>x.messages).Where(x=>x.type==LogType.Error || x.type==LogType.Exception).Select(x=>x.content);
                File.WriteAllLines(folder+"/build-errors.txt",failures);
            } catch(Exception ex) {File.WriteAllText(folder+"/error.txt",ex.ToString());}
            finally {
                PlayerSettings.Android.useCustomKeystore=signing;
                PlayerSettings.Android.targetArchitectures=arch;
                EditorUserBuildSettings.buildAppBundle=bundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject=export;
                EditorUserBuildSettings.development=dev;
                AssetDatabase.SaveAssets();
                File.WriteAllText(folder+"/restored.json",JsonUtility.ToJson(new Restore {customSigning=PlayerSettings.Android.useCustomKeystore,architecture=PlayerSettings.Android.targetArchitectures.ToString(),bundle=EditorUserBuildSettings.buildAppBundle,export=EditorUserBuildSettings.exportAsGoogleAndroidProject,development=EditorUserBuildSettings.development},true));
            }
        };
        EditorApplication.update+=run;
        return new {scheduled=true,output,folder,scenes};
    }
    [Serializable] private class Restore {public bool customSigning,bundle,export,development;public string architecture;}
}
