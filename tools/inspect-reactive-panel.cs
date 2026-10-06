using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public static class InspectReactivePanel {
 public static object Main() {
  var d=UnityEngine.Object.FindFirstObjectByType<Chapter45Director>();
  var data=d.targets.Select(t=>new {t.name,t.floor,t.Eligible,t.Open,hitEnabled=t.hitCollider!=null&&t.hitCollider.enabled,
   assigned=t.targetRenderers.Select(r=>r==null?null:r.name).ToArray(),
   children=t.GetComponentsInChildren<Renderer>(true).Select(r=>new {r.name,r.enabled,active=r.gameObject.activeInHierarchy,center=r.bounds.center.ToString()}).ToArray(),
   panel=t.panel==null?null:t.panel.name }).ToArray();
  var runner=AppDomain.CurrentDomain.GetAssemblies().SelectMany(a=>a.GetTypes()).First(t=>t.Name=="Chapters45ReactivePlaytest" && (bool)t.GetField("active",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null) && !(bool)t.GetField("finished",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null));
  runner.GetMethod("Finish",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"harness-panel-observation-incomplete"});
  return new {targets=data,gameUnchanged=true,diagnosticRunNotClear=true};
 }
}
