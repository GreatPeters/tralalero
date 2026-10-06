using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class ApplySweepOptions11 {
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Clean Edit required");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scene");
  string original=SceneManager.GetActiveScene().path;EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity");
  var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();var targets=d.hazards.Where(h=>h.name=="Wrong way kickboard"||h.name.StartsWith("Side delivery motorcycle ")||h.name.StartsWith("Risk route taxi ")).ToArray();
  if(targets.Length!=7||targets.Any(h=>h.showSweepPath))throw new Exception("Expected seven fresh opt-ins");
  foreach(var h in targets){Undo.RecordObject(h,"Show verified swept hazard warning");h.showSweepPath=true;EditorUtility.SetDirty(h);}
  var names=targets.Select(h=>h.name).ToArray();EditorSceneManager.MarkSceneDirty(d.gameObject.scene);if(!EditorSceneManager.SaveScene(d.gameObject.scene))throw new Exception("Scene save failed");
  if(SceneManager.GetActiveScene().path!=original)EditorSceneManager.OpenScene(original);return new{changed=names,fields=new[]{"showSweepPath"},sceneSaved=true,collidersAndTimingsChanged=false};
 }
}
