using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.SceneManagement;using UnityEditor;using UnityEditor.SceneManagement;using TMPro;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class FinishVisibleEnvironment{
 const string Art="Assets/ShooterSurvival/Models/Chapters/Chapters45/RetailProduction20261003";
 const string RootName="VisibleFinish_20261003";
 const string Out="outputs/department-store-2026-10-02/trellis-recovery";
 static Dictionary<string,Material> materials=new Dictionary<string,Material>();static List<object> placed;static string revision;static Material glass;
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 public static object Main(bool save=false,string only=""){
  RequireIdle();var setup=EditorSceneManager.GetSceneManagerSetup();string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");string folder=Out+"/visible-finish-"+(save?"applied-":"preview-")+stamp;Directory.CreateDirectory(folder);AssetDatabase.Refresh();var reports=new List<object>();
  try{
   foreach(string name in new[]{"ShoeTower","Jamsil"}.Where(n=>only==""||n==only)){
    revision=Art+"/VisibleFinish-"+stamp+"-"+name;Directory.CreateDirectory(revision);AssetDatabase.Refresh();
    string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=EditorSceneManager.OpenScene(path);var d=Object.FindFirstObjectByType<Chapter45Director>();var environment=d.transform.Find("Scenery/EnvironmentArt_20261003");if(environment==null||environment.Find(RootName)!=null)throw new Exception("Expected prior environment without this finish");
    string before=ProtectedState(scene);Capture(name,folder+"/"+name+"/before");scene=EditorSceneManager.OpenScene(path);d=Object.FindFirstObjectByType<Chapter45Director>();if(before!=ProtectedState(scene))throw new Exception("Capture altered saved state");environment=d.transform.Find("Scenery/EnvironmentArt_20261003");
    var root=Group(environment,RootName);placed=new List<object>();ExistingMaterials(environment);if(name=="ShoeTower")FinishMall(d,environment,root);else FinishCity(d,environment,root);
    if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Decorative collider");
    if(before!=ProtectedState(scene))throw new Exception("Protected components changed before save");
    var stats=new{triangles=environment.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh==null?0:f.sharedMesh.triangles.Length/3),renderers=environment.GetComponentsInChildren<Renderer>(true).Length,groups=environment.GetComponentsInChildren<Chapter45SceneryGroup>(true).Length,addedRootTriangles=root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh==null?0:f.sharedMesh.triangles.Length/3)};
    foreach(var guid in AssetDatabase.FindAssets("",new[]{revision}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
    if(save){File.Copy(path,folder+"/"+name+"-before.unity",false);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");scene=EditorSceneManager.OpenScene(path);if(before!=ProtectedState(scene))throw new Exception("Protected state changed after reopen");}
    var receipt=new{scene=name,saved=save,protectedStateUnchanged=true,addedColliders=0,instances=placed.ToArray(),revision,stats,shaderCompiledAndSupported=true,scope="Bounded visible finish: two Ch4 frontages and inexpensive glazing; four lower mall rooms and six upper cells with existing goods redistributed. No paid generation."};reports.Add(receipt);File.WriteAllText(folder+"/"+name+"/installation.json",Json(receipt));Capture(name,folder+"/"+name+"/after");
   }
   File.WriteAllText(folder+"/installation.json",Json(reports));return folder;
  }finally{EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }

 static Material NativeMaterial(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.28f);AssetDatabase.CreateAsset(m,revision+"/"+name+".mat");return m;}

 static Transform Group(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
 static Transform Bay(Transform root,string name,int floor,float from,float to,bool always=false){var t=Group(root,name);var g=t.gameObject.AddComponent<Chapter45SceneryGroup>();g.floor=floor;g.startDistance=from;g.endDistance=to;g.alwaysVisible=always;return t;}
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)throw new Exception("No renderers");var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject Model(Transform parent,string name,Vector3 local,float height,float yaw){
  var holder=Group(parent,name);holder.localPosition=local;holder.localRotation=Quaternion.Euler(0,yaw,0);var src=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+name+".fbx");if(src==null)throw new Exception(name+" source missing");var g=Object.Instantiate(src,holder);
  foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=materials[name];r.shadowCastingMode=ShadowCastingMode.Off;}
  var b=BoundsOf(g);g.transform.localScale*=height/b.size.y;b=BoundsOf(g);g.transform.position+=holder.position-new Vector3(b.center.x,b.min.y,b.center.z);b=BoundsOf(g);
  if(Mathf.Abs(b.size.y-height)>.003f||Mathf.Abs(b.min.y-holder.position.y)>.003f)throw new Exception("Scale/pivot validation: "+name);
  placed.Add(new{name,path=PathOf(holder),height,worldBottom=holder.position.ToString("R"),dimensions=b.size.ToString("R"),source=Art+"/"+name+".fbx"});return holder.gameObject;
 }
 static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return g.transform;}



 static void BatchGoods(Transform p,string label){label=SceneManager.GetActiveScene().name+"-"+label;var fs=p.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.GetComponent<TMP_Text>()==null).ToArray();int n=0;foreach(var set in fs.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial)){var mesh=new Mesh{name=label+" "+set.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(set.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=p.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,revision+"/"+label+"-goods-"+(n++)+".asset");var t=Group(p,mesh.name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=set.Key;r.shadowCastingMode=ShadowCastingMode.Off;}foreach(var f in fs){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}}
 static void Align(Transform target,Transform source){target.SetPositionAndRotation(source.position,source.rotation);}




 static void Capture(string sceneName,string folder){Directory.CreateDirectory(folder);var d=Object.FindFirstObjectByType<Chapter45Director>();var player=Object.FindFirstObjectByType<PlayerScript>();var c=Camera.main;var offset=c.transform.position-player.transform.position;var rot=c.transform.rotation;foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
  foreach(float station in sceneName=="Jamsil"?new[]{140f,230,275,310,370,1050,1220,1545,1800,1815}:new[]{0f,35,70,175,238,320,465,710,770,950,1380,1630}){d.route.Sample(station,out var p,out var f);int floor=sceneName=="Jamsil"?0:station<530?0:station<1120?1:2;foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible((g.floor<0||g.floor==floor)&&(g.alwaysVisible||g.floor<0&&g.endDistance<=g.startDistance||station>=g.startDistance-100&&station<=g.endDistance+100));var q=Quaternion.LookRotation(f);player.transform.SetPositionAndRotation(p+Vector3.up*d.footOffset,q);c.transform.SetPositionAndRotation(player.transform.position+q*offset,q*rot);Shot(c,folder+"/route-"+station.ToString("0000")+".png");}
  foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible(g.floor<0||g.floor==0);
  if(sceneName=="ShoeTower"){c.transform.position=new Vector3(-49,3.4f,21);c.transform.LookAt(new Vector3(-43,1.1f,15));Shot(c,folder+"/glass-case.png");c.transform.position=new Vector3(-68.5f,3.5f,-70.4f);c.transform.LookAt(new Vector3(-75.65f,1.6f,-70.4f));Shot(c,folder+"/shelf-goods.png");}
  else{var shop=d.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Shop bay 0");c.transform.position=shop.TransformPoint(new Vector3(-2,3.3f,-6));c.transform.LookAt(shop.TransformPoint(new Vector3(-2,1.8f,-.3f)));Shot(c,folder+"/street-window.png");}if(sceneName=="ShoeTower"){
 var shops=d.transform.Find("Scenery/DepartmentStore_20261002").GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Shop bay ")).ToArray();var shop=shops[23];c.transform.position=shop.TransformPoint(new Vector3(-5,3.8f,-12));c.transform.LookAt(shop.TransformPoint(new Vector3(-1,2.5f,1)));Shot(c,folder+"/boutique-23.png");c.transform.position=new Vector3(-17,9,-72);c.transform.LookAt(new Vector3(-17,9.5f,-92));Shot(c,folder+"/upper-six.png");
 }else{var shops=d.transform.Find("Scenery/Jamsil_ReferenceStreet_20261002").GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Shop bay ")).ToArray();foreach(int i in new[]{22,23,27,28}){var shop=shops[i];c.transform.position=shop.TransformPoint(new Vector3(-10,6,-18));c.transform.LookAt(shop.TransformPoint(new Vector3(0,6.4f,1)));Shot(c,folder+"/frontage-"+i+".png");}}
 File.WriteAllText(folder+"/conditions.json",Json(new{width=720,height=1280,uiHidden=true,kind="Native editor render; director floor/distance visibility rule; UI hidden. Actual play separately verifies movement/performance"}));
 }

 static Material charcoal,clay,sand,ivory2,teal2,window,windowLight,warm2,walnut,shade,tileJoint,boutiqueGlow;
 static void PrepareMaterials(){
  charcoal=NativeMaterial("Graphite reveals",new Color(.075f,.10f,.12f));clay=NativeMaterial("Oxide brick",new Color(.43f,.22f,.14f));sand=NativeMaterial("Sandstone",new Color(.69f,.62f,.48f));ivory2=NativeMaterial("Pearl stone",new Color(.88f,.86f,.79f));teal2=NativeMaterial("Sage mineral plaster",new Color(.22f,.34f,.30f));window=NativeMaterial("Blue grey glazing",new Color(.18f,.33f,.39f));windowLight=NativeMaterial("Sky reflection panes",new Color(.40f,.57f,.61f));walnut=NativeMaterial("Walnut shadow",new Color(.24f,.14f,.09f));shade=NativeMaterial("Warm interior shadow",new Color(.37f,.32f,.25f));tileJoint=NativeMaterial("Fine warm tile joints",new Color(.83f,.81f,.75f));warm2=NativeMaterial("Warm light diffuser",new Color(.99f,.81f,.52f));warm2.EnableKeyword("_EMISSION");warm2.SetColor("_EmissionColor",new Color(.35f,.19f,.065f));
  boutiqueGlow=NativeMaterial("Selected warm boutique backlight",new Color(.96f,.78f,.48f));boutiqueGlow.EnableKeyword("_EMISSION");boutiqueGlow.SetColor("_EmissionColor",new Color(.34f,.20f,.075f));
  var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/Chapters45/DepartmentStore/20261002T170206609/Low tint retail glass.mat");glass=new Material(source){name="Architecture clear balustrade"};glass.SetColor("_BaseColor",new Color(.65f,.82f,.85f,.15f));glass.SetFloat("_Smoothness",.3f);AssetDatabase.CreateAsset(glass,revision+"/Clear railing.mat");
 }
 static void Beam(Transform p,string n,Vector3 a,Vector3 b,float width,Material m){var t=Box(p,n,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),m);t.localRotation=Quaternion.LookRotation(b-a);}
 static void City(Chapter45Director d,Transform root){
  var street=d.transform.Find("Scenery/Jamsil_ReferenceStreet_20261002");var stripNames=new[]{"Building mass","Recess dark back","Window reflection","Display warm panel","Upper window","Upper sill","Upper sash","Roof trim","Distant retail mass","Distant window band"};
  placed.Add(CubeSurgery.Strip(street,street.GetComponentsInChildren<Transform>(true).Where(t=>stripNames.Contains(t.name)).ToArray(),revision,"City-shallow-shells"));
  var all=Bay(root,"Continuous varied street architecture",0,0,1900,true);int i=0;
  foreach(var shop in street.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Shop bay ")).ToArray()){
   var g=Group(all,"Facade family "+i);Align(g,shop);int type=(i/2+i%5)%5;int floors=new[]{2,3,4,3,5}[type];float h=5.1f+(floors-1)*3.6f;var skin=type==0?teal2:type==1?ivory2:type==2?clay:type==3?sand:charcoal;
   Box(g,"Ground shop rear at3.5m",new Vector3(0,2.55f,3.5f),new Vector3(12.6f,5.1f,.3f),shade);
   foreach(float x in new[]{-6.22f,6.22f})Box(g,"Deep side return",new Vector3(x,2.6f,1.7f),new Vector3(.25f,5.2f,3.8f),skin);
   Box(g,"Interior timber floor",new Vector3(0,.29f,1.7f),new Vector3(12.3f,.08f,3.8f),walnut);
   Box(g,"Interior ceiling shadow",new Vector3(0,4.03f,1.8f),new Vector3(12.3f,.09f,3.5f),shade);
   foreach(float x in new[]{-4f,0,4f}){Box(g,"Recessed room light",new Vector3(x,3.85f,2.1f),new Vector3(2.5f,.08f,.8f),warm2);Box(g,"Rear joinery",new Vector3(x,2.2f,3.28f),new Vector3(2.8f,3.0f,.12f),type%2==0?walnut:sand);for(int tier=0;tier<2;tier++)Box(g,"Depth shelf",new Vector3(x,1.25f+tier*1.0f,2.85f),new Vector3(2.7f,.10f,.75f),ivory2);}
   Box(g,"Upper solid wall",new Vector3(0,(h+5.1f)*.5f,3.0f),new Vector3(12.6f,h-5.1f,6.0f),skin);
   bool curtain=type==2||type==4;
   for(int level=0;level<floors-1;level++){float y=6.65f+level*3.6f;int count=curtain?4:3;float spacing=curtain?3.0f:3.8f;
    for(int k=0;k<count;k++){float x=(k-(count-1)*.5f)*spacing;float w=curtain?2.86f:2.55f;
     Box(g,"Inset window pocket",new Vector3(x,y,-.12f),new Vector3(w+.25f,2.65f,.2f),charcoal);Box(g,"Glass pane",new Vector3(x,y,-.235f),new Vector3(w,2.4f,.05f),(k+level+i)%4==0?windowLight:window);Box(g,"Sill depth",new Vector3(x,y-1.32f,-.38f),new Vector3(w+.4f,.12f,.6f),curtain?clay:ivory2);Box(g,"Window central bar",new Vector3(x,y,-.29f),new Vector3(.07f,2.4f,.09f),curtain?clay:charcoal);
    }
    if(curtain)Box(g,"Exposed steel floor edge",new Vector3(0,y-1.6f,-.30f),new Vector3(12.7f,.24f,.5f),clay);
   }
   foreach(float x in new[]{-6.15f,6.15f})Box(g,"Facade corner pilaster",new Vector3(x,(h+5)*.5f,-.28f),new Vector3(curtain?.28f:.38f,h-5,.44f),curtain?clay:sand);
   Box(g,"Roof cap shadow",new Vector3(0,h+.07f,2.8f),new Vector3(12.85f,.14f,6.5f),charcoal);Box(g,"Roof cornice",new Vector3(0,h+.23f,2.8f),new Vector3(12.8f,.18f,6.4f),skin);
   if(type==4){Box(g,"Setback upper pavilion",new Vector3(0,h+1.4f,3.7f),new Vector3(8.5f,2.5f,3),window);Box(g,"Pavilion cap",new Vector3(0,h+2.75f,3.7f),new Vector3(9,.2f,3.5f),charcoal);}
   i++;
  }
  BatchGoods(all,"City-facades");placed.Add(new{facades=i,storeyFamilies="2,3,4,3,5",roomDepth=3.5});
  SkylineDetail(d,root);TowerCurtain(d,root);EntranceDepth(d,root);
 }
 static void SkylineDetail(Chapter45Director d,Transform root){
  var g=Bay(root,"Midground office facade scale",0,0,1900,true);var old=d.transform.Find("Scenery/Jamsil_CityPolish_V1/SetbackSkyline");int i=0;
  foreach(var office in old.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("SetbackOffice_")).ToArray()){
   var b=office.Find("GroundedOfficeBody");float w=b.localScale.x,h=b.localScale.y,z=b.localScale.z;var block=Group(g,office.name);Align(block,office);
   foreach(float yaw in new[]{0f,90f,180f,270f}){var face=Group(block,"Articulated elevation");face.localRotation=Quaternion.Euler(0,yaw,0);float width=yaw%180==0?w:z,depth=yaw%180==0?z:w;
    for(float y=8;y<h-1;y+=3.5f){Box(face,"Recessed horizontal glazing",new Vector3(0,y,-depth*.5f-.13f),new Vector3(width-.9f,2.55f,.12f),(i+(int)y)%4==0?windowLight:window);Box(face,"Stone spandrel edge",new Vector3(0,y-1.5f,-depth*.5f-.25f),new Vector3(width,.17f,.35f),i%2==0?sand:ivory2);}
    for(float x=-width*.5f+2;x<width*.5f;x+=3)Box(face,"Slender vertical window fin",new Vector3(x,(h+5)*.5f,-depth*.5f-.25f),new Vector3(.12f,h-5,.28f),i%3==0?clay:charcoal);
   }i++;
  }BatchGoods(g,"Office-facades");placed.Add(new{officeFacades=i});
 }
 static Vector3 TowerPoint(float y,int side,float fraction){float t=(y-12)/378f;float f=t*13;int lo=Mathf.Clamp(Mathf.FloorToInt(f),0,12);float ra=Mathf.Lerp(35,8,Mathf.Pow(lo/13f,.83f)),rb=Mathf.Lerp(35,8,Mathf.Pow((lo+1)/13f,.83f));float r=Mathf.Lerp(ra,rb,f-lo)+.24f;float a=side*Mathf.PI*2/16,b=(side+1)*Mathf.PI*2/16;return Vector3.Lerp(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r),new Vector3(Mathf.Cos(b)*r,y,Mathf.Sin(b)*r),fraction);}
 static void TowerCurtain(Chapter45Director d,Transform root){var g=Bay(root,"Canonical tower glazing divisions",0,1590,1900,false);g.localPosition=new Vector3(0,0,1880);
  for(int side=0;side<16;side++){
   for(int k=0;k<4;k++)for(int r=0;r<13;r++)Beam(g,"Fine continuous curtainwall mullion",TowerPoint(12+r*378/13f,side,k/4f),TowerPoint(12+(r+1)*378/13f,side,k/4f),.16f,windowLight);
   for(float y=16;y<389;y+=4.2f)Beam(g,"Human storey spandrel",TowerPoint(y,side,0),TowerPoint(y,side,1),.11f,windowLight);
  }BatchGoods(g,"Tower-curtainwall");
 }
 static void EntranceDepth(Chapter45Director d,Transform root){var g=Bay(root,"Department store layered entrance",0,1500,1900,true);g.localPosition=new Vector3(0,0,1837);
  foreach(float side in new[]{-1f,1f}){Box(g,"Deep entrance pier",new Vector3(side*12,5.4f,1),new Vector3(1.15f,10.8f,7),sand);Box(g,"Pier inset bronze",new Vector3(side*11.38f,5.4f,-2.2f),new Vector3(.15f,9.6f,.2f),walnut);
   for(int j=0;j<4;j++){float x=side*(16+j*4.3f);Box(g,"Upper podium glazing",new Vector3(x,14,3.8f),new Vector3(3.8f,6.2f,.18f),j%2==0?windowLight:window);Box(g,"Podium vertical bronze fin",new Vector3(x-2.0f,14,3.5f),new Vector3(.2f,7.1f,.8f),walnut);}
   Box(g,"Podium upper horizontal ledge",new Vector3(side*23,10.6f,3),new Vector3(24,.65f,5.8f),ivory2);Box(g,"Podium roof shadow",new Vector3(side*23,17.4f,3),new Vector3(24,.4f,5.8f),charcoal);
  }
  Box(g,"Layered entrance canopy",new Vector3(0,10.5f,-2.6f),new Vector3(25,.32f,7.6f),charcoal);Box(g,"Recessed luminous entry soffit",new Vector3(0,10.30f,-2.5f),new Vector3(22,.06f,6),warm2);for(int x=-10;x<=10;x+=2)Box(g,"Canopy bronze slat",new Vector3(x,10.22f,-2.5f),new Vector3(.08f,.14f,6),walnut);Box(g,"Visible vestibule floor",new Vector3(0,.048f,21),new Vector3(17,.02f,29),ivory2);
  foreach(float side in new[]{-1f,1f}){Box(g,"Vestibule side glazing",new Vector3(side*8.4f,4.4f,22),new Vector3(.13f,8.8f,24),window);for(int z=12;z<=32;z+=5)Box(g,"Vestibule luminous pier",new Vector3(side*8.15f,4.4f,z),new Vector3(.16f,8.1f,.16f),warm2);}
  Box(g,"Vestibule distant interior",new Vector3(0,4.4f,34),new Vector3(17,8.8f,.2f),shade);Box(g,"Vestibule warm welcome wall",new Vector3(0,4.5f,33.84f),new Vector3(10,4.8f,.1f),boutiqueGlow);for(int z=12;z<=30;z+=6)Box(g,"Vestibule ceiling light",new Vector3(0,9.2f,z),new Vector3(15,.08f,.35f),warm2);
  BatchGoods(g,"Layered-entry");
 }
 static void Tower(Chapter45Director d,Transform root){
  var podium=Bay(root,"Connected upper department galleries",0,0,530,true);
  foreach(float z in new[]{75f,-88.5f}){var wall=Group(podium,"Continuous endwall galleries");wall.localPosition=new Vector3(0,0,z);wall.localRotation=Quaternion.Euler(0,z>0?0:180,0);GalleryWall(wall,152);}
  foreach(float side in new[]{-1f,1f}){var wall=Group(podium,"Continuous lateral galleries");wall.localPosition=new Vector3(side*73.5f,0,-5);wall.localRotation=Quaternion.Euler(0,side<0?-90:90,0);GalleryWall(wall,168);}
  BatchGoods(podium,"Connected-galleries");
  var upperGoods=Bay(root,"Reviewed goods in upper gallery boutiques",0,0,530,true);materials["RetailVase"]=AssetDatabase.LoadAssetAtPath<Material>(Art+"/RetailVase.mat");materials["RetailTote"]=AssetDatabase.LoadAssetAtPath<Material>(Art+"/RetailTote.mat");int product=0;
  foreach(float z in new[]{75f,-88.5f})foreach(int level in new[]{1,2})for(float x=-72;x<=72;x+=32){var shop=Group(upperGoods,"Open boutique "+product);shop.localPosition=new Vector3(x,level*6.4f,z);shop.localRotation=Quaternion.Euler(0,z>0?0:180,0);Model(shop,product%2==0?"RetailVase":"RetailTote",new Vector3(0,1.40f,4.08f),1.20f,product%2==0?0:180);product++;}
  foreach(float z in new[]{68f,-80f})foreach(float x in new[]{-53f,0,53}){var lamp=Group(root,"Gallery warm wall wash");lamp.position=new Vector3(x,10,z);lamp.rotation=Quaternion.LookRotation(new Vector3(0,-.1f,z>0?1:-1));var l=lamp.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.range=24;l.spotAngle=100;l.innerSpotAngle=70;l.intensity=28;l.color=new Color(1,.79f,.56f);l.shadows=LightShadows.None;}

  var floor=Bay(root,"Human scale porcelain and retail zones",0,0,530,true);
  for(float x=-78;x<=78;x+=3f)Box(floor,"Subtle3m tile joint",new Vector3(x,.033f,-5),new Vector3(.022f,.006f,178),tileJoint);
  for(float z=-89;z<=79;z+=3f)Box(floor,"Subtle3m tile joint",new Vector3(0,.034f,z),new Vector3(158,.006f,.022f),tileJoint);
  foreach(float x in new[]{-40f,0,40}){
   Ring(floor,"Curved dark stone zoning",new Vector3(x,.036f,-13),14.9f,31,1.1f,.015f,sand);
   for(int level=1;level<=2;level++){float y=level*6.4f;Ring(floor,"Connected curved gallery fascia",new Vector3(x,y-.15f,-13),12.15f,27.15f,.36f,.85f,ivory2);Ring(floor,"Recess below fascia",new Vector3(x,y-.24f,-13),12.0f,27.0f,.14f,.065f,walnut);}
  }
  BatchGoods(floor,"Mall-surface-scale");
  var store=d.transform.Find("Scenery/DepartmentStore_20261002");var trim=Bay(root,"Retail ceiling light and depth",0,0,530,true);
  foreach(var shop in store.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Shop bay ")).ToArray()){
   var g=Group(trim,"Shop ceiling finish");Align(g,shop);Box(g,"Dark recessed soffit",new Vector3(0,5.55f,.55f),new Vector3(12.6f,.08f,4.5f),shade);foreach(float x in new[]{-5f,-2.5f,0,2.5f,5})Box(g,"Individual linear shop luminaire",new Vector3(x,5.47f,1.2f),new Vector3(.10f,.07f,2.7f),warm2);
   foreach(float x in new[]{-6.1f,6.1f})Box(g,"Deep portal reveals",new Vector3(x,2.6f,.65f),new Vector3(.12f,5.1f,4.3f),walnut);
  }BatchGoods(trim,"Retail-light-depth");placed.Add(new{continuousGalleryLevels=2,storeyHeight=6.4,existingAtriums=3,existingEscalators=6,tileMetres=3,actualLiftsUnchanged=2});
  UpperFloors(d,root);
 }
 static void GalleryWall(Transform p,float width){
  for(int level=1;level<=2;level++){float y=level*6.4f;
   Box(p,"Gallery floor depth",new Vector3(0,y+.1f,2.7f),new Vector3(width,.32f,6.3f),ivory2);Box(p,"Continuous rounded fascia",new Vector3(0,y+.05f,-.65f),new Vector3(width,.75f,.36f),ivory2);Box(p,"Soffit shadow",new Vector3(0,y-.37f,1.7f),new Vector3(width,.12f,4.4f),shade);Box(p,"Continuous warm edge",new Vector3(0,y-.40f,-.39f),new Vector3(width-.6f,.06f,.1f),warm2);
   Box(p,"Continuous clear balustrade",new Vector3(0,y+1.02f,-.58f),new Vector3(width,1.25f,.05f),glass);Box(p,"Slim dark handrail",new Vector3(0,y+1.67f,-.60f),new Vector3(width,.07f,.095f),charcoal);
   for(float x=-width*.5f+4;x<width*.5f;x+=8){
    bool featured=Mathf.RoundToInt((x+width*.5f-4)/8)%4==0;
    Box(p,"Upper shop interior shadow",new Vector3(x,y+2.7f,4.9f),new Vector3(7.6f,4.5f,.18f),walnut);Box(p,"Upper warm shop interior",new Vector3(x,y+2.6f,4.77f),new Vector3(6.4f,3.5f,.08f),featured?boutiqueGlow:((int)(x+width)/8+level)%3==0?teal2:((int)(x+width)/8+level)%3==1?sand:ivory2);Box(p,"Shop entrance frame",new Vector3(x-3.75f,y+2.8f,2.8f),new Vector3(.3f,5.2f,.5f),sand);Box(p,"Gallery showroom glazing",new Vector3(x,y+2.6f,2.75f),new Vector3(6.4f,3.8f,.05f),glass);
    for(int k=0;k<2;k++){float sy=y+1.3f+k*1.35f;Box(p,"Interior display shelf",new Vector3(x,sy,4.1f),new Vector3(6.4f,.10f,1),sand);Box(p,"Shelf light band",new Vector3(x,sy+.13f,4.65f),new Vector3(6.1f,.07f,.15f),warm2);}
    if(((int)(x+width)/8)%2==0)Box(p,"Slim boutique entry mullion",new Vector3(x+1.6f,y+2.6f,2.55f),new Vector3(.08f,4.3f,.12f),charcoal);
    if(featured){var sign=Group(p,"Featured shop identity");sign.localPosition=new Vector3(x,y+4.72f,2.48f);var label=sign.gameObject.AddComponent<TextMeshPro>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");label.text=level==1?"OBJECTS":"ATELIER";label.fontSize=3.2f;label.color=new Color(.96f,.90f,.70f);label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(6.8f,.66f);label.ForceMeshUpdate(true,true);Box(p,"Feature window sign ground",new Vector3(x,y+4.74f,2.54f),new Vector3(7.2f,.75f,.08f),walnut);}
    Box(p,"Continuous shop identity panel",new Vector3(x,y+5.1f,2.57f),new Vector3(7.4f,.7f,.14f),((int)(x+width)/8)%3==0?teal2:walnut);
    Box(p,"Recessed boutique ceiling light",new Vector3(x,y+5.3f,3.7f),new Vector3(6.7f,.06f,2.6f),warm2);Box(p,"Balustrade post",new Vector3(x,y+1.0f,-.6f),new Vector3(.045f,1.2f,.08f),charcoal);
   }
  }
  StoreySign(p,"2F   DESIGN  /  LIFESTYLE",new Vector3(0,6.48f,-.88f));StoreySign(p,"3F   COLLECTION  /  HOME",new Vector3(0,12.88f,-.88f));
  Box(p,"Upper daylight louver base",new Vector3(0,19.4f,3.2f),new Vector3(width,1.0f,.5f),ivory2);
 }
 static void UpperFloors(Chapter45Director d,Transform root){
  foreach(int floor in new[]{1,2}){var g=Bay(root,"Upper level architectural rhythm "+floor,floor,0,1800,true);g.localPosition=new Vector3(0,floor==1?95:235,0);
   // Perimeter-only wall articulation leaves all existing branch lanes, lifts and offering visible.
   foreach(float side in new[]{-1f,1f})for(float z=-60;z<=60;z+=15){var bay=Group(g,"Upper curtainwall bay");bay.localPosition=new Vector3(side*76,0,z);bay.localRotation=Quaternion.Euler(0,side<0?-90:90,0);Box(bay,"Warm lower wall panel",new Vector3(0,1.1f,0),new Vector3(12.7f,2.1f,.12f),sand);for(int k=0;k<5;k++)Box(bay,"Walnut wall flute",new Vector3(-5.6f+k*.25f,2.9f,-.1f),new Vector3(.10f,5.5f,.15f),walnut);Box(bay,"Interior cornice",new Vector3(0,7.2f,-.25f),new Vector3(13.5f,.4f,.8f),ivory2);Box(bay,"Wall wash slot",new Vector3(0,6.95f,-.3f),new Vector3(11.8f,.055f,.12f),warm2);}
   BatchGoods(g,"Upper-rhythm-"+floor);
  }
 }

 static void StoreySign(Transform p,string text,Vector3 position){var t=Group(p,"Architectural level identity");t.localPosition=position;var label=t.gameObject.AddComponent<TextMeshPro>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");label.text=text;label.fontSize=2.6f;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.20f,.22f,.20f);label.rectTransform.sizeDelta=new Vector2(35,.65f);label.textWrappingMode=TextWrappingModes.NoWrap;label.ForceMeshUpdate(true,true);}

 static void Ring(Transform p,string name,Vector3 center,float rx,float rz,float width,float height,Material mat,int segments=64){
  var v=new List<Vector3>();var ids=new List<int>();
  for(int i=0;i<segments;i++){
   float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
   Vector3 V(float t,float x,float z,float y)=>center+new Vector3(Mathf.Cos(t)*x,y,Mathf.Sin(t)*z);
   var q=new[]{V(a,rx,rz,0),V(b,rx,rz,0),V(b,rx,rz,height),V(a,rx,rz,height),V(a,rx-width,rz-width,0),V(b,rx-width,rz-width,0),V(b,rx-width,rz-width,height),V(a,rx-width,rz-width,height)};
   int k=v.Count;v.AddRange(q);foreach(int j in new[]{0,2,1,0,3,2,4,5,6,4,6,7,3,7,6,3,6,2,0,1,5,0,5,4})ids.Add(k+j);
  }
  var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(ids,0);mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject(p,name,mesh,mat);
 }
 static void MeshObject(Transform p,string name,Mesh mesh,Material mat){var t=Group(p,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;t.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
 static void InteriorAndEntry(Transform g,int type){
  var stone=type==1?ivory2:type==0?clay:teal2;
  Box(g,"Continuous recessed room back",new Vector3(0,2.5f,3.55f),new Vector3(12.6f,5,.24f),shade);
  foreach(float x in new[]{-6.2f,6.2f})Box(g,"Shop side return",new Vector3(x,2.5f,1.4f),new Vector3(.22f,5,4.25f),stone);
  Box(g,"Timber interior floor",new Vector3(0,.27f,1.25f),new Vector3(12.4f,.06f,4.4f),walnut);
  Box(g,"Continuous recessed shop soffit",new Vector3(0,4.02f,1.25f),new Vector3(12.4f,.12f,4.4f),shade);
  foreach(float x in new[]{-4f,0f}){
   Box(g,"Interior joinery mass",new Vector3(x,1.5f,2.95f),new Vector3(3.2f,2.45f,.8f),type==1?walnut:sand);
   Box(g,"Inset rear display panel",new Vector3(x,2.6f,2.5f),new Vector3(2.8f,1.05f,.05f),charcoal);
   Box(g,"Rear shelf plane",new Vector3(x,1.8f,2.3f),new Vector3(2.85f,.1f,.55f),ivory2);
   Box(g,"Recessed warm rear light",new Vector3(x,3.65f,2.85f),new Vector3(2.75f,.08f,.35f),warm2);
   Box(g,"Large clear display glazing",new Vector3(x,2.24f,-.73f),new Vector3(3.72f,3.3f,.025f),glass);
  }
  foreach(float x in new[]{-5.9f,-2.0f,2.0f})Box(g,"Slender storefront mullion",new Vector3(x,2.22f,-.76f),new Vector3(.07f,3.42f,.1f),charcoal);
  Box(g,"Display upper transom",new Vector3(-1.9f,3.94f,-.77f),new Vector3(8.2f,.07f,.1f),charcoal);
  Box(g,"Display stone base",new Vector3(-1.65f,.3f,-.6f),new Vector3(9.15f,.42f,.5f),stone);
  Box(g,"Door outer jamb",new Vector3(2.95f,1.85f,.25f),new Vector3(.25f,3.7f,2.05f),stone);
  Box(g,"Door inner jamb",new Vector3(5.05f,1.85f,.25f),new Vector3(.25f,3.7f,2.05f),stone);
  Box(g,"Recessed entry head",new Vector3(4,3.63f,.25f),new Vector3(2.35f,.24f,2.05f),stone);
  Box(g,"Door threshold into room",new Vector3(4,.09f,.3f),new Vector3(1.85f,.06f,2.1f),ivory2);
  Box(g,"Recessed bronze door leaf",new Vector3(4,1.76f,1.31f),new Vector3(1.82f,3.27f,.1f),charcoal);
  Box(g,"Recessed door glazing",new Vector3(4,1.87f,1.24f),new Vector3(1.61f,2.68f,.035f),window);
  foreach(float x in new[]{3.12f,4.88f})Box(g,"Bronze door stile",new Vector3(x,1.76f,1.2f),new Vector3(.065f,3.3f,.1f),sand);
  Box(g,"Door pull",new Vector3(3.40f,1.56f,1.09f),new Vector3(.042f,.61f,.07f),ivory2);
  Box(g,"Inner vestibule light",new Vector3(4,3.4f,.7f),new Vector3(1.56f,.055f,.9f),warm2);
  Box(g,"Door right wall base",new Vector3(5.7f,.3f,-.4f),new Vector3(1.0f,.42f,.4f),stone);
  float signWidth=type==2?6.7f:8.5f;
  Box(g,"Individual shop sign fascia",new Vector3(type==2?-2f:-1.4f,4.57f,-.69f),new Vector3(signWidth,.58f,.22f),type==1?walnut:charcoal);
  Box(g,"Thin individual entrance canopy",new Vector3(type==2?4:0,4.08f,-.64f),new Vector3(type==2?3:12.45f,.12f,1.3f),stone);
 }
 static void WallBetween(Transform p,string name,Vector2 a,Vector2 b,float y,float height,float depth,Material m){
  var delta=b-a;var t=Box(p,name,new Vector3((a.x+b.x)*.5f,y,(a.y+b.y)*.5f),new Vector3(delta.magnitude,height,depth),m);t.localRotation=Quaternion.Euler(0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,0);
 }
 static void PolygonSlab(Transform p,string name,Vector2[] outline,float y,float thickness,Material m){
  var v=new List<Vector3>();var t=new List<int>();
  Action<Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d)=>{int i=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});};
  for(int i=0;i<outline.Length;i++){var a=outline[i];var b=outline[(i+1)%outline.Length];quad(new Vector3(a.x,y,a.y),new Vector3(a.x,y+thickness,a.y),new Vector3(b.x,y+thickness,b.y),new Vector3(b.x,y,b.y));}
  for(int i=1;i<outline.Length-1;i++){int q=v.Count;v.AddRange(new[]{new Vector3(outline[0].x,y+thickness,outline[0].y),new Vector3(outline[i+1].x,y+thickness,outline[i+1].y),new Vector3(outline[i].x,y+thickness,outline[i].y)});t.AddRange(new[]{q,q+1,q+2});}
  for(int i=1;i<outline.Length-1;i++){int q=v.Count;v.AddRange(new[]{new Vector3(outline[0].x,y,outline[0].y),new Vector3(outline[i].x,y,outline[i].y),new Vector3(outline[i+1].x,y,outline[i+1].y)});t.AddRange(new[]{q,q+1,q+2});}
  var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var g=Group(p,name);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;
 }
 static void GlassCorner(Transform g){
  var outline=new[]{new Vector2(-6.25f,1),new Vector2(-5.15f,-.48f),new Vector2(5.45f,-.48f),new Vector2(6.25f,.4f),new Vector2(6.25f,6),new Vector2(-6.25f,6)};
  Box(g,"Brick rear wall",new Vector3(0,8.8f,5.85f),new Vector3(12.5f,7.4f,.3f),clay);
  for(int l=0;l<2;l++){
   float b=5.1f+l*3.6f;PolygonSlab(g,"Chamfered corner floor",outline,b,.32f,clay);
   Box(g,"Continuous corner glazing",new Vector3(.1f,b+1.9f,-.20f),new Vector3(10.4f,2.95f,.045f),windowLight);
   WallBetween(g,"Wrapped corner glazing",new Vector2(-6.12f,1.05f),new Vector2(-5.14f,-.26f),b+1.9f,2.95f,.045f,window);
   WallBetween(g,"Return glazing",new Vector2(-6.08f,1.1f),new Vector2(-6.08f,4.5f),b+1.9f,2.95f,.045f,windowLight);
   foreach(float x in new[]{-5.14f,-1.64f,1.86f,5.4f})Box(g,"Brown structural curtainwall post",new Vector3(x,b+1.92f,-.45f),new Vector3(.16f,3.2f,.34f),clay);
   Box(g,"Brick side return",new Vector3(6.1f,b+1.8f,3.1f),new Vector3(.3f,3.6f,5.8f),clay);
  }
  PolygonSlab(g,"Chamfered roof parapet datum",outline,12.3f,.4f,clay);
 }
 static void StoneWindows(Transform g){
  Box(g,"Limestone rear wall",new Vector3(0,10.5f,5.85f),new Vector3(12.6f,10.8f,.3f),ivory2);
  foreach(float x in new[]{-6.13f,6.13f})Box(g,"Limestone side wall",new Vector3(x,10.5f,2.9f),new Vector3(.34f,10.8f,6.2f),ivory2);
  for(int l=0;l<3;l++){
   float b=5.1f+l*3.6f;
   Box(g,"Continuous masonry sill",new Vector3(0,b+.43f,-.04f),new Vector3(12.4f,.86f,.64f),ivory2);
   Box(g,"Masonry lintel",new Vector3(0,b+3.32f,-.04f),new Vector3(12.4f,.56f,.64f),ivory2);
   foreach(float x in new[]{-5.59f,-2f,2f,5.59f})Box(g,"Full-depth stone pier",new Vector3(x,b+1.95f,-.04f),new Vector3(Mathf.Abs(x)>5?1.22f:1.9f,2.18f,.64f),ivory2);
   foreach(float x in new[]{-4f,0f,4f}){
    Box(g,"Deep set window glass",new Vector3(x,b+1.95f,.39f),new Vector3(2.05f,2.18f,.035f),l%2==0?window:windowLight);
    foreach(float dx in new[]{-1.015f,1.015f})Box(g,"Dark inner jamb reveal",new Vector3(x+dx,b+1.95f,.13f),new Vector3(.035f,2.18f,.54f),shade);
    Box(g,"Window sill projection",new Vector3(x,b+.88f,-.25f),new Vector3(2.34f,.13f,.83f),sand);
    Box(g,"Slim recessed window sash",new Vector3(x,b+1.95f,.34f),new Vector3(.055f,2.18f,.06f),charcoal);
   }
  }
  Box(g,"Stepped stone coping",new Vector3(0,16.01f,2.85f),new Vector3(12.9f,.22f,6.6f),sand);
 }
 static void TerracedShop(Transform g){
  for(int l=0;l<3;l++){
   float b=5.1f+l*3.6f;bool top=l==2;float w=top?7.4f:12.5f,cx=top?-2.55f:0,front=top?1.15f:-.24f;
   Box(g,"Stepped volume rear",new Vector3(cx,b+1.8f,5.85f),new Vector3(w,3.6f,.3f),teal2);
   foreach(float x in new[]{cx-w*.5f+.14f,cx+w*.5f-.14f})Box(g,"Stepped volume return",new Vector3(x,b+1.8f,(front+5.8f)*.5f),new Vector3(.28f,3.6f,5.8f-front),teal2);
   Box(g,"Stepped floor slab",new Vector3(cx,b+.12f,(front+5.8f)*.5f),new Vector3(w,.24f,5.8f-front),sand);
   Box(g,"Broad recessed loft glazing",new Vector3(cx,b+1.85f,front+.27f),new Vector3(w-.75f,2.77f,.05f),window);
   Box(g,"Loft upper lintel",new Vector3(cx,b+3.37f,front+.05f),new Vector3(w,.46f,.58f),teal2);
   foreach(float x in new[]{cx-w*.25f,cx+w*.25f})Box(g,"Loft slender glazing frame",new Vector3(x,b+1.85f,front+.21f),new Vector3(.08f,2.77f,.11f),sand);
  }
  Box(g,"Open roof terrace floor",new Vector3(3.72f,12.47f,2.7f),new Vector3(5.05f,.28f,6.15f),sand);
  Box(g,"Terrace front parapet",new Vector3(3.72f,12.91f,-.32f),new Vector3(5.05f,.62f,.22f),teal2);
  Box(g,"Terrace bronze top rail",new Vector3(3.72f,13.39f,-.32f),new Vector3(5.05f,.065f,.07f),charcoal);
  foreach(float x in new[]{1.22f,3.72f,6.2f})Box(g,"Terrace rail upright",new Vector3(x,13.2f,-.32f),new Vector3(.055f,.42f,.07f),charcoal);
  Box(g,"Setback upper roof",new Vector3(-2.55f,16.01f,3.4f),new Vector3(7.65f,.22f,4.9f),sand);
  Box(g,"Vertical shop identity blade",new Vector3(5.86f,7.1f,-.53f),new Vector3(.72f,3.25f,.17f),walnut);
  var text=Group(g,"Vertical small shop lettering");text.localPosition=new Vector3(5.86f,7.1f,-.63f);var label=text.gameObject.AddComponent<TextMeshPro>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");label.text="S\nT\nU\nD\nI\nO";label.fontSize=2.5f;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.93f,.86f,.68f);label.rectTransform.sizeDelta=new Vector2(.66f,3.1f);label.ForceMeshUpdate(true,true);
 }

 static Material finishGlassA,finishGlassB;
 static void ExistingMaterials(Transform environment){
  var shared=environment.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).GroupBy(m=>m.name).ToDictionary(g=>g.Key,g=>g.First());
  charcoal=shared["Graphite reveals"];sand=shared["Sandstone"];ivory2=shared["Pearl stone"];walnut=shared["Walnut shadow"];shade=shared["Warm interior shadow"];warm2=shared["Warm light diffuser"];
  clay=shared.ContainsKey("Oxide brick")?shared["Oxide brick"]:NativeMaterial("Local baked clay",new Color(.43f,.22f,.14f));
  teal2=shared.ContainsKey("Sage mineral plaster")?shared["Sage mineral plaster"]:NativeMaterial("Local sage plaster",new Color(.22f,.34f,.30f));
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/ShooterSurvival/Models/Chapters/Chapters45/RetailProduction20261003/ArchitecturalGlazingFinish.shader");
  if(shader==null||ShaderUtil.ShaderHasError(shader)||!shader.isSupported)throw new Exception("Architectural glazing shader compile/support failure");
  finishGlassA=new Material(shader){name="Cool sky and interior glazing",enableInstancing=true};finishGlassA.SetColor("_BaseColor",new Color(.15f,.26f,.29f));finishGlassA.SetColor("_SkyColor",new Color(.49f,.64f,.68f));finishGlassA.SetColor("_InteriorColor",new Color(.19f,.25f,.25f));finishGlassA.SetFloat("_ReflectionStrength",.72f);AssetDatabase.CreateAsset(finishGlassA,revision+"/Cool sky glazing.mat");
  finishGlassB=new Material(finishGlassA){name="Soft neutral reflected glazing"};finishGlassB.SetColor("_BaseColor",new Color(.24f,.31f,.32f));finishGlassB.SetColor("_SkyColor",new Color(.57f,.66f,.65f));finishGlassB.SetFloat("_ReflectionStrength",.66f);AssetDatabase.CreateAsset(finishGlassB,revision+"/Neutral sky glazing.mat");
  window=finishGlassA;windowLight=finishGlassB;
  var src=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/Chapters45/DepartmentStore/20261002T170206609/Low tint retail glass.mat");glass=new Material(src){name="Finished shopfront low tint"};glass.SetColor("_BaseColor",new Color(.64f,.79f,.80f,.08f));AssetDatabase.CreateAsset(glass,revision+"/Finished shopfront clear glass.mat");
 }
 static void FinishCity(Chapter45Director d,Transform environment,Transform root){
  var street=d.transform.Find("Scenery/Jamsil_ReferenceStreet_20261002");var shops=street.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Shop bay ")).ToArray();
  int[] indices={23,28};var architecture=environment.Find("Continuous varied street architecture");
  placed.Add(CubeSurgery.Strip(architecture,indices.SelectMany(i=>architecture.Find("Facade family "+i).Cast<Transform>()),revision,"Two-visible-repeat-facades"));
  string[] frontNames={"Window transom","Window mullion","Entry dark glass","Door handle","Stone footing","Shop sign fascia","Canopy","Warm strip"};
  placed.Add(CubeSurgery.Strip(street,indices.SelectMany(i=>shops[i].GetComponentsInChildren<Transform>(true)).Where(t=>frontNames.Contains(t.name)||(t.name=="Display shelf"&&t.localPosition.x>2)),revision,"Two-visible-repeat-shopfronts"));
  var all=Bay(root,"Two foreground facade corrections",0,0,1900,true);
  for(int j=0;j<2;j++){int i=indices[j],type=j==0?2:1;var g=Group(all,"Visible store "+i);Align(g,shops[i]);InteriorAndEntry(g,type);if(type==1)StoneWindows(g);else TerracedShop(g);
   var label=shops[i].Cast<Transform>().First(t=>t.name.StartsWith("Sign ")&&t.GetComponent<TMP_Text>()!=null).GetComponent<TMP_Text>();label.transform.localPosition=new Vector3(type==2?-2f:-1.4f,4.57f,-.81f);label.rectTransform.sizeDelta=new Vector2(type==2?6.3f:8.1f,.46f);label.fontSizeMax=4;label.ForceMeshUpdate(true,true);
  }BatchGoods(all,"Two-frontage-corrections");
  int changed=0;foreach(var groupName in new[]{"Continuous varied street architecture","Hero storefront architecture v2"})foreach(var r in environment.Find(groupName).GetComponentsInChildren<MeshRenderer>(true)){
   string name=r.sharedMaterial.name;if(name=="Blue grey glazing"||name=="Hero recessed cool glazing"){r.sharedMaterial=finishGlassA;changed++;}else if(name=="Sky reflection panes"||name=="Hero reflected sky glazing"){r.sharedMaterial=finishGlassB;changed++;}
  }
  placed.Add(new{additionalForegroundStores=indices,retainedHeroStores=8,glazingBatchesUpdated=changed,glazingMethod="Opaque single-pass approximate sky reflection and recessed-edge shade; not real-time reflections",newTextures=0,newColliders=0});
 }
 static void RelocateExistingMallGoods(Chapter45Director d,int index){
  var group=d.transform.Find("Scenery/RetailGoods_20261003/Mall display "+index);if(group==null)throw new Exception("Mall goods group missing "+index);
  int count=0;foreach(var f in group.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null)){
   var mesh=Object.Instantiate(f.sharedMesh);var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++){
    var p=group.InverseTransformPoint(f.transform.TransformPoint(vertices[i]));Vector3 delta;
    if(p.z>1.6f)delta=new Vector3(0,0,1.55f);
    else if(p.x<-2.2f)delta=new Vector3(.2f,-.37f,.3f);
    else if(p.x>2.2f)delta=new Vector3(-8.2f,.8f,4.2f);
    else delta=new Vector3(-1.2f,-.37f,1.05f);
    vertices[i]=f.transform.InverseTransformPoint(group.TransformPoint(p+delta));
   }
   mesh.vertices=vertices;mesh.RecalculateBounds();mesh.name="Redistributed existing mall goods "+index+" "+count;AssetDatabase.CreateAsset(mesh,revision+"/MallGoods-"+index+"-"+(count++)+".asset");f.sharedMesh=mesh;
  }placed.Add(new{relocatedGoodsShop=index,copiedMeshes=count,newGoods=0,sourceGroupPreserved=true});
 }
 static void FinishMall(Chapter45Director d,Transform environment,Transform root){
  var store=d.transform.Find("Scenery/DepartmentStore_20261002");var shops=store.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Shop bay ")).ToArray();
  int[] chosen={21,23,25,33};string[] names={"Walnut recessed display","Display floor","Deep shop reveal","Porcelain jamb","Thin floating canopy","Warm recessed lighting","Store name fascia","Display plinth","Framed back niche","Linen display niche","Back display shelf","Shelf warm reveal","Display glazing","Slim brass mullion"};
  placed.Add(CubeSurgery.Strip(store,chosen.SelectMany(i=>shops[i].GetComponentsInChildren<Transform>(true)).Where(t=>names.Contains(t.name)),revision,"Visible-mall-booths"));
  var all=Bay(root,"Depth and distinct retail frontages",0,0,530,true);
  for(int n=0;n<chosen.Length;n++){int index=chosen[n];var shop=shops[index];if(Vector3.Distance(shop.position,d.transform.Find("Scenery/RetailGoods_20261003/Mall display "+index).position)>.01f)throw new Exception("Shop index mismatch");
   var g=Group(all,"Finished visible boutique "+index);Align(g,shop);DetailedMallRoom(g,n);RelocateExistingMallGoods(d,index);
   foreach(var label in shop.GetComponentsInChildren<TMP_Text>(true)){label.transform.localPosition=new Vector3(-2.1f,5.0f,-2.08f);label.rectTransform.sizeDelta=new Vector2(8.0f,.48f);label.fontSizeMax=4.0f;label.ForceMeshUpdate(true,true);}
  }BatchGoods(all,"Visible-boutique-rooms");
  FinishUpperRooms(d,environment,root);placed.Add(new{groundBoutiques=chosen,upperBoutiques=6,existingGoodsRedistributed=true,groundRoomDepth=6.8,visibleGlazingRetained=true});
 }
 static void DetailedMallRoom(Transform g,int variant){
  Material skin=variant%2==0?sand:ivory2;
  Box(g,"Deep boutique back wall",new Vector3(0,2.7f,4.85f),new Vector3(13.2f,5.4f,.18f),variant%2==0?shade:teal2);
  foreach(float x in new[]{-6.55f,6.55f})Box(g,"Full room side wall",new Vector3(x,2.7f,1.45f),new Vector3(.22f,5.4f,6.8f),skin);
  Box(g,"Room floor inset",new Vector3(0,.1f,1.4f),new Vector3(13.2f,.2f,6.9f),walnut);
  Box(g,"Continuous retail ceiling",new Vector3(0,5.4f,1.4f),new Vector3(15.8f,.22f,6.9f),skin);
  foreach(float x in new[]{-7.15f,7.15f})Box(g,"Linked wall between boutiques",new Vector3(x,2.7f,-.4f),new Vector3(1.2f,5.4f,2.4f),skin);
  Box(g,"Room rear timber joinery",new Vector3(-1.5f,2.15f,4.54f),new Vector3(9.4f,3.95f,.42f),walnut);
  foreach(float x in new[]{-4.0f,0f}){Box(g,"Deep rear display opening",new Vector3(x,2.8f,4.27f),new Vector3(3.2f,2.6f,.05f),shade);Box(g,"Rear goods support shelf",new Vector3(x,2.15f,4.2f),new Vector3(3.4f,.1f,.9f),skin);Box(g,"Warm joinery downlight",new Vector3(x,4.08f,4.05f),new Vector3(2.95f,.075f,.5f),warm2);}
  // Existing front products now share a long cabinet; the third object moves to rear joinery.
  Box(g,"Recessed dark cabinet toe",new Vector3(-2.7f,.18f,.75f),new Vector3(6.35f,.24f,1.2f),charcoal);
  Box(g,"Long walnut cabinet",new Vector3(-2.7f,.59f,.75f),new Vector3(6.6f,.68f,1.35f),walnut);
  Box(g,"Thin warm stone display top",new Vector3(-2.7f,.98f,.75f),new Vector3(6.88f,.1f,1.55f),sand);
  foreach(float x in new[]{-4.9f,-2.7f,-.5f}){Box(g,"Cabinet inset panel",new Vector3(x,.59f,.054f),new Vector3(2.06f,.52f,.024f),shade);Box(g,"Recessed cabinet pull",new Vector3(x,.77f,.024f),new Vector3(.72f,.025f,.035f),sand);}
  foreach(float x in new[]{2.65f,6.1f})Box(g,"Deep entry side reveal",new Vector3(x,2.5f,.57f),new Vector3(.24f,5,4.55f),skin);
  Box(g,"Deep entry ceiling reveal",new Vector3(4.38f,4.91f,.57f),new Vector3(3.6f,.22f,4.55f),shade);
  Box(g,"Recessed retail doorway",new Vector3(4.4f,2.05f,2.87f),new Vector3(2.85f,4.1f,.12f),charcoal);
  Box(g,"Retail door interior glimpse",new Vector3(4.4f,2.15f,2.78f),new Vector3(2.45f,3.55f,.04f),windowLight);
  Box(g,"Entry ceiling light",new Vector3(4.4f,4.72f,1.9f),new Vector3(2.7f,.075f,.85f),warm2);
  Box(g,"Clear large display window",new Vector3(-2.1f,2.61f,-1.91f),new Vector3(8.4f,4.15f,.03f),glass);
  foreach(float x in new[]{-6.35f,-2.1f,2.2f})Box(g,"Slim retail window frame",new Vector3(x,2.61f,-1.94f),new Vector3(.075f,4.26f,.10f),walnut);
  Box(g,"Store identity recessed fascia",new Vector3(-2.1f,5.0f,-1.98f),new Vector3(8.7f,.62f,.16f),walnut);
  Box(g,"Thin display soffit shadow",new Vector3(-2.1f,4.63f,-.8f),new Vector3(8.7f,.12f,2.3f),shade);
  Box(g,"Continuous warm interior ceiling slot",new Vector3(-2.1f,4.53f,-.65f),new Vector3(7.9f,.05f,.25f),warm2);
 }
 static void FinishUpperRooms(Chapter45Director d,Transform environment,Transform root){
  var podium=environment.Find("Connected upper department galleries");var wall=podium.Cast<Transform>().First(t=>t.name=="Continuous endwall galleries"&&t.position.z<0);
  float[] xs={8,16,24,-48,-56,-64};string[] names={"Upper shop interior shadow","Upper warm shop interior","Shop entrance frame","Gallery showroom glazing","Interior display shelf","Shelf light band","Slim boutique entry mullion","Feature window sign ground","Continuous shop identity panel","Recessed boutique ceiling light"};
  var targets=wall.Cast<Transform>().Where(t=>names.Contains(t.name)&&t.localPosition.y>6.4f&&t.localPosition.y<12f&&xs.Any(x=>Mathf.Abs(t.localPosition.x-x)<3.9f)).ToArray();
  placed.Add(CubeSurgery.Strip(podium,targets,revision,"Six-visible-upper-storefronts"));
  var all=Bay(root,"Six visible upper boutique rooms",0,0,530,true);var facade=Group(all,"South gallery room frame");Align(facade,wall);
  var reusable=environment.Find("Reviewed goods in upper gallery boutiques").Cast<Transform>().Where(t=>t.localPosition.y>12).Take(6).ToArray();if(reusable.Length!=6)throw new Exception("Expected six reusable upper goods");
  for(int i=0;i<xs.Length;i++){
   float x=xs[i];var g=Group(facade,"Upper room "+x);g.localPosition=new Vector3(x,6.4f,0);int style=i%3;
   Box(g,"Upper room deep rear",new Vector3(0,2.72f,4.9f),new Vector3(7.55f,5.12f,.13f),style==0?shade:style==1?walnut:teal2);
   foreach(float side in new[]{-1f,1f})Box(g,"Upper room visible side return",new Vector3(side*3.64f,2.72f,3.53f),new Vector3(.20f,5.12f,2.8f),style==1?walnut:ivory2);
   Box(g,"Upper room ceiling shadow",new Vector3(0,5.25f,3.55f),new Vector3(7.45f,.14f,2.9f),shade);
   Box(g,"Upper ceiling recessed light",new Vector3(0,5.12f,3.76f),new Vector3(6.7f,.05f,1.1f),warm2);
   Box(g,"Upper room timber floor",new Vector3(0,.28f,3.55f),new Vector3(7.45f,.08f,2.9f),walnut);
   float featureX=style==1?-1.5f:1.6f;
   Box(g,"Upper asymmetric display joinery",new Vector3(featureX,1.1f,4.05f),new Vector3(3.35f,1.75f,1.25f),style==2?sand:walnut);
   Box(g,"Upper feature display top",new Vector3(featureX,2.02f,4.05f),new Vector3(3.55f,.09f,1.35f),ivory2);
   Box(g,"Upper tall background contrast",new Vector3(featureX,3.19f,4.72f),new Vector3(3.2f,2.0f,.07f),style==0?warm2:shade);
   if(style==1){for(int k=0;k<4;k++)Box(g,"Timber vertical joinery division",new Vector3(-3.0f+k*1.1f,3.0f,4.55f),new Vector3(.11f,4.2f,.32f),sand);}
   else if(style==2){Box(g,"Upper large clear display window",new Vector3(0,2.65f,2.25f),new Vector3(6.8f,4.6f,.03f),glass);Box(g,"Asymmetric entrance stile",new Vector3(-1.8f,2.65f,2.18f),new Vector3(.075f,4.65f,.12f),walnut);}
   Box(g,"Upper shop thin identity panel",new Vector3(0,5.36f,2.3f),new Vector3(7.45f,.58f,.14f),walnut);
   reusable[i].SetPositionAndRotation(g.TransformPoint(new Vector3(featureX,2.07f,4.05f)),g.rotation);
   var model=reusable[i].GetChild(0);model.localPosition=Vector3.zero;
  }
  foreach(var label in wall.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.transform.localPosition.y<12&&xs.Any(x=>Mathf.Abs(t.transform.localPosition.x-x)<3.9f))){label.transform.localPosition=new Vector3(label.transform.localPosition.x,11.74f,2.18f);label.rectTransform.sizeDelta=new Vector2(6.7f,.44f);label.fontSize=2.8f;label.ForceMeshUpdate(true,true);}
  BatchGoods(all,"Six-upper-room-depths");
 }

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
            bool protect = c is Collider || c is Rigidbody || c is Camera || c is Light || c is MonoBehaviour && !(c is TMP_Text);
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
// Remove only matched primitive triangles from copied, combined decorative meshes.
// Original asset files and transform markers remain intact.
static class CubeSurgery {
 sealed class Face {public MeshFilter filter;public int sub,index;public Vector3 a,b,c,center;}
 static string Key(Vector3 p)=>Mathf.RoundToInt(p.x*100)+","+Mathf.RoundToInt(p.y*100)+","+Mathf.RoundToInt(p.z*100);
 public static object Strip(Transform scope,IEnumerable<Transform> candidates,string assetFolder,string label){
  var targets=candidates.Distinct().ToArray();if(targets.Length==0)return new{label,boxes=0,triangles=0};
  var grid=new Dictionary<string,List<Face>>();var filters=scope.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).ToArray();
  foreach(var f in filters){var m=f.sharedMesh;var v=m.vertices.Select(f.transform.TransformPoint).ToArray();for(int s=0;s<m.subMeshCount;s++){var t=m.GetTriangles(s);for(int i=0;i<t.Length;i+=3){var face=new Face{filter=f,sub=s,index=i,a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]]};face.center=(face.a+face.b+face.c)/3;foreach(var point in new[]{face.a,face.b,face.c}){string k=Key(point);if(!grid.ContainsKey(k))grid[k]=new List<Face>();grid[k].Add(face);}}}}
  var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var cm=cube.GetComponent<MeshFilter>().sharedMesh;var cv=cm.vertices;var ct=cm.triangles;Object.DestroyImmediate(cube);
  var removals=new Dictionary<MeshFilter,Dictionary<int,HashSet<int>>>();var consumed=new HashSet<Face>();
  foreach(var target in targets){var corners=cv.Select(target.TransformPoint).Distinct().ToArray();var options=new HashSet<Face>();foreach(var corner in corners)for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++){if(grid.TryGetValue((Mathf.RoundToInt(corner.x*100)+x)+","+(Mathf.RoundToInt(corner.y*100)+y)+","+(Mathf.RoundToInt(corner.z*100)+z),out var found))foreach(var face in found)options.Add(face);}
   var matched=options.Where(face=>!consumed.Contains(face)&&Vector3.Dot(Vector3.Cross(face.b-face.a,face.c-face.a).normalized,(face.center-target.position).normalized)>0&&new[]{face.a,face.b,face.c}.All(v=>corners.Any(w=>(v-w).sqrMagnitude<.000025f))).ToArray();if(matched.Length!=12){File.WriteAllText("outputs/department-store-2026-10-02/trellis-recovery/cube-diagnostic.json",(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{new{target=target.name,corners=corners.Select(v=>v.ToString("R")).ToArray(),matched=matched.Select(f=>new{a=f.a.ToString("R"),b=f.b.ToString("R"),c=f.c.ToString("R")}).ToArray(),options=options.Count}}));throw new Exception("Expected12 cube faces, found "+matched.Length+" for "+target.name+" at "+target.position);}
   foreach(var face in matched){consumed.Add(face);if(!removals.ContainsKey(face.filter))removals[face.filter]=new Dictionary<int,HashSet<int>>();var parts=removals[face.filter];if(!parts.ContainsKey(face.sub))parts[face.sub]=new HashSet<int>();parts[face.sub].Add(face.index);}
  }
  int index=0;foreach(var item in removals){var copy=Object.Instantiate(item.Key.sharedMesh);copy.name=label+" retained architecture "+index;foreach(var part in item.Value){var t=copy.GetTriangles(part.Key);var kept=new List<int>();for(int i=0;i<t.Length;i+=3)if(!part.Value.Contains(i)){kept.Add(t[i]);kept.Add(t[i+1]);kept.Add(t[i+2]);}copy.SetTriangles(kept,part.Key);}copy.RecalculateBounds();AssetDatabase.CreateAsset(copy,assetFolder+"/"+label+"-"+(index++)+".asset");item.Key.sharedMesh=copy;}
  return new{label,boxes=targets.Length,triangles=consumed.Count,copiedMeshes=removals.Count,originalAssetsUnchanged=true};
 }
}
