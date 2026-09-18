using IndianOceanAssets.ShooterSurvival.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Reject an incompatible device floor before expensive shader/IL2CPP compilation.
public sealed class RewardedAdsBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => -10001;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android || Resources.Load<RewardedAdsSettings>(RewardedAdsSettings.ResourcePath) == null) return;
        if ((int)PlayerSettings.Android.minSdkVersion < 24)
            throw new BuildFailedException("Google Mobile Ads11.5.0 requires Android API24or newer. Set Android Minimum API Level to24(Android7.0) before building.");
    }
}
