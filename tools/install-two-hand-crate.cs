using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
public static class InstallTwoHandCrate
{
 public static string Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
  var scene=SceneManager.GetActiveScene();if(scene.name!="Noryangjin_MapTool_Mode_SR18"||scene.isDirty)throw new Exception("Clean SR18 required");
  string folder="map-concepts/two-hand-crate-2026-09-20/before";Directory.CreateDirectory(folder);
  string path="Assets/JH/Model/Prefab/Enemy_FatMan.prefab";
  if(!File.Exists(folder+"/FatMan.prefab"))File.Copy(path,folder+"/FatMan.prefab");
  if(!File.Exists(folder+"/SR18.unity"))File.Copy(scene.path,folder+"/SR18.unity");
  var prefab=PrefabUtility.LoadPrefabContents(path);
  try{Configure(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  var actors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EnemyEventController>(true)).Where(e=>e.name.Contains("FatMan")).ToArray();
  foreach(var actor in actors)Configure(actor.gameObject);
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();
  return "Installed independent crate and two-hand pose on prefab and "+actors.Length+" actors";
 }
 public static void Configure(GameObject go)
 {
  var model=go.GetComponentInChildren<Animator>(true);
  var prop=go.GetComponentsInChildren<SimpleProjectile>(true).Single(p=>p.name=="Arrow2").transform;
  var pose=go.GetComponent<FatManCratePose>();if(pose==null)pose=go.AddComponent<FatManCratePose>();
  pose.Configure(model,prop);
  foreach(var item in new UnityEngine.Object[]{pose,prop}){EditorUtility.SetDirty(item);if(PrefabUtility.IsPartOfPrefabInstance(item))PrefabUtility.RecordPrefabInstancePropertyModifications(item);}
 }
}
