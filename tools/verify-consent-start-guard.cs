if(!UnityEditor.EditorApplication.isPlaying||!UnityEngine.Application.dataPath.Replace('\\','/').Contains("/tmp/q/"))throw new System.InvalidOperationException("QA Play Mode required");
OpeningStoryUI.Instance?.Skip();
var ads=IndianOceanAssets.ShooterSurvival.Ads.RewardedAdsService.Instance;
var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
if(ads==null||IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new System.InvalidOperationException("Idle scene and ad service required");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var form=ads.GetType().GetField("waitingForConsentForm",flags);
var initializing=ads.GetType().GetField("initializing",flags);
bool originalForm=(bool)form.GetValue(ads),originalInitializing=(bool)initializing.GetValue(ads);
bool activeFormBlocksStart=false,loadingAllowsStart=false;
try{
 form.SetValue(ads,true);canvas.PlayerPressedStartButton();
 activeFormBlocksStart=!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning;
 form.SetValue(ads,false);initializing.SetValue(ads,true);canvas.PlayerPressedStartButton();
 loadingAllowsStart=IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning;
}finally{form.SetValue(ads,originalForm);initializing.SetValue(ads,originalInitializing);}
var report=new{activeFormBlocksStart,loadingAllowsStart,simulation="Only the form/loading state is injected; actual public game-start handler is exercised. This is not device consent delivery."};
var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/chapters-polish-2026-09-12/consent-start-guard.json",(string)serialize.Invoke(null,new object[]{report}));
return report;
