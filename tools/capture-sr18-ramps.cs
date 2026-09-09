// Capture persisted SR18 ramp materials without modifying scene objects or assets.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!scene.isLoaded)throw new System.InvalidOperationException("Open SR18 in Edit Mode.");
var source=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var root=new GameObject("Ramp capture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
string folder="tmp/image-previews/sr18-ramp-shading-2026-09-05";System.IO.Directory.CreateDirectory(folder);
try{
 foreach(string part in new[]{"Roads","Props","Water"}){var go=UnityEngine.Object.Instantiate(source.Find(part).gameObject,root.transform);foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;}
 var cg=new GameObject("Camera");cg.transform.SetParent(root.transform);var camera=cg.AddComponent<Camera>();camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=28;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.38f,.44f,.48f);camera.nearClipPlane=.1f;camera.farClipPlane=1000;
 var lg=new GameObject("Light");lg.transform.SetParent(root.transform);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.cullingMask=1<<31;lg.transform.rotation=Quaternion.Euler(50,-25,0);
 foreach(int start in new[]{70,87,151,156}){
  var ramp=source.Find("Roads").Cast<Transform>().Single(t=>t.name.StartsWith("SR18_Road_"+start.ToString("000")+"_"));var end=source.Find("Roads").Cast<Transform>().Single(t=>t.name.StartsWith("SR18_Road_"+(start+2).ToString("000")+"_"));
  var target=(ramp.position+end.position)*.5f-ramp.forward*5.625f;target.y+=ramp.name.EndsWith("Uphill")?-2:2;
  camera.transform.rotation=Quaternion.Euler(40,ramp.eulerAngles.y-65,0);camera.transform.position=target-camera.transform.forward*180;
  string path=folder+"/applied-ramp-"+start.ToString("000")+".png";if(System.IO.File.Exists(path))throw new System.InvalidOperationException("Already captured "+path);
  var old=RenderTexture.active;var rt=new RenderTexture(1600,1000,24);var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);
  try{camera.aspect=1.6f;camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());}
  finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
 return new{folder,scene.isDirty};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
