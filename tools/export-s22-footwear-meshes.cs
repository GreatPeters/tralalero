using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class S22FootwearMeshes
{
 const string Dir="outputs/s22-polish-2026-10-01/player-source-meshes";
 static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
 static void Export(Mesh mesh,string name){if(!mesh.isReadable)throw new Exception("Mesh must be readable: "+name);File.WriteAllText(Dir+"/"+name+".json",Json(new{vertices=mesh.vertices.Select(v=>new[]{v.x,v.y,v.z}).ToArray(),normals=mesh.normals.Select(v=>new[]{v.x,v.y,v.z}).ToArray(),uv=mesh.uv.Select(v=>new[]{v.x,v.y}).ToArray(),weights=mesh.boneWeights.Select(w=>new[]{(float)w.boneIndex0,w.weight0,w.boneIndex1,w.weight1,w.boneIndex2,w.weight2,w.boneIndex3,w.weight3}).ToArray(),triangles=Enumerable.Range(0,mesh.subMeshCount).Select(i=>mesh.GetTriangles(i)).ToArray()}));}
 public static object Main(){Directory.CreateDirectory(Dir);var c=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var rows=new List<object>();Export(c.splitSharkMesh,"default");rows.Add(new{name="default",path=AssetDatabase.GetAssetPath(c.splitSharkMesh),shoeOnly=false,bodyOnly=false});Export(c.bodyOnlyMesh,"body");rows.Add(new{name="body",path=AssetDatabase.GetAssetPath(c.bodyOnlyMesh),shoeOnly=false,bodyOnly=true});foreach(var e in c.entries.Where(e=>e.fittedShoeMesh!=null)){Export(e.fittedShoeMesh,e.key);rows.Add(new{name=e.key,path=AssetDatabase.GetAssetPath(e.fittedShoeMesh),shoeOnly=true,bodyOnly=false});if(e.fittedBodyMesh!=null){Export(e.fittedBodyMesh,e.key+"_body");rows.Add(new{name=e.key+"_body",path=AssetDatabase.GetAssetPath(e.fittedBodyMesh),shoeOnly=false,bodyOnly=true});}}File.WriteAllText(Dir+"/manifest.json",Json(rows));return new{meshes=rows.Count};}
}
