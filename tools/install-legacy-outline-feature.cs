using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

// Adds a "Legacy Outline" RenderObjects feature (LightMode OutlineLegacy, opaque
// queue, after opaques) to the Mobile and PC renderers. Pairs with the
// SRP-Batcher-compatible FlatKit/Stylized Surface With Outline shader.
// Idempotent; Status() reports compatibility without changing anything.
public static class LegacyOutlineFeatureInstall
{
    const string FeatureName="Legacy Outline (FlatKit With Outline)";
    static readonly string[] Renderers={"Assets/Settings/Mobile RP.asset","Assets/Settings/PC RP.asset"};

    static int SrpCode(Shader s)
    {
        var m=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
        return m==null?-99:(int)m.Invoke(null,new object[]{s,0});
    }

    public static object Status()
    {
        var shader=Shader.Find("FlatKit/Stylized Surface With Outline");
        return new{
            shaderFound=shader!=null,shaderErrors=shader==null?-1:ShaderUtil.GetShaderMessageCount(shader),
            srpBatcherCode=shader==null?-1:SrpCode(shader),srpBatcherCodePlain=SrpCode(Shader.Find("FlatKit/Stylized Surface")),
            passes=shader==null?null:Enumerable.Range(0,shader.passCount).Select(i=>shader.FindPassTagValue(i,new ShaderTagId("LightMode")).name).ToArray(),
            renderers=Renderers.Select(p=>{var d=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(p);return new{p,features=d.rendererFeatures.Select(f=>f==null?"<null>":f.name+(f.isActive?"":" (off)")).ToArray()};}).ToArray()};
    }

    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        foreach(var path in Renderers)
        {
            var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);if(data==null)throw new Exception("Missing "+path);
            if(data.rendererFeatures.Any(f=>f!=null&&f.name==FeatureName))continue;
            var feature=ScriptableObject.CreateInstance<RenderObjects>();feature.name=FeatureName;
            feature.settings.passTag="LegacyOutline";feature.settings.Event=RenderPassEvent.AfterRenderingOpaques;
            feature.settings.filterSettings.RenderQueueType=RenderQueueType.Opaque;feature.settings.filterSettings.LayerMask=~0;
            feature.settings.filterSettings.PassNames=new[]{"OutlineLegacy"};
            AssetDatabase.AddObjectToAsset(feature,data);
            var so=new SerializedObject(data);var list=so.FindProperty("m_RendererFeatures");var map=so.FindProperty("m_RendererFeatureMap");
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature,out string _,out long localId);
            list.arraySize++;list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=feature;
            map.arraySize++;map.GetArrayElementAtIndex(map.arraySize-1).longValue=localId;
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
        }
        return Status();
    }
}
