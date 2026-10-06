using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

// Read-only: size of the highway rolling log in its own frame and the road height under it.
public static class TralaleroLogProbe
{
    public static object Main()
    {
        var c=Object.FindFirstObjectByType<HighwayChapter2Controller>(FindObjectsInactive.Include);if(c==null)throw new Exception("controller");
        var log=c.singleLog;var mfs=log.GetComponentsInChildren<MeshFilter>(true);
        Vector3 min=Vector3.positiveInfinity,max=Vector3.negativeInfinity;
        foreach(var mf in mfs){var m=mf.sharedMesh;if(m==null)continue;foreach(var v in m.vertices){var p=log.InverseTransformPoint(mf.transform.TransformPoint(v));min=Vector3.Min(min,p);max=Vector3.Max(max,p);}}
        var box=log.GetComponent<BoxCollider>();
        return new{log=log.name,active=log.gameObject.activeSelf,scale=log.lossyScale.ToString("F3"),localMin=min.ToString("F3"),localMax=max.ToString("F3"),
            children=mfs.Select(m=>m.name+" "+m.transform.localPosition.ToString("F2")+" "+m.transform.localEulerAngles.ToString("F0")+" "+m.transform.localScale.ToString("F2")).ToArray(),
            box=box==null?null:new{center=box.center.ToString("F3"),size=box.size.ToString("F3")}};
    }
}
