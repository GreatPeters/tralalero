using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class DiverHelmetAperture
{
    public static void Apply(Transform visual,string folder)
    {
        var data=JObject.Parse(File.ReadAllText("map-concepts/skins-reststop-2026-09-12/diver-open-port.json"));
        using var hash=SHA256.Create();string actual=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes((string)data["sourceFile"]))).Replace("-","").ToLowerInvariant();
        if(actual!=(string)data["sourceSha256"])throw new InvalidOperationException("Diver source changed; rebuild and review the port cut.");
        var filter=visual.GetComponentsInChildren<MeshFilter>(true).Single();Vector3 V(JToken value)=>new((float)value[0],(float)value[1],(float)value[2]);
        var mesh=new Mesh{name="Diver helmet with open face port",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.vertices=data["vertices"].Select(v=>filter.transform.InverseTransformPoint(V(v))).ToArray();
        mesh.normals=data["normals"].Select(v=>filter.transform.InverseTransformDirection(V(v)).normalized).ToArray();
        mesh.uv=data["uv"].Select(v=>new Vector2((float)v[0],(float)v[1])).ToArray();mesh.triangles=data["triangles"].Values<int>().ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();
        string path=folder+"/OpenVisor.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null)AssetDatabase.CreateAsset(mesh,path);
        else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
        filter.sharedMesh=mesh;
    }
    public static void ApplyToCatalog()
    {
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");var entry=catalog.Find("hat_diver");string path=AssetDatabase.GetAssetPath(entry.accessory);var root=PrefabUtility.LoadPrefabContents(path);
        try{Apply(root.transform,WearableAssetImporter.ModelRoot+"/hat_diver");entry.accessory=PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
    }
}
