using System;using System.Linq;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.Localization.Settings;
public static class RegressionFinalCycle08 {
 public static object Main(){
  var callbacks=typeof(EditorApplication).GetField("update",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(null) as Delegate;
  var owned=callbacks?.GetInvocationList().Select(d=>d.Method.DeclaringType?.FullName+"."+d.Method.Name).Where(n=>n!=null&&n.Contains("Chapters45RegressionCycle08")).ToArray()??Array.Empty<string>();
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||owned.Length!=0)throw new Exception("QA has not returned to clean Edit");
  return new{pid=System.Diagnostics.Process.GetCurrentProcess().Id,scene=SceneManager.GetActiveScene().name,locale=LocalizationSettings.SelectedLocale.Identifier.Code,ownedEditorCallbacks=owned,timeScale=Time.timeScale,captureDeltaTime=Time.captureDeltaTime,editorCleanupExists=EditorPrefs.HasKey("PT_ResourcesCleanup"),editorCleanup=EditorPrefs.GetBool("PT_ResourcesCleanup",false),playerCleanupExists=PlayerPrefs.HasKey("PT_ResourcesCleanup"),readOnly=true};
 }
}
