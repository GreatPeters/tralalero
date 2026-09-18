if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
int before=(int)UnityEditor.PlayerSettings.Android.minSdkVersion;
if(before<24)UnityEditor.PlayerSettings.Android.minSdkVersion=UnityEditor.AndroidSdkVersions.AndroidApiLevel24;
UnityEditor.AssetDatabase.SaveAssets();
var report=new{before,after=(int)UnityEditor.PlayerSettings.Android.minSdkVersion,reason="Pinned Google Mobile Ads11.5.0 Android manifest requires API24",android6Supported=false};
var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/chapters-polish-2026-09-12/android-minimum.json",(string)serialize.Invoke(null,new object[]{report}));
return report;
