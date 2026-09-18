using System;
using System.IO;
using IndianOceanAssets.ShooterSurvival.Ads;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MobilePlaytestBuild
{
    [Serializable]
    private sealed class Result
    {
        public string result, output;
        public int errors, warnings;
        public double seconds;
        public long apkBytes;
        public bool development, testAds;
    }

    public static object Schedule(string folder = "tmp/mobile-device-playtest-2026-09-13", string output = "Builds/Android/TralaleroShooter-Optimized-20260913.apk")
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Idle Edit Mode required.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Android target required.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Preserve the unsaved scene before building.");
        var ads = Resources.Load<RewardedAdsSettings>(RewardedAdsSettings.ResourcePath);
        if (ads == null || !ads.useTestAds) throw new InvalidOperationException("Test ads required.");
        if (Directory.Exists(folder) || File.Exists(output))
            throw new InvalidOperationException("Preserve prior build artifacts.");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.Copy("ProjectSettings/ProjectSettings.asset", folder + "/ProjectSettings.before.asset");
        bool customSigning = PlayerSettings.Android.useCustomKeystore;
        var architecture = PlayerSettings.Android.targetArchitectures;
        bool appBundle = EditorUserBuildSettings.buildAppBundle;
        bool export = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        bool development = EditorUserBuildSettings.development;
        EditorApplication.CallbackFunction buildOnce = null;
        buildOnce = () =>
        {
            EditorApplication.update -= buildOnce;
            try
            {
                File.WriteAllText(folder + "/running.txt", DateTime.UtcNow.ToString("O"));
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.useCustomKeystore = false;
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                EditorUserBuildSettings.development = false;
                var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[]
                    {
                        "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity",
                        "Assets/ShooterSurvival/Scenes/Tools/HighWay.unity",
                        "Assets/ShooterSurvival/Scenes/Tools/RestStop.unity"
                    },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.DetailedBuildReport
                });
                var summary = build.summary;
                File.WriteAllText(folder + "/result.json", JsonUtility.ToJson(new Result
                {
                    result = summary.result.ToString(), output = output,
                    errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings,
                    seconds = summary.totalTime.TotalSeconds,
                    apkBytes = File.Exists(output) ? new FileInfo(output).Length : 0,
                    development = false, testAds = true
                }, true));
            }
            catch (Exception error) { File.WriteAllText(folder + "/error.txt", error.ToString()); }
            finally
            {
                PlayerSettings.Android.targetArchitectures = architecture;
                PlayerSettings.Android.useCustomKeystore = customSigning;
                EditorUserBuildSettings.buildAppBundle = appBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = export;
                EditorUserBuildSettings.development = development;
                // PlayerSettings setters restore the live state, but this Unity
                // version left the temporary signing flag on disk after a build.
                // Restore the before-image only when that flag is the sole change.
                const string settingsPath = "ProjectSettings/ProjectSettings.asset";
                string baseline = File.ReadAllText(folder + "/ProjectSettings.before.asset");
                string current = File.ReadAllText(settingsPath);
                if (current != baseline)
                {
                    string temporary = baseline.Replace("androidUseCustomKeystore: " + (customSigning ? "1" : "0"), "androidUseCustomKeystore: 0");
                    if (current != temporary)
                    {
                        File.WriteAllText(folder + "/settings-restore-error.txt", "Unexpected settings changes: preserve them for review.");
                        throw new InvalidOperationException("Refuse to overwrite additional PlayerSettings changes.");
                    }
                    File.Copy(folder + "/ProjectSettings.before.asset", settingsPath, true);
                }
                File.WriteAllText(folder + "/settings-restored.txt", "Live architecture, signing, APK/export mode and development settings restored; on-disk PlayerSettings match the pre-build bytes.");
            }
        };
        EditorApplication.update += buildOnce;
        return new { output, folder, scheduled = true };
    }
}
