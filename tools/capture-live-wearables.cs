if(!UnityEditor.EditorApplication.isPlaying||!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new System.InvalidOperationException("Running gameplay required");
const string folder="tmp/image-previews/skins-reststop-2026-09-12/live-wearables";if(System.IO.Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier capture");System.IO.Directory.CreateDirectory(folder);
var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();var customizer=player.GetComponentInChildren<PlayerCosmeticCustomizer>(true);var catalog=customizer.visuals;
var keys=new[]{"shoes_gold","shoes_mint","shoes_ruby","shoes_spring","shoes_steel","shoes_relic","shoes_salvage"};var rows=new System.Collections.Generic.List<object>();int index=0;bool capture=false;double next=UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
 if(UnityEditor.EditorApplication.timeSinceStartup<next)return;
 try
 {
  if(!UnityEditor.EditorApplication.isPlaying||customizer==null)throw new System.InvalidOperationException("Player ended during capture");
  if(index>=keys.Length)
  {
   customizer.RefreshAppearance();var serializer=System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});System.IO.File.WriteAllText(folder+"/result.json",(string)serializer.Invoke(null,new object[]{rows}));UnityEditor.EditorApplication.update-=tick;return;
  }
  if(!capture){CosmeticAppearance.Apply(customizer.modelRoot,catalog,"skin_armor",keys[index],"hat_goggles");capture=true;next=UnityEditor.EditorApplication.timeSinceStartup+.4;return;}
  var foot=customizer.modelRoot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="__CosmeticShoes"&&r.gameObject.activeInHierarchy);
  if(foot.GetComponentsInChildren<UnityEngine.Collider>(true).Length!=0||foot.bones.Any(b=>b==null))throw new System.InvalidOperationException("Invalid cosmetic binding");
  string path=folder+"/"+keys[index]+".png";UnityEngine.ScreenCapture.CaptureScreenshot(path);rows.Add(new{key=keys[index],path,bones=foot.bones.Length,vertices=foot.sharedMesh.vertexCount,playerPosition=player.transform.position.ToString(),colliders=0});index++;capture=false;next=UnityEditor.EditorApplication.timeSinceStartup+.2;
 }
 catch(System.Exception error){UnityEditor.EditorApplication.update-=tick;if(customizer!=null)customizer.RefreshAppearance();System.IO.File.WriteAllText(folder+"/error.txt",error.ToString());}
};
UnityEditor.EditorApplication.update+=tick;return folder;
