// HISTORICAL: the 417-shop interior-fill concept was rejected after in-game review.
// Current layout: rebuild-sr18-roadside-market.cs. This preview does not save scenes.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Use Edit Mode.");
var map1=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity");
var sr18=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
var props=map1.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Props");
var roads=sr18.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Roads");
bool map1Dirty=map1.isDirty,sr18Dirty=sr18.isDirty;
string PathOf(Transform t){return UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);}
bool Id(Transform t,string id){return PathOf(t).Contains("/"+id+"_STAGE01_");}
var sourceProps=props.Cast<Transform>().ToArray();var shops=sourceProps.Where(t=>Id(t,"014")||Id(t,"015")||Id(t,"016")).ToArray();
if(shops.Length!=6||roads.childCount!=230)throw new System.InvalidOperationException("Unexpected source layout.");
var ground=sourceProps.First(t=>Id(t,"049"));var water=sourceProps.First(t=>Id(t,"017"));
var originalCenter=new Vector3(-10.8f,0,-54.85f);
Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
void Visual(GameObject g){foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var b in g.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;foreach(var c in g.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var r in g.GetComponentsInChildren<Renderer>(true)){var m=new MaterialPropertyBlock();r.GetPropertyBlock(m);m.SetFloat("_OutlineWidth",0);m.SetFloat("_OutlineEnabled",0);r.SetPropertyBlock(m);}}
var allSegments=new System.Collections.Generic.List<(Vector3 A,Vector3 B)>();
var flatRuns=new System.Collections.Generic.List<(Vector3 A,Vector3 B)>();
var prefix=new[]{new Vector3(34.2f,0,-114.75f),new Vector3(-10.8f,0,-114.75f),new Vector3(-10.8f,0,-7.2f),new Vector3(191.7f,0,-7.2f),new Vector3(191.7f,0,53.55f),new Vector3(124.25f,0,53.55f),new Vector3(124.25f,0,-44.00109f)};
for(int i=0;i<prefix.Length-1;i++){allSegments.Add((prefix[i],prefix[i+1]));flatRuns.Add((prefix[i],prefix[i+1]));}
Vector3 previous=prefix[prefix.Length-1];Vector3 runStart=previous,runEnd=previous,runDirection=Vector3.zero;bool activeRun=false;
for(int i=1;i<=179;i++){
 var road=roads.GetChild(50+i);var end=road.position;allSegments.Add((previous,end));Vector3 delta=end-previous;var direction=new Vector3(delta.x,0,delta.z).normalized;
 bool flat=Mathf.Abs(previous.y)<.1f&&Mathf.Abs(end.y)<.1f;
 if(!flat||activeRun&&Vector3.Dot(direction,runDirection)<.99f){if(activeRun)flatRuns.Add((runStart,runEnd));activeRun=false;}
 if(flat){if(!activeRun){runStart=previous;runDirection=direction;activeRun=true;}runEnd=end;}
 previous=end;
}
if(activeRun)flatRuns.Add((runStart,runEnd));
var occupied=shops.Select(t=>BoundsOf(t.gameObject)).ToList();
bool OverlapXZ(Bounds a,Bounds b,float gap){return a.min.x<b.max.x+gap&&a.max.x>b.min.x-gap&&a.min.z<b.max.z+gap&&a.max.z>b.min.z-gap;}
bool LaneClear(Bounds b){foreach(var s in allSegments){float minX=Mathf.Min(s.A.x,s.B.x)-2.7f,maxX=Mathf.Max(s.A.x,s.B.x)+2.7f,minZ=Mathf.Min(s.A.z,s.B.z)-2.7f,maxZ=Mathf.Max(s.A.z,s.B.z)+2.7f;if(b.min.x<maxX&&b.max.x>minX&&b.min.z<maxZ&&b.max.z>minZ)return false;}return true;}
string folder="map-concepts/sr18-dense-market-2026-09-05";System.IO.Directory.CreateDirectory(folder);
if(System.IO.File.Exists(folder+"/dense-overview.png"))throw new System.InvalidOperationException("Dense preview exists.");
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var root=new GameObject("Dense SR18 market concept");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);var oldRT=RenderTexture.active;
var added=new System.Collections.Generic.List<(GameObject Instance,Transform Source,string Zone)>();var addedGround=new System.Collections.Generic.List<GameObject>();
int rejected=0;
try{
 var roadCopy=UnityEngine.Object.Instantiate(roads.gameObject,root.transform);Visual(roadCopy);
 foreach(var source in sourceProps.Where(t=>!Id(t,"017"))){var go=UnityEngine.Object.Instantiate(source.gameObject,root.transform);go.transform.SetPositionAndRotation(source.position,source.rotation);go.transform.localScale=source.lossyScale;Visual(go);}
 GameObject Copy(Transform source,Vector3 position,Quaternion delta){var go=UnityEngine.Object.Instantiate(source.gameObject,root.transform);go.transform.SetPositionAndRotation(position,delta*source.rotation);go.transform.localScale=source.lossyScale;Visual(go);return go;}
 bool AddShop(Vector3 point,Quaternion delta,int bank,int ordinal,string zone){
  var choices=shops.Where(t=>bank*(t.position.x-originalCenter.x)>0).OrderBy(t=>t.position.z).ToArray();var source=choices[ordinal%choices.Length];
  Vector3 offset=new Vector3(source.position.x-originalCenter.x,0,0);var go=Copy(source,point+delta*offset,delta);var b=BoundsOf(go);
  if(!LaneClear(b)||occupied.Any(o=>OverlapXZ(b,o,.12f))){UnityEngine.Object.DestroyImmediate(go);rejected++;return false;}
  occupied.Add(b);added.Add((go,source,zone));return true;
 }
 void Floor(Vector3 center,Quaternion delta){
  var go=Copy(ground,new Vector3(center.x,ground.position.y,center.z),delta);var b=BoundsOf(go);go.transform.position+=new Vector3(center.x-b.center.x,0,center.z-b.center.z);addedGround.Add(go);
 }
 // Storefront rhythm is dense: approximately one shop every road module on
 // both banks, with only about one storefront of setback at run ends.
 int runIndex=0;
 foreach(var run in flatRuns){
  runIndex++;var delta=run.B-run.A;float length=new Vector2(delta.x,delta.z).magnitude;if(length<28)continue;var forward=new Vector3(delta.x,0,delta.z).normalized;var rotation=Quaternion.LookRotation(forward);var side=rotation*Vector3.right;
  for(int bank=-1;bank<=1;bank+=2){
   int floorCount=Mathf.CeilToInt((length-12)/20f);
   for(int j=0;j<floorCount;j++){float distance=10+j*20;if(distance>length-4)break;Floor(run.A+forward*distance+side*(bank*10.45f),rotation);}
   int ordinal=0;for(float distance=11.6f;distance<=length-11.6f;distance+=12.1f)AddShop(run.A+forward*distance,rotation,bank,ordinal++,"Street "+runIndex);
  }
 }
 // Three paired rows inside the large lower loop make a real market block.
 // Existing main-road corridors remain clear; these are decorative courtyards.
 for(float x=-46;x<=97;x+=13.4f)for(float z=-324;z<=-124;z+=20f)Floor(new Vector3(x,0,z),Quaternion.identity);
 int interiorOrdinal=0;
 foreach(float aisleX in new[]{-25f,27f,79f}){
  for(float z=-317;z<=-123;z+=12.1f){
   foreach(int bank in new[]{-1,1})AddShop(new Vector3(aisleX,0,z),Quaternion.identity,bank,interiorOrdinal++,"Lower market block");
  }
 }
 // Fill the current gray upper yard using the same three authored stores.
 foreach(float z in new[]{8f,21f,34f})foreach(int bank in new[]{-1,1})AddShop(new Vector3(155,0,z),Quaternion.identity,bank,interiorOrdinal++,"Existing upper yard");
 // The final loop remains a market district, not another empty sea rectangle.
 for(float x=345;x<=420;x+=13.4f)for(float z=155;z<=190;z+=20f)Floor(new Vector3(x,0,z),Quaternion.identity);
 foreach(float aisleX in new[]{358f,407f})foreach(float z in new[]{154f,167f,180f,193f})foreach(int bank in new[]{-1,1})AddShop(new Vector3(aisleX,0,z),Quaternion.identity,bank,interiorOrdinal++,"Upper market block");
 var ocean=Copy(water,water.position,Quaternion.identity);var wb=BoundsOf(ocean);float rx=920/wb.size.x,rz=1050/wb.size.z;
 float Factor(Vector3 axis){var d=ocean.transform.rotation*axis;return Mathf.Abs(d.x)*rx+Mathf.Abs(d.z)*rz+Mathf.Abs(d.y);}
 ocean.transform.localScale=Vector3.Scale(ocean.transform.localScale,new Vector3(Factor(Vector3.right),Factor(Vector3.up),Factor(Vector3.forward)));wb=BoundsOf(ocean);ocean.transform.position+=new Vector3(140-wb.center.x,0,-10-wb.center.z);
 foreach(var r in ocean.GetComponentsInChildren<Renderer>(true)){var m=new MaterialPropertyBlock();r.GetPropertyBlock(m);m.SetVector("_BaseMap_ST",new Vector4(rx,rz,0,0));m.SetVector("_MainTex_ST",new Vector4(rx,rz,0,0));r.SetPropertyBlock(m);}
 var cameraGo=new GameObject("Preview Camera");cameraGo.transform.SetParent(root.transform);var camera=cameraGo.AddComponent<Camera>();camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=1<<31;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.18f,.22f);camera.nearClipPlane=.1f;camera.farClipPlane=2500;
 var sunGo=new GameObject("Preview Sun");sunGo.transform.SetParent(root.transform);sunGo.transform.rotation=Quaternion.Euler(50,-25,0);var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.cullingMask=1<<31;
 void Capture(string name,Vector3 target,Quaternion rotation,float size,int width,int height){var rt=new RenderTexture(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);try{camera.transform.rotation=rotation;camera.transform.position=target-rotation*Vector3.forward*1200;camera.orthographicSize=size;camera.aspect=(float)width/height;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();System.IO.File.WriteAllBytes(folder+"/"+name+".png",tex.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=oldRT;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}}
 Capture("dense-overview",new Vector3(140,0,-10),Quaternion.Euler(90,0,0),415,2600,2800);
 Capture("dense-lower-block",new Vector3(28,0,-220),Quaternion.Euler(62,-25,0),150,2400,1700);
 Capture("dense-street-close",new Vector3(124.25f,0,-204),Quaternion.Euler(50,110,0),46,2000,1400);
 Capture("dense-upper-block",new Vector3(380,0,165),Quaternion.Euler(65,-25,0),100,2000,1400);
 var report=new{source="Current Map1 placed shop instances",originalShops=6,addedShops=added.Count,totalShops=6+added.Count,groundTilesAdded=addedGround.Count,rejectedForOverlap=rejected,zones=added.GroupBy(x=>x.Zone).Select(g=>new{zone=g.Key,shops=g.Count()}).ToArray(),placements=added.Select(x=>new{sourceInstance=x.Source.name,prefab=PathOf(x.Source),zone=x.Zone,position=new[]{x.Instance.transform.position.x,x.Instance.transform.position.y,x.Instance.transform.position.z},rotation=new[]{x.Instance.transform.eulerAngles.x,x.Instance.transform.eulerAngles.y,x.Instance.transform.eulerAngles.z},scale=new[]{x.Instance.transform.localScale.x,x.Instance.transform.localScale.y,x.Instance.transform.localScale.z}}).ToArray()};
 var assembly=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=assembly.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});System.IO.File.WriteAllText(folder+"/placements.json",(string)serialize.Invoke(null,new object[]{report}));
 return new{totalShops=6+added.Count,addedShops=added.Count,groundTiles=addedGround.Count,rejected,folder};
}finally{RenderTexture.active=oldRT;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);if(map1.isDirty!=map1Dirty||sr18.isDirty!=sr18Dirty)throw new System.InvalidOperationException("Source dirty state changed.");}
