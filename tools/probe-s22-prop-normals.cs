using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class S22PropNormals
{
 public static object Main(){var f=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.sharedMesh!=null&&x.sharedMesh.name.StartsWith("MobileHole"));var m=f.sharedMesh;var colors=m.colors;var normals=m.normals;return new{path=AssetDatabase.GetAssetPath(m),matrix=f.transform.localToWorldMatrix.ToString(),localSize=m.bounds.size.ToString(),samples=Enumerable.Range(0,m.vertexCount).GroupBy(i=>colors[i].ToString()).Select(g=>new{color=g.Key,count=g.Count(),localNormal=normals[g.First()].ToString(),worldNormal=f.transform.TransformDirection(normals[g.First()]).ToString(),normalY=g.Average(i=>f.transform.TransformDirection(normals[i]).y)}).ToArray()};}
}
