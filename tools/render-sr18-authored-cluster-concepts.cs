// Concept renders cloned from already placed Map1 instances, not palette defaults.
// No scene saves, source edits, material edits, or runtime behavior changes.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Use Edit Mode.");
var sourceScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity");
var routeScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var sourceMap=sourceScene.GetRootGameObjects().Single(x=>x.name=="Noryangjin_MapTool").transform;
var routeMap=routeScene.GetRootGameObjects().Single(x=>x.name=="Noryangjin_MapTool").transform;
var sourceProps=sourceMap.Find("Props");var roads=routeMap.Find("Roads");
bool sourceDirty=sourceScene.isDirty,routeDirty=routeScene.isDirty;
string PathOf(Transform t){return UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);}
bool Id(Transform t,string id){return PathOf(t).Contains("/"+id+"_STAGE01_");}
var sourcePlacements=sourceProps.Cast<Transform>().ToArray();
var market=sourcePlacements.Where(t=>Id(t,"014")||Id(t,"015")||Id(t,"016")).ToArray();
if(market.Length!=6||roads.childCount!=230)throw new System.InvalidOperationException("Unexpected authored reference.");
var ground=sourcePlacements.First(t=>Id(t,"049"));var water=sourcePlacements.First(t=>Id(t,"017"));var boat=sourcePlacements.First(t=>Id(t,"018"));var gull=sourcePlacements.First(t=>Id(t,"008"));var anchor=sourcePlacements.First(t=>Id(t,"035"));var ring=sourcePlacements.First(t=>Id(t,"024"));var gantry=sourcePlacements.First(t=>Id(t,"045"));
var specs=new[]{
 new{Id=1,Name="Market extension",Blocks=new[]{(12,2),(35,2),(101,1)},Boats=new[]{20,54,120},LongQuay=false},
 new{Id=2,Name="Harbor emphasis",Blocks=new[]{(11,2),(56,1)},Boats=new[]{18,23,34,53,61},LongQuay=false},
 new{Id=3,Name="Market after turns",Blocks=new[]{(33,2),(51,2),(101,1)},Boats=new[]{21,60,122},LongQuay=false},
 new{Id=4,Name="Inner bank market",Blocks=new[]{(12,1),(36,1),(56,1),(101,1)},Boats=new[]{21,59,121},LongQuay=false},
 new{Id=5,Name="Two compact markets",Blocks=new[]{(12,2),(57,2)},Boats=new[]{22,34,122},LongQuay=false},
 new{Id=6,Name="Open pier rhythm",Blocks=new[]{(12,1),(101,1)},Boats=new[]{22,54,122},LongQuay=false},
 new{Id=7,Name="Continuous working quay",Blocks=new[]{(12,2),(36,1),(101,1)},Boats=new[]{20,59,122},LongQuay=true},
 new{Id=8,Name="Shore facing market",Blocks=new[]{(53,2),(60,1),(101,1)},Boats=new[]{16,32,122},LongQuay=true},
 new{Id=9,Name="Quiet upper loop",Blocks=new[]{(12,2),(35,2),(58,1)},Boats=new[]{22,54,119},LongQuay=false},
 new{Id=10,Name="Recommended progression",Blocks=new[]{(12,2),(35,1),(101,1)},Boats=new[]{23,55,121},LongQuay=true}
};
string folder="map-concepts/sr18-authored-props-2026-09-05/final";System.IO.Directory.CreateDirectory(folder);
var reports=new System.Collections.Generic.List<object>();
Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
void Visual(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var p=new MaterialPropertyBlock();r.GetPropertyBlock(p);p.SetFloat("_OutlineWidth",0);p.SetFloat("_OutlineEnabled",0);r.SetPropertyBlock(p);}}
foreach(var spec in specs){
 string output=folder+"/"+spec.Id.ToString("00")+"-authored-style.png";if(System.IO.File.Exists(output))throw new System.InvalidOperationException("Preview exists: "+output);
 var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var root=new GameObject("Authored layout proposal "+spec.Id);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);var previousRT=RenderTexture.active;
 var additions=new System.Collections.Generic.List<object>();
 try{
  var roadCopy=UnityEngine.Object.Instantiate(roads.gameObject,root.transform);Visual(roadCopy);
  // Preserve the actual authored ensemble. Coverage is rendered with one scaled
  // copy of its same water mesh/material instead of multiplying hundreds of tiles.
  foreach(var t in sourcePlacements.Where(t=>!Id(t,"017"))){var go=UnityEngine.Object.Instantiate(t.gameObject,root.transform);go.transform.SetPositionAndRotation(t.position,t.rotation);go.transform.localScale=t.lossyScale;Visual(go);}
  GameObject Clone(Transform source,Vector3 position,Quaternion delta){
   var go=UnityEngine.Object.Instantiate(source.gameObject,root.transform);go.transform.SetPositionAndRotation(position,delta*source.rotation);go.transform.localScale=source.lossyScale;Visual(go);
   additions.Add(new{sourceInstance=source.name,prefab=PathOf(source),position=new[]{position.x,position.y,position.z},rotation=new[]{go.transform.eulerAngles.x,go.transform.eulerAngles.y,go.transform.eulerAngles.z},scale=new[]{go.transform.localScale.x,go.transform.localScale.y,go.transform.localScale.z}});return go;
  }
  Vector3 Point(int index){var r=roads.GetChild(50+index);return r.position-r.forward*5.625f;}
  void Quay(Vector3 center,Quaternion delta,int banks,float length,bool skipInner=false){
   var forward=delta*Vector3.forward;var side=delta*Vector3.right;int tiles=Mathf.CeilToInt(length/20f);
   for(int bank=skipInner?1:0;bank<banks;bank++)for(int j=0;j<tiles;j++){
    float sign=bank==0?1:-1;Vector3 p=center+side*(10.45f*sign)+forward*((j-(tiles-1)*.5f)*20f);
    var go=Clone(ground,new Vector3(p.x,ground.position.y,p.z),delta);var b=BoundsOf(go);go.transform.position+=new Vector3(p.x-b.center.x,0,p.z-b.center.z);
   }
  }
  var referenceCenter=new Vector3(-10.8f,0,-54.85f);
  foreach(var block in spec.Blocks){
   Vector3 p=Point(block.Item1);p.y=0;var direction=roads.GetChild(50+block.Item1).forward;var rotation=Quaternion.LookRotation(direction);
   bool onLongQuay=spec.LongQuay&&(spec.Id==8?block.Item1>=45&&block.Item1<=68:block.Item1<28);
   Quay(p,rotation,block.Item2,60,onLongQuay);
   foreach(var stall in market.Where(t=>block.Item2==2||t.position.x>referenceCenter.x))Clone(stall,p+rotation*(stall.position-referenceCenter),rotation);
   Clone(gull,p+rotation*new Vector3(3.5f,2.705f,-22),rotation);Clone(gull,p+rotation*new Vector3(-3.5f,2.705f,22),rotation);
  }
  // Wide continuous gray strips, matching the current quay, not little
  // individually protruding wooden pads under every storefront.
  if(spec.LongQuay){
   int index=spec.Id==8?56:15;var r=roads.GetChild(50+index);var p=Point(index);p.y=0;Quay(p,Quaternion.LookRotation(r.forward),1,spec.Id==8?180:260);
  }
  foreach(int index in spec.Boats){var r=roads.GetChild(50+index);var rotation=Quaternion.LookRotation(r.forward);Vector3 p=Point(index);p.y=0;var side=rotation*Vector3.right;Clone(boat,p+side*24+Vector3.up*boat.position.y,rotation);Clone(anchor,p+side*2+Vector3.up*anchor.position.y,rotation);Clone(ring,p-side*2+Vector3.up*ring.position.y,rotation);}
  foreach(int index in new[]{5,22,42,64,92,114,126,147,160,171}){var r=roads.GetChild(50+index);var p=Point(index);p.y=r.position.y;Clone(gull,p+r.right*(index%2==0?3.5f:-3.5f)+Vector3.up*2.705f,Quaternion.LookRotation(r.forward));}
  var exit=Point(176);exit.y=gantry.position.y;Clone(gantry,exit,Quaternion.identity);
  if(spec.Id==9){Quay(Point(132),Quaternion.LookRotation(Vector3.right),1,40);}
  var ocean=UnityEngine.Object.Instantiate(water.gameObject,root.transform);ocean.transform.SetPositionAndRotation(water.position,water.rotation);ocean.transform.localScale=water.lossyScale;Visual(ocean);var originalBounds=BoundsOf(ocean);float rx=900/originalBounds.size.x,rz=1000/originalBounds.size.z;
  float Factor(Vector3 axis){var direction=ocean.transform.rotation*axis;return Mathf.Abs(direction.x)*rx+Mathf.Abs(direction.z)*rz+Mathf.Abs(direction.y);}
  ocean.transform.localScale=Vector3.Scale(ocean.transform.localScale,new Vector3(Factor(Vector3.right),Factor(Vector3.up),Factor(Vector3.forward)));var ob=BoundsOf(ocean);ocean.transform.position+=new Vector3(140-ob.center.x,0,-15-ob.center.z);
  foreach(var r in ocean.GetComponentsInChildren<Renderer>(true)){var mpb=new MaterialPropertyBlock();r.GetPropertyBlock(mpb);mpb.SetVector("_BaseMap_ST",new Vector4(rx,rz,0,0));mpb.SetVector("_MainTex_ST",new Vector4(rx,rz,0,0));r.SetPropertyBlock(mpb);}
  var cameraGo=new GameObject("Preview Camera");cameraGo.transform.SetParent(root.transform);var camera=cameraGo.AddComponent<Camera>();camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.18f,.22f);camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=2200;
  var sunGo=new GameObject("Preview Sun");sunGo.transform.SetParent(root.transform);sunGo.transform.rotation=Quaternion.Euler(50,-25,0);var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.cullingMask=1<<31;
  var board=new Texture2D(2400,1600,TextureFormat.RGB24,false);
  void Panel(Vector3 target,Quaternion rotation,float size,int width,int height,int x,int y){var rt=new RenderTexture(width,height,24);try{camera.transform.rotation=rotation;camera.transform.position=target-rotation*Vector3.forward*1000;camera.orthographicSize=size;camera.aspect=(float)width/height;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;board.ReadPixels(new Rect(0,0,width,height),x,y);}finally{camera.targetTexture=null;RenderTexture.active=previousRT;UnityEngine.Object.DestroyImmediate(rt);}}
  try{Panel(new Vector3(146,0,-8),Quaternion.Euler(90,0,0),480,1100,1600,0,0);int focus=spec.Blocks[0].Item1;float yaw=roads.GetChild(50+focus).eulerAngles.y;Panel(Point(focus),Quaternion.Euler(62,yaw-25,0),46,1300,850,1100,750);Panel(referenceCenter,Quaternion.Euler(52,-65,0),31,1300,750,1100,0);board.Apply();System.IO.File.WriteAllBytes(output,board.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(board);}
  reports.Add(new{concept=spec.Id,name=spec.Name,image=output,sourceProps=sourcePlacements.Length,preservedVisualSourceProps=sourcePlacements.Count(t=>!Id(t,"017")),roadCount=230,additions});
 }finally{RenderTexture.active=previousRT;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
}
if(sourceScene.isDirty!=sourceDirty||routeScene.isDirty!=routeDirty)throw new System.InvalidOperationException("Source dirty state changed.");
var assembly=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=assembly.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/sr18-authored-props-2026-09-05/placements.json",(string)serialize.Invoke(null,new object[]{reports}));
return new{images=reports.Count,sourceProps=sourcePlacements.Length,sourceDirty,routeDirty};
