var counts=new System.Collections.Generic.Dictionary<int,int>();
double end=UnityEditor.EditorApplication.timeSinceStartup+5;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 if(UnityEditor.EditorApplication.timeSinceStartup>=end){
  var report=new{frames=counts.Count,calls=counts.Values.Sum(),maximumCallsPerFrame=counts.Count>0?counts.Values.Max():0};
  var serialize=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
  System.IO.File.WriteAllText("map-concepts/chapters-polish-2026-09-12/steering-cadence.json",(string)serialize.Invoke(null,new object[]{report}));UnityEditor.EditorApplication.update-=tick;return;
 }
 if(UnityEditor.EditorApplication.isPlaying&&!UnityEditor.EditorApplication.isPaused&&IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning){int frame=UnityEngine.Time.frameCount;counts[frame]=counts.TryGetValue(frame,out int value)?value+1:1;}
};
UnityEditor.EditorApplication.update+=tick;return "Measuring Editor tick / gameplay-frame ratio for5seconds";
