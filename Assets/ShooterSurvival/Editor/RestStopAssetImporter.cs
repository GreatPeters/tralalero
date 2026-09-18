using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class RestStopAssetImporter
{
    public const string Models="Assets/ShooterSurvival/Models/Highway/RestStop";
    public const string Prefabs="Assets/ShooterSurvival/Prefabs/Highway/RestStop";
    private static readonly Dictionary<string,(string label,float size,bool height)> Specs=new()
    {
        ["reststop_hall"]=("휴게소 본관",6,true),["reststop_fuel_canopy"]=("휴게소 주유소",5.5f,true),
        ["reststop_ev_charger"]=("전기차 충전기",2.4f,true),["reststop_restroom"]=("휴게소 화장실",3.8f,true),
        ["reststop_kiosk"]=("간식 매점",3.2f,true),["reststop_picnic_shelter"]=("야외 쉼터",3.4f,true),
        ["reststop_vending"]=("음료와 간식 자판기",2.1f,true),["reststop_wayfinding"]=("휴게소 안내판",3.4f,true)
    };
    public static object BuildAvailable()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        const string root="outputs/skins-reststop-2026-09-12/production/reststop";
        var jobs=JObject.Parse(File.ReadAllText(root+"/task-status.json"))["jobs"].Where(j=>(string)j["status"]=="done").ToArray();
        Directory.CreateDirectory(Models);Directory.CreateDirectory(Prefabs);var report=new List<object>();
        foreach(var job in jobs)
        {
            string key=Path.GetFileNameWithoutExtension((string)job["name"]),source=(string)job["folder"];
            if(!Specs.TryGetValue(key,out var spec))throw new InvalidOperationException("Unexpected prop "+key);
            if(!Path.GetFullPath(source).StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!File.Exists(source+"/validation.json"))throw new InvalidOperationException("Unverified prop source.");
            string repair="outputs/reststop-repairs-2026-09-12/"+key;
            bool refined=key=="reststop_hall"&&File.Exists(repair+"/refined-report.json");
            string modelSource=refined?repair+"/refined.fbx":source+"/model.fbx",textureSource=refined?repair+"/textures":source+"/textures";
            string folder=Models+"/"+key;Directory.CreateDirectory(folder);Copy(modelSource,folder+"/Model.fbx");
            foreach(string map in new[]{"BaseColor","Normal","Roughness","Metallic"})Copy(textureSource+"/"+map+".png",folder+"/"+map+".png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"/Model.fbx");importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;importer.isReadable=true;importer.addCollider=false;importer.SaveAndReimport();
            foreach(string map in new[]{"BaseColor","Normal","Roughness","Metallic"})
            {
                var texture=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+map+".png");texture.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;texture.sRGBTexture=map=="BaseColor";texture.maxTextureSize=2048;texture.wrapMode=TextureWrapMode.Clamp;texture.isReadable=map=="Roughness"||map=="Metallic";texture.SaveAndReimport();
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Surface.mat");if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,folder+"/Surface.mat");}
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/BaseColor.png"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Normal.png"));material.SetFloat("_BumpScale",.6f);material.EnableKeyword("_NORMALMAP");
            var metal=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Metallic.png");var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Roughness.png");var pixels=metal.GetPixels32();var roughPixels=rough.GetPixels32();
            if(pixels.Length!=roughPixels.Length)throw new InvalidOperationException("PBR texture sizes differ.");for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(pixels[i].r,0,0,(byte)(255-roughPixels[i].r));
            var mask=new Texture2D(metal.width,metal.height,TextureFormat.RGBA32,false,true);mask.SetPixels32(pixels);mask.Apply();File.WriteAllBytes(folder+"/Mask.png",mask.EncodeToPNG());UnityEngine.Object.DestroyImmediate(mask);AssetDatabase.ImportAsset(folder+"/Mask.png");
            var maskImporter=(TextureImporter)AssetImporter.GetAtPath(folder+"/Mask.png");maskImporter.sRGBTexture=false;maskImporter.maxTextureSize=2048;maskImporter.SaveAndReimport();material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Mask.png"));material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",.75f);material.EnableKeyword("_METALLICSPECGLOSSMAP");material.enableInstancing=true;EditorUtility.SetDirty(material);
            GeneratedStylizedSurface.Apply(material);
            var container=new GameObject(spec.label);
            try
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Model.fbx"),container.transform);
                // This generated sign's front is -X; the palette convention is +Z.
                if(key=="reststop_wayfinding")visual.transform.localRotation=Quaternion.Euler(0,90,0)*visual.transform.localRotation;
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                foreach(var collider in visual.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                var bounds=HighwayAssetImporter.BoundsOf(visual);float dimension=spec.height?bounds.size.y:Mathf.Max(bounds.size.x,bounds.size.z);visual.transform.localScale*=spec.size/dimension;
                bounds=HighwayAssetImporter.BoundsOf(visual);visual.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                string path=Prefabs+"/"+key+".prefab";PrefabUtility.SaveAsPrefabAsset(container,path);
                report.Add(new{key,label=spec.label,source,modelSource,refined,prefab=path,triangles=visual.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3),size=HighwayAssetImporter.BoundsOf(visual).size.ToString()});
            }
            finally{UnityEngine.Object.DestroyImmediate(container);}
        }
        AssetDatabase.SaveAssets();File.WriteAllText("map-concepts/skins-reststop-2026-09-12/reststop-import.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{count=report.Count};
    }
    private static void Copy(string source,string target){if(!File.Exists(source))throw new FileNotFoundException(source);if(!File.Exists(target)||new FileInfo(source).Length!=new FileInfo(target).Length||File.GetLastWriteTimeUtc(source)>File.GetLastWriteTimeUtc(target))File.Copy(source,target,true);}
}
