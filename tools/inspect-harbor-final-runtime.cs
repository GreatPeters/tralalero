using System;using System.Linq;using UnityEngine;using UnityEditor;using TMPro;using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class InspectHarborFinalRuntime{public static object Main(){
 var f=HarborRefinementFontBuilder.Font;string Describe(UnityEngine.Object o){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string guid,out long id);return o.name+" instance="+o.GetInstanceID()+" "+guid+":"+id;}
 var report=string.Join("\n",new[]{"Coastal_White","Coastal_Ink"}.Select(n=>{var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Resources/UI/HarborMaterials/"+n+".mat");return n+" "+Describe(m.GetTexture("_MainTex"))+" equal="+(m.GetTexture("_MainTex")==f.atlasTextures[0])+" outline="+m.IsKeywordEnabled("OUTLINE_ON");}))+"\nfont "+Describe(f.atlasTextures[0]);
 var player=Object.FindFirstObjectByType<PlayerScript>();var so=new SerializedObject(player);var image=so.FindProperty("healthBar").objectReferenceValue as UnityEngine.UI.Image;
 report+="\nPlayer canvas "+string.Join(",",player.GetComponentsInChildren<Canvas>(true).Select(c=>c.name+"="+c.gameObject.activeInHierarchy))+" bar="+image.gameObject.activeInHierarchy;
 report+="\nHealth renderers "+string.Join(",",Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name.ToLower().Contains("health")).Select(r=>r.name+"/"+r.transform.parent.name+"/"+r.transform.root.name));
 System.IO.File.WriteAllText("map-concepts/harbor-opening-refinement-2026-09-16/final-runtime-inspection.txt",report);return report;
}}
