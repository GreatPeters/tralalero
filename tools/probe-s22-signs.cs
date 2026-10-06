using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TMPro;
public static class S22SignProbe
{
 public static object Main(){return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="NumberedStoreSign").OrderBy(t=>t.localPosition.z).Skip(4).Take(3).Select(t=>new{t.name,pos=t.position.ToString("F3"),frame=t.Find("Frame").GetComponent<Renderer>().bounds.center.ToString("F3"),local=t.localPosition.ToString("F3"),scale=t.lossyScale.ToString(),texts=t.GetComponentsInChildren<TMP_Text>(true).Select(x=>new{x.name,x.text,local=x.transform.localPosition.ToString("F3"),world=x.transform.position.ToString("F3"),rotation=x.transform.localEulerAngles.ToString()}).ToArray()}).ToArray();}
}
