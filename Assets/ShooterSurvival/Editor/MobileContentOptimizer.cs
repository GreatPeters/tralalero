using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class MobileContentOptimizer
{
    private const string Evidence="map-concepts/mobile-presentation-2026-09-14/optimization";
    [Serializable] private sealed class TextureChange
    {
        public string path,role,beforeFormat,afterFormat;
        public int beforeMax,afterMax;
    }
    [Serializable] private sealed class Progress
    {
        public string state,error;
        public int plannedTextures,completedTextures,staleReferences,compressedWearables;
        public TextureChange[] textures;
    }
    private static EditorApplication.CallbackFunction work;

    public static int TextureBudget(string path,TextureImporter importer,out string role)
    {
        string key=path.ToLowerInvariant();
        if(importer.textureShape==TextureImporterShape.TextureCube){role="sky";return 512;}
        if(importer.textureType==TextureImporterType.Sprite||key.Contains("/ui/")||key.Contains("/icons/")||key.Contains("/story/")) {role="ui";return 2048;}
        bool detail=importer.textureType==TextureImporterType.NormalMap||key.Contains("normal")||key.Contains("emission");
        if(key.Contains("/mask.")||key.Contains("_mask.")||key.Contains("/maskmap.")){role="surface mask";return 256;}
        if(detail){role="surface detail";return 512;}
        if(key.Contains("/cosmetics/")||key.Contains("/player/")||key.Contains("/enemy/")||key.Contains("/enemies/")){role="character";return 2048;}
        if(key.Contains("vfx")||key.Contains("/particles/")){role="effect";return 1024;}
        if(key.Contains("/textures/universal/")||key.Contains("/textures/city/")||key.Contains("reststop_hall")||key.Contains("/road")||key.Contains("/counters/")){role="large environment";return 1024;}
        role="environment";return 512;
    }
    private static TextureImporterFormat FormatFor(string role) => role=="ui"?TextureImporterFormat.ASTC_4x4:
        role=="character"||role=="effect"||role=="surface detail"?TextureImporterFormat.ASTC_6x6:TextureImporterFormat.ASTC_8x8;

    private static string[] Dependencies()
    {
        // Include automatically packed Resources, not only scene references.
        var resourceAssets=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&p.Contains("/Resources/")&&!AssetDatabase.IsValidFolder(p)).ToArray();
        return AssetDatabase.GetDependencies(MobileFontMigration.Scenes.Concat(resourceAssets).ToArray(),true).Distinct().ToArray();
    }
    private static List<TextureChange> PlanTextures(IEnumerable<string> paths)
    {
        var changes=new List<TextureChange>();
        foreach(string path in paths)
        {
            if(!path.StartsWith("Assets/"))continue;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
            var settings=importer.GetPlatformTextureSettings("Android");
            int previous=settings.overridden?settings.maxTextureSize:importer.maxTextureSize;
            int target=Math.Min(previous,TextureBudget(path,importer,out string role));
            var format=FormatFor(role);
            if(settings.overridden&&settings.maxTextureSize==target&&settings.format==format)continue;
            changes.Add(new TextureChange {path=path,role=role,beforeMax=previous,afterMax=target,beforeFormat=settings.format.ToString(),afterFormat=format.ToString()});
        }
        return changes;
    }
    public static object Analyze()
    {
        Directory.CreateDirectory(Evidence);var changes=PlanTextures(Dependencies());
        var plan=new Progress {state="planned",plannedTextures=changes.Count,textures=changes.ToArray()};
        Write("plan.json",plan);
        return new {textures=changes.Count,roles=changes.GroupBy(c=>c.role).Select(g=>g.Key+": "+g.Count()).ToArray()};
    }
    public static object Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        if(work!=null)throw new InvalidOperationException("Optimization already running");
        Directory.CreateDirectory(Evidence);
        var paths=Dependencies();
        var progress=new Progress {state="running"};
        // First remove material fields that the actual shader cannot use.
        foreach(string path in paths.Where(p=>p.StartsWith("Assets/")&&p.EndsWith(".mat")))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null||material.shader==null)continue;
            var data=new SerializedObject(material);var saved=data.FindProperty("m_SavedProperties.m_TexEnvs");
            if(saved==null)continue;bool changed=false;
            for(int i=saved.arraySize-1;i>=0;i--)
            {
                var entry=saved.GetArrayElementAtIndex(i);string name=entry.FindPropertyRelative("first").stringValue;
                if(material.HasProperty(name)||entry.FindPropertyRelative("second.m_Texture").objectReferenceValue==null)continue;
                if(!changed)MobileFontMigration.Backup(path);
                saved.DeleteArrayElementAtIndex(i);progress.staleReferences++;changed=true;
            }
            if(changed){data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(material);}
        }
        // Only the generated, cosmetic-only fitted shoe meshes receive lossy mesh compression.
        foreach(string path in paths.Where(p=>p.StartsWith("Assets/ShooterSurvival/Models/Cosmetics/Trellis/shoes_")&&p.EndsWith("/Fitted.asset")))
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null||mesh.blendShapeCount!=0||MeshUtility.GetMeshCompression(mesh)!=ModelImporterMeshCompression.Off)continue;
            // These fitted meshes are rigidly attached to individual feet, not blended body skin.
            if(mesh.boneWeights.Any(w=>Mathf.Abs(w.weight0-1f)>.0001f||w.weight1>0||w.weight2>0||w.weight3>0))continue;
            MobileFontMigration.Backup(path);MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Medium);
            EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);progress.compressedWearables++;
        }
        var changes=PlanTextures(Dependencies());
        progress.plannedTextures=changes.Count;progress.textures=changes.ToArray();
        // Android already enables only Mobile quality. Match the default fallback to it so
        // a high-feature demo pipeline does not retain variants unused by either mobile scene.
        const string mobilePath="Assets/Settings/Mobile RP Asset.asset";
        var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(mobilePath);
        if(mobile==null)throw new InvalidOperationException("Verified mobile pipeline missing");
        MobileFontMigration.Backup("ProjectSettings/GraphicsSettings.asset");
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        graphics.FindProperty("m_CustomRenderPipeline").objectReferenceValue=mobile;
        graphics.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
        Write("progress.json",progress);
        int index=0;
        work=()=>
        {
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Play Mode started during import");
                if(index>=changes.Count)
                {
                    EditorApplication.update-=work;work=null;progress.state="completed";Write("progress.json",progress);return;
                }
                var change=changes[index];var importer=(TextureImporter)AssetImporter.GetAtPath(change.path);
                string backup="tmp/backups/mobile-presentation-2026-09-14/importers/"+change.path+".meta";
                Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(change.path+".meta",backup);
                var settings=importer.GetPlatformTextureSettings("Android");settings.name="Android";settings.overridden=true;
                settings.maxTextureSize=change.afterMax;settings.format=Enum.Parse<TextureImporterFormat>(change.afterFormat);
                settings.crunchedCompression=false;settings.compressionQuality=80;
                importer.SetPlatformTextureSettings(settings);importer.SaveAndReimport();
                index++;progress.completedTextures=index;Write("progress.json",progress);
            }
            catch(Exception error)
            {
                EditorApplication.update-=work;work=null;progress.state="failed";progress.error=error.ToString();Write("progress.json",progress);Debug.LogException(error);
            }
        };
        EditorApplication.update+=work;
        return new {scheduledTextures=changes.Count,progress.staleReferences,progress.compressedWearables};
    }
    private static void Write(string name,Progress progress)
    {
        string json=JsonUtility.ToJson(progress,true);
        if(!json.Contains("textures"))throw new IOException("Incomplete optimization report");
        File.WriteAllText(Path.Combine(Evidence,name),json);
    }
}
