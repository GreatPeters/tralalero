using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
public static class AuditEnvironmentArt {
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var setup=EditorSceneManager.GetSceneManagerSetup();var reports=new List<object>();var dependencies=new HashSet<string>();
  try{foreach(string sceneName in new[]{"Jamsil","ShoeTower"}){
   string path="Assets/ShooterSurvival/Scenes/Tools/"+sceneName+".unity";var scene=EditorSceneManager.OpenScene(path);var director=Object.FindFirstObjectByType<Chapter45Director>();var roots=new List<object>();
   foreach(string name in new[]{"RetailCore_20261003","ChapterFinish_20261003","RetailGoods_20261003","RetailTowelMeshy_20261003","EnvironmentArt_20261003"}){
    var matches=director.GetComponentsInChildren<Transform>(true).Where(t=>t.name==name).ToArray();if(matches.Length!=1)throw new Exception("Duplicate or missing "+name);var root=matches[0];var colliders=root.GetComponentsInChildren<Collider>(true);if(colliders.Length!=0)throw new Exception("Decorative collision added");
    var meshes=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).ToArray();var renderers=root.GetComponentsInChildren<Renderer>(true);if(renderers.Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null)))throw new Exception("Missing material/shader");
    roots.Add(new{name,colliders=colliders.Length,meshRenderers=renderers.Length,meshTriangles=meshes.Sum(f=>(long)Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(long)f.sharedMesh.GetIndexCount(i)/3)),groups=root.GetComponentsInChildren<Chapter45SceneryGroup>(true).Length});
   }
   var deps=AssetDatabase.GetDependencies(path,true);foreach(string dep in deps)dependencies.Add(dep);var rejected=deps.Where(x=>x.Contains("RetailTextiles")).ToArray();if(rejected.Length!=0)throw new Exception("Rejected textile referenced by scene");
   reports.Add(new{scene=sceneName,roots,routeLength=director.route.Length,lifts=director.lifts.Length,choices=director.choices.Length,rejectedTextileDependencies=rejected,dependencies=deps.Length});
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  const string art="Assets/ShooterSurvival/Models/Chapters/Chapters45/RetailProduction20261003";var imports=new List<object>();foreach(string name in new[]{"RetailCounter","RetailShelf","RetailCaseBase","RetailBench","RetailPlanter","RetailTote","RetailVase","RetailSneaker","RetailTowelMeshy"}){
   var model=(ModelImporter)AssetImporter.GetAtPath(art+"/"+name+".fbx");var texture=(TextureImporter)AssetImporter.GetAtPath(art+"/"+name+"_BaseColor.png");var android=texture.GetPlatformTextureSettings("Android");if(model.addCollider||model.isReadable||texture.isReadable||texture.maxTextureSize>1024||android.format!=TextureImporterFormat.ASTC_6x6)throw new Exception("Import budget mismatch "+name);
   imports.Add(new{name,model.isReadable,model.addCollider,texture.maxTextureSize,texture.mipmapEnabled,androidFormat=android.format.ToString()});
  }
  string output="outputs/department-store-2026-10-02/trellis-recovery/environment-qa-20261003/scene-audit.json";File.WriteAllText(output,Json(new{scenes=reports,imports,referencedNativeFolders=dependencies.Where(x=>x.Contains("RetailProduction20261003/Native-")).Select(x=>Path.GetDirectoryName(x).Replace('\\','/')).Distinct().OrderBy(x=>x).ToArray(),limits="Saved scene dependency/import/decorative-collision audit; does not certify phone frame rate."}));return output;
 }
}
