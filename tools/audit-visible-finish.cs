using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
public static class AuditVisibleFinish {
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();var reports=new List<object>();var dependencies=new HashSet<string>();
  try{foreach(string sceneName in new[]{"Jamsil","ShoeTower"}){
   string path="Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity";var scene=EditorSceneManager.OpenScene(path);var director=Object.FindFirstObjectByType<Chapter45Director>();var roots=new List<object>();
   foreach(string name in new[]{"RetailCore_20261003","ChapterFinish_20261003","RetailGoods_20261003","RetailTowelMeshy_20261003","EnvironmentArt_20261003","VisibleFinish_20261003"}){
    var matches=director.GetComponentsInChildren<Transform>(true).Where(t=>t.name==name).ToArray();if(matches.Length!=1)throw new Exception("Duplicate or missing "+name);var root=matches[0];var colliders=root.GetComponentsInChildren<Collider>(true);if(colliders.Length!=0)throw new Exception("Decorative collision added");
    var meshes=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).ToArray();var renderers=root.GetComponentsInChildren<Renderer>(true);if(renderers.Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||ShaderUtil.ShaderHasError(m.shader)||!m.shader.isSupported)))throw new Exception("Missing material/shader");
    roots.Add(new{name,colliders=colliders.Length,meshRenderers=renderers.Length,meshTriangles=meshes.Sum(f=>(long)Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(long)f.sharedMesh.GetIndexCount(i)/3)),groups=root.GetComponentsInChildren<Chapter45SceneryGroup>(true).Length});
   }
   
   var visible=director.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="VisibleFinish_20261003");
   if(sceneName=="Jamsil"){
    if(visible.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Visible store "))!=2)throw new Exception("Foreground count");
    var heroes=director.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hero storefront architecture v2");if(heroes.Cast<Transform>().Count(t=>t.name.StartsWith("Hero "))!=8)throw new Exception("Existing hero stores not preserved");
   }else{
    if(visible.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Finished visible boutique "))!=4||visible.GetComponentsInChildren<Transform>(true).Count(t=>t.parent!=null&&t.parent.name=="South gallery room frame"&&t.name.StartsWith("Upper room "))!=6)throw new Exception("Mall room count");
    var goods=director.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Reviewed goods in upper gallery boutiques");if(goods.childCount!=20)throw new Exception("Upper existing goods count changed");
    var relocated=goods.Cast<Transform>().Where(t=>Mathf.Abs(t.position.y-8.47f)<.01f).ToArray();if(relocated.Length!=6)throw new Exception("Relocated upper goods count");foreach(var t in relocated){var rs=t.GetComponentsInChildren<Renderer>(true);float bottom=rs.Min(r=>r.bounds.min.y);if(Mathf.Abs(bottom-8.47f)>.02f)throw new Exception("Upper goods bottom not supported "+t.name+":"+bottom);}
   }
   var deps=AssetDatabase.GetDependencies(path,true);foreach(string dep in deps)dependencies.Add(dep);var rejected=deps.Where(x=>x.Contains("RetailTextiles")).ToArray();if(rejected.Length!=0)throw new Exception("Rejected textile referenced by scene");
   reports.Add(new{scene=sceneName,roots,routeLength=director.route.Length,lifts=director.lifts.Length,choices=director.choices.Length,rejectedTextileDependencies=rejected,dependencies=deps.Length});
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  const string art="Assets/ShooterSurvival/Models/Chapters/Chapters45/RetailProduction20261003";var imports=new List<object>();foreach(string name in new[]{"RetailCounter","RetailShelf","RetailCaseBase","RetailBench","RetailPlanter","RetailTote","RetailVase","RetailSneaker","RetailTowelMeshy"}){
   var model=(ModelImporter)AssetImporter.GetAtPath(art+"/"+name+".fbx");var texture=(TextureImporter)AssetImporter.GetAtPath(art+"/"+name+"_BaseColor.png");var android=texture.GetPlatformTextureSettings("Android");if(model.addCollider||model.isReadable||texture.isReadable||texture.maxTextureSize>1024||android.format!=TextureImporterFormat.ASTC_6x6)throw new Exception("Import budget mismatch "+name);
   imports.Add(new{name,model.isReadable,model.addCollider,texture.maxTextureSize,texture.mipmapEnabled,androidFormat=android.format.ToString()});
  }
  string output="outputs/department-store-2026-10-02/trellis-recovery/environment-qa-20261003/scene-audit.json";File.WriteAllText(output,Json(new{scenes=reports,imports,referencedNativeFolders=dependencies.Where(x=>x.Contains("RetailProduction20261003/Native-")).Select(x=>Path.GetDirectoryName(x).Replace('\\','/')).Distinct().OrderBy(x=>x).ToArray(),limits="Saved scene dependency/import/decorative-collision audit; includes duplicate root, material/shader compile, room count, existing upper goods count and6 relocated support-bottom checks. Does not certify phone frame rate."}));return output;
 }
}
