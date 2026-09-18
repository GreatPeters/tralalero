using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using TMPro;
public static class ProbeUITextAlignment{public static object Main(){
 var canvas=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Canvas").transform;
 var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);var textList=story.GetComponentsInChildren<TMP_Text>(true).Concat(canvas.Find("UI/Main").GetComponentsInChildren<TMP_Text>(true)).Distinct();var lines=new List<string>();
 foreach(var text in textList){
  text.ForceMeshUpdate(true,true);var glyphs=text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible).ToArray();if(glyphs.Length==0)continue;
  var low=new Vector2(glyphs.Min(c=>c.bottomLeft.x),glyphs.Min(c=>c.bottomLeft.y));var high=new Vector2(glyphs.Max(c=>c.topRight.x),glyphs.Max(c=>c.topRight.y));var center=(low+high)*.5f;
  var rect=text.rectTransform;string Hier(Transform t)=>t==canvas?"Canvas":Hier(t.parent)+"/"+t.name;
  lines.Add(Hier(text.transform)+" text="+text.text.Replace('\n','|')+" align="+text.alignment+" margin="+text.margin+" glyphOffset="+(center-rect.rect.center)+" anchors="+rect.anchorMin+".."+rect.anchorMax+" rect="+rect.rect+" offset="+rect.offsetMin+".."+rect.offsetMax+" parent="+text.transform.parent.name);
 }
 string phase=EditorApplication.isPlaying?"runtime":"edit-after";File.WriteAllLines("map-concepts/ui-text-alignment-2026-09-17/probe-"+phase+".txt",lines);return string.Join("\n",lines);
}}
