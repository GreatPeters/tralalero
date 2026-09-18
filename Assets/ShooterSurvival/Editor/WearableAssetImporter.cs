using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class WearableAssetImporter
{
    public const string ModelRoot="Assets/ShooterSurvival/Models/Cosmetics/Trellis";
    public const string PrefabRoot="Assets/ShooterSurvival/Prefabs/Cosmetics/Trellis";
    public static object BuildAvailable()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        var catalog=Resources.Load<CosmeticVisualCatalog>("Cosmetics/Catalog");ConfigureFeet(catalog);FitHeadwear();
        var imported=new List<object>();Directory.CreateDirectory(PrefabRoot);
        foreach(string kind in new[]{"footwear","headwear"})
        {
            string root="outputs/skins-reststop-2026-09-12/production/"+kind;if(!Directory.Exists(root))continue;
            string statePath=root+"/task-status.json";if(!File.Exists(statePath))continue;
            var ready=JObject.Parse(File.ReadAllText(statePath))["jobs"].Where(j=>(string)j["status"]=="done").Select(j=>(string)j["folder"]).ToArray();
            foreach(string source in ready)
            {
                if(!Path.GetFullPath(source).StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unexpected model source.");
                if(!File.Exists(source+"/model.fbx")||!File.Exists(source+"/validation.json"))throw new InvalidOperationException("Incomplete export "+source);
                string name=Path.GetFileName(source);string key=catalog.entries.Where(e=>name.StartsWith(e.key+"_",StringComparison.Ordinal)).Select(e=>e.key).SingleOrDefault();
                if(key==null)continue;
                string folder=ModelRoot+"/"+key;Directory.CreateDirectory(folder);
                CopyChanged(source+"/model.fbx",folder+"/Model.fbx");
                foreach(string map in new[]{"BaseColor","Normal","Roughness","Metallic"})CopyChanged(source+"/textures/"+map+".png",folder+"/"+map+".png");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var modelImporter=(ModelImporter)AssetImporter.GetAtPath(folder+"/Model.fbx");modelImporter.materialImportMode=ModelImporterMaterialImportMode.None;modelImporter.isReadable=true;modelImporter.addCollider=false;modelImporter.SaveAndReimport();
                foreach(string map in new[]{"BaseColor","Normal","Roughness","Metallic"})
                {
                    var texture=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+map+".png");texture.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;texture.sRGBTexture=map=="BaseColor";texture.maxTextureSize=2048;texture.wrapMode=TextureWrapMode.Clamp;texture.isReadable=map=="Metallic"||map=="Roughness";texture.SaveAndReimport();
                }
                string materialPath=folder+"/Surface.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
                material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/BaseColor.png"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Normal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.65f);material.SetFloat("_Metallic",.25f);material.SetFloat("_Smoothness",.38f);EditorUtility.SetDirty(material);
                material.SetTexture("_MetallicGlossMap",PackMask(folder));material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",.8f);
                GeneratedStylizedSurface.Apply(material);
                var container=new GameObject(key);
                try
                {
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Model.fbx"),container.transform);
                    foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                    foreach(var collider in visual.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                    if(kind=="footwear")NormalizeShoe(visual.transform);
                    else NormalizeHat(visual.transform);
                    if(key=="hat_diver")DiverHelmetAperture.Apply(visual.transform,folder);
                    var prefab=PrefabUtility.SaveAsPrefabAsset(container,PrefabRoot+"/"+key+".prefab");
                    var item=catalog.Find(key);item.accessory=prefab;item.material=material;item.accessoryAnchor=kind=="footwear"?"feet":"head";item.accessoryOffset=Vector3.zero;item.accessoryWorldScale=1;item.replacesBaseShoes=kind=="footwear";
                    if(kind=="footwear"){item.fittedShoeMesh=FitShoeMesh(visual.transform,catalog,folder);item.fittedBodyMesh=SharkFootwearFitImporter.Import(key,folder,catalog);}
                    imported.Add(new{key,kind,source,prefab=AssetDatabase.GetAssetPath(prefab)});
                }
                finally{UnityEngine.Object.DestroyImmediate(container);}
            }
        }
        SharkHeadwearFitter.Apply(catalog);
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        File.WriteAllText("map-concepts/skins-reststop-2026-09-12/wearable-import.json",Newtonsoft.Json.JsonConvert.SerializeObject(imported,Newtonsoft.Json.Formatting.Indented));
        return new{count=imported.Count};
    }
    private static void CopyChanged(string from,string to)
    {
        if(!File.Exists(from))throw new FileNotFoundException(from);
        if(!File.Exists(to)||new FileInfo(from).Length!=new FileInfo(to).Length||File.GetLastWriteTimeUtc(from)>File.GetLastWriteTimeUtc(to))File.Copy(from,to,true);
    }
    private static Vector3[] Points(Transform root)=>root.GetComponentsInChildren<MeshFilter>(true).SelectMany(m=>m.sharedMesh.vertices.Select(v=>m.transform.TransformPoint(v))).ToArray();
    private static Texture2D PackMask(string folder)
    {
        var metallic=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Metallic.png");var roughness=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Roughness.png");
        if(metallic.width!=roughness.width||metallic.height!=roughness.height)throw new InvalidOperationException("PBR map sizes differ.");
        var pixels=metallic.GetPixels32();var rough=roughness.GetPixels32();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(pixels[i].r,0,0,(byte)(255-rough[i].r));
        var texture=new Texture2D(metallic.width,metallic.height,TextureFormat.RGBA32,false,true);texture.SetPixels32(pixels);texture.Apply();string path=folder+"/Mask.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static void NormalizeShoe(Transform visual)
    {
        var points=Points(visual);float mx=points.Average(p=>p.x),mz=points.Average(p=>p.z);
        double xx=points.Average(p=>(double)(p.x-mx)*(p.x-mx)),zz=points.Average(p=>(double)(p.z-mz)*(p.z-mz)),xz=points.Average(p=>(double)(p.x-mx)*(p.z-mz));
        float angle=(float)(.5*Math.Atan2(2*xz,xx-zz));var axis=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));visual.rotation=Quaternion.FromToRotation(axis,Vector3.forward)*visual.rotation;
        points=Points(visual);float min=points.Min(p=>p.z),max=points.Max(p=>p.z),length=max-min;
        float Height(IEnumerable<Vector3> subset){var ys=subset.Select(p=>p.y).OrderBy(v=>v).ToArray();return ys[Mathf.Min(ys.Length-1,Mathf.FloorToInt(ys.Length*.9f))];}
        if(Height(points.Where(p=>p.z>max-length*.25f))>Height(points.Where(p=>p.z<min+length*.25f)))visual.rotation=Quaternion.Euler(0,180,0)*visual.rotation;
        points=Points(visual);length=points.Max(p=>p.z)-points.Min(p=>p.z);visual.localScale/=length;
        points=Points(visual);float bottom=points.Min(p=>p.y),top=points.Max(p=>p.y);float centerX=(points.Min(p=>p.x)+points.Max(p=>p.x))*.5f;
        var upper=points.Where(p=>p.y>bottom+(top-bottom)*.6f).Select(p=>p.z).OrderBy(v=>v).ToArray();float ankleZ=upper[upper.Length/2];
        visual.position-=new Vector3(centerX,bottom,ankleZ);
    }
    private static void NormalizeHat(Transform visual)
    {
        var b=HighwayAssetImporter.BoundsOf(visual.gameObject);visual.localScale/=Mathf.Max(b.size.x,b.size.z);b=HighwayAssetImporter.BoundsOf(visual.gameObject);visual.position-=new Vector3(b.center.x,b.min.y,b.center.z);
    }
    private static Mesh FitShoeMesh(Transform visual,CosmeticVisualCatalog catalog,string folder)
    {
        var data=JObject.Parse(File.ReadAllText("map-concepts/skins-reststop-2026-09-12/unity-shark-mesh.json"));
        var source=AssetDatabase.LoadAssetAtPath<Mesh>((string)data["mesh"]);var oldVertices=source.vertices;var oldWeights=source.boneWeights;
        // Fit clothing in the mesh bind space. The imported FBX's current bone
        // transforms can be a walking pose and are not a footwear fitting frame.
        var names=data["bones"].Select(b=>(string)b["name"]).ToArray();var footIds=catalog.footMounts.Select(m=>Array.IndexOf(names,m.bone)).ToArray();
        float Weight(BoneWeight w,int id)=>(w.boneIndex0==id?w.weight0:0)+(w.boneIndex1==id?w.weight1:0)+(w.boneIndex2==id?w.weight2:0)+(w.boneIndex3==id?w.weight3:0);
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var weights=new List<BoneWeight>();
        foreach(int id in footIds)
        {
            var candidates=source.GetTriangles(1).Distinct().Where(i=>Weight(oldWeights[i],id)>.1f && footIds.All(other=>Weight(oldWeights[i],other)<=Weight(oldWeights[i],id))).ToArray();
            float length=candidates.Max(i=>oldVertices[i].z)-candidates.Min(i=>oldVertices[i].z);
            var ankle=source.bindposes[id].inverse.GetColumn(3);
            var origin=new Vector3(ankle.x,candidates.Min(i=>oldVertices[i].y),ankle.z);
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;int offset=vertices.Count;var positions=mesh.vertices;var sourceNormals=mesh.normals;var sourceUv=mesh.uv;
                for(int i=0;i<positions.Length;i++)
                {
                    Vector3 point=origin+filter.transform.TransformPoint(positions[i])*length;
                    int nearest=candidates[0];float best=float.PositiveInfinity;
                    foreach(int candidate in candidates){float d=(oldVertices[candidate]-point).sqrMagnitude;if(d<best){best=d;nearest=candidate;}}
                    vertices.Add(point);normals.Add(filter.transform.TransformDirection(sourceNormals[i]).normalized);uv.Add(sourceUv[i]);weights.Add(oldWeights[nearest]);
                }
                triangles.AddRange(mesh.triangles.Select(i=>i+offset));
            }
        }
        var fitted=new Mesh{name="Fitted footwear",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};fitted.SetVertices(vertices);fitted.SetNormals(normals);fitted.SetUVs(0,uv);fitted.SetTriangles(triangles,0);fitted.bindposes=source.bindposes;fitted.boneWeights=weights.ToArray();fitted.RecalculateBounds();fitted.RecalculateTangents();
        string path=folder+"/Fitted.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(fitted,path);return fitted;}
        EditorUtility.CopySerialized(fitted,existing);UnityEngine.Object.DestroyImmediate(fitted);EditorUtility.SetDirty(existing);return existing;
    }
    private static void ConfigureFeet(CosmeticVisualCatalog catalog)
    {
        var data=JObject.Parse(File.ReadAllText("map-concepts/skins-reststop-2026-09-12/unity-shark-mesh.json"));var source=AssetDatabase.LoadAssetAtPath<Mesh>((string)data["mesh"]);
        var names=data["bones"].Select(b=>(string)b["name"]).ToArray();var feet=names.Select((n,i)=>(name:n,index:i)).Where(x=>x.name.EndsWith("leg2",StringComparison.Ordinal)).ToArray();
        var vertices=source.vertices;var weights=source.boneWeights;var shoeVertices=source.GetTriangles(1).Distinct().ToArray();
        float Weight(BoneWeight w,int index)=>(w.boneIndex0==index?w.weight0:0)+(w.boneIndex1==index?w.weight1:0)+(w.boneIndex2==index?w.weight2:0)+(w.boneIndex3==index?w.weight3:0);
        catalog.footMounts=feet.Select(foot=>
        {
            var selected=shoeVertices.Where(i=>Weight(weights[i],foot.index)>.1f&&feet.All(f=>Weight(weights[i],f.index)<=Weight(weights[i],foot.index))).Select(i=>vertices[i]).ToArray();
            // Only actual weighted footwear determines mount count; perspective can hide a foot.
            if(selected.Length<10)return null;
            var bind=source.bindposes[foot.index];var ankle=bind.inverse.GetColumn(3);float length=selected.Max(p=>p.z)-selected.Min(p=>p.z);
            return new CosmeticVisualCatalog.FootMount{bone=foot.name,localPosition=bind.MultiplyPoint3x4(new Vector3(ankle.x,selected.Min(p=>p.y),ankle.z)),localRotation=bind.rotation,localScale=bind.lossyScale*length};
        }).Where(m=>m!=null).ToArray();
        if(catalog.footMounts.Length==0)throw new InvalidOperationException("No weighted footwear found.");
    }
    public static object FitHeadwear()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<CosmeticVisualCatalog>("Assets/ShooterSurvival/Resources/Cosmetics/Catalog.asset");
        SharkHeadwearFitter.Apply(catalog);
        return new{foreheadHeight=SharkHeadwearFitter.SurfaceHeight(catalog.splitSharkMesh,SharkHeadwearFitter.ForeheadZ)};
    }
}
