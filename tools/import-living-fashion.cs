using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using Object=UnityEngine.Object;
public static class ImportLivingFashion {
 const string Base="Assets/ShooterSurvival/Models/Chapters/Chapters45/DetailedMall-20261003T092859667/";
 const string Dir=Base+"GeneratedModels";
 static readonly List<object> audit=new();
 static GameObject Import(string name){
  var mi=AssetImporter.GetAtPath(Dir+"/"+name+".fbx") as ModelImporter;if(mi==null)throw new Exception("Missing model "+name);mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.animationType=ModelImporterAnimationType.None;mi.importAnimation=false;mi.isReadable=false;mi.SaveAndReimport();
  string texPath=Dir+"/"+name+"_M0_0.png";var ti=(TextureImporter)AssetImporter.GetAtPath(texPath);ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.Compressed;ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6,compressionQuality=50});ti.SaveAndReimport();
  var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name+" generated base color",enableInstancing=true};mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",name.Contains("Vacuum")?.3f:.16f);AssetDatabase.CreateAsset(mat,Dir+"/"+name+".mat");
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/"+name+".fbx");var g=new GameObject(name);var imported=(GameObject)PrefabUtility.InstantiatePrefab(source);imported.transform.SetParent(g.transform,false);imported.transform.localPosition=Vector3.zero;
  var rr=g.GetComponentsInChildren<MeshRenderer>(true);foreach(var r in rr){r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);int triangles=g.GetComponentsInChildren<MeshFilter>(true).Sum(m=>(int)m.sharedMesh.GetIndexCount(0)/3);
  if(Mathf.Abs(b.size.y-1)>.03f||Mathf.Abs(b.min.y)>.03f||triangles>(name=="DetailedClothingRack"?16000:3600)||g.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Model native contract failed: "+name+" "+b+" tris"+triangles);
  audit.Add(new{name,triangles,height=b.size.y,bottom=b.min.y,size=new[]{b.size.x,b.size.y,b.size.z},sourceRotationPreserved=Quaternion.Angle(source.transform.localRotation,imported.transform.localRotation)<.01f,colliders=0,android="1024 ASTC6x6 mipmaps"});var prefab=PrefabUtility.SaveAsPrefabAsset(g,Dir+"/"+name+".prefab");Object.DestroyImmediate(g);return prefab;
 }
 static void Place(GameObject prefab,Transform parent,string label,Vector3 position,float height,float yaw=0){var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name=label;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*height;}
 static GameObject Box(Transform p,string n,Vector3 pos,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 public static object Main(string phase){
  if(phase!="living"&&phase!="fashion")throw new Exception("Unknown phase");if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Dirty scene");
  EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");var d=Object.FindFirstObjectByType<Chapter45Director>();var root=d.transform.Find("DetailedMall_20261003");if(root.Find("Generated "+phase+" revision")!=null)throw new Exception("Already applied");
  File.Copy(d.gameObject.scene.path,"outputs/chapter45-detailed-design-2026-10-03/scene-recovery/ShoeTower-before-generated-"+phase+".unity",false);AssetDatabase.Refresh();audit.Clear();int beds=0,vacuums=0,racks=0,archived=0;var scenery=root.Find("Scenery");
  if(phase=="living"){
   var bed=Import("DetailedBed");var vacuum=Import("DetailedVacuum");var living=scenery.Cast<Transform>().Single(t=>t.name.StartsWith("4F "));var wood=AssetDatabase.LoadAssetAtPath<Material>(Base+"Warm oak.mat");
   foreach(var shop in living.Cast<Transform>().Where(t=>t.name.Contains("storefront"))){
    var old=new GameObject("Preserved living bench display").transform;old.SetParent(shop,false);foreach(var t in shop.Cast<Transform>().Where(t=>t.name.StartsWith("RetailBench")).ToArray()){t.SetParent(old,true);archived++;}old.gameObject.SetActive(false);
    int i=0;foreach(var w in shop.Cast<Transform>().Where(t=>t.name=="Generated washer wall-backed display"))w.localPosition=new Vector3(-5.4f+1.7f*i++,0,3.7f);
    Place(bed,shop,"Generated TRELLIS bed display",new Vector3(2.7f,0,2.5f),.9f,90);beds++;
    Place(vacuum,shop,"Generated TRELLIS vacuum display",new Vector3(6.1f,0,3.7f),1.2f);vacuums++;
    Box(shop,"Low ceramic display table",new Vector3(0,.18f,2),new Vector3(1.1f,.36f,1.1f),wood);
   }
  }else{
   var rack=Import("DetailedClothingRack");var fashion=scenery.Cast<Transform>().Single(t=>t.name.StartsWith("2F "));
   foreach(var shop in fashion.Cast<Transform>().Where(t=>t.name.Contains("storefront"))){var old=new GameObject("Preserved provisional garment rails").transform;old.SetParent(shop,false);foreach(var t in shop.Cast<Transform>().Where(t=>t.name=="Garment rail"||t.name=="Rack left"||t.name=="Rack right"||t.name.StartsWith("Garment silhouette")).ToArray()){t.SetParent(old,true);archived++;}old.gameObject.SetActive(false);foreach(float x in new[]{0f}){Place(rack,shop,"Generated TRELLIS clothing rack",new Vector3(x,0,3.1f),1.9f);racks++;}}
  }
  new GameObject("Generated "+phase+" revision").transform.SetParent(root,false);EditorSceneManager.MarkSceneDirty(d.gameObject.scene);if(!EditorSceneManager.SaveScene(d.gameObject.scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();return new{phase,imported=audit.ToArray(),bedInstances=beds,vacuumInstances=vacuums,rackInstances=racks,archivedProvisionalObjects=archived,newColliders=0,routeAndCombatChanged=false,review="Native bounds/pivot/material contracts passed; actual Play visual review pending"};
 }
}
