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
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class InstallMallTrellis {
 const string Art="Assets/ShooterSurvival/Models/Chapters/Chapters45/DepartmentStore/Trellis20261003";
 const string ScenePath="Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity";
 const string Out="outputs/department-store-2026-10-02/trellis-recovery";
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static long Tris(GameObject g)=>g.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).Sum(f=>(long)f.sharedMesh.GetIndexCount(0)/3);
 public static object Probe(){RequireIdle();var d=Object.FindFirstObjectByType<Chapter45Director>();var old=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/F10.prefab");return new{materials=old.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Select(m=>new{path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,textures=m.GetTexturePropertyNames()}),segments=d.route.segments.Where(s=>s.floor==0).Select(s=>new{start=s.start.ToString("R"),end=s.end.ToString("R"),s.length})};}
 static Material Import(string kind){
  string basePath=Art+"/"+kind;string modelPath=basePath+".fbx",imagePath=basePath+"_BaseColor.png";
  AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport);AssetDatabase.ImportAsset(imagePath,ImportAssetOptions.ForceSynchronousImport);
  var mi=(ModelImporter)AssetImporter.GetAtPath(modelPath);mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.importAnimation=false;mi.addCollider=false;mi.isReadable=false;mi.importNormals=ModelImporterNormals.Import;mi.importTangents=ModelImporterTangents.CalculateMikk;mi.globalScale=1;mi.SaveAndReimport();
  var ti=(TextureImporter)AssetImporter.GetAtPath(imagePath);ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.None;ti.mipmapEnabled=true;ti.maxTextureSize=1024;ti.isReadable=false;ti.textureCompression=TextureImporterCompression.CompressedHQ;
  var mobile=ti.GetPlatformTextureSettings("Android");mobile.overridden=true;mobile.maxTextureSize=1024;mobile.format=TextureImporterFormat.ASTC_6x6;ti.SetPlatformTextureSettings(mobile);ti.SaveAndReimport();
  var style=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/RestStopProduction20260925/F10/Surface_0.mat");if(style==null)throw new Exception("Existing scene prop style missing");
  var mat=AssetDatabase.LoadAssetAtPath<Material>(basePath+".mat");if(mat==null){mat=new Material(style);AssetDatabase.CreateAsset(mat,basePath+".mat");}else{mat.shader=style.shader;mat.CopyPropertiesFromMaterial(style);}mat.name=kind+" matte generated surface";mat.enableInstancing=true;
  foreach(string p in new[]{"_BumpMap","_MetallicGlossMap","_SpecGlossMap"})if(mat.HasProperty(p))mat.SetTexture(p,null);mat.DisableKeyword("_NORMALMAP");mat.DisableKeyword("_METALLICSPECGLOSSMAP");mat.DisableKeyword("DR_OUTLINE_ON");
  mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.2f);mat.SetFloat("_SpecularEnabled",0);mat.SetFloat("_SpecularHighlights",0);mat.SetFloat("_OutlineEnabled",0);mat.SetFloat("_Cull",kind=="MallPlant"?0:2);mat.doubleSidedGI=kind=="MallPlant";EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);return mat;
 }
 static GameObject Place(string kind,Transform parent,Vector3 bottom,float height,Material mat,string name){
  var src=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+kind+".fbx");if(src==null)throw new Exception("Missing generated asset "+kind);
  var g=(GameObject)PrefabUtility.InstantiatePrefab(src,parent);g.name=name;g.transform.localPosition=Vector3.zero; // Preserve the FBX's native axis-conversion rotation.
  if(g.GetComponentsInChildren<Collider>(true).Length!=0||g.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new Exception("Visual asset has gameplay component");
  foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;}
  var b=BoundsOf(g);g.transform.localScale*=height/b.size.y;b=BoundsOf(g);g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);
  b=BoundsOf(g);if(Mathf.Abs(b.size.y-height)>.001f||Mathf.Abs(b.min.y-bottom.y)>.001f)throw new Exception("Model scale/pivot mismatch");return g;
 }
 public static object Main(bool save=false,bool includeCounter=false){
  RequireIdle();var setup=EditorSceneManager.GetSceneManagerSetup();string folder=Out+"/"+(save?"applied-":"preview-")+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");Directory.CreateDirectory(folder);
  try{
   var scene=EditorSceneManager.OpenScene(ScenePath);var d=Object.FindFirstObjectByType<Chapter45Director>();var owned=d.transform.Find("Scenery/DepartmentStore_20261002");
   if(owned.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Generated TRELLIS")))throw new Exception("Already installed: do not duplicate");
   string before=ProtectedState(scene);Capture(folder+"/before");scene=EditorSceneManager.OpenScene(ScenePath);d=Object.FindFirstObjectByType<Chapter45Director>();owned=d.transform.Find("Scenery/DepartmentStore_20261002");
   if(before!=ProtectedState(scene))throw new Exception("Capture changed saved source state");
   var plantMat=Import("MallPlant");var counterMat=includeCounter?Import("MallCounter"):null;
   var plants=owned.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Reused TRELLIS F10 tabletop plant").ToArray();if(plants.Length!=3)throw new Exception("Expected exactly3 existing plants");
   long oldTris=plants.Sum(t=>Tris(t.gameObject));var rows=new List<object>();
   foreach(var old in plants){var b=BoundsOf(old.gameObject);var bottom=new Vector3(b.center.x,b.min.y,b.center.z);var g=Place("MallPlant",old.parent,bottom,b.size.y,plantMat,"Generated TRELLIS MallPlant 20261003");rows.Add(new{kind="plant",path=PathOf(g.transform),triangles=Tris(g),bottom=bottom.ToString("R"),dimensions=BoundsOf(g).size.ToString("R")});Object.DestroyImmediate(old.gameObject);}
   if(includeCounter)foreach(string atrium in new[]{"Atrium 1","Atrium 3"}){var p=owned.Find(atrium);var pos=p.TransformPoint(new Vector3(5.8f,.031f,21));var g=Place("MallCounter",p,pos,1.3f,counterMat,"Generated TRELLIS MallCounter 20261003");var b=BoundsOf(g);float clearance=RouteClearance(d,b);if(clearance<8)throw new Exception("Counter too close to travel corridor");rows.Add(new{kind="counter",path=PathOf(g.transform),triangles=Tris(g),bottom=pos.ToString("R"),dimensions=b.size.ToString("R"),minimumRouteCenterClearance=clearance});}
   if(before!=ProtectedState(scene))throw new Exception("Protected gameplay/cameras/lights changed");
   var newObjects=owned.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Generated TRELLIS")).ToArray();long newTris=newObjects.Sum(t=>Tris(t.gameObject));
   if(newObjects.Any(t=>t.GetComponentsInChildren<Collider>(true).Length!=0))throw new Exception("New collision");
   if(save){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");scene=EditorSceneManager.OpenScene(ScenePath);if(before!=ProtectedState(scene))throw new Exception("Reopened state changed");}
   Capture(folder+"/after");var report=new{saved=save,folder,newModels=includeCounter?2:1,instances=rows,oldPlantTriangles=oldTris,newTriangles=newTris,triangleDelta=newTris-oldTris,texturePolicy="1 shared1K basecolor per type, Android ASTC6x6, mipmaps, unreadable",addedColliders=0,protectedStateUnchanged=true,sourceScene=ScenePath,art=Art};File.WriteAllText(folder+"/installation.json",Json(report));return report;
  }finally{EditorSceneManager.OpenScene(ScenePath);EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
 static void Capture(string folder){
  Directory.CreateDirectory(folder);var d=Object.FindFirstObjectByType<Chapter45Director>();var player=Object.FindFirstObjectByType<PlayerScript>();var c=Camera.main;var offset=c.transform.position-player.transform.position;var rotation=c.transform.rotation;
  foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
  foreach(var g in d.GetComponentsInChildren<Chapter45SceneryGroup>(true))g.SetVisible(g.floor==0);
  var poses=new List<object>();foreach(float station in new[]{0f,45f,175f,320f}){d.route.Sample(station,out var center,out var forward);var yaw=Quaternion.LookRotation(forward);player.transform.SetPositionAndRotation(center+Vector3.up*d.footOffset,yaw);c.transform.SetPositionAndRotation(player.transform.position+yaw*offset,yaw*rotation);Shot(c,folder+"/route-"+station.ToString("0000")+".png");poses.Add(new{station,position=c.transform.position.ToString("R"),rotation=c.transform.rotation.ToString("R"),fov=c.fieldOfView});}
  c.transform.position=new Vector3(-43,5.5f,12);c.transform.LookAt(new Vector3(-40,2.0f,6));Shot(c,folder+"/plant-kiosk.png");
  c.transform.position=new Vector3(-28,6,18);c.transform.LookAt(new Vector3(-34.2f,1.4f,8));Shot(c,folder+"/wood-counter.png");
  c.transform.position=new Vector3(-56,10,23);c.transform.LookAt(new Vector3(-39,2.5f,6));Shot(c,folder+"/atrium-service.png");
  File.WriteAllText(folder+"/camera-conditions.json",Json(new{routePoses=poses,width=720,height=1280,uiHidden=true,directedViews=new[]{"plant-kiosk","wood-counter","atrium-service"},limits="Editor native camera renders, not ordinary gameplay evidence"}));
 }
 static void Shot(Camera c,string path){var rt=RenderTexture.GetTemporary(720,1280,24,RenderTextureFormat.ARGB32);var target=c.targetTexture;var active=RenderTexture.active;float aspect=c.aspect;try{c.targetTexture=rt;c.aspect=720f/1280;c.Render();RenderTexture.active=rt;var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);}finally{c.targetTexture=target;c.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}}
 static float RouteClearance(Chapter45Director d,Bounds b){float min=float.PositiveInfinity;foreach(var s in d.route.segments.Where(s=>s.floor==0)){int steps=Mathf.CeilToInt(Vector3.Distance(s.start,s.end)*4);for(int i=0;i<=steps;i++){var p=Vector3.Lerp(s.start,s.end,i/(float)Mathf.Max(1,steps));p.y=b.center.y;min=Mathf.Min(min,Vector3.Distance(p,b.ClosestPoint(p)));}}return min;}
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
            if (c == null || PathOf(c.transform).Contains("Reused TRELLIS F10 tabletop plant") || PathOf(c.transform).Contains("Generated TRELLIS")) continue;
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
