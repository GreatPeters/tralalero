using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class OpenWorkshopEntrance20260917{public static object Main(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var roads=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Roads");
 const string folder="Assets/ShooterSurvival/Resources/HarborRefinement/ShopEntranceMeshes";Directory.CreateDirectory(folder);AssetDatabase.Refresh();var report=new System.Collections.Generic.List<string>();
 foreach(string name in new[]{"HarborOpeningPier_02","HarborOpeningPier_03","HarborWorkshopSideDeck"}){
  var root=roads.Find(name);var filter=root.GetComponent<MeshFilter>();var source=filter.sharedMesh;
  string path=folder+"/"+name+".asset";if(AssetDatabase.GetAssetPath(source)==path){report.Add(name+" already has entrance");continue;}
  var mesh=UnityEngine.Object.Instantiate(source);mesh.name=name+"_ShopEntrance";var vertices=mesh.vertices;int changed=0;
  for(int i=0;i<vertices.Length;i++){var world=root.TransformPoint(vertices[i]);if(world.x<48||world.x>58||world.z<-120||world.z>-117||world.y<=.15f)continue;world.y=.10f;vertices[i]=root.InverseTransformPoint(world);changed++;}
  if(changed==0){UnityEngine.Object.DestroyImmediate(mesh);continue;}
  mesh.vertices=vertices;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);filter.sharedMesh=mesh;
  var collider=root.GetComponent<MeshCollider>();if(collider!=null)collider.sharedMesh=mesh;
  EditorUtility.SetDirty(filter);if(collider!=null)EditorUtility.SetDirty(collider);if(PrefabUtility.IsPartOfPrefabInstance(filter))PrefabUtility.RecordPrefabInstancePropertyModifications(filter);if(collider!=null&&PrefabUtility.IsPartOfPrefabInstance(collider))PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
  report.Add(name+" vertices lowered="+changed+" original="+AssetDatabase.GetAssetPath(source));
 }
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllLines("map-concepts/harbor-reference-fidelity-2026-09-17/entrance-meshes.txt",report);return string.Join("\n",report);
}}
