using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
public static class S22PlayerTailImport
{
 const string Input="outputs/s22-polish-2026-10-01/player-tail-meshes";
 const string Target="Assets/ShooterSurvival/Models/Generated/S22Polish/Player";
 [Serializable] private class Part{public int[] triangles;}
 [Serializable] private class Data{public float[] positions,uv,normals,weights,tailOffset;public Part[] submeshes;}
 [Serializable] private class SourceEntry{public string name,path;}
 public static object Revise(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");var json=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");var rows=(SourceEntry[])json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText("outputs/s22-polish-2026-10-01/player-source-meshes/manifest.json"),typeof(SourceEntry[])});foreach(var row in rows)Import(row.name,AssetDatabase.LoadAssetAtPath<Mesh>(row.path),true);return new{revised=rows.Length};}
 static void Folder(){string parent="Assets";foreach(var part in Target.Split('/').Skip(1)){if(!AssetDatabase.IsValidFolder(parent+"/"+part))AssetDatabase.CreateFolder(parent,part);parent+="/"+part;}}
 static Data Read(string path){var json=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");return (Data)json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText(path),typeof(Data)});}
 static Mesh Import(string key,Mesh original,bool revise=false){string path=Target+"/"+key+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null&&!revise)throw new Exception("Preserve prior tail candidate: "+path);var d=Read(Input+"/"+key+".json");int count=d.positions.Length/3;var v=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];var weights=new BoneWeight[count];
  for(int i=0;i<count;i++){v[i]=new Vector3(d.positions[i*3],d.positions[i*3+1],d.positions[i*3+2]);normals[i]=new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2]);uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);int j=i*8;weights[i]=new BoneWeight{boneIndex0=(int)d.weights[j],weight0=d.weights[j+1],boneIndex1=(int)d.weights[j+2],weight1=d.weights[j+3],boneIndex2=(int)d.weights[j+4],weight2=d.weights[j+5],boneIndex3=(int)d.weights[j+6],weight3=d.weights[j+7]};if(float.IsNaN(v[i].sqrMagnitude)||Mathf.Abs(weights[i].weight0+weights[i].weight1+weights[i].weight2+weights[i].weight3-1)>.001f)throw new Exception("Invalid skinned vertex "+i);}
  var offset=new Vector3(d.tailOffset[0],d.tailOffset[1],d.tailOffset[2]);var bind=original.bindposes.ToList();if(bind.Count!=27)throw new Exception("Unexpected source skeleton");foreach(int b in new[]{6,7,8,9})bind.Add(bind[b]*Matrix4x4.Translate(-offset));
  var mesh=existing!=null?existing:new Mesh();mesh.Clear();mesh.name="Two feet and tail - "+key;mesh.indexFormat=count>65535?IndexFormat.UInt32:IndexFormat.UInt16;mesh.vertices=v;mesh.normals=normals;mesh.uv=uv;mesh.boneWeights=weights;mesh.bindposes=bind.ToArray();mesh.subMeshCount=d.submeshes.Length;for(int i=0;i<d.submeshes.Length;i++)mesh.SetTriangles(d.submeshes[i].triangles,i);mesh.RecalculateBounds();mesh.RecalculateTangents();if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);}return mesh;
 }
 public static object Main(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");Folder();var source=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");if(source.usesTailFoot)throw new Exception("Already installed");var path=AssetDatabase.GetAssetPath(source);var backup="outputs/s22-polish-2026-10-01/before/"+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
  var candidate=UnityEngine.Object.Instantiate(source);candidate.name="S22 tail foot catalog candidate";candidate.splitSharkMesh=Import("default",source.splitSharkMesh);candidate.bodyOnlyMesh=Import("body",source.bodyOnlyMesh);foreach(var item in candidate.entries.Where(e=>e.fittedShoeMesh!=null)){var original=source.Find(item.key);item.fittedShoeMesh=Import(item.key,original.fittedShoeMesh);if(original.fittedBodyMesh!=null)item.fittedBodyMesh=Import(item.key+"_body",original.fittedBodyMesh);}
  var dto=Read(Input+"/default.json");candidate.usesTailFoot=true;candidate.tailFootOffset=new Vector3(dto.tailOffset[0],dto.tailOffset[1],dto.tailOffset[2]);var rear=source.footMounts.First(f=>f.bone=="backleg2");candidate.footMounts=new[]{source.footMounts.First(f=>f.bone=="frontleg2"),source.footMounts.First(f=>f.bone=="R_frontleg2"),new CosmeticVisualCatalog.FootMount{bone="TailFoot",localPosition=rear.localPosition,localRotation=rear.localRotation,localScale=rear.localScale}};AssetDatabase.CreateAsset(candidate,Target+"/CatalogCandidate.asset");
  EditorUtility.CopySerialized(candidate,source);source.name="Catalog";EditorUtility.SetDirty(source);AssetDatabase.SaveAssetIfDirty(source);return new{vertices=source.splitSharkMesh.vertexCount,bones=source.splitSharkMesh.bindposes.Length,footMounts=source.footMounts.Select(x=>x.bone).ToArray(),variants=source.entries.Count(e=>e.fittedShoeMesh!=null)};
 }
}
