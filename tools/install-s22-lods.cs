using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class S22SceneryLods
{
 const string Dir="Assets/ShooterSurvival/Models/Generated/S22Polish/LODs";
 static readonly Dictionary<string,Mesh> cache=new();
 static Mesh Build(Mesh original,string source,string label,int level){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string guid,out long id);string key=label+"_"+level+"_"+guid+"_"+id;string dest=Dir+"/"+key+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(dest);if(saved!=null)return saved;if(cache.TryGetValue(key,out saved))return saved;string p=Dir+"/"+label+"_LOD"+level+".fbx";AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);var imp=(ModelImporter)AssetImporter.GetAtPath(p);imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importCameras=false;imp.importLights=false;imp.SaveAndReimport();
  var oldModel=AssetDatabase.LoadAssetAtPath<GameObject>(source);var oldFilter=oldModel.GetComponentsInChildren<MeshFilter>(true).First(f=>f.sharedMesh==original);var model=AssetDatabase.LoadAssetAtPath<GameObject>(p);var combine=new List<CombineInstance>();foreach(var f in model.GetComponentsInChildren<MeshFilter>(true))for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++)combine.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=sub,transform=oldFilter.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
  var mesh=new Mesh{name=key};mesh.CombineMeshes(combine.ToArray(),true,true);var v=mesh.vertices;if(v.Any(x=>float.IsNaN(x.x)||float.IsNaN(x.y)||float.IsNaN(x.z)||float.IsInfinity(x.sqrMagnitude)))throw new Exception("Invalid LOD coordinates: "+key);
  var a=mesh.bounds;var b=original.bounds;var scale=new Vector3(b.size.x/Mathf.Max(.000001f,a.size.x),b.size.y/Mathf.Max(.000001f,a.size.y),b.size.z/Mathf.Max(.000001f,a.size.z));
  for(int i=0;i<v.Length;i++)v[i]=b.center+Vector3.Scale(v[i]-a.center,scale);mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,dest);cache.Add(key,mesh);return mesh;
 }
 public static object Main(){if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");var setup=EditorSceneManager.GetSceneManagerSetup();var rows=new List<object>();
  try{foreach(var name in new[]{"Noryangjin_MapTool_Mode_SR18_Revamp","HighWay","RestStop"}){var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");int count=0;foreach(var f in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).ToArray()){if(f.sharedMesh==null||f.GetComponentInParent<LODGroup>()!=null)continue;string p=AssetDatabase.GetAssetPath(f.sharedMesh);string label=p.Contains("014_STAGE01")?"shop":p.Contains("016_STAGE01")?"display":p.Contains("015_STAGE01")?"restaurant":p.Contains("040_STAGE01")?"scatter":p.Contains("065_STAGE02")?"skyline":null;if(label==null)continue;var r=f.GetComponent<MeshRenderer>();if(r==null||r.sharedMaterials.Length!=1)continue;var group=f.gameObject.AddComponent<LODGroup>();var renderers=new Renderer[2];renderers[0]=r;
   for(int level=1;level<=1;level++){var child=new GameObject("S22_LOD"+level);child.transform.SetParent(f.transform,false);child.layer=f.gameObject.layer;GameObjectUtility.SetStaticEditorFlags(child,GameObjectUtility.GetStaticEditorFlags(f.gameObject));child.AddComponent<MeshFilter>().sharedMesh=Build(f.sharedMesh,p,label,level);var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterials=r.sharedMaterials;renderer.enabled=r.enabled;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=r.receiveShadows;renderers[level]=renderer;}
   group.SetLODs(new[]{new LOD(.14f,new[]{renderers[0]}),new LOD(0,new[]{renderers[1]})});group.RecalculateBounds();EditorUtility.SetDirty(group);count++;
  }EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);rows.Add(new{scene=name,groups=count});}}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)rows});File.WriteAllText("outputs/s22-polish-2026-10-01/lod-install.json",json);return rows;
 }
}
