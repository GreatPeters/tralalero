using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class S22StoryProbe
{
 static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
 public static object Open(){var all=Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include,FindObjectsSortMode.None);foreach(var s in all)s.Open();return Main();}
 public static object Main()=>new{stories=Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(s=>new{path=Path(s.transform),s.enabled,self=s.gameObject.activeSelf,hierarchy=s.gameObject.activeInHierarchy,canvas=s.GetComponent<Canvas>()?.renderMode.ToString(),rootCanvas=s.GetComponent<Canvas>()?.rootCanvas.name,parents=s.GetComponentsInParent<Canvas>(true).Select(c=>new{path=Path(c.transform),c.enabled,active=c.gameObject.activeInHierarchy,mode=c.renderMode.ToString(),c.sortingOrder}).ToArray(),movie=s.movieDisplay.gameObject.activeInHierarchy,layer=s.movieDisplay.gameObject.layer,opacity=s.GetComponentsInParent<CanvasGroup>(true).Select(c=>new{c.name,c.alpha}).ToArray()}).ToArray(),mask=Camera.main.cullingMask};
 public static object Skip(){foreach(var s in Object.FindObjectsByType<OpeningStoryUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))s.Skip();return Main();}
}
