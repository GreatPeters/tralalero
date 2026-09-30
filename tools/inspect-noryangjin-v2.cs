using System.Linq;
using UnityEngine;
public static class InspectNoryangjinV2
{
 static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
 public static object Main()=>Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!Path(r.transform).StartsWith("NoryangjinRevamp/")&&r.bounds.Intersects(new Bounds(new Vector3(124,3,-10),new Vector3(20,10,40)))).Select(r=>new{path=Path(r.transform),size=r.bounds.size.ToString(),material=r.sharedMaterial?.name}).ToArray();
}
