using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class CleanupFeedbackRoadMeshes
{
 public static object Main()
 {
  var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.name!="Noryangjin_MapTool_Mode_SR18_Revamp")throw new Exception("Saved safe-copy scene required");
  const string dir="Assets/ShooterSurvival/Models/Generated/NoryangjinFeedbackV3/";
  var used=new HashSet<string>(AssetDatabase.GetDependencies(scene.path,true));
  var candidates=AssetDatabase.FindAssets("t:Mesh",new[]{dir.TrimEnd('/')}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>(p.StartsWith(dir+"RoadGround_")||p.StartsWith(dir+"RoadScenery_"))&&!used.Contains(p)).ToArray();
  File.WriteAllLines("outputs/noryangjin-feedback-v3-2026-09-28/retired-duplicate-road-meshes.txt",candidates);
  int deleted=0;AssetDatabase.StartAssetEditing();try{foreach(var path in candidates)if(AssetDatabase.DeleteAsset(path))deleted++;}finally{AssetDatabase.StopAssetEditing();}
  return new{deleted,kept=used.Count(p=>p.StartsWith(dir+"RoadGround_")||p.StartsWith(dir+"RoadScenery_"))};
 }
}
