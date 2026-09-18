if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
var type = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings")).FirstOrDefault(t => t != null);
if (type == null) throw new System.InvalidOperationException("Wait for the pinned Google Mobile Ads package to import");
var settings = (UnityEngine.Object)type.GetMethod("LoadInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
var serialized = new UnityEditor.SerializedObject(settings);
var android = serialized.FindProperty("adMobAndroidAppId");
var ios = serialized.FindProperty("adMobIOSAppId");
if (string.IsNullOrEmpty(android.stringValue)) android.stringValue = "ca-app-pub-3940256099942544~3347511713";
if (string.IsNullOrEmpty(ios.stringValue)) ios.stringValue = "ca-app-pub-3940256099942544~1458002511";
serialized.FindProperty("overrideDefaultGmaAndroidSdk").boolValue = true;
serialized.FindProperty("selectedGmaAndroidSdk").intValue = 0;
serialized.ApplyModifiedPropertiesWithoutUndo();
const string path = "Assets/ShooterSurvival/Resources/Ads/RewardedAdsSettings.asset";
System.IO.Directory.CreateDirectory("Assets/ShooterSurvival/Resources/Ads");
var gameSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsSettings>(path);
if (gameSettings == null)
{
    gameSettings = UnityEngine.ScriptableObject.CreateInstance<IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsSettings>();
    UnityEditor.AssetDatabase.CreateAsset(gameSettings, path);
}
UnityEditor.EditorUtility.SetDirty(settings); UnityEditor.EditorUtility.SetDirty(gameSettings); UnityEditor.AssetDatabase.SaveAssets();
return new { configured = true, testAds = gameSettings.useTestAds, productionIdsRequired = string.IsNullOrEmpty(gameSettings.androidRewardedUnitId) };
