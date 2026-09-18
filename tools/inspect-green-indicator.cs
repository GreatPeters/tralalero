using System;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEditor;using Object=UnityEngine.Object;
public static class InspectGreenIndicator{public static object Main(){
 string Hierarchy(Transform t)=>t.parent!=null?Hierarchy(t.parent)+"/"+t.name:t.name;
 return string.Join("\n",Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None).Where(g=>g.isActiveAndEnabled&&g.color.g>.6f&&g.color.r<.5f&&g.color.b<.5f).Select(g=>Hierarchy(g.transform)+" "+g.color+" "+g.GetType().Name));
}}
