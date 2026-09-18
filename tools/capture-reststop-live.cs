if(!UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isPaused)throw new System.InvalidOperationException("Unpaused Play mode required");
const string folder="tmp/image-previews/skins-reststop-2026-09-12/reststop-live";System.IO.Directory.CreateDirectory(folder);var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
float[] positions={-125,-75,15,45};int index=0;var records=new System.Collections.Generic.List<object>();UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 if(!UnityEditor.EditorApplication.isPlaying||player==null){UnityEditor.EditorApplication.update-=tick;return;}
 if(index>=positions.Length){var serializer=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});System.IO.File.WriteAllText(folder+"/positions.json",(string)serializer.Invoke(null,new object[]{records}));UnityEditor.EditorApplication.update-=tick;return;}
 var point=player.transform.position;if(point.z<210||point.z>228||point.x<positions[index])return;
 string path=folder+"/view-"+index+".png";UnityEngine.ScreenCapture.CaptureScreenshot(path);records.Add(new{path,position=point.ToString(),health=player.currentHealth});index++;
};UnityEditor.EditorApplication.update+=tick;return "Watching rest-stop approaches";
