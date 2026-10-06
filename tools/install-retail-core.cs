using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.SceneManagement;using UnityEditor;using UnityEditor.SceneManagement;using TMPro;using IndianOceanAssets.ShooterSurvival;using Object=UnityEngine.Object;
public static class InstallRetailCore{
 const string Art="Assets/ShooterSurvival/Models/Chapters/Chapters45/RetailProduction20261003";
 const string RootName="RetailCore_20261003";
 const string Out="outputs/department-store-2026-10-02/trellis-recovery";
 static Dictionary<string,Material> materials=new Dictionary<string,Material>();static List<object> placed;static string revision;static Material wood,ivory,glass,metal,soil;
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 public static object Main(bool save=false){
  RequireIdle();var setup=EditorSceneManager.GetSceneManagerSetup();string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");string folder=Out+"/core-"+(save?"applied-":"preview-")+stamp;Directory.CreateDirectory(folder);revision=Art+"/Native-"+stamp;Directory.CreateDirectory(revision);AssetDatabase.Refresh();var reports=new List<object>();
  try{
   foreach(string n in new[]{"RetailCounter","RetailShelf","RetailCaseBase","RetailBench","RetailPlanter"})materials[n]=Import(n);
   PrepareMaterials();
   foreach(string name in new[]{"ShoeTower","Jamsil"}){
    string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=EditorSceneManager.OpenScene(path);var d=Object.FindFirstObjectByType<Chapter45Director>();if(d.transform.Find("Scenery/"+RootName)!=null)throw new Exception("Core already installed; use incremental follow-up");
    string before=ProtectedState(scene);Capture(name,folder+"/"+name+"/before");scene=EditorSceneManager.OpenScene(path);d=Object.FindFirstObjectByType<Chapter45Director>();if(before!=ProtectedState(scene))throw new Exception("Capture altered saved state");
    var root=Group(d.transform.Find("Scenery"),RootName);placed=new List<object>();if(name=="ShoeTower")Tower(d,root);else City(d,root);
    if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Decorative collider");
    if(before!=ProtectedState(scene))throw new Exception("Protected components changed before save");
    foreach(var guid in AssetDatabase.FindAssets("",new[]{revision}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
    if(save){File.Copy(path,folder+"/"+name+"-before.unity",false);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");scene=EditorSceneManager.OpenScene(path);if(before!=ProtectedState(scene))throw new Exception("Protected state changed after reopen");}
    var receipt=new{scene=name,saved=save,protectedStateUnchanged=true,addedColliders=0,instances=placed.ToArray(),revision,scope="Core fixtures only; merchandise, street/podium and upper-floor follow-up remain pending"};reports.Add(receipt);File.WriteAllText(folder+"/"+name+"/installation.json",Json(receipt));Capture(name,folder+"/"+name+"/after");
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
 static void Tower(Chapter45Director d,Transform root){
  var floor=Bay(root,"Lobby fixtures",0,0,530,true);
  foreach(float x in new[]{-34.2f,45.8f}){var g=Model(floor,"RetailCounter",new Vector3(x,.031f,8),1.3f,180);if(Clearance(d,BoundsOf(g),0)<8)throw new Exception("Counter route clearance");}
  foreach(float x in new[]{-43f,43f})Case(floor,new Vector3(x,.031f,15),0);
  var store=d.transform.Find("Scenery/DepartmentStore_20261002");
  foreach(int i in new[]{0,6,9,15}){var bay=store.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Shop bay "+i);var g=Group(floor,"Shop shelf "+i);g.SetPositionAndRotation(bay.position,bay.rotation);Shelf(g,new Vector3(-4.4f,.21f,.85f),0,2.75f);
   var names=new[]{"Display plinth","Back display shelf","Shelf warm reveal","Display volume","Folded textile"};
   var targets=bay.GetComponentsInChildren<Transform>(true).Where(t=>names.Contains(t.name)&&t.localPosition.x < -2.5f);
   placed.Add(CubeSurgery.Strip(store,targets,revision,"Shelf-bay-"+i));}
  // The rest of the upper-floor layout is added in the separate audited scope pass.
 }
 static void City(Chapter45Director d,Transform root){
  var entrance=d.transform.Find("Scenery/DepartmentStore_20261002/Tower department store entrance");if(entrance==null)throw new Exception("Existing entrance missing");var entry=Bay(root,"Entrance display cases",0,1640,1860);entry.SetPositionAndRotation(entrance.position,entrance.rotation);
  foreach(float side in new[]{-1f,1f})Case(entry,new Vector3(side*16,.031f,-21),0);
  foreach(float station in new[]{1545f,1680f}){
   d.route.Sample(station,out var p,out var f);var g=Bay(root,"Plaza seating "+station,0,station-50,station+40);g.SetPositionAndRotation(p,Quaternion.LookRotation(f));float side=station<1600?-1:1;
   var bench=Model(g,"RetailBench",new Vector3(side*18,.04f,0),1.1f,side>0?-90:90);if(Clearance(d,BoundsOf(bench),0)<8)throw new Exception("Bench route clearance");Planter(g,new Vector3(side*18,.04f,4.2f),90);
  }
 }
 static float Clearance(Chapter45Director d,Bounds b,int floor){float min=float.PositiveInfinity;float station=0;var choice=d.GetComponentsInChildren<Chapter45Choice>(true).FirstOrDefault(c=>c.kind==Chapter45Choice.ChoiceKind.RouteFork);foreach(var s in d.route.segments){if(s.floor==floor){int count=Mathf.Max(1,Mathf.CeilToInt(s.Length*2));for(int i=0;i<=count;i++){float t=i/(float)count;var p=Vector3.Lerp(s.start,s.end,t);var right=Vector3.Cross(Vector3.up,(s.end-s.start).normalized);foreach(int branch in new[]{0,1}){var v=p+right*(choice==null?0:choice.Offset(station+s.Length*t,branch));v.y=b.center.y;min=Mathf.Min(min,Vector3.Distance(v,b.ClosestPoint(v)));}}}station+=s.Length;}return min;}
 static void Capture(string sceneName,string folder){
  Directory.CreateDirectory(folder);var d=Object.FindFirstObjectByType<Chapter45Director>();var player=Object.FindFirstObjectByType<PlayerScript>();var c=Camera.main;var offset=c.transform.position-player.transform.position;var rot=c.transform.rotation;foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible(g.floor<=0);
  foreach(float station in sceneName=="Jamsil"?new[]{230f,1050,1545,1740,1800}:new[]{45f,175,320}){d.route.Sample(station,out var p,out var f);var q=Quaternion.LookRotation(f);player.transform.SetPositionAndRotation(p+Vector3.up*d.footOffset,q);c.transform.SetPositionAndRotation(player.transform.position+q*offset,q*rot);Shot(c,folder+"/route-"+station.ToString("0000")+".png");}
  if(sceneName=="ShoeTower"){
   c.transform.position=new Vector3(-28,5,16);c.transform.LookAt(new Vector3(-34.2f,1.1f,8));Shot(c,folder+"/counter.png");
   c.transform.position=new Vector3(-49,3.4f,21);c.transform.LookAt(new Vector3(-43,1.1f,15));Shot(c,folder+"/glass-case.png");
   c.transform.position=new Vector3(-68.5f,3.5f,-70.4f);c.transform.LookAt(new Vector3(-75.65f,1.6f,-70.4f));Shot(c,folder+"/shop-shelf.png");
  }else{
   var p=d.transform.Find("Scenery/DepartmentStore_20261002/Tower department store entrance").position;c.transform.position=p+new Vector3(-23,4,-29);c.transform.LookAt(p+new Vector3(-16,1.1f,-21));Shot(c,folder+"/entrance-case.png");
   d.route.Sample(1545,out var pp,out var ff);c.transform.position=pp+new Vector3(-10,4,-7);c.transform.LookAt(pp+new Vector3(-18,1,2));Shot(c,folder+"/plaza-seating.png");
  }
  File.WriteAllText(folder+"/conditions.json",Json(new{width=720,height=1280,uiHidden=true,kind="Native editor camera render; not live performance play"}));
 }
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
