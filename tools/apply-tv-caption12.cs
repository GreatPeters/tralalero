using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using IndianOceanAssets.ShooterSurvival;
public static class ValidationApplyCaption12 {
 public class Row{public long id;public bool style,color;public int styleValue;public uint rgba;}
 public class Spec{public long[] originalLightData;public Row[] rows;}
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Clean Edit only");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="ShoeTower"||scene.isDirty)throw new Exception("Preserve scene");
  var jt=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");var spec=(Spec)jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText("outputs/chapter45-detailed-design-2026-10-03/validation-cycle12/caption-save-preservation-spec.json"),typeof(Spec)});
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();var h=all.OfType<Chapter45Hazard>().Single(z=>z.name=="4F falling television");if(!h.showSweepPath||h.damageCause!=PlayerDamageCause.Traffic)throw new Exception("Unexpected TV state");h.damageCause=PlayerDamageCause.FallingObject;EditorUtility.SetDirty(h);
  var texts=all.OfType<TMPro.TMP_Text>().ToDictionary(c=>(long)GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId);foreach(var r in spec.rows){var so=new SerializedObject(texts[r.id]);if(r.style)so.FindProperty("m_TextStyleHashCode").intValue=r.styleValue;if(r.color)so.FindProperty("m_fontColor32.rgba").longValue=r.rgba;so.ApplyModifiedPropertiesWithoutUndo();}
  var added=all.Where(c=>c.GetType().Name=="UniversalAdditionalLightData"&&!spec.originalLightData.Contains((long)GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId)).ToArray();foreach(var c in added)UnityEngine.Object.DestroyImmediate(c);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{applied=true,damageCause=h.damageCause.ToString(),label=PlayerDamageCauseText.Label(h.damageCause),advice=PlayerDamageCauseText.Advice(h.damageCause),removedOnlyUnsavedAutoLightData=added.Length,restoredOnlyTmpCacheRows=spec.rows.Length,scene=scene.path};
 }
}
