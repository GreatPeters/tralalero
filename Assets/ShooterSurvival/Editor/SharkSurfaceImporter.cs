using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SharkSurfaceImporter
{
    private const string Source="outputs/skin-surfaces-2026-09-12";
    public const string Folder="Assets/ShooterSurvival/Resources/Cosmetics/BodyAtlas";
    public static void BuildAndRecord()
    {
        try{var result=Build();File.WriteAllText("map-concepts/skins-reststop-2026-09-12/unity-surface-import.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));}
        catch(Exception error){File.WriteAllText("map-concepts/skins-reststop-2026-09-12/unity-surface-import-error.txt",error.ToString());Debug.LogException(error);}
    }
    public static object Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        var mapping=JObject.Parse(File.ReadAllText(Source+"/body-corner-uv.json"));
        var source=AssetDatabase.LoadAssetAtPath<Mesh>((string)mapping["sourceMesh"]);
        if(source==null||source.vertexCount!=(int)mapping["sourceVertexCount"])throw new InvalidOperationException("Source mesh changed.");
        var bodyIndices=mapping["triangleIndices"].Values<int>().ToArray();
        if(!bodyIndices.SequenceEqual(source.GetTriangles(0)))throw new InvalidOperationException("Body topology changed.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var oldVertices=source.vertices;var oldNormals=source.normals;var oldUv=source.uv;var oldWeights=source.boneWeights;var oldColors=source.colors32;var oldUv2=source.uv2;
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();var origins=new List<int>();
        var cache=new Dictionary<(int,int,int),int>();
        int Map(int original,Vector2 coordinate)
        {
            var key=(original,Mathf.RoundToInt(coordinate.x*10000000),Mathf.RoundToInt(coordinate.y*10000000));
            if(cache.TryGetValue(key,out int found))return found;
            int index=vertices.Count;cache.Add(key,index);vertices.Add(oldVertices[original]);normals.Add(oldNormals[original]);uv.Add(coordinate);weights.Add(oldWeights[original]);origins.Add(original);return index;
        }
        var newBody=new int[bodyIndices.Length];
        for(int i=0;i<bodyIndices.Length;i++){var pair=mapping["cornerUV"][i];newBody[i]=Map(bodyIndices[i],new Vector2((float)pair[0],(float)pair[1]));}
        var newShoes=source.GetTriangles(1).Select(i=>Map(i,oldUv[i])).ToArray();
        var mesh=new Mesh{name="Shark mapped body and default footwear",indexFormat=IndexFormat.UInt32};
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.bindposes=source.bindposes;mesh.boneWeights=weights.ToArray();
        if(oldColors.Length==source.vertexCount)mesh.colors32=origins.Select(i=>oldColors[i]).ToArray();
        if(oldUv2.Length==source.vertexCount)mesh.uv2=origins.Select(i=>oldUv2[i]).ToArray();
        mesh.subMeshCount=2;mesh.SetTriangles(newBody,0);mesh.SetTriangles(newShoes,1);
        for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
        {
            var dv=new Vector3[source.vertexCount];var dn=new Vector3[source.vertexCount];var dt=new Vector3[source.vertexCount];source.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);
            mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),origins.Select(i=>dv[i]).ToArray(),origins.Select(i=>dn[i]).ToArray(),origins.Select(i=>dt[i]).ToArray());
        }
        SmoothBodyNormals(mesh);mesh.RecalculateTangents();mesh.RecalculateBounds();mesh=SaveMesh(mesh,Folder+"/SharkMapped.asset");
        var bodyOnly=UnityEngine.Object.Instantiate(mesh);bodyOnly.name="Shark body with replacement footwear opening";bodyOnly.subMeshCount=1;bodyOnly.SetTriangles(RemoveOriginalShoeCollar(source,newBody,bodyIndices),0);bodyOnly=SaveMesh(bodyOnly,Folder+"/SharkBodyOnly.asset");
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        var exported=JObject.Parse(File.ReadAllText("map-concepts/skins-reststop-2026-09-12/unity-shark-mesh.json"));
        catalog.sourceSharkMesh=AssetDatabase.LoadAllAssetsAtPath((string)exported["sourceMesh"]).OfType<Mesh>().Single(m=>m.vertexCount==source.vertexCount);
        catalog.splitSharkMesh=mesh;catalog.bodyOnlyMesh=bodyOnly;
        var rows=JArray.Parse(File.ReadAllText(Source+"/bake-report.json"));
        foreach(var row in rows)
        {
            string key=(string)row["key"],folder=Folder+"/"+key;Directory.CreateDirectory(folder);
            foreach(string map in new[]{"Albedo","Normal","Mask"})File.Copy(Source+"/"+key+"/"+map+".png",folder+"/"+map+".png",true);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var row in rows)
        {
            string key=(string)row["key"],folder=Folder+"/"+key;
            foreach(string map in new[]{"Albedo","Normal","Mask"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+map+".png");importer.maxTextureSize=2048;importer.sRGBTexture=map=="Albedo";importer.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.SaveAndReimport();
            }
            string path=folder+"/Surface.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Albedo.png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Normal.png"));material.SetFloat("_BumpScale",.7f);material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Mask.png"));material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",.8f);
            material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(material);
            GeneratedStylizedSurface.Apply(material);
            var entry=catalog.Find(key);if(entry==null)throw new InvalidOperationException("Missing cosmetic "+key);entry.material=material;entry.accessory=null;
        }
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        return new{sourceVertices=source.vertexCount,mappedVertices=mesh.vertexCount,bodyTriangles=newBody.Length/3,shoeTriangles=newShoes.Length/3,skins=rows.Count};
    }
    public static object RepairFootwearSeam()
    {
        var data=JObject.Parse(File.ReadAllText(Source+"/body-corner-uv.json"));
        var source=AssetDatabase.LoadAssetAtPath<Mesh>((string)data["sourceMesh"]);var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        var body=catalog.splitSharkMesh.GetTriangles(0);var cleaned=RemoveOriginalShoeCollar(source,body,data["triangleIndices"].Values<int>().ToArray());
        catalog.bodyOnlyMesh.SetTriangles(cleaned,0);catalog.bodyOnlyMesh.RecalculateBounds();EditorUtility.SetDirty(catalog.bodyOnlyMesh);AssetDatabase.SaveAssets();
        return new{removedTriangles=(body.Length-cleaned.Length)/3};
    }
    public static void RepairBodyNormals()
    {
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");
        foreach(var mesh in new[]{catalog.splitSharkMesh,catalog.bodyOnlyMesh}){SmoothBodyNormals(mesh);mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);}AssetDatabase.SaveAssets();
    }
    internal static void SmoothBodyNormals(Mesh mesh)
    {
        var vertices=mesh.vertices;var normals=mesh.normals;var triangles=mesh.GetTriangles(0);
        (int,int,int) Key(int i)=>(Mathf.RoundToInt(vertices[i].x*100000000),Mathf.RoundToInt(vertices[i].y*100000000),Mathf.RoundToInt(vertices[i].z*100000000));
        var sums=new Dictionary<(int,int,int),Vector3>();
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var face=Vector3.Cross((vertices[b]-vertices[a])*1000,(vertices[c]-vertices[a])*1000);
            foreach(int index in new[]{a,b,c}){var key=Key(index);sums.TryGetValue(key,out var value);sums[key]=value+face;}
        }
        foreach(int i in triangles.Distinct()){var value=sums[Key(i)];if(value.sqrMagnitude>1e-12f)normals[i]=value.normalized;}
        mesh.normals=normals;
    }
    private static int[] RemoveOriginalShoeCollar(Mesh source,int[] mapped,int[] original)
    {
        var exported=JObject.Parse(File.ReadAllText("map-concepts/skins-reststop-2026-09-12/unity-shark-mesh.json"));var texture=new Texture2D(2,2);
        try
        {
            var paths=exported["textures"];string path=(string)(paths is JArray?paths[0]:paths);
            texture.LoadImage(File.ReadAllBytes(path));var uv=source.uv;var vertices=source.vertices;
            float lowerLeg=source.bounds.min.y+source.bounds.size.y*.36f;var result=new List<int>();
            bool Blue(Vector2 coordinate){var c=texture.GetPixelBilinear(coordinate.x,coordinate.y);return c.g-c.r>.25f&&c.b-c.r>.28f&&c.g>.42f;}
            for(int i=0;i<original.Length;i+=3)
            {
                int a=original[i],b=original[i+1],c=original[i+2];
                bool nearFoot=Mathf.Max(vertices[a].y,vertices[b].y,vertices[c].y)<lowerLeg;
                bool collar=nearFoot&&(Blue((uv[a]+uv[b]+uv[c])/3)||Blue(uv[a]*.6f+uv[b]*.2f+uv[c]*.2f)||Blue(uv[b]*.6f+uv[a]*.2f+uv[c]*.2f)||Blue(uv[c]*.6f+uv[a]*.2f+uv[b]*.2f));
                if(!collar){result.Add(mapped[i]);result.Add(mapped[i+1]);result.Add(mapped[i+2]);}
            }
            return result.ToArray();
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    private static Mesh SaveMesh(Mesh mesh,string path)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
    }
}
