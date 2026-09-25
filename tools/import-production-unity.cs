using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;

public static class ImportProductionUnity
{
    const string Stage="outputs/reststop-scene-integration-2026-09-25/staged";
    const string Root="Assets/ShooterSurvival/Models/RestStopProduction20260925";
    const string Prefabs="Assets/ShooterSurvival/Prefabs/RestStopProduction20260925";
    [Serializable] public class Catalog { public Item[] items; }
    [Serializable] public class Item { public string id,title; public bool rig; public Surface[] materials; public string[] clips; }
    [Serializable] public class Surface { public string name,alpha,@base,normal,mask; public float[] color; public float metallic,roughness,cutoff,normalScale; public bool doubleSided; }
    public static object Main(string first="B01", string last="V06")
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        var converter=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        var catalog=(Catalog)converter.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{"{\"items\":"+File.ReadAllText(Stage+"/catalog.json")+"}",typeof(Catalog)});
        var selected=catalog.items.Where(i=>string.CompareOrdinal(i.id,first)>=0&&string.CompareOrdinal(i.id,last)<=0).ToArray();
        Directory.CreateDirectory(Prefabs);Directory.CreateDirectory(Root);
        AssetDatabase.StartAssetEditing();
        try{foreach(var item in selected){string dir=Root+"/"+item.id;Directory.CreateDirectory(dir);foreach(var file in Directory.GetFiles(Stage+"/"+item.id))File.Copy(file,dir+"/"+Path.GetFileName(file),true);}}
        finally{AssetDatabase.StopAssetEditing();}
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var reports=new List<object>();
        foreach(var item in selected)
        {
            var folder=Root+"/"+item.id;var fbx=folder+"/"+item.id+".fbx";
            foreach(var file in Directory.GetFiles(folder,"*.png"))
            {
                var path=file.Replace('\\','/');var ti=(TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType=Path.GetFileName(path).StartsWith("normal_")?TextureImporterType.NormalMap:TextureImporterType.Default;
                ti.sRGBTexture=Path.GetFileName(path).StartsWith("base_");ti.alphaSource=TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency=false;ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
            }
            var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation=ModelImporterMaterialLocation.InPrefab;
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.meshCompression=ModelImporterMeshCompression.Off;importer.isReadable=true;
            importer.addCollider=false;importer.importCameras=false;importer.importLights=false;
            importer.importAnimation=item.rig;
            importer.animationType=item.rig?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
            if(item.rig){importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.animationCompression=ModelImporterAnimationCompression.Off;}
            importer.SaveAndReimport();
            if(item.rig){var clips=importer.defaultClipAnimations;foreach(var c in clips){c.loopTime=!(c.name.EndsWith("die")||c.name.EndsWith("hit")||c.name.EndsWith("attack_once")||c.name.EndsWith("greet"));c.lockRootPositionXZ=true;c.lockRootHeightY=true;c.lockRootRotation=true;}importer.clipAnimations=clips;importer.SaveAndReimport();}
            var mats=item.materials.Select((s,i)=>MakeMaterial(folder,s,i)).ToArray();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var root=new GameObject("RST_"+item.id);
            try
            {
                var fit=new GameObject("Fit").transform;fit.SetParent(root.transform,false);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(asset,fit);model.name="Model";
                Orient(model.transform,fit,item.id);
                foreach(var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    var old=r.sharedMaterials;var mapped=new Material[old.Length];
                    for(int k=0;k<old.Length;k++)
                    {
                        int ix=Array.FindIndex(item.materials,s=>Key(s.name)==Key(old[k].name));
                        if(ix<0 && old.Length==mats.Length)ix=k;
                        if(ix<0)throw new Exception(item.id+": unmapped material "+old[k].name);
                        mapped[k]=mats[ix];
                    }
                    r.sharedMaterials=mapped;r.shadowCastingMode=ShadowCastingMode.On;
                    if(r is SkinnedMeshRenderer skin){skin.forceMatrixRecalculationPerRender=true;skin.updateWhenOffscreen=false;}
                }
                var b=GeometryBounds(model.transform,root.transform);
                var dimensions=Dimensions(item.id,b.size);
                fit.localScale=new Vector3(dimensions.x/b.size.x,dimensions.y/b.size.y,dimensions.z/b.size.z);
                b=GeometryBounds(model.transform,root.transform);fit.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
                if(item.rig)
                {
                    var animator=model.GetComponent<Animator>()??model.AddComponent<Animator>();
                    animator.runtimeAnimatorController=Controller(folder,fbx,item.clips);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/"+item.id+".prefab");
                reports.Add(new{id=item.id,materials=mats.Length,size=new[]{dimensions.x,dimensions.y,dimensions.z},clips=item.clips.Length,meshSlots=model.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterials.Length).ToArray()});
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        AssetDatabase.SaveAssets();
        var json=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        File.WriteAllText("outputs/reststop-scene-integration-2026-09-25/import-"+first+"-"+last+".json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{reports}));
        return reports;
    }
    public static string Refit()
    {
        foreach(var path in Directory.GetFiles(Prefabs,"*.prefab"))
        {
            string id=Path.GetFileNameWithoutExtension(path);var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var model=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Model");
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+id+"/"+id+".fbx").transform;
                var fit=root.transform.Find("Fit")??new GameObject("Fit").transform;fit.SetParent(root.transform,false);fit.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);fit.localScale=Vector3.one;
                model.SetParent(fit,false);model.SetLocalPositionAndRotation(source.localPosition,source.localRotation);model.localScale=source.localScale;
                Orient(model,fit,id);
                var b=GeometryBounds(model,root.transform);var size=Dimensions(id,b.size);fit.localScale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
                b=GeometryBounds(model,root.transform);fit.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
                b=GeometryBounds(model,root.transform);if(Vector3.Distance(size,b.size)>.01f||Mathf.Abs(b.min.y)>.005f)throw new Exception("Fit mismatch "+id);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return "90 prefab dimensions and ground pivots verified";
    }
    static void Orient(Transform model,Transform fit,string id)
    {
        var orient=fit.Find("Orientation")??new GameObject("Orientation").transform;orient.SetParent(fit,false);orient.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);orient.localScale=Vector3.one;model.SetParent(orient,true);
        var b=GeometryBounds(model,fit);
        string wide="B02 B03 B05 B06 B07 B08 B09 B10 B11 B12 E03 E07 F01 F07 G01 G03 R04 R05 R06 R08 R09 R12 S01 S02 S03 S06 S07 S08 T02 T06 T08 T09";
        bool turn=wide.Split(' ').Contains(id)?b.size.z>b.size.x:id=="T01"||id=="R01"||id[0]=='V'?b.size.x>b.size.z:false;
        if(turn)orient.localRotation=Quaternion.Euler(0,90,0);
    }
    static string Key(string s)=>new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    static Material MakeMaterial(string folder,Surface s,int index)
    {
        string path=folder+"/Surface_"+index+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.shader=Shader.Find("Universal Render Pipeline/Lit");mat.shaderKeywords=Array.Empty<string>();mat.name=s.name;
        mat.SetColor("_BaseColor",new Color(s.color[0],s.color[1],s.color[2],s.color[3]));
        mat.SetTexture("_BaseMap",Load(folder,s.@base));mat.SetTexture("_BumpMap",Load(folder,s.normal));mat.SetFloat("_BumpScale",s.normalScale);
        mat.SetTexture("_MetallicGlossMap",Load(folder,s.mask));mat.SetFloat("_Metallic",s.metallic);mat.SetFloat("_Smoothness",s.mask==null?1-s.roughness:1);
        bool blend=s.alpha=="BLEND",cutout=s.alpha=="MASK";
        mat.SetFloat("_Surface",blend?1:0);mat.SetFloat("_Blend",0);mat.SetFloat("_AlphaClip",cutout?1:0);mat.SetFloat("_Cutoff",s.cutoff);
        mat.SetFloat("_Cull",s.doubleSided?0:2);mat.SetFloat("_SrcBlend",blend?(float)BlendMode.SrcAlpha:(float)BlendMode.One);mat.SetFloat("_DstBlend",blend?(float)BlendMode.OneMinusSrcAlpha:(float)BlendMode.Zero);
        mat.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);mat.SetFloat("_DstBlendAlpha",blend?(float)BlendMode.OneMinusSrcAlpha:(float)BlendMode.Zero);mat.SetFloat("_ZWrite",blend?0:1);
        mat.renderQueue=blend?3000:cutout?2450:2000;
        GeneratedStylizedSurface.Apply(mat);
        if(s.mask!=null)mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        if(blend){mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetShaderPassEnabled("ShadowCaster",false);}
        mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
    }
    static Texture2D Load(string f,string n)=>string.IsNullOrEmpty(n)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(f+"/"+n);
    static AnimatorController Controller(string folder,string fbx,string[] actions)
    {
        string path=folder+"/Actions.controller";var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path)??AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm=c.layers[0].stateMachine;foreach(var s in sm.states)sm.RemoveState(s.state);
        var clips=AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
        if(clips.Length!=actions.Length)throw new Exception(folder+": wrong clip count "+clips.Length);
        foreach(var action in actions){var state=sm.AddState(action);state.motion=clips.Single(a=>a.name==action||a.name.EndsWith("|"+action));if(action=="idle")sm.defaultState=state;}
        return c;
    }
    static Vector3 Dimensions(string id,Vector3 raw)
    {
        if(id[0]=='H')return raw*(3.05f/raw.y);
        var vehicle=new[]{new Vector3(4.98384f,3.1266f,8.40078f),new Vector3(4.68f,3.114f,7.65f),new Vector3(5.31882f,3.9807f,8.42544f),new Vector3(5.4f,5.67f,11.7f),new Vector3(5.85f,6.84f,12.24f),new Vector3(6.39f,6.66f,18.72f)};
        if(id[0]=='V')return vehicle[int.Parse(id.Substring(1))-1];
        var sizes=new Dictionary<string,Vector3>{
            {"B01",new Vector3(6,.12f,6)},{"B02",new Vector3(6,4,.3f)},{"B03",new Vector3(4,4,.22f)},
            {"B04",new Vector3(.9f,4.8f,.9f)},{"B05",new Vector3(4,3,.18f)},{"B06",new Vector3(5,4,.35f)},
            {"B07",new Vector3(2.25f,3.6f,.12f)},{"B08",new Vector3(12,.45f,9)},{"B09",new Vector3(12,.8f,.35f)},
            {"B10",new Vector3(8,5,.5f)},{"B11",new Vector3(8,1.8f,5)},{"B12",new Vector3(6,.18f,6)},
            {"R01",new Vector3(8,.12f,14)},{"R02",new Vector3(12,.12f,12)},{"R03",new Vector3(.3f,.3f,6)},
            {"R04",new Vector3(6,1,.35f)},{"R05",new Vector3(5,2,.15f)},{"R06",new Vector3(10,7,.5f)},
            {"R08",new Vector3(5,.18f,.18f)},{"R09",new Vector3(2.4f,.25f,.35f)},{"R12",new Vector3(2,.07f,.6f)},
            {"F01",new Vector3(4,1.5f,1.1f)},{"F07",new Vector3(3,1.4f,2)},
            {"G01",new Vector3(16,.9f,10)},{"G03",new Vector3(4,.25f,1.8f)},
            {"T01",new Vector3(.15f,3.5f,2.5f)},{"T02",new Vector3(1.5f,3.2f,.12f)},
            {"T06",new Vector3(3,1.5f,1.1f)},{"T08",new Vector3(2,1.5f,.08f)},
            {"F11",new Vector3(2,.06f,1)},{"E03",new Vector3(4,2.5f,1.3f)}
            ,{"E07",new Vector3(5,.8f,.16f)}
        };
        if(sizes.TryGetValue(id,out var size))return size;
        var heights=new Dictionary<string,float>{{"E01",1.2f},{"E02",3.8f},{"E04",3.2f},{"E05",7},{"E06",1.2f},{"E07",2.7f},{"F02",1.1f},{"F03",1.6f},{"F04",7},{"F05",.65f},{"F06",.16f},{"F08",1.9f},{"F09",1.9f},{"F10",.65f},{"G02",2.6f},{"G04",1.5f},{"P01",1.6f},{"P02",.9f},{"P03",.9f},{"P04",2.5f},{"P05",1.1f},{"P06",.07f},{"R07",1.5f},{"R10",2.8f},{"R11",1.5f},{"S01",1.6f},{"S02",1.8f},{"S03",1.6f},{"S04",.6f},{"S05",1},{"S06",3},{"S07",3.4f},{"S08",3.6f},{"S09",.4f},{"S10",.5f},{"S11",.08f},{"S12",.18f},{"S13",.4f},{"S14",.5f},{"S15",.4f},{"T03",1.35f},{"T04",1.45f},{"T05",2.1f},{"T07",.4f},{"T09",.65f}};
        if(!heights.TryGetValue(id,out var h))throw new Exception("Missing dimensions "+id);
        return raw*(h/raw.y);
    }
    static Bounds GeometryBounds(Transform root,Transform space)
    {
        bool first=true;var b=new Bounds();
        Action<Transform,Mesh> add=(t,m)=>{foreach(var v in m.vertices){var p=space.InverseTransformPoint(t.TransformPoint(v));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}};
        foreach(var m in root.GetComponentsInChildren<MeshFilter>(true))add(m.transform,m.sharedMesh);
        foreach(var s in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))add(s.transform,s.sharedMesh);
        if(first)throw new Exception("Empty geometry");return b;
    }
}
