using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.SceneManagement;using UnityEditor;using UnityEditor.SceneManagement;using TMPro;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class InstallChapterFinish{
 const string Art="Assets/ShooterSurvival/Models/Chapters/Chapters45/RetailProduction20261003";
 const string RootName="ChapterFinish_20261003";
 const string Out="outputs/department-store-2026-10-02/trellis-recovery";
 static Dictionary<string,Material> materials=new Dictionary<string,Material>();static List<object> placed;static string revision;static Material wood,ivory,glass,metal,soil;
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 public static object Main(bool save=false){
  RequireIdle();var setup=EditorSceneManager.GetSceneManagerSetup();string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");string folder=Out+"/finish-"+(save?"applied-":"preview-")+stamp;Directory.CreateDirectory(folder);revision=Art+"/Native-"+stamp;Directory.CreateDirectory(revision);AssetDatabase.Refresh();var reports=new List<object>();
  try{
   foreach(string n in new[]{"RetailCounter","RetailShelf","RetailCaseBase","RetailBench","RetailPlanter"})materials[n]=Import(n);
   PrepareMaterials();
   foreach(string name in new[]{"ShoeTower","Jamsil"}){
    string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=EditorSceneManager.OpenScene(path);var d=Object.FindFirstObjectByType<Chapter45Director>();if(d.transform.Find("Scenery/"+RootName)!=null)throw new Exception("Finish already installed; use incremental follow-up");
    string before=ProtectedState(scene);Capture(name,folder+"/"+name+"/before");scene=EditorSceneManager.OpenScene(path);d=Object.FindFirstObjectByType<Chapter45Director>();if(before!=ProtectedState(scene))throw new Exception("Capture altered saved state");
    var root=Group(d.transform.Find("Scenery"),RootName);placed=new List<object>();if(name=="ShoeTower")Tower(d,root);else City(d,root);
    if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Decorative collider");
    if(before!=ProtectedState(scene)){var after=ProtectedState(scene);File.WriteAllText(folder+"/protected-diff.json",Json(new{scene=name,removed=before.Split('\n').Except(after.Split('\n')).ToArray(),added=after.Split('\n').Except(before.Split('\n')).ToArray()}));throw new Exception("Protected components changed before save; inspect "+folder+"/protected-diff.json");}
    foreach(var guid in AssetDatabase.FindAssets("",new[]{revision}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
    if(save){File.Copy(path,folder+"/"+name+"-before.unity",false);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");scene=EditorSceneManager.OpenScene(path);if(before!=ProtectedState(scene))throw new Exception("Protected state changed after reopen");}
    var receipt=new{scene=name,saved=save,protectedStateUnchanged=true,addedColliders=0,instances=placed.ToArray(),revision,scope="Chapter4 crossings/promenade/podium; Chapter5 maintenance/archive/gallery/city depth/rooftop presentation. Merchandise remains separate."};reports.Add(receipt);File.WriteAllText(folder+"/"+name+"/installation.json",Json(receipt));Capture(name,folder+"/"+name+"/after");
   }
   File.WriteAllText(folder+"/installation.json",Json(reports));return folder;
  }finally{EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
 static void PrepareMaterials(){
  wood=NativeMaterial("Warm wood cap",new Color(.40f,.24f,.12f));ivory=NativeMaterial("Porcelain case support",new Color(.88f,.85f,.77f));metal=NativeMaterial("Champagne case frame",new Color(.60f,.49f,.32f));soil=NativeMaterial("Planter soil",new Color(.12f,.085f,.055f));
  var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/Chapters45/DepartmentStore/20261002T170206609/Low tint retail glass.mat");if(source==null)throw new Exception("Approved v5 glass missing");glass=new Material(source){name="Retail case clear glass"};glass.SetColor("_BaseColor",new Color(.84f,.92f,.93f,.075f));glass.SetFloat("_Smoothness",.25f);glass.SetFloat("_SpecularHighlights",0);glass.SetFloat("_EnvironmentReflections",0);glass.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");glass.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");AssetDatabase.CreateAsset(glass,revision+"/Retail case glass.mat");
 }
 static Material NativeMaterial(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.28f);AssetDatabase.CreateAsset(m,revision+"/"+name+".mat");return m;}
 static Material Import(string name){
  string model=Art+"/"+name+".fbx",image=Art+"/"+name+"_BaseColor.png";AssetDatabase.ImportAsset(model,ImportAssetOptions.ForceSynchronousImport);AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceSynchronousImport);
  var mi=(ModelImporter)AssetImporter.GetAtPath(model);mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.importAnimation=false;mi.addCollider=false;mi.isReadable=false;mi.importNormals=ModelImporterNormals.Import;mi.importTangents=ModelImporterTangents.CalculateMikk;mi.globalScale=1;mi.SaveAndReimport();
  var ti=(TextureImporter)AssetImporter.GetAtPath(image);ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.None;ti.mipmapEnabled=true;ti.maxTextureSize=1024;ti.isReadable=false;ti.textureCompression=TextureImporterCompression.CompressedHQ;var a=ti.GetPlatformTextureSettings("Android");a.overridden=true;a.maxTextureSize=1024;a.format=TextureImporterFormat.ASTC_6x6;ti.SetPlatformTextureSettings(a);ti.SaveAndReimport();
  string matPath=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(m!=null)return m;
  var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/RestStopProduction20260925/F10/Surface_0.mat");m=new Material(source){name=name+" reviewed matte",enableInstancing=true};
  foreach(string p in new[]{"_BumpMap","_MetallicGlossMap","_SpecGlossMap"})if(m.HasProperty(p))m.SetTexture(p,null);foreach(string keyword in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","DR_OUTLINE_ON"})m.DisableKeyword(keyword);
  m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(image));m.SetColor("_BaseColor",Color.white);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.15f);m.SetFloat("_SpecularEnabled",0);m.SetFloat("_SpecularHighlights",0);m.SetFloat("_OutlineEnabled",0);m.SetFloat("_Cull",2);AssetDatabase.CreateAsset(m,matPath);return m;
 }
 static Transform Group(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
 static Transform Bay(Transform root,string name,int floor,float from,float to,bool always=false){var t=Group(root,name);var g=t.gameObject.AddComponent<Chapter45SceneryGroup>();g.floor=floor;g.startDistance=from;g.endDistance=to;g.alwaysVisible=always;return t;}
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)throw new Exception("No renderers");var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject Model(Transform parent,string name,Vector3 local,float height,float yaw){
  var holder=Group(parent,name);holder.localPosition=local;holder.localRotation=Quaternion.Euler(0,yaw,0);var src=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+name+".fbx");if(src==null)throw new Exception(name+" source missing");var g=(GameObject)PrefabUtility.InstantiatePrefab(src,holder);
  foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=materials[name];r.shadowCastingMode=ShadowCastingMode.Off;}
  var b=BoundsOf(g);g.transform.localScale*=height/b.size.y;b=BoundsOf(g);g.transform.position+=holder.position-new Vector3(b.center.x,b.min.y,b.center.z);b=BoundsOf(g);
  if(Mathf.Abs(b.size.y-height)>.003f||Mathf.Abs(b.min.y-holder.position.y)>.003f)throw new Exception("Scale/pivot validation: "+name);
  placed.Add(new{name,path=PathOf(holder),height,worldBottom=holder.position.ToString("R"),dimensions=b.size.ToString("R"),source=Art+"/"+name+".fbx"});return holder.gameObject;
 }
 static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return g.transform;}
 static void Shelf(Transform parent,Vector3 p,float yaw,float height){
  var g=Group(parent,"Finished wood shelf");g.localPosition=p;g.localRotation=Quaternion.Euler(0,yaw,0);Model(g,"RetailShelf",Vector3.zero,height,180);
  Box(g,"Closed top timber cap",new Vector3(0,height-.018f,0),new Vector3(height*.76f,.075f,height*.37f),wood);
 }
 static void Case(Transform parent,Vector3 p,float yaw){
  var g=Group(parent,"Hybrid AI wood and native glass case");g.localPosition=p;g.localRotation=Quaternion.Euler(0,yaw,0);
  Box(g,"Case support",new Vector3(0,.38f,0),new Vector3(2.08f,.76f,1.20f),ivory);
  Model(g,"RetailCaseBase",new Vector3(0,.76f,0),.20f,90);
  foreach(float side in new[]{-1f,1f})Box(g,"Flush brass lock plate",new Vector3(0,.855f,side*.72f),new Vector3(.24f,.14f,.025f),metal);
  foreach(float z in new[]{-.67f,.67f})Box(g,"Inner long-edge liner",new Vector3(0,.91f,z),new Vector3(2.24f,.13f,.08f),metal);
  Box(g,"Linen presentation tray",new Vector3(0,.945f,0),new Vector3(2.16f,.025f,1.26f),ivory);
  foreach(float x in new[]{-1.09f,1.09f})Box(g,"Clear glass side",new Vector3(x,1.29f,0),new Vector3(.015f,.66f,1.27f),glass);
  foreach(float z in new[]{-.635f,.635f})Box(g,"Clear glass front",new Vector3(0,1.29f,z),new Vector3(2.18f,.66f,.015f),glass);
  Box(g,"Clear glass lid",new Vector3(0,1.63f,0),new Vector3(2.2f,.015f,1.3f),glass);
  foreach(float x in new[]{-1.1f,1.1f})foreach(float z in new[]{-.65f,.65f})Box(g,"Slender frame upright",new Vector3(x,1.29f,z),new Vector3(.025f,.69f,.025f),metal);
  foreach(float z in new[]{-.65f,.65f})Box(g,"Lid trim",new Vector3(0,1.64f,z),new Vector3(2.22f,.024f,.024f),metal);
 }
 static void Planter(Transform parent,Vector3 p,float yaw){
  var g=Group(parent,"Finished stone planter");g.localPosition=p;g.localRotation=Quaternion.Euler(0,yaw,0);Model(g,"RetailPlanter",Vector3.zero,.72f,0);Box(g,"Recessed planting earth",new Vector3(0,.60f,0),new Vector3(2.20f,.055f,.64f),soil);
  var src=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/polyperfect/Poly Universal Pack/Prefabs/Nature/Trees City/Tree_Japanese_Pagoda_A.prefab");if(src==null)throw new Exception("Verified existing street tree missing");var t=Group(g,"Reused street tree");t.localPosition=new Vector3(0,.61f,0);var model=(GameObject)PrefabUtility.InstantiatePrefab(src,t);foreach(var c in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);foreach(var c in model.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=ShadowCastingMode.Off;var b=BoundsOf(model);model.transform.localScale*=4/b.size.y;b=BoundsOf(model);model.transform.position+=t.position-new Vector3(b.center.x,b.min.y,b.center.z);
 }
 static Material paving,dark,blue,warm;
 static void FinishMaterials(){paving=NativeMaterial("Fine limestone pavers",new Color(.57f,.55f,.48f));dark=NativeMaterial("Dark bronze joints",new Color(.17f,.20f,.20f));blue=NativeMaterial("Facade blue glass",new Color(.18f,.32f,.39f));warm=NativeMaterial("Warm fascia",new Color(.85f,.66f,.37f));}
 static void Sign(Transform p,string text,Vector3 at,Vector2 size,float fontSize){var g=Group(p,"Sign "+text);g.localPosition=at;var t=g.gameObject.AddComponent<TextMeshPro>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");t.text=text;t.fontSize=fontSize;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=size;t.color=new Color(.96f,.91f,.79f);t.textWrappingMode=TextWrappingModes.NoWrap;t.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
 static void BatchNative(Transform p,string label){var fs=p.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<TMP_Text>()==null&&f.sharedMesh!=null&&AssetDatabase.GetAssetPath(f.sharedMesh)=="Library/unity default resources").ToArray();int n=0;foreach(var set in fs.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial)){var mesh=new Mesh{name=label+" "+set.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(set.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=p.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,revision+"/"+label+"-batch-"+(n++)+".asset");var t=Group(p,mesh.name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=set.Key;r.shadowCastingMode=ShadowCastingMode.Off;}foreach(var f in fs){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}}
 static void City(Chapter45Director d,Transform root){
  FinishMaterials();
  foreach(float station in new[]{370f,510,970,1460}){d.route.Sample(station,out var at,out var forward);var g=Bay(root,"Paved crossing "+station,0,station-75,station+75);g.SetPositionAndRotation(at,Quaternion.LookRotation(forward));
   Box(g,"Raised clay crossing",new Vector3(0,.088f,0),new Vector3(60,.025f,18),paving);
   foreach(float z in new[]{-8f,8f}){Box(g,"Crossing border",new Vector3(0,.109f,z),new Vector3(60,.012f,.22f),dark);for(float x=-27;x<=27;x+=3)Box(g,"Pedestrian crossing stripe",new Vector3(x,.122f,z*.65f),new Vector3(1.25f,.014f,3),ivory);}
   for(float x=-29;x<30;x+=4)Box(g,"Stone paving seam",new Vector3(x,.11f,0),new Vector3(.055f,.014f,18),metal);
   BatchNative(g,"Crossing"+station);
  }
  foreach(float station in new[]{620f,820,1020,1220,1590,1725}){d.route.Sample(station,out var at,out var forward);var g=Bay(root,"Promenade room "+station,0,station-50,station+55);g.SetPositionAndRotation(at,Quaternion.LookRotation(forward));float x=station<910?-38:station<1400?-20:station==1590?24:-24;
   Box(g,"Seating paving inset",new Vector3(x,.098f,0),new Vector3(7,.025f,11),paving);
   var b=Model(g,"RetailBench",new Vector3(x,.114f,-1.7f),1.06f,x<0?90:-90);if(Clearance(d,BoundsOf(b),0)<7)throw new Exception("Promenade bench clearance");Planter(g,new Vector3(x,.114f,3),90);BatchNative(g,"Promenade"+station);
  }
  var podium=Bay(root,"Stone tower forecourt",0,1510,1860);d.route.Sample(1685,out var plaza,out var facing);podium.SetPositionAndRotation(plaza,Quaternion.LookRotation(facing));
  foreach(float x in new[]{-11f,11f})Box(podium,"Approach stone band",new Vector3(x,.115f,0),new Vector3(2,.022f,292),paving);
  for(float z=-135;z<=145;z+=10){Box(podium,"Forecourt paving joint",new Vector3(0,.12f,z),new Vector3(92,.012f,.075f),metal);foreach(float x in new[]{-34f,34f})Box(podium,"Side paving panel",new Vector3(x,.102f,z),new Vector3(14,.012f,9.90f),paving);}
  BatchNative(podium,"Forecourt");
  var canonical=d.transform.Find("Scenery/Jamsil_CityPolish_V1/JamsilCanonicalTower");if(canonical==null)throw new Exception("Canonical tower missing");
  var oldNames=new[]{"GroundedEntryPodiumWing","EntranceGlazingWing","EntryMullion","EntryCanopy","PodiumRoofBehindThreshold"};placed.Add(CubeSurgery.Strip(canonical,canonical.GetComponentsInChildren<Transform>(true).Where(t=>oldNames.Contains(t.name)),revision,"Old-entry-overlap"));
  // Existing tapered shaft and rooftop sneaker are untouched. The department-store portal supplies the sole entry.
  var facade=Bay(root,"Retail podium side bays",0,1510,1860);facade.SetPositionAndRotation(canonical.position,canonical.rotation);
  foreach(float side in new[]{-1f,1f})for(int i=0;i<4;i++){float x=side*(18+i*7);Box(facade,"Limestone wing bay",new Vector3(x,6,-19),new Vector3(6.8f,12,10),ivory);Box(facade,"Recessed podium window",new Vector3(x,5.2f,-24.04f),new Vector3(5.7f,8.3f,.10f),blue);Box(facade,"Warm canopy line",new Vector3(x,9.6f,-24.25f),new Vector3(6.5f,.12f,.40f),warm);foreach(float dx in new[]{-2.8f,0,2.8f})Box(facade,"Vertical podium fin",new Vector3(x+dx,5.3f,-24.3f),new Vector3(.13f,8.9f,.48f),metal);Box(facade,"Stone sill",new Vector3(x,.65f,-24.4f),new Vector3(6.2f,.28f,.8f),paving);}
  BatchNative(facade,"PodiumFacade");
  foreach(float station in new[]{370f,970,1460})AddCrossingWalkers(d,root,station);
 }
 static void AddCrossingWalkers(Chapter45Director d,Transform root,float station){d.route.Sample(station,out var at,out var f);var parent=Bay(root,"Crossing pedestrian room "+station,0,station-60,station+40);parent.SetPositionAndRotation(at,Quaternion.LookRotation(f));string[] ids={"A01_dad","A02_mom","A05_student","A06_grandma"};for(int i=0;i<4;i++){var motion=Group(parent,"Lateral walker "+i);motion.localPosition=new Vector3(i%2==0?-5.8f:5.8f,.13f,i<2?-5.5f:5.5f);motion.localRotation=Quaternion.Euler(0,i%2==0?90:-90,0);var holder=Group(motion,"Walker body");var src=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/"+ids[i]+".prefab");var person=Object.Instantiate(src,holder);foreach(var c in person.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);foreach(var c in person.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);var animator=person.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullCompletely;var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name=="walk");clip.SampleAnimation(animator.gameObject,.3f);var b=BoundsOf(person);person.transform.localScale*=2.1f/b.size.y;b=BoundsOf(person);person.transform.position+=holder.position-new Vector3(b.center.x,b.min.y,b.center.z);foreach(var r in person.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;var life=motion.gameObject.AddComponent<Chapter4StreetLife>();life.director=d;life.station=station;life.walkers=new[]{new Chapter4StreetLife.Walker{body=holder,animator=animator,origin=Vector3.zero,phase=i*1.7f,travel=1.1f}};}placed.Add(new{name=parent.name,reusedMeshyPedestrians=4,colliders=0,bodyCentreMinimumRouteOffset=4.7f,motion="Existing pause-aware street-life driver, lateral parent rotation"});}
 static void Tower(Chapter45Director d,Transform root){
  FinishMaterials();
  // Side rooms support the existing maintenance/security choice without changing lanes or rewards.
  var roof=Bay(root,"58F daylight roof and floor finish",1,530,1120,true);
  foreach(float side in new[]{-1f,1f}){Box(roof,"Warm soffit wing",new Vector3(side*59,114.8f,-5),new Vector3(44,.45f,180),ivory);Box(roof,"End soffit",new Vector3(0,114.8f,-5+side*83),new Vector3(74,.45f,14),ivory);for(float z=-79;z<=77;z+=26){Box(roof,"Slender perimeter column",new Vector3(side*74,104.8f,z),new Vector3(.8f,19.6f,.8f),ivory);Box(roof,"Column brass inset",new Vector3(side*73.55f,104.8f,z),new Vector3(.075f,18.6f,.4f),metal);}}
  Box(roof,"Daylight glazing",new Vector3(0,115.1f,-5),new Vector3(73,.06f,152),glass);
  for(float x=-34;x<=34;x+=4)Box(roof,"Timber daylight louver",new Vector3(x,114.65f,-5),new Vector3(.16f,.5f,151),wood);
  for(float z=-72;z<=72;z+=24)Box(roof,"Skylight structural crossbar",new Vector3(0,114.4f,z-5),new Vector3(74,.28f,.25f),metal);
  for(float z=-88;z<=80;z+=8)Box(roof,"Fine upper floor joint",new Vector3(0,95.04f,z),new Vector3(154,.01f,.035f),paving);
  BatchNative(roof,"UpperDaylight");
  foreach(float side in new[]{-1f,1f}){
   var g=Bay(root,side<0?"58F maintenance room":"58F security archive",1,640,1040);g.position=new Vector3(side*42,95.025f,-14);g.rotation=Quaternion.Euler(0,side<0?-90:90,0);
   Box(g,"Inset workroom floor",new Vector3(0,.006f,0),new Vector3(13,.025f,12),paving);Box(g,"Timber back wall",new Vector3(0,2.5f,4.9f),new Vector3(13,5,.28f),side<0?dark:wood);
   Box(g,"Workroom canopy",new Vector3(0,5.12f,1),new Vector3(13.5f,.22f,8.5f),ivory);Box(g,"Recessed ceiling band",new Vector3(0,4.92f,1),new Vector3(12,.06f,6.9f),warm);
   foreach(float x in new[]{-6.35f,6.35f})Box(g,"Room end return",new Vector3(x,2.5f,1),new Vector3(.20f,5,8),wood);
   Sign(g,side<0?"58F  MAINTENANCE":"58F  SECURITY ARCHIVE",new Vector3(0,4.3f,-3.2f),new Vector2(12,1),4.0f);
   if(side>0){foreach(float x in new[]{-4.2f,0,4.2f}){Shelf(g,new Vector3(x,.02f,3.6f),0,2.75f);for(int level=0;level<4;level++)for(int k=0;k<5;k++){Box(g,"Archive binder",new Vector3(x-.66f+k*.30f,.39f+level*.48f,3.6f),new Vector3(.24f,.38f,.30f),k%2==0?ivory:blue);}}}
   else{foreach(float x in new[]{-4.1f,0,4.1f}){Box(g,"Flush service cabinet",new Vector3(x,1.5f,4.25f),new Vector3(3.4f,3,1),blue);Box(g,"Recessed cabinet door",new Vector3(x,1.5f,3.7f),new Vector3(3.1f,2.75f,.07f),dark);for(int j=0;j<5;j++)Box(g,"Vent slat",new Vector3(x,2.1f+j*.12f,3.65f),new Vector3(2.6f,.05f,.08f),metal);Box(g,"Cabinet handle",new Vector3(x+1.25f,1.3f,3.59f),new Vector3(.06f,.45f,.1f),ivory);}}
   var bounds=BoundsOf(g.gameObject);if(Clearance(d,bounds,1)<7)throw new Exception("58F room swept route clearance");placed.Add(new{name=g.name,clearance=Clearance(d,bounds,1)});BatchNative(g,"Room"+(side<0?"Maintenance":"Archive"));
  }
  foreach(float side in new[]{-1f,1f}){var g=Bay(root,"58F lounge "+side,1,580,1090);g.position=new Vector3(side*41,95.035f,34);Box(g,"Lounge stone inset",new Vector3(0,0,0),new Vector3(9,.025f,12),paving);Model(g,"RetailBench",new Vector3(0,.018f,0),1.08f,side<0?90:-90);Planter(g,new Vector3(0,.018f,4.5f),90);if(Clearance(d,BoundsOf(g.gameObject),1)<7)throw new Exception("Lounge clearance");BatchNative(g,"Lounge"+side);}
  foreach(float z in new[]{-34f,2,36}){var g=Bay(root,"58F inner display island "+z,1,650,1040);g.position=new Vector3(0,95.03f,z);Box(g,"Island stone inset",new Vector3(0,0,0),new Vector3(6,.025f,9),paving);Case(g,new Vector3(0,.016f,0),90);Model(g,"RetailBench",new Vector3(0,.016f,3.2f),1.05f,90);if(Clearance(d,BoundsOf(g.gameObject),1)<7)throw new Exception("Inner display island clearance");BatchNative(g,"InnerIsland"+z);}
  foreach(float side in new[]{-1f,1f}){var g=Bay(root,"118F curated gallery "+side,2,1170,1565);g.position=new Vector3(side*37,235.02f,10);g.rotation=Quaternion.Euler(0,side<0?90:-90,0);Box(g,"Gallery inset",new Vector3(0,0,0),new Vector3(10,.025f,13),paving);Case(g,new Vector3(0,.015f,0),0);Model(g,"RetailBench",new Vector3(0,.015f,-4.5f),1.05f,180);Sign(g,"118F  CROWN GALLERY",new Vector3(0,3,4),new Vector2(9,.9f),3.4f);if(Clearance(d,BoundsOf(g.gameObject),2)<7)throw new Exception("Gallery clearance");BatchNative(g,"Gallery"+side);}
  var city=Bay(root,"City depth beyond lift glazing",-1,0,1650,true);
  for(int i=0;i<22;i++){float side=i%2==0?-1:1;float x=i<14?side*(150+(i%3)*52):-75+(i%4)*50;float z=i<14?-155+(i/2)*49:side*(185+(i%3)*25);float h=i<14?32+(i*37%101):116+(i*37%99);float width=23+(i%3)*7;Box(city,"City stone volume",new Vector3(x,h*.5f,z),new Vector3(width,h,28),i%3==0?paving:ivory);foreach(float face in new[]{-1f,1f})for(float yy=5;yy<h-3;yy+=5.4f){Box(city,"Recessed city window band",new Vector3(x,yy,z+face*14.04f),new Vector3(width-2,3.6f,.12f),blue);for(float xx=-width*.5f+4;xx<width*.5f;xx+=5)Box(city,"City glazing mullion",new Vector3(x+xx,yy,z+face*14.15f),new Vector3(.15f,3.7f,.14f),metal);}Box(city,"Mechanical roof cap",new Vector3(x,h+1.1f,z),new Vector3(width*.65f,2.2f,15),dark);}
  BatchNative(city,"LiftCityDepth");
  AddRouteDisplays(d,root,1,new[]{590f,670,750,910,990,1070});AddRouteDisplays(d,root,2,new[]{1180f,1260,1340,1420,1500});
  var original=d.transform.Find("Scenery/InsideTheSneaker/OfferingExhibit").GetComponent<TMP_Text>();string story=original.text;original.gameObject.SetActive(false);
  var plaque=Bay(root,"Offering story plaque",2,1550,1650);Box(plaque,"Small offering plaque",new Vector3(-3.2f,237.25f,45.9f),new Vector3(5.6f,3,.12f),dark);
  var label=Group(plaque,"Readable original offering story");label.position=new Vector3(-3.2f,237.25f,45.45f);var offering=label.gameObject.AddComponent<TextMeshPro>();offering.text=story;offering.fontSize=5.2f;offering.rectTransform.sizeDelta=new Vector2(5.2f,2.8f);offering.alignment=TextAlignmentOptions.Center;offering.textWrappingMode=TextWrappingModes.NoWrap;offering.enableAutoSizing=false;offering.overflowMode=TextOverflowModes.Overflow;offering.color=Color.white;UseOfferingFont(offering);
  placed.Add(new{name="OfferingExhibit",change="Original label retained inactive; fresh visual label carries identical story on small plaque inside sneaker corridor",fontSize=offering.fontSize,position=offering.transform.position.ToString("R")});
 }
 static void AddRouteDisplays(Chapter45Director d,Transform root,int floor,float[] stations){int index=0;foreach(float station in stations){d.route.Sample(station,out var at,out var f);var right=Vector3.Cross(Vector3.up,f);bool installed=false;foreach(float side in new[]{-1f,1f}){var p=at+right*side*(floor==1?20:14);if(Mathf.Abs(p.x)>71||p.z < -85||p.z>75)continue;var test=new Bounds(p+Vector3.up*2,new Vector3(6,4,6));if(Clearance(d,test,floor)<7)continue;bool overlaps=root.GetComponentsInChildren<Chapter45SceneryGroup>(true).Where(g=>g.floor==floor&&!g.name.Contains("roof")&&g.GetComponentsInChildren<Renderer>(true).Length>0).Any(g=>{var b=BoundsOf(g.gameObject);b.Expand(2);return b.Intersects(test);});if(overlaps)continue;
    var g=Bay(root,"Route display "+floor+" "+station,floor,station-70,station+45);g.SetPositionAndRotation(p,Quaternion.LookRotation(right*side));Box(g,"Gallery floor inset",new Vector3(0,.02f,0),new Vector3(5.6f,.04f,4.4f),paving);Box(g,"Gallery timber backing",new Vector3(0,2.1f,1.8f),new Vector3(5.6f,4.2f,.16f),wood);Box(g,"Gallery warm header",new Vector3(0,4.2f,0),new Vector3(5.9f,.18f,4),ivory);Box(g,"Gallery ceiling reveal",new Vector3(0,4.08f,0),new Vector3(4.8f,.06f,3),warm);Case(g,new Vector3(0,.045f,0),0);Sign(g,floor==1?"58F  COLLECTION":"118F  WHITE COLLECTION",new Vector3(0,3.4f,1.65f),new Vector2(5.1f,.7f),2.25f);if(Clearance(d,BoundsOf(g.gameObject),floor)<7)throw new Exception("Route display clearance");BatchNative(g,"RouteDisplay"+floor+"-"+index);placed.Add(new{name=g.name,clearance=Clearance(d,BoundsOf(g.gameObject),floor)});installed=true;index++;break;
   }if(!installed)placed.Add(new{station,floor,omitted="No unoccupied safe pocket; route takes priority"});}}
 static void Capture(string sceneName,string folder){Directory.CreateDirectory(folder);var d=Object.FindFirstObjectByType<Chapter45Director>();var player=Object.FindFirstObjectByType<PlayerScript>();var c=Camera.main;var offset=c.transform.position-player.transform.position;var rot=c.transform.rotation;foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
  foreach(float station in sceneName=="Jamsil"?new[]{230f,370,620,970,1220,1545,1740,1815}:new[]{45f,175,320,600,750,900,1040,1210,1380,1576,1630}){d.route.Sample(station,out var p,out var f);int floor=sceneName=="Jamsil"?0:station<530?0:station<1120?1:2;foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible(g.floor<0||g.floor==floor);var q=Quaternion.LookRotation(f);player.transform.SetPositionAndRotation(p+Vector3.up*d.footOffset,q);c.transform.SetPositionAndRotation(player.transform.position+q*offset,q*rot);Shot(c,folder+"/route-"+station.ToString("0000")+".png");}
  if(sceneName=="ShoeTower"){foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible(g.floor<0||g.floor==1);c.transform.position=new Vector3(20,101,-14);c.transform.LookAt(new Vector3(42,97,-14));Shot(c,folder+"/archive-detail.png");c.transform.position=new Vector3(-20,101,-14);c.transform.LookAt(new Vector3(-42,97,-14));Shot(c,folder+"/maintenance-detail.png");}File.WriteAllText(folder+"/conditions.json",Json(new{width=720,height=1280,uiHidden=true,kind="Native editor render; performance requires later live play"}));
 }

 static void UseOfferingFont(TMP_Text text){var path=AssetDatabase.FindAssets("GmarketSansTTFBold t:Font").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>p.EndsWith(".ttf"));if(path==null)throw new Exception("Local Korean source font missing");var source=AssetDatabase.LoadAssetAtPath<Font>(path);var f=TMP_FontAsset.CreateFontAsset(source,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);f.name="Offering Korean complete glyphs";AssetDatabase.CreateAsset(f,revision+"/OfferingReadableFont.asset");if(!f.TryAddCharacters(text.text,out var missing))throw new Exception("Missing offering glyphs "+missing);foreach(var atlas in f.atlasTextures){atlas.name="Offering glyph atlas";AssetDatabase.AddObjectToAsset(atlas,f);}AssetDatabase.AddObjectToAsset(f.material,f);f.atlasPopulationMode=AtlasPopulationMode.Static;text.font=f;text.fontSharedMaterial=f.material;text.ForceMeshUpdate(true,true);placed.Add(new{name="Offering task-owned static font",originalFontsModified=false,missingCharacters=missing});}

 static float Clearance(Chapter45Director d,Bounds b,int floor){float min=float.PositiveInfinity;float station=0;var choice=d.GetComponentsInChildren<Chapter45Choice>(true).FirstOrDefault(c=>c.kind==Chapter45Choice.ChoiceKind.RouteFork);foreach(var s in d.route.segments){if(s.floor==floor){int count=Mathf.Max(1,Mathf.CeilToInt(s.Length*2));for(int i=0;i<=count;i++){float t=i/(float)count;var p=Vector3.Lerp(s.start,s.end,t);var right=Vector3.Cross(Vector3.up,(s.end-s.start).normalized);foreach(int branch in new[]{0,1}){var v=p+right*(choice==null?0:choice.Offset(station+s.Length*t,branch));v.y=b.center.y;min=Mathf.Min(min,Vector3.Distance(v,b.ClosestPoint(v)));}}}station+=s.Length;}return min;}
 // Preservation and rendering helpers are copied from the already verified plant installer.
  static void Shot(Camera c,string path){var rt=RenderTexture.GetTemporary(720,1280,24,RenderTextureFormat.ARGB32);var target=c.targetTexture;var active=RenderTexture.active;float aspect=c.aspect;try{c.targetTexture=rt;c.aspect=720f/1280;c.Render();RenderTexture.active=rt;var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);}finally{c.targetTexture=target;c.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}}

 static void RequireIdle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Idle editor required.");
        for (int i=0;i<SceneManager.sceneCount;i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene present.");
    }
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    static string ProtectedState(Scene scene)
    {
        var records = new List<string>();
        foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
        {
            if (c == null || PathOf(c.transform).Contains(RootName)) continue;
            bool protect = c is Collider || c is Rigidbody || c is Camera || c is Light || c is MonoBehaviour && !(c is TMP_Text) && !(c is TMP_SubMesh) && !(c is TMP_SubMeshUI);
            if (!protect) continue;
            // Serialized object references use stable GlobalObjectId, not session instance IDs.
            var s = new SerializedObject(c); var it = s.GetIterator();
            var fields = new List<string>();
            bool descend=true;
            while (it.Next(descend))
            {
                // Object references expose an internal session m_FileID child.
                // Compare the stable reference above, never descend into its storage.
                descend=it.propertyType==SerializedPropertyType.Generic;
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                    fields.Add(it.propertyPath + "=" + (it.objectReferenceValue == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(it.objectReferenceValue).ToString()));
                else if (it.propertyType != SerializedPropertyType.Generic)
                    fields.Add(it.propertyPath + "=" + it.type + ":" + Scalar(it));
            }
            records.Add(PathOf(c.transform)+":"+c.GetType().FullName+":"+c.transform.localToWorldMatrix.ToString("R")+":"+string.Join("|",fields));
        }
        return string.Join("\n", records.OrderBy(r=>r,StringComparer.Ordinal));
    }
    static string Scalar(SerializedProperty p)
    {
        switch(p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.longValue.ToString();
            case SerializedPropertyType.Boolean: return p.boolValue.ToString();
            case SerializedPropertyType.Float: return p.doubleValue.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            case SerializedPropertyType.String: return p.stringValue;
            case SerializedPropertyType.Enum: return p.intValue.ToString();
            case SerializedPropertyType.ArraySize: return p.intValue.ToString();
            case SerializedPropertyType.Color: return p.colorValue.ToString("R");
            case SerializedPropertyType.Vector2: return p.vector2Value.ToString("R");
            case SerializedPropertyType.Vector3: return p.vector3Value.ToString("R");
            case SerializedPropertyType.Vector4: return p.vector4Value.ToString("R");
            case SerializedPropertyType.Quaternion: return p.quaternionValue.ToString("R");
            default: return p.propertyType.ToString();
        }
    }



}

// Remove only matched primitive triangles from copied, combined decorative meshes.
// Original asset files and transform markers remain intact.
static class CubeSurgery {
 sealed class Face {public MeshFilter filter;public int sub,index;public Vector3 a,b,c,center;}
 static string Key(Vector3 p)=>Mathf.FloorToInt(p.x*100)+","+Mathf.FloorToInt(p.y*100)+","+Mathf.FloorToInt(p.z*100);
 public static object Strip(Transform scope,IEnumerable<Transform> candidates,string assetFolder,string label){
  var targets=candidates.Distinct().ToArray();if(targets.Length==0)return new{label,boxes=0,triangles=0};
  var grid=new Dictionary<string,List<Face>>();var filters=scope.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).ToArray();
  foreach(var f in filters){var m=f.sharedMesh;var v=m.vertices.Select(f.transform.TransformPoint).ToArray();for(int s=0;s<m.subMeshCount;s++){var t=m.GetTriangles(s);for(int i=0;i<t.Length;i+=3){var face=new Face{filter=f,sub=s,index=i,a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]]};face.center=(face.a+face.b+face.c)/3;string k=Key(face.center);if(!grid.ContainsKey(k))grid[k]=new List<Face>();grid[k].Add(face);}}}
  var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var cm=cube.GetComponent<MeshFilter>().sharedMesh;var cv=cm.vertices;var ct=cm.triangles;Object.DestroyImmediate(cube);
  var removals=new Dictionary<MeshFilter,Dictionary<int,HashSet<int>>>();var consumed=new HashSet<Face>();
  foreach(var target in targets){var w=cv.Select(target.TransformPoint).ToArray();int matched=0;for(int i=0;i<ct.Length;i+=3){var a=w[ct[i]];var b=w[ct[i+1]];var c=w[ct[i+2]];var center=(a+b+c)/3;Face found=null;
    for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++){if(!grid.TryGetValue(Key(center+new Vector3(x,y,z)*.01f),out var options))continue;foreach(var face in options){if(consumed.Contains(face)||(face.center-center).sqrMagnitude>.000025f)continue;var q=new[]{face.a,face.b,face.c};if(new[]{a,b,c}.All(p=>q.Any(v=>(v-p).sqrMagnitude<.000025f))){if(found!=null&&found!=face)throw new Exception("Ambiguous cube triangle: "+target.name);found=face;}}}
    if(found==null)throw new Exception("Missing cube triangle "+i+" for "+target.name+" at "+target.position);
    consumed.Add(found);if(!removals.ContainsKey(found.filter))removals[found.filter]=new Dictionary<int,HashSet<int>>();var parts=removals[found.filter];if(!parts.ContainsKey(found.sub))parts[found.sub]=new HashSet<int>();parts[found.sub].Add(found.index);matched++;
   }if(matched!=12)throw new Exception("Expected 12 cube triangles");
  }
  int index=0;foreach(var item in removals){var copy=Object.Instantiate(item.Key.sharedMesh);copy.name=label+" retained architecture "+index;foreach(var part in item.Value){var t=copy.GetTriangles(part.Key);var kept=new List<int>();for(int i=0;i<t.Length;i+=3)if(!part.Value.Contains(i)){kept.Add(t[i]);kept.Add(t[i+1]);kept.Add(t[i+2]);}copy.SetTriangles(kept,part.Key);}copy.RecalculateBounds();AssetDatabase.CreateAsset(copy,assetFolder+"/"+label+"-"+(index++)+".asset");item.Key.sharedMesh=copy;}
  return new{label,boxes=targets.Length,triangles=consumed.Count,copiedMeshes=removals.Count,originalAssetsUnchanged=true};
 }
}
