using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class S22PlayerGeometry
{
 public static object Main(){var c=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var s=c.previewModel.GetComponentInChildren<SkinnedMeshRenderer>(true);var m=c.splitSharkMesh;var v=m.vertices;var uv=m.uv;var b=m.boneWeights;var result=new{vertices=v.Select(x=>new[]{x.x,x.y,x.z}).ToArray(),normals=m.normals.Select(x=>new[]{x.x,x.y,x.z}).ToArray(),uv=uv.Select(x=>new[]{x.x,x.y}).ToArray(),triangles=Enumerable.Range(0,m.subMeshCount).Select(i=>m.GetTriangles(i)).ToArray(),weights=b.Select(x=>new[]{(float)x.boneIndex0,x.weight0,x.boneIndex1,x.weight1,x.boneIndex2,x.weight2,x.boneIndex3,x.weight3}).ToArray(),bones=s.bones.Select(x=>x.name).ToArray(),bindposes=m.bindposes.Select(x=>Enumerable.Range(0,16).Select(i=>x[i]).ToArray()).ToArray(),textures=new[]{"skin_original","shoes_original"}.Select(k=>new{key=k,path=AssetDatabase.GetAssetPath(c.Find(k).material.GetTexture("_BaseMap"))}).ToArray()};var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)result});File.WriteAllText("outputs/s22-polish-2026-10-01/player-geometry-before.json",json);return new{vertices=v.Length,bones=s.bones.Length,textures=result.textures};}
}
