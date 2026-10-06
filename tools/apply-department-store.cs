using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

// Presentation only. User-authorized continuation of the existing Astra scene.
public static class ApplyDepartmentStore {
 const string RootName="DepartmentStore_20261002";
 const string Out="outputs/department-store-2026-10-02";
 static string art;
 static Chapter45Director director;
 static Transform root;
 static Material stone,ivory,wood,ink,glass,metal,warm,grout,blue,leaf,retailGlass,linen,terracotta;
 static TMP_FontAsset font;
 static int meshes,stores,escalators;
 static string Json(object value)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
 public static object Main(bool towerOnly=false) {
  RequireIdle(); Directory.CreateDirectory(Out);var setup=EditorSceneManager.GetSceneManagerSetup(); var receipts=new List<object>();string current="setup";
  string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
  art="Assets/ShooterSurvival/Models/Chapters/Chapters45/DepartmentStore/"+stamp;
  Directory.CreateDirectory(art); AssetDatabase.Refresh(); Materials();
  font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");
  if(font==null)throw new Exception("Existing font missing");
  try {
   foreach(string name in towerOnly?new[]{"ShoeTower"}:new[]{"ShoeTower","Jamsil"}) {
    current=name;string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";
    var scene=EditorSceneManager.OpenScene(path); director=Object.FindFirstObjectByType<Chapter45Director>();
    string before=ProtectedState(scene); var scenery=director.transform.Find("Scenery");
    var previous=scenery.Find(RootName); if(previous!=null)Object.DestroyImmediate(previous.gameObject);
    root=Group(scenery,RootName); meshes=stores=escalators=0;
    if(name=="ShoeTower") Interior(); else Entrance();
    if(before!=ProtectedState(scene))throw new Exception("Existing gameplay/camera/collider preservation failed before save: "+name);
    if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Decorative collision added");
    foreach(var guid in AssetDatabase.FindAssets("",new[]{art}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
    EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
    File.WriteAllText(Out+"/progress-"+stamp+".json",Json(new {lastSavedScene=name,reopenVerificationPending=true,completed=receipts}));
    scene=EditorSceneManager.OpenScene(path); director=Object.FindFirstObjectByType<Chapter45Director>();root=director.transform.Find("Scenery/"+RootName);
    bool same=before==ProtectedState(scene);
    if(!same)throw new Exception("Saved scene preservation failed: "+name);
    receipts.Add(new {scene=name,art,meshes,stores,escalators,addedColliders=0,protectedStateUnchanged=same,renderers=root.GetComponentsInChildren<Renderer>(true).Length,newAiModels=0,reusedTrellisF10Plants=name=="ShoeTower"?3:0,localWarmLights=root.GetComponentsInChildren<Light>(true).Length,
      triangles=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).Sum(f=>(long)f.sharedMesh.GetIndexCount(0)/3)});
    File.WriteAllText(Out+"/apply-"+stamp+".json",Json(receipts));
   }
   File.WriteAllText(Out+"/apply-"+stamp+".json",Json(receipts));return receipts.ToArray();
  } catch(Exception ex){File.WriteAllText(Out+"/failure-"+stamp+".json",Json(new{scene=current,error=ex.ToString(),completed=receipts}));throw;}
  finally { EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");EditorSceneManager.RestoreSceneManagerSetup(setup); }
 }
 static Transform Group(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
 static Transform Bay(string name,float from=0,float to=530,bool always=true){var t=Group(root,name);var g=t.gameObject.AddComponent<Chapter45SceneryGroup>();g.floor=0;g.startDistance=from;g.endDistance=to;g.alwaysVisible=always;return t;}
 static Material Mat(string name,Color c,float gloss=.35f,float metallic=0){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",gloss);m.SetFloat("_Metallic",metallic);AssetDatabase.CreateAsset(m,art+"/"+name+".mat");return m;}
 static void Materials(){
  stone=Mat("Pearl limestone",new Color(.81f,.79f,.70f));ivory=Mat("Ivory porcelain",new Color(.94f,.92f,.84f),.5f);
  wood=Mat("Warm walnut",new Color(.30f,.16f,.075f));ink=Mat("Black handrail",new Color(.035f,.05f,.06f),.5f);
  metal=Mat("Champagne metal",new Color(.62f,.51f,.34f),.65f,.65f);grout=Mat("Stone joints",new Color(.60f,.61f,.58f));
  blue=Mat("Recessed reflective glazing",new Color(.22f,.42f,.47f),.8f,.25f);leaf=Mat("Plant foliage",new Color(.19f,.32f,.13f));
  warm=Mat("Warm retail luminance",new Color(.98f,.77f,.45f));warm.EnableKeyword("_EMISSION");warm.SetColor("_EmissionColor",new Color(.5f,.3f,.12f));
  glass=Mat("Clear pale glass",new Color(.48f,.78f,.79f,.24f),.82f,.1f);glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;
  retailGlass=new Material(glass){name="Low tint retail glass"};retailGlass.SetColor("_BaseColor",new Color(.82f,.9f,.93f,.09f));retailGlass.SetFloat("_Metallic",0);AssetDatabase.CreateAsset(retailGlass,art+"/Low tint retail glass.mat");
  linen=Mat("Warm linen interior",new Color(.73f,.65f,.50f),.25f);terracotta=Mat("Terracotta display textile",new Color(.46f,.19f,.105f),.3f);
 }
 static Transform Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat){var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(p,false);t.localPosition=pos;t.localScale=size;Object.DestroyImmediate(t.GetComponent<Collider>());var r=t.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;return t;}
 static void Beam(Transform p,Vector3 a,Vector3 b,float w,Material m){var t=Box(p,"Joinery",(a+b)*.5f,new Vector3(w,w,Vector3.Distance(a,b)),m);t.localRotation=Quaternion.LookRotation(b-a);}
 static void Label(Transform p,string text,Vector3 pos,Vector2 size,float yaw=0){var t=Group(p,"Wayfinding "+text);t.localPosition=pos;t.localRotation=Quaternion.Euler(0,yaw,0);var tm=t.gameObject.AddComponent<TextMeshPro>();tm.font=font;tm.text=text;tm.fontSize=5;tm.enableAutoSizing=true;tm.fontSizeMax=5;tm.fontSizeMin=1.7f;tm.color=new Color(.99f,.94f,.81f);tm.alignment=TextAlignmentOptions.Center;tm.textWrappingMode=TextWrappingModes.NoWrap;tm.rectTransform.sizeDelta=size;tm.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
 // Thick elliptical rings: shared geometry for slab, rounded fascia, handrail and glass.
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
 static void Merge(Transform p,string key){
  var filters=p.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.GetComponent<TMP_Text>()==null).ToArray();var batches=new Dictionary<Material,List<CombineInstance>>();
  foreach(var f in filters){var r=f.GetComponent<MeshRenderer>();if(r==null)continue;for(int s=0;s<Mathf.Min(f.sharedMesh.subMeshCount,r.sharedMaterials.Length);s++){var m=r.sharedMaterials[s];if(m==null)continue;if(!batches.ContainsKey(m))batches[m]=new List<CombineInstance>();batches[m].Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=s,transform=p.worldToLocalMatrix*f.transform.localToWorldMatrix});}}
  int i=0;foreach(var b in batches){var mesh=new Mesh{name=key+" "+i,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(b.Value.ToArray(),true,true);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,art+"/"+key+"_"+i+".asset");MeshObject(p,"Finished "+b.Key.name,mesh,b.Key);i++;meshes++;}
  foreach(var f in filters){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
 }
 static void Interior(){
  var old=director.transform.Find("Scenery/TowerInterior_V1");if(old==null)throw new Exception("Refined tower required");
  foreach(Transform t in old)if(t.name=="BaseArchitecture_0"||t.name.StartsWith("InteriorBay_0_")||t.name=="LandingDirectory_0")foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;
  var g=Bay("Podium floor and daylight envelope");
  Box(g,"Continuous supported floor",new Vector3(0,.012f,-5),new Vector3(158,.024f,178),ivory);
  for(int x=-78;x<=78;x+=6)Box(g,"Fine floor joint",new Vector3(x,.028f,-5),new Vector3(.025f,.008f,178),grout);
  for(int z=-89;z<=79;z+=6)Box(g,"Fine floor joint",new Vector3(0,.029f,z),new Vector3(158,.008f,.025f),grout);
  RouteBorder(g);
  // Glazed tall enclosure and ceiling grille: above the native camera, open route beneath.
  foreach(int side in new[]{-1,1}){
   Box(g,"Tall shop wall",new Vector3(side*79,10,-5),new Vector3(.5f,20,180),stone);
   for(int z=-85;z<=75;z+=10){Box(g,"Vertical daylight louver",new Vector3(side*77.8f,16,z),new Vector3(.5f,12,.4f),ivory);Box(g,"Clerestory glass",new Vector3(side*78.3f,19,z+4.8f),new Vector3(.18f,7,8.7f),glass);}
  }
  foreach(float z in new[]{-94f,84f}){
   Box(g,"Retail enclosing wall",new Vector3(0,7,z+.3f),new Vector3(158,14,.5f),stone);
   Box(g,"End clerestory",new Vector3(0,19,z),new Vector3(158,10,.3f),blue);
   Box(g,"Upper end wall",new Vector3(0,19,z+.2f),new Vector3(158,8,.3f),ivory);
   for(int x=-78;x<=78;x+=6)Box(g,"End vertical louver",new Vector3(x,17,z-.5f),new Vector3(.3f,11,.6f),stone);
  }
  for(int x=-78;x<=78;x+=13)Box(g,"Skylight roof mullion",new Vector3(x,25,-5),new Vector3(.32f,.5f,178),ivory);
  for(int z=-89;z<=79;z+=12)Box(g,"Skylight cross mullion",new Vector3(0,25,z),new Vector3(158,.5f,.3f),ivory);
  Box(g,"Daylit glass roof",new Vector3(0,25.35f,-5),new Vector3(157,.06f,178),glass);
  // Outer continuous storefronts maintain believable human-scale bays.
  foreach(int side in new[]{-1,1})for(int z=-66;z<=58;z+=16)Shop(g,new Vector3(side*73.5f,0,z),side<0?-90:90,"ATELIER",stores++);
  Merge(g,"Podium");
  for(int i=0;i<3;i++)Atrium(i,-40+i*40);
  // Compact transverse storefronts close the horizon through the gentle turns.
  var ends=Bay("Retail ends");
  for(int x=-65;x<=65;x+=16){Shop(ends,new Vector3(x,0,75),0,x%2==0?"MAISON":"GALLERY",stores++);Shop(ends,new Vector3(x,0,-88.5f),180,"SHOE TOWER",stores++);}
  Merge(ends,"RetailEnds");
  var entry=Bay("Arrival identity",0,90,false);entry.localPosition=new Vector3(-60,0,-54);
  DirectorySign(entry,new Vector3(8.5f,0,18),"1F  ATRIUM","RETAIL  ·  SKY LIFT →");Merge(entry,"Arrival");
 }
 static void Shop(Transform parent,Vector3 pos,float yaw,string name,int variant){
  var g=Group(parent,"Shop bay "+variant);g.localPosition=pos;g.localRotation=Quaternion.Euler(0,yaw,0);
  Box(g,"Walnut recessed display",new Vector3(0,2.6f,3.5f),new Vector3(13,5.2f,.22f),variant%3==0?wood:linen);
  Box(g,"Display floor",new Vector3(0,.1f,.8f),new Vector3(13,.2f,5.6f),stone);
  foreach(float x in new[]{-6.4f,6.4f})Box(g,"Deep shop reveal",new Vector3(x,2.65f,.8f),new Vector3(.18f,5.3f,5.5f),wood);
  foreach(float x in new[]{-6.55f,6.55f})Box(g,"Porcelain jamb",new Vector3(x,3,0),new Vector3(.42f,6,.7f),stone);
  Box(g,"Thin floating canopy",new Vector3(0,6,.8f),new Vector3(13.6f,.42f,6),ivory);
  Box(g,"Warm recessed lighting",new Vector3(0,5.74f,1.1f),new Vector3(12.9f,.09f,3.8f),warm);
  Box(g,"Store name fascia",new Vector3(0,5.14f,-1.9f),new Vector3(12.8f,1,.14f),ink);Label(g,name,new Vector3(0,5.13f,-2.02f),new Vector2(11.4f,.85f));
  foreach(float x in new[]{-4.4f,0,4.4f}){
   Box(g,"Display plinth",new Vector3(x,.7f,.1f),new Vector3(2.6f,1.4f,2.2f),ivory);
   Box(g,"Framed back niche",new Vector3(x,3.1f,3.33f),new Vector3(3.4f,3.4f,.11f),ink);
   Box(g,"Linen display niche",new Vector3(x,3.1f,3.23f),new Vector3(3.05f,3.05f,.08f),variant%3==1?blue:linen);
   for(int level=0;level<2;level++){float sy=2.15f+level*1.35f;Box(g,"Back display shelf",new Vector3(x,sy,2.77f),new Vector3(3.15f,.10f,1.0f),wood);Box(g,"Shelf warm reveal",new Vector3(x,sy-.09f,3),new Vector3(2.9f,.055f,.45f),warm);for(int k=0;k<2;k++)Box(g,"Display volume",new Vector3(x-.65f+k*1.3f,sy+.35f,2.75f),new Vector3(.65f,.6f,.5f),((variant+k+level)%2==0)?ivory:terracotta);}
   // Bags, folded textiles and ceramics are explicitly native props, not AI output.
   for(int n=0;n<3;n++){Box(g,"Folded textile",new Vector3(x,1.48f+n*.16f,.1f),new Vector3(1.5f,.14f,1.3f),n%2==0?wood:stone);}
  }
  foreach(float x in new[]{-4.5f,4.5f}){Box(g,"Display glazing",new Vector3(x,2.75f,-1.67f),new Vector3(3.8f,4.3f,.06f),retailGlass);Box(g,"Slim brass mullion",new Vector3(x-2,2.7f,-1.76f),new Vector3(.08f,4.5f,.12f),metal);}
 }
 static void Atrium(int index,float x){
  var g=Bay("Atrium "+(index+1));g.localPosition=new Vector3(x,0,-13);
  for(int level=1;level<=2;level++){
   float y=level*6.4f;
   Ring(g,"Curved mezzanine slab",new Vector3(0,y,0),12,27,3.1f,.5f,ivory);
   Ring(g,"Warm curved soffit",new Vector3(0,y-.10f,0),11.8f,26.8f,.13f,.08f,warm);
   Ring(g,"Glass balustrade",new Vector3(0,y+.5f,0),9,24,.07f,1.15f,glass);
   Ring(g,"Metal handrail",new Vector3(0,y+1.63f,0),9.04f,24.04f,.12f,.075f,metal);
   for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;Box(g,"Guardrail post",new Vector3(Mathf.Cos(a)*8.98f,y+1.05f,Mathf.Sin(a)*23.98f),new Vector3(.065f,1.15f,.065f),metal);}
   // Warm upper shop windows behind the curved fronts.
   foreach(int side in new[]{-1,1})for(int z=-16;z<=16;z+=8){
    float edge=12*Mathf.Sqrt(1-z*z/(27f*27f));
    Box(g,"Upper shop back",new Vector3(side*(edge-3.0f),y+2.5f,z),new Vector3(.2f,4.1f,7.4f),((z+index)%3==0)?wood:linen);
    Box(g,"Upper display room floor",new Vector3(side*(edge-1.5f),y+.2f,z),new Vector3(3,.18f,7.4f),stone);
    foreach(float dz in new[]{-3.65f,3.65f})Box(g,"Upper display room return",new Vector3(side*(edge-1.5f),y+2.5f,z+dz),new Vector3(3,4.1f,.06f),retailGlass);
    Box(g,"Upper store ceiling glow",new Vector3(side*(edge-1.6f),y+4.40f,z),new Vector3(2.5f,.07f,6.6f),warm);
    for(int tier=0;tier<2;tier++){
     float sy=y+1.3f+tier*1.25f;
     Box(g,"Upper recessed shelf",new Vector3(side*(edge-2.35f),sy,z),new Vector3(1.05f,.1f,6.7f),wood);
     Box(g,"Upper under-shelf light",new Vector3(side*(edge-2.65f),sy-.10f,z),new Vector3(.30f,.06f,6.4f),warm);
     for(int item=0;item<4;item++){float iz=z-2.4f+item*1.6f;float h=.42f+((item+index+tier)%3)*.17f;Box(g,"Upper arranged display",new Vector3(side*(edge-2.20f),sy+h*.5f+.06f,iz),new Vector3(.55f,h,.83f),((item+index)%3==0)?terracotta:ivory);}
    }
    Box(g,"Upper shop glazing",new Vector3(side*(edge-.4f),y+2.5f,z),new Vector3(.06f,3.1f,6.1f),retailGlass);
    Box(g,"Upper store canopy",new Vector3(side*(edge-1.5f),y+4.65f,z),new Vector3(3.5f,.25f,7.9f),ivory);
    foreach(float dz in new[]{-3.7f,3.7f})Box(g,"Upper store jamb",new Vector3(side*(edge-.36f),y+2.55f,z+dz),new Vector3(.3f,4.2f,.18f),metal);
   }
  }
  foreach(float z in new[]{-18f,18f}){Box(g,"Atrium support",new Vector3(0,6.3f,z),new Vector3(.65f,12.6f,.65f),stone);}
  Escalator(g,new Vector3(-3.2f,0,-17),false);Escalator(g,new Vector3(3.2f,6.4f,-1),true);
  LandingBridge(g,6.4f,-1.3f);LandingBridge(g,12.8f,-16.2f);
  var k=Group(g,"Walnut information kiosk");k.localPosition=new Vector3(0,0,17);
  Ring(k,"Curved timber desk",Vector3.zero,3.5f,2.3f,.8f,1.25f,wood,40);Ring(k,"Stone desktop",new Vector3(0,1.25f,0),3.6f,2.4f,1,.13f,ivory,40);
  foreach(float side in new[]{-1f,1f})Box(k,"Directory display",new Vector3(side*1.5f,2,0),new Vector3(1.6f,1.2f,.12f),ink);
  DirectorySign(g,new Vector3(0,0,-24),"ATRIUM  "+(index+1),"1F / 2F / 3F");
  Merge(g,"Atrium"+index);
  AddReusedPlant(g,new Vector3(0,1.39f,19),1.15f);
  foreach(float side in new[]{-1f,1f}){
   var lamp=Group(g,"Warm local retail light");lamp.localPosition=new Vector3(side*14f,9.6f,-6);lamp.localRotation=Quaternion.LookRotation(new Vector3(-side,-.12f,0));
   var light=lamp.gameObject.AddComponent<Light>();light.type=LightType.Spot;light.range=13;light.spotAngle=105;light.innerSpotAngle=65;light.color=new Color(1,.76f,.51f);light.intensity=20;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.Auto;
  }
 }
 static void AddReusedPlant(Transform p,Vector3 position,float height){
  const string path="Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/F10.prefab";
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new Exception("Previously approved TRELLIS F10 missing");
  var model=(GameObject)PrefabUtility.InstantiatePrefab(source,p);model.name="Reused TRELLIS F10 tabletop plant";model.transform.localPosition=Vector3.zero;
  foreach(var c in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
  foreach(var c in model.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);
  var renderers=model.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
  model.transform.localScale*=height/bounds.size.y;bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.shadowCastingMode=ShadowCastingMode.Off;}
  model.transform.position+=p.TransformPoint(position)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
 }
 static void LandingBridge(Transform g,float y,float z){
  Box(g,"Escalator arrival bridge",new Vector3(0,y+.05f,z),new Vector3(23,.24f,4.2f),ivory);
  // Openings aligned to both escalator mouths; no rail across a landing.
  foreach(float side in new[]{-1f,1f})foreach(float x in new[]{-8f,0,8f}){
   Box(g,"Bridge glazing",new Vector3(x,y+.75f,z+side*2.05f),new Vector3(x==0?2.2f:5.5f,1.15f,.06f),glass);
   Box(g,"Bridge metal handrail",new Vector3(x,y+1.34f,z+side*2.05f),new Vector3(x==0?2.2f:5.5f,.08f,.10f),metal);
  }
 }
 static void RouteBorder(Transform p){
  foreach(float side in new[]{-1f,1f}){
   var verts=new List<Vector3>();var ids=new List<int>();var points=new List<Vector3>();
   foreach(var s in director.route.segments.Where(s=>s.floor==0)){if(points.Count==0)points.Add(s.start);points.Add(s.end);}
   for(int i=0;i<points.Count;i++){
    Vector3 f=(i==0?points[1]-points[0]:i==points.Count-1?points[i]-points[i-1]:points[i+1]-points[i-1]).normalized;
    Vector3 right=Vector3.Cross(Vector3.up,f);verts.Add(points[i]+right*(side*6.8f-.045f)+Vector3.up*.043f);verts.Add(points[i]+right*(side*6.8f+.045f)+Vector3.up*.043f);
    if(i>0){int k=i*2;ids.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}
   }
   var mesh=new Mesh{name="Continuous brass route inlay"};mesh.SetVertices(verts);mesh.SetTriangles(ids,0);mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject(p,"Brass route inlay",mesh,metal);
  }
 }
 static void Escalator(Transform parent,Vector3 pos,bool reverse){
  var g=Group(parent,"Crossing escalator "+escalators++);g.localPosition=pos;g.localRotation=Quaternion.Euler(0,reverse?180:0,0);
  int n=28;float rise=6.4f,run=13.4f;
  Box(g,"Metal lower landing",new Vector3(0,.09f,-1.4f),new Vector3(2.9f,.18f,3.3f),metal);
  Box(g,"Metal upper landing",new Vector3(0,rise+.09f,run+1.2f),new Vector3(2.9f,.18f,2.9f),metal);
  var housing=Box(g,"Enclosed escalator housing",new Vector3(0,rise*.5f-.40f,run*.5f),new Vector3(2.9f,.48f,Mathf.Sqrt(rise*rise+run*run)+.5f),metal);housing.localRotation=Quaternion.LookRotation(new Vector3(0,rise,run));
  for(int i=0;i<n;i++){float z=(i+.5f)*run/n,y=(i+1)*rise/n,depth=rise/n+.025f;Box(g,"Metal tread",new Vector3(0,y-depth*.5f,z),new Vector3(2.4f,depth,run/n+.01f),grout);Box(g,"Tread safety edge",new Vector3(0,y+.006f,z-run/n*.43f),new Vector3(2.4f,.025f,.055f),metal);}
  foreach(float side in new[]{-1f,1f}){
   Beam(g,new Vector3(side*1.52f,-.18f,-.4f),new Vector3(side*1.52f,rise-.18f,run+.4f),.36f,ivory);
   for(int i=0;i<n;i++){float a=i/(float)n,b=(i+1)/(float)n;var pane=Box(g,"Sloped glass balustrade",new Vector3(side*1.5f,(a+b)*.5f*rise+.55f,(a+b)*.5f*run),new Vector3(.055f,1.1f,run/n+.015f),glass);}
   Beam(g,new Vector3(side*1.52f,1.12f,-.5f),new Vector3(side*1.52f,rise+1.12f,run+.5f),.13f,ink);
   Beam(g,new Vector3(side*1.52f,1.12f,-1.8f),new Vector3(side*1.52f,1.12f,-.5f),.13f,ink);
   Beam(g,new Vector3(side*1.52f,rise+1.12f,run+.5f),new Vector3(side*1.52f,rise+1.12f,run+1.8f),.13f,ink);
  }
 }
 static void DirectorySign(Transform p,Vector3 pos,string title,string detail){var g=Group(p,"Directory");g.localPosition=pos;Box(g,"Bronze directory",new Vector3(0,1.75f,0),new Vector3(4.2f,3.5f,.24f),ink);Box(g,"Brass cap",new Vector3(0,3.48f,-.15f),new Vector3(4.3f,.1f,.08f),metal);Label(g,title,new Vector3(0,2.2f,-.15f),new Vector2(3.9f,1.1f));Label(g,detail,new Vector3(0,1,-.15f),new Vector2(3.9f,.65f));}
 static void Entrance(){
  var goal=director.GetComponentsInChildren<Chapter45Goal>(true).Single();var g=Bay("Tower department store entrance",1640,1860,false);g.position=new Vector3(goal.transform.position.x,0,goal.transform.position.z+5);
  foreach(int side in new[]{-1,1}){
   Box(g,"Limestone podium wing",new Vector3(side*22,8,5),new Vector3(25,16,8),ivory);
   for(int i=0;i<5;i++){float x=side*(12.5f+i*4.5f);Box(g,"Recessed shopfront",new Vector3(x,5,.8f),new Vector3(3.8f,9,.3f),blue);Box(g,"Brass vertical fin",new Vector3(x-2,7,.4f),new Vector3(.18f,13,.55f),metal);}
   Box(g,"Warm entrance jamb",new Vector3(side*9.5f,5.5f,0),new Vector3(.6f,11,2),wood);
   Box(g,"Vertical portal light",new Vector3(side*9.15f,5.1f,-1.05f),new Vector3(.12f,9.3f,.10f),warm);
   DirectorySign(g,new Vector3(side*13,0,-9),"SHOE TOWER","ATRIUM  ·  ENTRANCE");
  }
  Box(g,"Floating entry canopy",new Vector3(0,11.1f,-1.7f),new Vector3(24,.7f,9),ivory);
  Box(g,"Glazed canopy inset",new Vector3(0,10.72f,-1.7f),new Vector3(17,.05f,7.8f),glass);
  Box(g,"Entry identity band",new Vector3(0,8.1f,-10),new Vector3(19,1.8f,.3f),ink);Label(g,"1F ATRIUM  ·  DEPARTMENT STORE",new Vector3(0,8.1f,-10.22f),new Vector2(18,1.3f));
  // Central 18m opening is unobstructed; the existing goal owns chapter transition.
  Box(g,"Warm welcome mat",new Vector3(0,.055f,-7),new Vector3(16,.04f,10),stone);
  for(int i=0;i<4;i++)Box(g,"Inset brass approach",new Vector3(0,.08f,-11+i*2.5f),new Vector3(14,.018f,.08f),metal);
  Merge(g,"Entrance");
 }
     public static void RequireIdle()
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
