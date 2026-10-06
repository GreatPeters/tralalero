using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class InspectCycle04Dirty {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");
  var s=SceneManager.GetActiveScene();string copy="outputs/chapter45-detailed-design-2026-10-03/phone-readability-cycle04/recovery/ShoeTower-unsaved-20261003T2025.unity";
  if(File.Exists(copy))throw new Exception("Copy already exists");
  var dirty=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Where(c=>c&&EditorUtility.IsDirty(c)).Select(c=>new{type=c.GetType().FullName,name=c.name,id=c.GetInstanceID()}).ToArray();
  bool saved=EditorSceneManager.SaveScene(s,copy,true);
  return new{scene=s.path,s.isDirty,copy,saved,dirty};
 }
}
