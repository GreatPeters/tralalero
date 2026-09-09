// Pipeline eval_file. Rebuild only storefronts/quays; never edit Roads or turns.
// Preview first. Set apply=true only after reviewing the generated road-relative layout.
const bool apply=false;
const string folder="map-concepts/sr18-roadside-market-2026-09-05";
const string shots="tmp/image-previews/sr18-roadside-market-2026-09-05";
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var roads=map.Find("Roads");var props=map.Find("Props");
if(roads.childCount!=230)throw new System.InvalidOperationException("Expected unchanged SR18 230-road layout.");
string P(Transform t)=>UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
Bounds B(Transform t){var rs=t.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
bool Shop(Transform t)=>P(t).Contains("_BLD_");
bool Ground(Transform t)=>P(t).Contains("049_STAGE01");
bool Overlap(Bounds a,Bounds b,float gap)=>a.min.x<b.max.x+gap&&a.max.x>b.min.x-gap&&a.min.z<b.max.z+gap&&a.max.z>b.min.z-gap;
var old=props.Cast<Transform>().Where(t=>Shop(t)||Ground(t)).ToArray();
var sourceShops=old.Where(t=>Shop(t)&&t.name.StartsWith("Prop_")&&t.position.x>-10).OrderBy(P).ToArray();
if(sourceShops.Length!=3)throw new System.InvalidOperationException("Expected original three east-facing source instances.");
var sourceGround=old.First(Ground);
var roadBounds=roads.Cast<Transform>().Select(B).ToArray();
int beforeCovered=old.Where(Ground).Count(t=>roadBounds.Any(b=>Overlap(B(t),b,0)));
string Signature()=>string.Join("|",roads.Cast<Transform>().Select(t=>t.name+t.position.ToString("F4")+t.rotation.ToString("F4")+t.localScale.ToString("F4")));
string signature=Signature();bool wasDirty=scene.isDirty;
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var staging=new GameObject("RoadsideMarketStaging");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(staging,preview);
var placed=new System.Collections.Generic.List<(GameObject Shop,GameObject Ground,string[] Roads)>();
var occupied=new System.Collections.Generic.List<Bounds>();
var optimize=typeof(NoryangjinMapStaticOptimizer).GetMethod("OptimizePlacedRoot",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(GameObject),typeof(bool)},null);
GameObject Copy(Transform source,Quaternion rotation,string name){
 var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(P(source)),staging.transform);
 UnityEditor.PrefabUtility.SetPropertyModifications(go,UnityEditor.PrefabUtility.GetPropertyModifications(source.gameObject));
 go.name=name;go.transform.SetPositionAndRotation(source.position,rotation);go.transform.localScale=source.lossyScale;return go;
}
void Center(GameObject go,Vector3 center){var b=B(go.transform);go.transform.position+=new Vector3(center.x-b.center.x,0,center.z-b.center.z);}
var flat=roads.Cast<Transform>().Where(t=>Mathf.Abs(t.position.y)<.1f&&!t.name.EndsWith("Uphill")&&!t.name.EndsWith("Downhill")&&!(t.name.StartsWith("SR18_")&&(t.name.EndsWith("RightTurn")||t.name.EndsWith("LeftTurn"))))
 .Select(t=>new{Root=t,Bounds=B(t),Vertical=Mathf.Abs(t.forward.z)>.5f}).ToArray();
int rejected=0,runCount=0;bool saved=false;int undo=-1;
try{
 // Merge collinear physical modules, using rendered positions rather than pivot assumptions.
 foreach(var group in flat.GroupBy(r=>(r.Vertical,Line:Mathf.RoundToInt((r.Vertical?r.Bounds.center.x:r.Bounds.center.z)*2)))){
  bool vertical=group.Key.Vertical;var ordered=group.OrderBy(r=>vertical?r.Bounds.min.z:r.Bounds.min.x).ToArray();
  int first=0;
  while(first<ordered.Length){
   int last=first;float min=vertical?ordered[first].Bounds.min.z:ordered[first].Bounds.min.x,max=vertical?ordered[first].Bounds.max.z:ordered[first].Bounds.max.x;
   while(last+1<ordered.Length&&(vertical?ordered[last+1].Bounds.min.z:ordered[last+1].Bounds.min.x)<=max+1){last++;max=Mathf.Max(max,vertical?ordered[last].Bounds.max.z:ordered[last].Bounds.max.x);}
   var members=ordered.Skip(first).Take(last-first+1).ToArray();first=last+1;runCount++;
   float line=members.Average(r=>vertical?r.Bounds.center.x:r.Bounds.center.z);
   float roadHalf=members.Max(r=>vertical?r.Bounds.extents.x:r.Bounds.extents.z);
   Vector3 forward=vertical?Vector3.forward:Vector3.right,side=Vector3.Cross(Vector3.up,forward);
   Quaternion delta=Quaternion.LookRotation(forward);
   int count=Mathf.FloorToInt((max-min-1)/12.1f);if(count<1)continue;
   float start=(min+max-(count-1)*12.1f)*.5f;
   for(int bank=-1;bank<=1;bank+=2)for(int index=0;index<count;index++){
    float along=start+index*12.1f;var anchor=vertical?new Vector3(line,0,along):new Vector3(along,0,line);
    var source=sourceShops[(index+runCount)%3];var rotation=delta*Quaternion.Euler(0,bank>0?0:180,0)*source.rotation;
    var shop=Copy(source,rotation,"SR18_Route_Shop_"+(placed.Count+1).ToString("000"));var sb=B(shop.transform);
    float depth=vertical?sb.extents.x:sb.extents.z;
    Center(shop,anchor+side*bank*(roadHalf+.4f+depth));sb=B(shop.transform);
    var ground=Copy(sourceGround,delta*sourceGround.rotation,"SR18_Route_Quay_"+(placed.Count+1).ToString("000"));var gb=B(ground.transform);
    float wantedX=vertical?9f:12.2f,wantedZ=vertical?12.2f:9f,rx=wantedX/gb.size.x,rz=wantedZ/gb.size.z;
    float Factor(Vector3 axis){var d=ground.transform.rotation*axis;return Mathf.Abs(d.x)*rx+Mathf.Abs(d.z)*rz+Mathf.Abs(d.y);}
    ground.transform.localScale=Vector3.Scale(ground.transform.localScale,new Vector3(Factor(Vector3.right),Factor(Vector3.up),Factor(Vector3.forward)));
    Center(ground,anchor+side*bank*(roadHalf+.2f+4.5f));gb=B(ground.transform);
    if(roadBounds.Any(b=>Overlap(sb,b,.12f)||Overlap(gb,b,.08f))||occupied.Any(b=>Overlap(sb,b,.12f))){UnityEngine.Object.DestroyImmediate(shop);UnityEngine.Object.DestroyImmediate(ground);rejected++;continue;}
    // Full storefront footprint, including awning, must sit on its quay.
    if(sb.min.x<gb.min.x-.05f||sb.max.x>gb.max.x+.05f||sb.min.z<gb.min.z-.05f||sb.max.z>gb.max.z+.05f)throw new System.InvalidOperationException("Quay does not support "+shop.name);
    occupied.Add(sb);placed.Add((shop,ground,members.Select(r=>r.Root.name).ToArray()));
   }
  }
 }
 if(placed.Count<220)throw new System.InvalidOperationException("Unexpectedly sparse layout: "+placed.Count);
 foreach(var pair in placed)foreach(var go in new[]{pair.Shop,pair.Ground}){
  optimize.Invoke(null,new object[]{go,false});UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
  if(UnityEditor.PrefabUtility.GetPrefabInstanceStatus(go)!=UnityEditor.PrefabInstanceStatus.Connected)throw new System.InvalidOperationException("Detached prefab");
 }
 if(Signature()!=signature||scene.isDirty!=wasDirty)throw new System.InvalidOperationException("Staging changed source state");
 System.IO.Directory.CreateDirectory(folder);System.IO.Directory.CreateDirectory(shots);
 var json=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");
 var report=new{scene=scene.path,totalShops=placed.Count,groundCount=placed.Count,roads=230,turnSpots=15,beforeGroundOverlaps=beforeCovered,afterGroundOverlaps=0,roadsideOnly=true,runCount,rejected,quayWidth=9f,quayLength=12.2f,shopPitch=12.1f,placements=placed.SelectMany(pair=>new[]{new{go=pair.Shop,kind="Shop",relatedRoads=pair.Roads},new{go=pair.Ground,kind="Ground",relatedRoads=pair.Roads}}).Select(x=>new{name=x.go.name,x.kind,prefab=P(x.go.transform),position=new[]{x.go.transform.position.x,x.go.transform.position.y,x.go.transform.position.z},rotation=new[]{x.go.transform.eulerAngles.x,x.go.transform.eulerAngles.y,x.go.transform.eulerAngles.z},scale=new[]{x.go.transform.localScale.x,x.go.transform.localScale.y,x.go.transform.localScale.z},x.relatedRoads}).ToArray()};
 if(apply){
  if(!System.IO.File.Exists("tmp/backups/sr18-before-route-market-20260905-082119.unity.tmp"))throw new System.InvalidOperationException("Live-state backup missing");
  if(System.IO.File.Exists(folder+"/applied-layout.json"))throw new System.InvalidOperationException("Already applied; do not overwrite author edits.");
  UnityEditor.Undo.IncrementCurrentGroup();undo=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Correct SR18 market to existing roads");
  foreach(var t in old)UnityEditor.Undo.DestroyObjectImmediate(t.gameObject);
  foreach(var pair in placed)foreach(var go in new[]{pair.Shop,pair.Ground}){
   go.transform.SetParent(null,true);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);UnityEditor.Undo.RegisterCreatedObjectUndo(go,"Roadside placement");UnityEditor.Undo.SetTransformParent(go.transform,props,"Organize roadside placement");
  }
  if(Signature()!=signature)throw new System.InvalidOperationException("Roads changed");
  UnityEditor.Undo.CollapseUndoOperations(undo);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
  if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.InvalidOperationException("Save failed");saved=true;
 }
 var serialize=json.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
 System.IO.File.WriteAllText(folder+(apply?"/applied-layout.json":"/preview-layout.json"),(string)serialize.Invoke(null,new object[]{report}));
 // Use a separate visual clone so capture does not change saved prefab/layer state.
 var visual=new GameObject("Capture only");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual,preview);
 void Visual(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var c in go.GetComponentsInChildren<MonoBehaviour>(true))c.enabled=false;}
 foreach(var pair in placed)foreach(var go in new[]{pair.Shop,pair.Ground}){var copy=UnityEngine.Object.Instantiate(go,visual.transform);copy.transform.SetPositionAndRotation(go.transform.position,go.transform.rotation);copy.transform.localScale=go.transform.lossyScale;Visual(copy);}
 foreach(var t in new[]{roads,map.Find("Water")}.Concat(props.Cast<Transform>().Where(t=>!Shop(t)&&!Ground(t)))){var copy=UnityEngine.Object.Instantiate(t.gameObject,visual.transform);copy.transform.SetPositionAndRotation(t.position,t.rotation);copy.transform.localScale=t.lossyScale;Visual(copy);}
 var cameraGo=new GameObject("Capture camera");cameraGo.transform.SetParent(visual.transform);var camera=cameraGo.AddComponent<Camera>();camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.18f,.22f);camera.nearClipPlane=.1f;camera.farClipPlane=2500;
 var sunGo=new GameObject("Capture light");sunGo.transform.SetParent(visual.transform);sunGo.transform.rotation=Quaternion.Euler(50,-25,0);var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.cullingMask=1<<31;
 void Capture(string name,Vector3 target,Quaternion rotation,float size,int width,int height,bool ortho){
  string path=shots+"/"+(apply?"applied-":"preview-")+name+".png";if(System.IO.File.Exists(path))throw new System.InvalidOperationException("Preview already exists: "+path);
  var oldRT=RenderTexture.active;var rt=new RenderTexture(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
  try{camera.orthographic=ortho;camera.fieldOfView=40;camera.transform.SetPositionAndRotation(target-rotation*Vector3.forward*(ortho?1200:40),rotation);camera.orthographicSize=size;camera.aspect=(float)width/height;camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());}
  finally{camera.targetTexture=null;RenderTexture.active=oldRT;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
 }
 Capture("overview",new Vector3(140,0,-10),Quaternion.Euler(90,0,0),415,2400,2600,true);
 Capture("start",new Vector3(-10.8f,0,-55),Quaternion.Euler(60,0,0),42,1600,1600,true);
 Capture("road-view",new Vector3(124.25f,0,-204),Quaternion.Euler(35,180,0),40,720,1280,false);
 return new{applied=apply,shops=placed.Count,quays=placed.Count,retainedProps=props.childCount,runCount,rejected,beforeCovered,afterCovered=0,saved};
}catch{if(!saved&&undo>=0)UnityEditor.Undo.RevertAllDownToGroup(undo);throw;}
finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
