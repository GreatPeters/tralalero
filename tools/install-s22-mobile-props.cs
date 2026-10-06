using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S22MobileProps
{
 const string Dir="Assets/ShooterSurvival/Models/Generated/S22Polish/Props";
 const string Out="outputs/s22-polish-2026-10-01";
 static readonly Dictionary<string,Mesh> variants=new();
 static readonly Dictionary<string,Mesh> sources=new();
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 static Mesh Source(string name){if(sources.TryGetValue(name,out var known))return known;string path=Dir+"/"+name+".fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var instances=new List<CombineInstance>();foreach(var f in model.GetComponentsInChildren<MeshFilter>(true))for(int i=0;i<f.sharedMesh.subMeshCount;i++)instances.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=model.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});var mesh=new Mesh{name=name};mesh.CombineMeshes(instances.ToArray(),true,true);if(mesh.colors.Length!=mesh.vertexCount)throw new Exception("Vertex colors missing: "+name);sources.Add(name,mesh);return mesh;}
 static Mesh Fit(string name,Mesh original,bool revise=false){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string guid,out long id);string key=name+"_"+guid+"_"+id;string path=Dir+"/"+key+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null&&!revise)return saved;if(variants.TryGetValue(key,out var found)&&!revise)return found;var src=Source(name);var mesh=UnityEngine.Object.Instantiate(src);mesh.name=key;
  // Preserve the authored placement's local bounds and axis ordering. This fits the
  // new physical prop without changing roots, trigger volumes or road support.
  var a=src.bounds;var b=original.bounds;int[] ia=Enumerable.Range(0,3).OrderByDescending(i=>a.size[i]).ToArray(),ib=Enumerable.Range(0,3).OrderByDescending(i=>b.size[i]).ToArray();var matrix=Matrix4x4.zero;matrix[3,3]=1;
  for(int i=0;i<3;i++)matrix[ib[i],ia[i]]=b.size[ib[i]]/Mathf.Max(.000001f,a.size[ia[i]]);
  var vertices=mesh.vertices;var normals=mesh.normals;var normalMatrix=matrix.inverse.transpose;
  for(int i=0;i<vertices.Length;i++){vertices[i]=b.center+matrix.MultiplyVector(vertices[i]-a.center);normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;}
  mesh.vertices=vertices;mesh.normals=normals;var tris=mesh.triangles;if(matrix.determinant<0)for(int i=0;i<tris.Length;i+=3){int t=tris[i];tris[i]=tris[i+1];tris[i+1]=t;}mesh.triangles=tris;mesh.RecalculateBounds();if(saved!=null){saved.Clear();saved.vertices=mesh.vertices;saved.normals=mesh.normals;saved.colors=mesh.colors;saved.triangles=mesh.triangles;saved.RecalculateBounds();EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(mesh);return saved;}AssetDatabase.CreateAsset(mesh,path);variants.Add(key,mesh);return mesh;
 }
 public static object ReviseHole(){if(EditorApplication.isPlaying)throw new Exception("Edit required");int count=0;foreach(var file in Directory.GetFiles(Dir,"MobileHole_*.asset")){string name=Path.GetFileNameWithoutExtension(file);var parts=name.Split('_');string sourcePath=AssetDatabase.GUIDToAssetPath(parts[1]);long fileId=long.Parse(parts[2]);var original=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Mesh>().Single(m=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m,out string _,out long id);return id==fileId;});Fit("MobileHole",original,true);count++;}AssetDatabase.SaveAssets();return new{revised=count,reason="Upward-facing ring normals; original collider and bounds retained"};}
 public static object Main(){if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");var mat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/MobilePropColors.mat");if(mat==null){mat=new Material(Shader.Find("FlatKit/Stylized Surface")){name="Mobile prop region colors"};mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_VertexColorsEnabled",1);mat.EnableKeyword("DR_VERTEX_COLORS_ON");mat.EnableKeyword("_CELPRIMARYMODE_SINGLE");mat.SetColor("_ColorDim",new Color(.66f,.70f,.74f));mat.SetFloat("_ShadowEdgeSize",.15f);mat.SetShaderPassEnabled("Outline",false);AssetDatabase.CreateAsset(mat,Dir+"/MobilePropColors.mat");}
  var setup=EditorSceneManager.GetSceneManagerSetup();var reports=new List<object>();
  try{foreach(var name in new[]{"Noryangjin_MapTool_Mode_SR18_Revamp","HighWay","RestStop"}){var scene=EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");var replacements=new List<object>();foreach(var f in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true))){if(f.sharedMesh==null)continue;string oldPath=AssetDatabase.GetAssetPath(f.sharedMesh);string kind=oldPath.Contains("/RestStopProduction20260925/R04/")?"MobileGuardrail":oldPath.Contains("/Obtacle/Hole/")?"MobileHole":oldPath.Contains("/reststop_vending/")?"MobileVending":null;if(kind==null)continue;var renderer=f.GetComponent<MeshRenderer>();if(renderer==null)continue;var old=f.sharedMesh;var replacement=Fit(kind,old);f.sharedMesh=replacement;renderer.sharedMaterials=new[]{mat};EditorUtility.SetDirty(f);EditorUtility.SetDirty(renderer);replacements.Add(new{kind,source=oldPath,originalTriangles=Enumerable.Range(0,old.subMeshCount).Sum(i=>(long)old.GetIndexCount(i)/3),newTriangles=replacement.triangles.Length/3,active=f.gameObject.activeInHierarchy});}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);reports.Add(new{scene=name,replacements});}}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var source in sources.Values)UnityEngine.Object.DestroyImmediate(source);sources.Clear();}
  File.WriteAllText(Out+"/prop-install.json",Json(reports));return reports.Select(r=>r.ToString()).Count();
 }
}
