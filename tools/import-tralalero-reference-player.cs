using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

// Overwrite the S22 tail-foot meshes in place (same asset GUIDs, so the
// catalog, prefabs and fitted shoes keep their references) with the
// reference-fit meshes from tools/build-tralalero-reference-player.py.
// The current asset files are copied to outputs/.../before once.
public static class TralaleroReferencePlayerImport
{
    const string Input="outputs/tralalero-reference-2026-10-01/player-meshes";
    const string Sources="outputs/s22-polish-2026-10-01/player-source-meshes/manifest.json";
    const string Target="Assets/ShooterSurvival/Models/Generated/S22Polish/Player";
    const string Backup="outputs/tralalero-reference-2026-10-01/before/Player";
    [Serializable] private class Part{public int[] triangles;}
    [Serializable] private class Data{public float[] positions,uv,normals,weights,tailOffset;public Part[] submeshes;}
    [Serializable] private class SourceEntry{public string name,path;}

    static T Json<T>(string path)
    {
        var json=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        return (T)json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{File.ReadAllText(path),typeof(T)});
    }

    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        if(catalog==null||!catalog.usesTailFoot)throw new Exception("S22 tail-foot catalog must already be installed");
        Directory.CreateDirectory(Backup);
        var rows=Json<SourceEntry[]>(Sources);
        foreach(var row in rows)
        {
            string path=Target+"/"+row.name+".asset";
            if(!File.Exists(path))throw new Exception("Missing installed mesh "+path);
            string copy=Backup+"/"+row.name+".asset";
            if(!File.Exists(copy))File.Copy(path,copy);
        }
        var counts=rows.Select(row=>Import(row.name,AssetDatabase.LoadAssetAtPath<Mesh>(row.path),catalog)).ToArray();
        return new{installed=rows.Length,defaultVertices=counts[0],backup=Backup};
    }

    static int Import(string key,Mesh original,CosmeticVisualCatalog catalog)
    {
        string path=Target+"/"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        var d=Json<Data>(Input+"/"+key+".json");int count=d.positions.Length/3;
        var offset=new Vector3(d.tailOffset[0],d.tailOffset[1],d.tailOffset[2]);
        if((offset-catalog.tailFootOffset).sqrMagnitude>1e-12f)throw new Exception("Tail shoe offset changed; catalog would be stale");
        var v=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];var weights=new BoneWeight[count];
        for(int i=0;i<count;i++)
        {
            v[i]=new Vector3(d.positions[i*3],d.positions[i*3+1],d.positions[i*3+2]);
            normals[i]=new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2]);
            uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);int j=i*8;
            weights[i]=new BoneWeight{boneIndex0=(int)d.weights[j],weight0=d.weights[j+1],boneIndex1=(int)d.weights[j+2],weight1=d.weights[j+3],boneIndex2=(int)d.weights[j+4],weight2=d.weights[j+5],boneIndex3=(int)d.weights[j+6],weight3=d.weights[j+7]};
            if(float.IsNaN(v[i].sqrMagnitude)||Mathf.Abs(weights[i].weight0+weights[i].weight1+weights[i].weight2+weights[i].weight3-1)>.001f)throw new Exception("Invalid skinned vertex "+key+" "+i);
        }
        var bind=original.bindposes.ToList();if(bind.Count!=27)throw new Exception("Unexpected source skeleton");
        foreach(int b in new[]{6,7,8,9})bind.Add(bind[b]*Matrix4x4.Translate(-offset));
        mesh.Clear();mesh.name="Reference tail fin foot - "+key;mesh.indexFormat=count>65535?IndexFormat.UInt32:IndexFormat.UInt16;
        mesh.vertices=v;mesh.normals=normals;mesh.uv=uv;mesh.boneWeights=weights;mesh.bindposes=bind.ToArray();
        mesh.subMeshCount=d.submeshes.Length;for(int i=0;i<d.submeshes.Length;i++)mesh.SetTriangles(d.submeshes[i].triangles,i);
        mesh.RecalculateBounds();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
        return count;
    }

    // Put the S22 v5 meshes back from the backup copies.
    public static object Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        int restored=0;
        foreach(var copy in Directory.GetFiles(Backup,"*.asset")){File.Copy(copy,Target+"/"+Path.GetFileName(copy),true);restored++;}
        AssetDatabase.Refresh();return new{restored};
    }
}
