using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class S22PlayerModelProbe
{
 public static object Main(){var c=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var skin=c.previewModel.GetComponentInChildren<SkinnedMeshRenderer>(true);var result=new{preview=AssetDatabase.GetAssetPath(c.previewModel),c.previewScale,renderer=skin.name,mesh=AssetDatabase.GetAssetPath(skin.sharedMesh),source=new{path=AssetDatabase.GetAssetPath(c.sourceSharkMesh),c.sourceSharkMesh.vertexCount,c.sourceSharkMesh.isReadable,bounds=c.sourceSharkMesh.bounds.ToString()},split=new{path=AssetDatabase.GetAssetPath(c.splitSharkMesh),c.splitSharkMesh.vertexCount,c.splitSharkMesh.isReadable,c.splitSharkMesh.subMeshCount},body=new{path=AssetDatabase.GetAssetPath(c.bodyOnlyMesh),c.bodyOnlyMesh.vertexCount,c.bodyOnlyMesh.isReadable},bones=skin.bones.Select((b,i)=>new{i,b.name,local=b.localPosition.ToString(),world=b.position.ToString(),rotation=b.localEulerAngles.ToString(),scale=b.lossyScale.ToString()}).ToArray(),mounts=c.footMounts.Select(m=>new{m.bone,pos=m.localPosition.ToString(),rot=m.localRotation.ToString(),scale=m.localScale.ToString()}).ToArray(),entries=c.entries.Select(e=>new{e.key,e.replacesBaseShoes,shoe=AssetDatabase.GetAssetPath(e.fittedShoeMesh),body=AssetDatabase.GetAssetPath(e.fittedBodyMesh),e.accessoryAnchor}).ToArray()};var json=(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{(object)result});File.WriteAllText("outputs/s22-polish-2026-10-01/player-model-before.json",json);return result;}
}
