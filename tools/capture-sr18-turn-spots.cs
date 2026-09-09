// Render editor-only annotations for all 15 persisted turn spots in an isolated preview.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
if(!scene.isLoaded||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Open SR18 in Edit Mode.");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var all=map.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.NoryangjinTurnSpot>(true).Where(s=>!s.IsSlopeTransition).ToArray();
var spots=all.Where(s=>!s.name.StartsWith("SR18_Turn_")).OrderBy(s=>s.transform.GetSiblingIndex()).Concat(all.Where(s=>s.name.StartsWith("SR18_Turn_")).OrderBy(s=>s.name)).ToArray();
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var root=new GameObject("Turn annotations capture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
var pink=new Material(Shader.Find("Universal Render Pipeline/Unlit"));pink.SetColor("_BaseColor",new Color(1,.05f,.55f));var yellow=new Material(pink);yellow.SetColor("_BaseColor",Color.yellow);
string folder="tmp/image-previews/sr18-turn-spots-2026-09-05";System.IO.Directory.CreateDirectory(folder);
try{
 foreach(string part in new[]{"Roads","Props","Water"}){var go=UnityEngine.Object.Instantiate(map.Find(part).gameObject,root.transform);foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;}
 for(int i=0;i<spots.Length;i++){
  var spot=spots[i];var box=spot.GetComponent<BoxCollider>();var center=spot.transform.TransformPoint(box.center);center.y=26;
  var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.transform.SetParent(root.transform);marker.layer=31;marker.transform.SetPositionAndRotation(center,spot.transform.rotation);var size=Vector3.Scale(box.size,spot.transform.lossyScale);marker.transform.localScale=new Vector3(size.x,.15f,Mathf.Max(size.z,1.5f));marker.GetComponent<Renderer>().sharedMaterial=pink;
  var arrow=new GameObject("Outgoing arrow");arrow.transform.SetParent(root.transform);arrow.layer=31;var line=arrow.AddComponent<LineRenderer>();line.sharedMaterial=yellow;line.widthMultiplier=1.3f;line.useWorldSpace=true;
  var start=spot.transform.position+Vector3.up*28;var forward=spot.TargetWorldDirection;var right=Vector3.Cross(Vector3.up,forward);var tip=start+forward*14;
  line.positionCount=5;line.SetPositions(new[]{start,tip,tip-forward*5+right*3.5f,tip,tip-forward*5-right*3.5f});
  var label=new GameObject("Order "+(i+1));label.transform.SetParent(root.transform);label.layer=31;label.transform.position=start-forward*10;label.transform.rotation=Quaternion.Euler(90,0,0);
  var text=label.AddComponent<TextMesh>();text.text=(i+1).ToString("00");text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=2;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.yellow;label.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
 }
 var cg=new GameObject("Camera");cg.transform.SetParent(root.transform);var camera=cg.AddComponent<Camera>();camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=1<<31;camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;camera.transform.rotation=Quaternion.Euler(90,0,0);
 var lg=new GameObject("Light");lg.transform.SetParent(root.transform);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.cullingMask=1<<31;lg.transform.rotation=Quaternion.Euler(50,-25,0);
 void Capture(string name,Vector3 center,float size,int width,int height){string path=folder+"/"+name+".png";if(System.IO.File.Exists(path))throw new System.InvalidOperationException("Image exists: "+path);var old=RenderTexture.active;var rt=new RenderTexture(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
  try{camera.transform.position=center+Vector3.up*1000;camera.orthographicSize=size;camera.aspect=(float)width/height;camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());}
  finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}}
 Capture("final-overview",new Vector3(140,0,-10),415,2400,2600);Capture("final-lower-corners",new Vector3(28,0,-230),160,1500,1700);
 return new{folder,count=spots.Length,scene.isDirty};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);UnityEngine.Object.DestroyImmediate(pink);UnityEngine.Object.DestroyImmediate(yellow);}
