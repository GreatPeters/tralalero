// Run as an eval_file through the connected Unity Pipeline. All authoring is in a
// temporary preview scene; the user's open/dirty scenes and assets are not saved.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Use Edit Mode for concept renders.");
var sourceScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
if(!sourceScene.IsValid()||!sourceScene.isLoaded)throw new System.InvalidOperationException("Open the SR18 scene first.");
var sourceRoads=sourceScene.GetRootGameObjects().Single(x=>x.name=="Noryangjin_MapTool").transform.Find("Roads");
bool sourceDirty=sourceScene.isDirty;int sourceCount=sourceRoads.childCount;
if(sourceCount!=230)throw new System.InvalidOperationException("Expected the 230-road SR18 layout.");
string outputFolder="map-concepts/sr18-existing-assets-2026-09-05/images-reviewed";
System.IO.Directory.CreateDirectory(outputFolder);
int firstConcept=1,lastConcept=10;
var paths=UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("/_old/")&&!p.Contains("_ROAD_")).ToArray();
var settings=new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>("Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset")).FindProperty("entries");
var prefabById=new System.Collections.Generic.Dictionary<int,GameObject>();
var scaleById=new System.Collections.Generic.Dictionary<int,Vector3>();
var yawById=new System.Collections.Generic.Dictionary<int,float>();
foreach(var path in paths){
 int id=int.Parse(System.IO.Path.GetFileName(path).Substring(0,3));var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);var scale=prefab.transform.localScale;float yaw=0;
 for(int j=0;j<settings.arraySize;j++){var e=settings.GetArrayElementAtIndex(j);if(e.FindPropertyRelative("prefabPath").stringValue==path){scale=Vector3.Scale(scale,e.FindPropertyRelative("scale").vector3Value);yaw=e.FindPropertyRelative("yawOffset").floatValue;break;}}
 prefabById[id]=prefab;scaleById[id]=scale;yawById[id]=yaw;
}
Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)throw new System.InvalidOperationException(go.name+" has no renderer");var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
void SetVisualOnly(GameObject go){foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetFloat("_OutlineWidth",0);block.SetFloat("_OutlineEnabled",0);r.SetPropertyBlock(block);}}
var conceptNames=new[]{"Single sided shopfronts","Two sided market","Three shop courtyards","Aquarium display lane","Crate loading dock","Fishing boat harbor","Sashimi restaurant row","Awning stalls","Village backdrop","Mixed progression"};
var reports=new System.Collections.Generic.List<object>();
for(int concept=firstConcept;concept<=lastConcept;concept++){
 string outputPath=outputFolder+"/"+concept.ToString("00")+"-existing-assets.png";
 if(System.IO.File.Exists(outputPath))throw new System.InvalidOperationException("Refusing to overwrite "+outputPath);
 var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var sceneRoot=new GameObject("SR18 asset-only concept "+concept);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sceneRoot,preview);
 var previousRT=RenderTexture.active;var placementRecords=new System.Collections.Generic.List<object>();
 var occupied=new System.Collections.Generic.List<Bounds>();
 var roadBounds=Enumerable.Range(0,230).Select(i=>BoundsOf(sourceRoads.GetChild(i).gameObject)).ToArray();
 try{
  var copiedRoads=UnityEngine.Object.Instantiate(sourceRoads.gameObject,sceneRoot.transform);copiedRoads.name="Unchanged_SR18_Roads";SetVisualOnly(copiedRoads);
  var sourceDeck=sourceRoads.Find("SR18_Road_001_Basic").gameObject;
  GameObject Place(int id,Vector3 center,float rotation=0,float multiplier=1,bool floating=false,bool record=true){
   var go=UnityEngine.Object.Instantiate(prefabById[id],sceneRoot.transform);go.name="Asset_"+id.ToString("000");SetVisualOnly(go);
   go.transform.localScale=scaleById[id]*multiplier;go.transform.rotation=Quaternion.Euler(0,rotation+yawById[id],0)*prefabById[id].transform.rotation;
   var b=BoundsOf(go);go.transform.position+=new Vector3(center.x-b.center.x,center.y-(floating?b.center.y:b.min.y),center.z-b.center.z);
   if(record)placementRecords.Add(new{prefab=UnityEditor.AssetDatabase.GetAssetPath(prefabById[id]),position=new[]{go.transform.position.x,go.transform.position.y,go.transform.position.z},rotation=new[]{go.transform.eulerAngles.x,go.transform.eulerAngles.y,go.transform.eulerAngles.z},scale=new[]{go.transform.localScale.x,go.transform.localScale.y,go.transform.localScale.z}});
   return go;
  }
  void Platform(Vector3 roadPoint,Vector3 side,Vector3 forward){
   var go=UnityEngine.Object.Instantiate(sourceDeck,sceneRoot.transform);go.name="Existing_pier_side_platform";SetVisualOnly(go);go.transform.localScale=new Vector3(4.05f,4f,2.6f);go.transform.rotation=Quaternion.LookRotation(forward);
   var b=BoundsOf(go);Vector3 center=roadPoint+side*8.4f;go.transform.position+=new Vector3(center.x-b.center.x,-.1f-go.transform.position.y,center.z-b.center.z);
  }
  bool Fits(Bounds b){
   foreach(var road in roadBounds)if(b.min.x<road.max.x+1.2f&&b.max.x>road.min.x-1.2f&&b.min.z<road.max.z+1.2f&&b.max.z>road.min.z-1.2f)return false;
   foreach(var other in occupied)if(b.min.x<other.max.x+1f&&b.max.x>other.min.x-1f&&b.min.z<other.max.z+1f&&b.max.z>other.min.z-1f)return false;
   return true;
  }
  void Group(Vector3 p,Vector3 forward,int sideSign,int groupIndex,int mode){
   Vector3 side=Vector3.Cross(Vector3.up,forward)*sideSign;float yaw=Quaternion.LookRotation(forward).eulerAngles.y;
   bool hasBuilding=mode!=5&&mode!=6&&mode!=8||groupIndex%3==0;
   int building=mode==7?15:mode==4||mode==8?16:(groupIndex%3==0?15:14);
   Vector3 center=p+side*11.4f;
   if(hasBuilding){
    var go=Place(building,center,yaw+(sideSign<0?180:0)+(building==15?180:0));var b=BoundsOf(go);
    if(!Fits(b)){UnityEngine.Object.DestroyImmediate(go);placementRecords.RemoveAt(placementRecords.Count-1);return;}
    occupied.Add(b);
   }
   Platform(p,side,forward);
   var ids=mode==4?new[]{21,3,31,30,29}:mode==5?new[]{20,22,1,2,4}:mode==6?new[]{34,35,24,25,42}:mode==7?new[]{3,31,27,7,4}:mode==8?new[]{1,32,33,28,4}:new[]{1,2,4,34,7};
   for(int i=0;i<ids.Length;i++){
    Vector3 spot=p+side*(5.3f+(i%2)*1.7f)+forward*((i-2)*2.05f);
    var go=Place(ids[i],spot,yaw+(sideSign<0?180:0));
    // Small props sit on the side deck. None are placed on the main running lane.
   }
   if(mode==8&&!hasBuilding)Place(43,p+side*10f,yaw+(sideSign<0?180:0));
   if(mode==5){for(int a=0;a<3;a++)for(int b=0;b<3;b++)Place(a%2==0?20:22,p+side*(7.3f+a*2.1f)+forward*(-3.8f+b*3f),yaw);}
   if(mode==6||concept==6){Place(18,p+side*28f+Vector3.up*(-2),yaw,1,true);Place(37,p+side*19f+forward*7+Vector3.up*(-2.8f),0,1,true);}
   if(mode==7)Place(27,p+side*7.7f+Vector3.up*4.6f,yaw);
  }
  // A common comparable close-up lies on the existing central eastward market run.
  for(int i=0;i<9;i++){
   if(concept==3&&i%4==3)continue;
   int mode=concept==10?1:concept;int side=concept==1?1:i%2==0?1:-1;
   Group(new Vector3(15+i*20,0,-7.2f),Vector3.right,side,i,mode);
   if(concept==2)Group(new Vector3(15+i*20,0,-7.2f),Vector3.right,-side,i+10,mode);
  }
  // Large lower loop: preserve the road, vary the market density and inner/outer banks.
  int stride=concept==6?4:concept==3?3:2;
  for(int i=3;i<25;i+=stride){
   var road=sourceRoads.Find("SR18_Road_"+i.ToString("000")+"_Basic");if(road==null)continue;
   Vector3 p=road.transform.position+Vector3.forward*5.625f;
   int mode=concept==10?5:concept;int side=concept==1?1:concept==6?-1:i%4==3?1:-1;
   Group(p,Vector3.back,side,i,mode);
   if(concept==2)Group(p,Vector3.back,-side,i+30,mode);
  }
  for(int i=48;i<=64;i+=concept==2?2:4){
   var road=sourceRoads.Find("SR18_Road_"+i.ToString("000")+"_Basic");if(road==null)continue;
   Group(road.transform.position-Vector3.forward*5.625f,Vector3.forward,-1,i,concept==10?6:concept);
  }
  // Later sections breathe; buildings are kept away from raised crossing spans and corners.
  foreach(int i in new[]{98,102,110,119,123,132,135,166,170,174}){
   var road=sourceRoads.GetChild(50+i);Vector3 forward=road.forward;
   int mode=concept==10?6:concept;Group(road.position-forward*5.625f,forward,i%2==0?1:-1,i,mode);
  }
  if(concept==9){Place(19,new Vector3(12,0,-233),0,1);Place(19,new Vector3(380,0,21),90,.75f);}
  var cameraGo=new GameObject("Preview Camera");cameraGo.transform.SetParent(sceneRoot.transform);var camera=cameraGo.AddComponent<Camera>();camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.17f,.2f);camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=3000;camera.allowHDR=false;
  var lightGo=new GameObject("Preview Sun");lightGo.transform.SetParent(sceneRoot.transform);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.color=new Color(1,.94f,.83f);light.cullingMask=1<<31;light.shadows=LightShadows.Soft;lightGo.transform.rotation=Quaternion.Euler(55,-35,0);
  var fillGo=new GameObject("Preview Fill");fillGo.transform.SetParent(sceneRoot.transform);var fill=fillGo.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fill.color=new Color(.7f,.82f,1);fill.cullingMask=1<<31;fillGo.transform.rotation=Quaternion.Euler(40,145,0);
  var board=new Texture2D(2400,1600,TextureFormat.RGB24,false);
  void RenderPanel(Vector3 target,Quaternion rotation,float size,int width,int height,int x,int y){
   var rt=new RenderTexture(width,height,24);
   try{camera.transform.rotation=rotation;camera.transform.position=target-rotation*Vector3.forward*1500;camera.orthographicSize=size;camera.aspect=(float)width/height;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;board.ReadPixels(new Rect(0,0,width,height),x,y);}
   finally{camera.targetTexture=null;RenderTexture.active=previousRT;UnityEngine.Object.DestroyImmediate(rt);}
  }
  try{
   RenderPanel(new Vector3(178,0,-6),Quaternion.Euler(90,0,0),480,1050,1600,0,0);
   Vector3 focus=concept==9?new Vector3(48,8,-220):new Vector3(94,0,-7.2f);
   RenderPanel(focus,concept==9?Quaternion.Euler(65,0,0):Quaternion.Euler(48,180,0),concept==9?80:44,1350,960,1050,640);
   RenderPanel(new Vector3(65,0,-7.2f),Quaternion.Euler(35,155,0),17,1350,640,1050,0);
   board.Apply();System.IO.File.WriteAllBytes(outputPath,board.EncodeToPNG());
  }finally{UnityEngine.Object.DestroyImmediate(board);}
  reports.Add(new{concept,name=conceptNames[concept-1],image=outputPath,roadCount=230,placements=placementRecords});
 }finally{RenderTexture.active=previousRT;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
}
if(sourceScene.isDirty!=sourceDirty||sourceRoads.childCount!=sourceCount)throw new System.InvalidOperationException("Source scene state changed.");
var jsonAssembly=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");var serialize=jsonAssembly.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("map-concepts/sr18-existing-assets-2026-09-05/placements-reviewed.json",(string)serialize.Invoke(null,new object[]{reports}));
return reports.Select(r=>r).Count()+" asset-only concepts rendered to "+outputFolder;
