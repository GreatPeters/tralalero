using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;

// Native, repeatable authoring. Original assets/scenes are backed up before first write.
public static class S22MobileAssets
{
    const string Out="outputs/s22-polish-2026-10-01";
    const string Mats="Assets/ShooterSurvival/Materials/Generated/S22Polish";
    public static readonly string[] Scenes={"Noryangjin_MapTool_Mode_SR18_Revamp","HighWay","RestStop"};
    static string Json(object v)=>(string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{v});
    static void Write(string name,object v){Directory.CreateDirectory(Out);File.WriteAllText(Out+"/"+name+".json",Json(v));}
    static void Backup(string path){string dest=Out+"/before/"+path;Directory.CreateDirectory(Path.GetDirectoryName(dest));if(File.Exists(path)&&!File.Exists(dest))File.Copy(path,dest);}
    static void Folder(string path){var parts=path.Split('/');string parent=parts[0];foreach(var part in parts.Skip(1)){if(!AssetDatabase.IsValidFolder(parent+"/"+part))AssetDatabase.CreateFolder(parent,part);parent+="/"+part;}}
    static void Guard(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle Edit Mode required");if(SceneManager.GetActiveScene().isDirty)throw new Exception("Preserve unsaved scene first");}
    public static object Rendering()
    {
        Guard();var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile RP Asset.asset");var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile RP.asset");
        Backup(AssetDatabase.GetAssetPath(rp));Backup(AssetDatabase.GetAssetPath(data));Backup("ProjectSettings/EditorBuildSettings.asset");Backup("ProjectSettings/ProjectSettings.asset");
        rp.supportsHDR=false;rp.msaaSampleCount=2;rp.renderScale=.75f;rp.upscalingFilter=UpscalingFilterSelection.Linear;rp.mainLightShadowmapResolution=1024;
        var serializedPipeline=new SerializedObject(rp);serializedPipeline.FindProperty("m_SoftShadowsSupported").boolValue=false;serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
        foreach(var feature in data.rendererFeatures)if(feature!=null&&feature.name.Contains("AmbientOcclusion")){feature.SetActive(false);EditorUtility.SetDirty(feature);}
        data.SetDirty();EditorUtility.SetDirty(rp);AssetDatabase.SaveAssetIfDirty(data);AssetDatabase.SaveAssetIfDirty(rp);
        EditorBuildSettings.scenes=Scenes.Select(n=>new EditorBuildSettingsScene("Assets/ShooterSurvival/Scenes/Tools/"+n+".unity",true)).ToArray();
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var timing=settings.FindProperty("enableFrameTimingStats");if(timing!=null){timing.boolValue=true;settings.ApplyModifiedPropertiesWithoutUndo();}
        var result=new{rp.supportsHDR,rp.msaaSampleCount,rp.renderScale,upscaler=rp.upscalingFilter.ToString(),rp.mainLightShadowmapResolution,rp.supportsSoftShadows,features=data.rendererFeatures.Select(f=>new{f.name,f.isActive}).ToArray(),scenes=EditorBuildSettings.scenes.Select(s=>s.path).ToArray()};Write("mobile-rendering",result);return result;
    }
    public static object Textures()
    {
        Guard();var report=BuildReport.GetLatestReport();if(report==null)throw new Exception("Prior build report required to scope dependencies");
        var paths=report.packedAssets.SelectMany(p=>p.contents).Select(c=>c.sourceAssetPath).Distinct().Where(p=>p.StartsWith("Assets/ShooterSurvival/Models/")||p.StartsWith("Assets/ShooterSurvival/Textures/MeshyAI/")).Where(p=>p.EndsWith(".png")||p.EndsWith(".jpg")).ToArray();
        var changes=new List<object>();var pending=new Queue<string>(paths);int total=paths.Length;
        EditorApplication.CallbackFunction tick=null;tick=()=>{
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.update-=tick;Write("texture-error",new{error="Play interrupted texture authoring"});return;}
            try{
                if(pending.Count==0){EditorApplication.update-=tick;Write("android-textures",new{examined=total,changes});return;}
                string path=pending.Dequeue();var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null||importer.textureType==TextureImporterType.Sprite)return;
                var platform=importer.GetPlatformTextureSettings("Android");int size=path.ToLowerInvariant().Contains("mask")?512:1024;
                bool stream=importer.mipmapEnabled;
                if(platform.overridden&&platform.maxTextureSize<=size&&platform.format==TextureImporterFormat.ASTC_6x6&&importer.streamingMipmaps==stream)return;
                Backup(path+".meta");changes.Add(new{path,oldMax=platform.maxTextureSize,oldFormat=platform.format.ToString(),oldOverride=platform.overridden,oldStreaming=importer.streamingMipmaps,newMax=size});
                platform.overridden=true;platform.maxTextureSize=Math.Min(size,importer.maxTextureSize);platform.format=TextureImporterFormat.ASTC_6x6;platform.textureCompression=TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(platform);importer.streamingMipmaps=stream;importer.SaveAndReimport();
                Write("texture-progress",new{total,remaining=pending.Count,changed=changes.Count,path});
            }catch(Exception ex){EditorApplication.update-=tick;File.WriteAllText(Out+"/texture-error.txt",ex.ToString());}
        };EditorApplication.update+=tick;return new{scheduled=true,total};
    }
    public static object SceneryMaterials()
    {
        Guard();Folder(Mats);var setup=EditorSceneManager.GetSceneManagerSetup();var changes=new List<object>();
        try{foreach(var name in Scenes){
            string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";Backup(path);var scene=EditorSceneManager.OpenScene(path);int changed=0,lights=0;
            foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))){
                if(r.GetComponent<TMPro.TMP_Text>()!=null||r.GetComponentInParent<PlayerScript>(true)!=null||r.GetComponentInParent<EnemyScript_space>(true)!=null||r.GetComponentInParent<UnityEngine.UI.Graphic>(true)!=null)continue;
                var values=r.sharedMaterials;bool dirty=false;
                for(int i=0;i<values.Length;i++){
                    var m=values[i];if(m==null||m.renderQueue>=3000||m.shader==null)continue;
                    bool flat=m.shader.name=="FlatKit/Stylized Surface",lit=m.shader.name=="Universal Render Pipeline/Lit";
                    if(!flat&&!lit)continue;if(flat&&!m.IsKeywordEnabled("DR_OUTLINE_ON")&&!m.GetShaderPassEnabled("Outline"))continue;
                    string original=AssetDatabase.GetAssetPath(m);if(original.StartsWith(Mats))continue;
                    string key=AssetDatabase.AssetPathToGUID(original);if(string.IsNullOrEmpty(key))continue;
                    string dest=Mats+"/Scenery_"+key+".mat";var copy=AssetDatabase.LoadAssetAtPath<Material>(dest);
                    if(copy==null){copy=flat?new Material(m):new Material(Shader.Find("FlatKit/Stylized Surface"));copy.name="Scenery "+m.name;
                        if(lit){copy.SetColor("_BaseColor",m.GetColor("_BaseColor"));copy.SetTexture("_BaseMap",m.GetTexture("_BaseMap"));copy.SetTextureScale("_BaseMap",m.GetTextureScale("_BaseMap"));copy.SetTextureOffset("_BaseMap",m.GetTextureOffset("_BaseMap"));copy.EnableKeyword("_CELPRIMARYMODE_SINGLE");copy.SetColor("_ColorDim",new Color(.68f,.73f,.78f,1));copy.SetFloat("_SelfShadingSize",.5f);copy.SetFloat("_ShadowEdgeSize",.15f);}
                        copy.SetFloat("_OutlineEnabled",0);copy.DisableKeyword("DR_OUTLINE_ON");copy.SetShaderPassEnabled("Outline",false);AssetDatabase.CreateAsset(copy,dest);
                    }
                    values[i]=copy;dirty=true;
                }
                if(dirty){r.sharedMaterials=values;EditorUtility.SetDirty(r);changed++;}
            }
            var atmosphere=Object.FindFirstObjectByType<NoryangjinMarketAtmosphere>();
            if(atmosphere!=null){var detail=Object.FindFirstObjectByType<NoryangjinInteriorDetailVisibility>();if(detail!=null){foreach(var l in detail.localLights)if(l!=null){l.enabled=false;EditorUtility.SetDirty(l);lights++;}detail.localLights=Array.Empty<Light>();EditorUtility.SetDirty(detail);}}
            foreach(var chapter in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ChapterProgression>(true)))if(chapter.nextScene=="Noryangjin_MapTool_Mode_SR18"){chapter.nextScene=Scenes[0];EditorUtility.SetDirty(chapter);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);changes.Add(new{name,renderers=changed,disabledLocalLights=lights});
        }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        Write("scenery-materials",changes);return changes;
    }
}
