using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class ValidationPreserveScene12 {
 public static object Probe(){var t=UnityEngine.Object.FindFirstObjectByType<TMPro.TMP_Text>();var s=new SerializedObject(t);var it=s.GetIterator();var names=new System.Collections.Generic.List<string>();while(it.Next(true))if(it.propertyPath.ToLowerInvariant().Contains("style")||it.propertyPath.ToLowerInvariant().Contains("color"))names.Add(it.propertyPath);return new{dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,names};}
 [Serializable]public class Row{public long id;public bool style,color;public int styleValue;public uint rgba;}
 [Serializable]public class Spec{public long[] addedLightData;public Row[] rows;}
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit only");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="ShoeTower"||scene.isDirty)throw new Exception("Clean ShoeTower required");
  var jt=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");var spec=(Spec)jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText("outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/serialization-restoration-spec.json"),typeof(Spec)});if(spec?.rows==null)throw new Exception("Invalid restoration spec");
  var components=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();var selected=components.Where(c=>c is TMPro.TMP_Text||c.GetType().Name=="UniversalAdditionalLightData").ToDictionary(c=>(long)GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId);
  foreach(var row in spec.rows){var so=new SerializedObject(selected[row.id]);if(row.style)so.FindProperty("m_TextStyleHashCode").intValue=row.styleValue;if(row.color)so.FindProperty("m_fontColor32.rgba").longValue=row.rgba;so.ApplyModifiedPropertiesWithoutUndo();}
  foreach(long id in spec.addedLightData){var c=selected[id];if(c.GetType().Name!="UniversalAdditionalLightData")throw new Exception("Only newly serialized light data allowed");UnityEngine.Object.DestroyImmediate(c);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{restoredTmpComponents=spec.rows.Length,removedOnlyCycle12AutoAddedComponents=spec.addedLightData.Length,tvOptInRetained=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chapter45Hazard>(true)).Single(h=>h.name=="4F falling television").showSweepPath};
 }
}
