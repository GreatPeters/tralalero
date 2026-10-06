using System;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class RestartPreflightCycle06 {
 public static object Main(){
  var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
  var prefab=PrefabStageUtility.GetCurrentPrefabStage();
  var callbacks=typeof(EditorApplication).GetField("update",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(null) as Delegate;
  var activeQa=callbacks?.GetInvocationList().Select(x=>x.Method.DeclaringType?.FullName+"."+x.Method.Name).Where(x=>x!=null&&(x.Contains("Chapters45Stability")||x.StartsWith("Verify")||x.Contains("ReactivePlaytest"))).ToArray()??Array.Empty<string>();
  return new{utc=DateTime.UtcNow.ToString("o"),pid=System.Diagnostics.Process.GetCurrentProcess().Id,playing=EditorApplication.isPlayingOrWillChangePlaymode,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating,applicationActive=UnityEditorInternal.InternalEditorUtility.isApplicationActive,editingTextField=EditorGUIUtility.editingTextField,hotControl=GUIUtility.hotControl,undoGroup=Undo.GetCurrentGroup(),prefabStageOpen=prefab!=null,prefabDirty=prefab!=null&&prefab.scene.isDirty,scenes=scenes.Select(s=>new{s.name,s.path,s.isDirty}).ToArray(),activeQa,company=Application.companyName,product=Application.productName,persistentDataPath=Application.persistentDataPath,enabledBuildScenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),timeScale=Time.timeScale,captureDeltaTime=Time.captureDeltaTime,cleanIdle=!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!scenes.Any(s=>s.isDirty)&&prefab==null&&activeQa.Length==0};
 }
}
