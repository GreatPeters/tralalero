using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class SharkFootwearFitImporter
{
    public static Mesh Import(string key,string folder,CosmeticVisualCatalog catalog)
    {
        string input="outputs/skin-fit-2026-09-12/"+key+"-body.json";
        var data=JObject.Parse(File.ReadAllText(input));
        if(catalog.bodyOnlyMesh.blendShapeCount!=0)throw new InvalidOperationException("Transfer blend shapes before changing this source mesh.");
        Vector3 V(JToken a)=>new((float)a[0],(float)a[1],(float)a[2]);
        var mesh=new Mesh{name="Shark fitted for "+key,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.vertices=data["vertices"].Select(V).ToArray();mesh.normals=data["normals"].Select(V).ToArray();mesh.uv=data["uv"].Select(a=>new Vector2((float)a[0],(float)a[1])).ToArray();
        mesh.boneWeights=data["weights"].Select(w=>new BoneWeight{boneIndex0=(int)w[0],weight0=(float)w[1],boneIndex1=(int)w[2],weight1=(float)w[3],boneIndex2=(int)w[4],weight2=(float)w[5],boneIndex3=(int)w[6],weight3=(float)w[7]}).ToArray();
        mesh.bindposes=catalog.splitSharkMesh.bindposes;mesh.triangles=data["triangles"].Values<int>().ToArray();SharkSurfaceImporter.SmoothBodyNormals(mesh);mesh.RecalculateTangents();mesh.RecalculateBounds();
        string path=folder+"/FittedBody.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
    }
}
