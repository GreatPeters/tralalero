// HISTORICAL: rejected after in-game review. Do not use for current SR18.
// Replaced by rebuild-sr18-roadside-market.cs (existing road footprint first).
// Apply the former 417-store concept through Unity Pipeline eval_file.
// Requires the live SR18 backup described in the application record.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Apply in Edit Mode.");
const string SourcePath="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";
const string TargetPath="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
var sourceScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(SourcePath);
var targetScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(TargetPath);
if(!sourceScene.isLoaded||!targetScene.isLoaded)throw new System.InvalidOperationException("Open Map1 and SR18 first.");
var sourceRoot=sourceScene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var targetRoot=targetScene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
var sourceProps=sourceRoot.Find("Props");var targetProps=targetRoot.Find("Props");var targetWater=targetRoot.Find("Water");var roads=targetRoot.Find("Roads");
if(roads.childCount!=230||targetProps.childCount!=15||targetProps.GetComponentsInChildren<Renderer>(true).Length!=0||targetWater.childCount!=0)throw new System.InvalidOperationException("SR18 has content already; refusing to overwrite it.");
string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=System.IO.File.OpenRead(path))return System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
string sourceHash=Hash(SourcePath);bool sourceDirty=sourceScene.isDirty;
string RoadSignature(){return string.Join("|",roads.Cast<Transform>().Select(t=>t.name+t.position.ToString("F4")+t.rotation.ToString("F4")+t.localScale.ToString("F4")));}
string roadSignature=RoadSignature();
string PrefabPath(Transform t){return UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);}
bool Id(Transform t,string id){return PrefabPath(t).Contains("/"+id+"_STAGE01_");}
Bounds BoundsOf(GameObject g){var renderers=g.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new System.InvalidOperationException("No renderer: "+g.name);var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
var sources=sourceProps.Cast<Transform>().ToArray();var ground=sources.First(t=>Id(t,"049"));var water=sources.First(t=>Id(t,"017"));
var baseline=sources.Where(t=>!Id(t,"017")&&t.GetComponentsInChildren<Renderer>(true).Length>0).ToArray();
if(baseline.Count(t=>Id(t,"014")||Id(t,"015")||Id(t,"016"))!=6)throw new System.InvalidOperationException("The authored six-shop baseline changed.");
foreach(var t in baseline.Concat(new[]{water})){
 if(UnityEditor.PrefabUtility.GetAddedGameObjects(t.gameObject).Count>0||UnityEditor.PrefabUtility.GetAddedComponents(t.gameObject).Count>0||UnityEditor.PrefabUtility.GetRemovedComponents(t.gameObject).Count>0)throw new System.InvalidOperationException("Unsupported structural override: "+t.name);
 if(UnityEditor.PrefabUtility.GetPropertyModifications(t.gameObject).Any(m=>m.objectReference!=null&&!UnityEditor.EditorUtility.IsPersistent(m.objectReference)))throw new System.InvalidOperationException("Scene reference in source override: "+t.name);
}
var jsonAssembly=System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json");
object plan=jsonAssembly.GetType("Newtonsoft.Json.Linq.JObject").GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{System.IO.File.ReadAllText("map-concepts/sr18-dense-market-2026-09-05/placements.json")});
object Get(object o,string k){return o.GetType().GetProperty("Item",new[]{typeof(string)}).GetValue(o,new object[]{k});}
object At(object o,int i){return o.GetType().GetProperty("Item",new[]{typeof(int)}).GetValue(o,new object[]{i});}
Vector3 Vector(object o){return new Vector3(float.Parse(At(o,0).ToString(),System.Globalization.CultureInfo.InvariantCulture),float.Parse(At(o,1).ToString(),System.Globalization.CultureInfo.InvariantCulture),float.Parse(At(o,2).ToString(),System.Globalization.CultureInfo.InvariantCulture));}
var rows=Get(plan,"placements");int rowCount=(int)rows.GetType().GetProperty("Count").GetValue(rows);
if(rowCount!=411||Get(plan,"totalShops").ToString()!="417")throw new System.InvalidOperationException("Expected the approved 417-store plan.");
string materialFolder="Assets/ShooterSurvival/Materials/Generated/SR18DenseMarket";
System.IO.Directory.CreateDirectory(materialFolder);UnityEditor.AssetDatabase.Refresh();
var materialCache=new System.Collections.Generic.Dictionary<Material,Material>();
var createdMaterialPaths=new System.Collections.Generic.List<string>();
Material SceneMaterial(Material original){
 if(original==null||!original.HasProperty("_OutlineWidth"))return original;
 if(materialCache.TryGetValue(original,out var cached))return cached;
 string path=materialFolder+"/"+UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(original))+".mat";
 if(UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path)!=null)throw new System.InvalidOperationException("Material variant already exists: "+path);
 var material=new Material(original);material.name="SR18 "+original.name;material.SetFloat("_OutlineWidth",0);if(material.HasProperty("_OutlineEnabled"))material.SetFloat("_OutlineEnabled",0);material.enableInstancing=true;
 UnityEditor.AssetDatabase.CreateAsset(material,path);createdMaterialPaths.Add(path);materialCache[original]=material;return material;
}
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var staged=new GameObject("DenseMarketStaging");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(staged,preview);
var objects=new System.Collections.Generic.List<(GameObject Object,string Kind)>();var newShops=new System.Collections.Generic.List<GameObject>();var groundObjects=new System.Collections.Generic.List<GameObject>();
var optimize=typeof(NoryangjinMapStaticOptimizer).GetMethod("OptimizePlacedRoot",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(GameObject),typeof(bool)},null);
GameObject Copy(Transform source,Vector3 position,Quaternion rotation,Vector3 scale,string name,string kind){
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(source));if(prefab==null)throw new System.InvalidOperationException("Missing prefab: "+source.name);
 var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,staged.transform);
 UnityEditor.PrefabUtility.SetPropertyModifications(go,UnityEditor.PrefabUtility.GetPropertyModifications(source.gameObject));
 go.name=name;go.transform.SetPositionAndRotation(position,rotation);go.transform.localScale=scale;
 optimize.Invoke(null,new object[]{go,false});
 foreach(var renderer in go.GetComponentsInChildren<Renderer>(true)){
  renderer.sharedMaterials=renderer.sharedMaterials.Select(SceneMaterial).ToArray();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
 }
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
 objects.Add((go,kind));if(Id(source,"049"))groundObjects.Add(go);return go;
}
var segments=new System.Collections.Generic.List<(Vector3 A,Vector3 B)>();var flatRuns=new System.Collections.Generic.List<(Vector3 A,Vector3 B)>();
var prefix=new[]{new Vector3(34.2f,0,-114.75f),new Vector3(-10.8f,0,-114.75f),new Vector3(-10.8f,0,-7.2f),new Vector3(191.7f,0,-7.2f),new Vector3(191.7f,0,53.55f),new Vector3(124.25f,0,53.55f),new Vector3(124.25f,0,-44.00109f)};
for(int i=0;i<prefix.Length-1;i++){segments.Add((prefix[i],prefix[i+1]));flatRuns.Add((prefix[i],prefix[i+1]));}
Vector3 previous=prefix[prefix.Length-1],runStart=previous,runEnd=previous,runDirection=Vector3.zero;bool runActive=false;
for(int i=1;i<=179;i++){
 var end=roads.GetChild(50+i).position;segments.Add((previous,end));var d=end-previous;var direction=new Vector3(d.x,0,d.z).normalized;bool flat=Mathf.Abs(previous.y)<.1f&&Mathf.Abs(end.y)<.1f;
 if(!flat||runActive&&Vector3.Dot(direction,runDirection)<.99f){if(runActive)flatRuns.Add((runStart,runEnd));runActive=false;}
 if(flat){if(!runActive){runStart=previous;runDirection=direction;runActive=true;}runEnd=end;}previous=end;
}
if(runActive)flatRuns.Add((runStart,runEnd));
bool LaneClear(Bounds b){foreach(var s in segments)if(b.min.x<Mathf.Max(s.A.x,s.B.x)+2.7f&&b.max.x>Mathf.Min(s.A.x,s.B.x)-2.7f&&b.min.z<Mathf.Max(s.A.z,s.B.z)+2.7f&&b.max.z>Mathf.Min(s.A.z,s.B.z)-2.7f)return false;return true;}
bool Overlaps(Bounds a,Bounds b){return a.min.x<b.max.x+.1f&&a.max.x>b.min.x-.1f&&a.min.z<b.max.z+.1f&&a.max.z>b.min.z-.1f;}
int floorOrdinal=0,skippedFloors=0;bool transferred=false;int undoGroup=-1;
try{
 foreach(var t in baseline)Copy(t,t.position,t.rotation,t.lossyScale,t.name,"Baseline");
 for(int i=0;i<rowCount;i++){
  var row=At(rows,i);var source=sources.Single(t=>t.name==Get(row,"sourceInstance").ToString());
  if(PrefabPath(source)!=Get(row,"prefab").ToString())throw new System.InvalidOperationException("Prefab changed: "+source.name);
  var go=Copy(source,Vector(Get(row,"position")),Quaternion.Euler(Vector(Get(row,"rotation"))),Vector(Get(row,"scale")),"SR18_Dense_Shop_"+(i+1).ToString("000"),"Shop");
  if(!LaneClear(BoundsOf(go)))throw new System.InvalidOperationException("Shop intrudes into the approved corridor: "+go.name);
  newShops.Add(go);
 }
 var allShops=objects.Where(x=>Id(x.Object.transform,"014")||Id(x.Object.transform,"015")||Id(x.Object.transform,"016")).Select(x=>x.Object).ToArray();
 if(allShops.Length!=417)throw new System.InvalidOperationException("Shop count mismatch.");
 for(int i=0;i<newShops.Count;i++)foreach(var other in allShops)if(other!=newShops[i]&&Overlaps(BoundsOf(newShops[i]),BoundsOf(other)))throw new System.InvalidOperationException("Shop overlap: "+newShops[i].name+" / "+other.name);
 void Floor(Vector3 center,Quaternion delta,bool deduplicate){
  var go=Copy(ground,new Vector3(center.x,ground.position.y-.02f,center.z),delta*ground.rotation,ground.lossyScale,"SR18_Dense_Ground_"+(++floorOrdinal).ToString("000"),"Ground");var b=BoundsOf(go);go.transform.position+=new Vector3(center.x-b.center.x,0,center.z-b.center.z);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);b=BoundsOf(go);
  if(deduplicate&&groundObjects.Where(g=>g!=go).Any(g=>{var o=BoundsOf(g);float overlap=Mathf.Max(0,Mathf.Min(o.max.x,b.max.x)-Mathf.Max(o.min.x,b.min.x))*Mathf.Max(0,Mathf.Min(o.max.z,b.max.z)-Mathf.Max(o.min.z,b.min.z));return overlap/(b.size.x*b.size.z)>.9f;})){
   objects.RemoveAll(x=>x.Object==go);groundObjects.Remove(go);UnityEngine.Object.DestroyImmediate(go);skippedFloors++;
  }
 }
 foreach(var run in flatRuns){var d=run.B-run.A;float length=new Vector2(d.x,d.z).magnitude;if(length<28)continue;var forward=new Vector3(d.x,0,d.z).normalized;var rotation=Quaternion.LookRotation(forward);var side=rotation*Vector3.right;
  for(int bank=-1;bank<=1;bank+=2){int count=Mathf.CeilToInt((length-12)/20f);for(int j=0;j<count;j++){float distance=10+j*20;if(distance>length-4)break;Floor(run.A+forward*distance+side*(bank*10.45f),rotation,true);}}
 }
 for(float x=-46;x<=97;x+=13.4f)for(float z=-324;z<=-124;z+=20f)Floor(new Vector3(x,0,z),Quaternion.identity,true);
 for(float x=345;x<=420;x+=13.4f)for(float z=155;z<=190;z+=20f)Floor(new Vector3(x,0,z),Quaternion.identity,true);
 // The authored awnings overhang the gray quay. Test the footing, not their tips.
 bool Grounded(Bounds b){var points=new[]{b.center,new Vector3(b.min.x+.7f,0,b.min.z+.7f),new Vector3(b.max.x-.7f,0,b.min.z+.7f),new Vector3(b.min.x+.7f,0,b.max.z-.7f),new Vector3(b.max.x-.7f,0,b.max.z-.7f)};return points.All(p=>groundObjects.Any(g=>{var q=BoundsOf(g);return p.x>=q.min.x-.05f&&p.x<=q.max.x+.05f&&p.z>=q.min.z-.05f&&p.z<=q.max.z+.05f;}));}
 int supportRepairs=0;
 foreach(var shop in allShops.Where(s=>!Grounded(BoundsOf(s)))){
  var b=BoundsOf(shop);Transform source=shopsSource(shop.transform);
  var delta=shop.transform.rotation*Quaternion.Inverse(source.rotation);Floor(new Vector3(b.center.x,0,b.center.z),delta,false);supportRepairs++;
 }
 Transform shopsSource(Transform shop){string path=PrefabPath(shop);return sources.First(t=>PrefabPath(t)==path);}
 if(allShops.Any(s=>!Grounded(BoundsOf(s))))throw new System.InvalidOperationException("A shop still lacks ground coverage.");
 var ocean=Copy(water,water.position,water.rotation,water.lossyScale,"Background_Water","Water");var wb=BoundsOf(ocean);float rx=920/wb.size.x,rz=1050/wb.size.z;
 float Factor(Vector3 axis){var d=ocean.transform.rotation*axis;return Mathf.Abs(d.x)*rx+Mathf.Abs(d.z)*rz+Mathf.Abs(d.y);}
 ocean.transform.localScale=Vector3.Scale(ocean.transform.localScale,new Vector3(Factor(Vector3.right),Factor(Vector3.up),Factor(Vector3.forward)));wb=BoundsOf(ocean);ocean.transform.position+=new Vector3(140-wb.center.x,0,-10-wb.center.z);
 foreach(var renderer in ocean.GetComponentsInChildren<Renderer>(true)){
  var original=renderer.sharedMaterial;var material=new Material(original);material.name="SR18 Dense Market Water";if(material.HasProperty("_BaseMap"))material.SetTextureScale("_BaseMap",new Vector2(rx,rz));if(material.HasProperty("_MainTex"))material.SetTextureScale("_MainTex",new Vector2(rx,rz));
  string path=materialFolder+"/Water.mat";UnityEditor.AssetDatabase.CreateAsset(material,path);createdMaterialPaths.Add(path);renderer.sharedMaterial=material;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
 }
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(ocean.transform);
 foreach(var item in objects)if(UnityEditor.PrefabUtility.GetPrefabInstanceStatus(item.Object)!=UnityEditor.PrefabInstanceStatus.Connected)throw new System.InvalidOperationException("Detached prefab: "+item.Object.name);
 if(sourceHash!=Hash(SourcePath)||sourceScene.isDirty!=sourceDirty||RoadSignature()!=roadSignature)throw new System.InvalidOperationException("Source or road layout changed during staging.");
 UnityEditor.Undo.IncrementCurrentGroup();undoGroup=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Apply SR18 417-store market");
 foreach(var item in objects){item.Object.transform.SetParent(null,true);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item.Object,targetScene);UnityEditor.Undo.RegisterCreatedObjectUndo(item.Object,"Add dense market placement");UnityEditor.Undo.SetTransformParent(item.Object.transform,item.Kind=="Water"?targetWater:targetProps,"Organize dense market placement");}
 if(RoadSignature()!=roadSignature||targetProps.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.NoryangjinTurnSpot>(true).Length!=15)throw new System.InvalidOperationException("Road/turn contract changed.");
 UnityEditor.Undo.CollapseUndoOperations(undoGroup);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetScene);
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(targetScene))throw new System.InvalidOperationException("SR18 save failed.");
 transferred=true;
 var report=new{scene=TargetPath,totalShops=417,addedShops=411,sourceVisualPlacements=baseline.Length,groundCount=groundObjects.Count,skippedFloors,supportRepairs,propsCount=targetProps.childCount,waterCount=targetWater.childCount,roads=230,turnSpots=15,sourceSha256=sourceHash,materials=createdMaterialPaths,placements=objects.Select(x=>new{name=x.Object.name,kind=x.Kind,prefab=PrefabPath(x.Object.transform),position=new[]{x.Object.transform.position.x,x.Object.transform.position.y,x.Object.transform.position.z},rotation=new[]{x.Object.transform.eulerAngles.x,x.Object.transform.eulerAngles.y,x.Object.transform.eulerAngles.z},scale=new[]{x.Object.transform.localScale.x,x.Object.transform.localScale.y,x.Object.transform.localScale.z}}).ToArray()};
 var serialize=jsonAssembly.GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)});System.IO.File.WriteAllText("map-concepts/sr18-dense-market-2026-09-05/applied-layout.json",(string)serialize.Invoke(null,new object[]{report}));
 return new{shops=417,props=targetProps.childCount,ground=groundObjects.Count,skippedFloors,supportRepairs,water=targetWater.childCount,saved=!targetScene.isDirty};
}catch{
 if(!transferred&&undoGroup>=0)UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
 if(!transferred)foreach(var item in objects)if(item.Object!=null&&item.Object.scene==targetScene)UnityEngine.Object.DestroyImmediate(item.Object);
 throw;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
