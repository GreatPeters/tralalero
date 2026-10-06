#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public sealed class MobileRenderingQualityTests
{
    [Test]
    public void MobilePipelineUsesBoundedBuffersAndRetainsTwoSampleMsaa()
    {
        UnityEngine.Object[] qualityAssets =
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        Assert.That(qualityAssets, Is.Not.Empty);

        using var serialized = new SerializedObject(qualityAssets[0]);
        SerializedProperty levels = serialized.FindProperty("m_QualitySettings");
        Assert.That(levels, Is.Not.Null);

        UniversalRenderPipelineAsset mobilePipeline = null;
        for (int index = 0; index < levels.arraySize; index++)
        {
            SerializedProperty level = levels.GetArrayElementAtIndex(index);
            if (level.FindPropertyRelative("name").stringValue != "Mobile")
                continue;

            mobilePipeline = level.FindPropertyRelative("customRenderPipeline")
                .objectReferenceValue as UniversalRenderPipelineAsset;
            break;
        }

        Assert.That(mobilePipeline, Is.Not.Null);
        Assert.That(mobilePipeline.msaaSampleCount, Is.EqualTo(2));
        Assert.That(mobilePipeline.supportsHDR,Is.False);
        Assert.That(mobilePipeline.upscalingFilter,Is.EqualTo(UpscalingFilterSelection.Linear));
        Assert.That(mobilePipeline.mainLightShadowmapResolution,Is.LessThanOrEqualTo(1024));
        Assert.That(mobilePipeline.supportsSoftShadows,Is.False);

        UnityEngine.Object[] graphicsAssets =
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        Assert.That(graphicsAssets, Is.Not.Empty);
        using var graphics = new SerializedObject(graphicsAssets[0]);
        var fallbackPipeline = graphics.FindProperty("m_CustomRenderPipeline")
            .objectReferenceValue as UniversalRenderPipelineAsset;
        Assert.That(fallbackPipeline, Is.Not.Null);
        Assert.That(fallbackPipeline.msaaSampleCount, Is.GreaterThanOrEqualTo(2));
    }
    [Test] public void MobileRendererKeepsCharacterOutlineWithoutSsao()
    {
        var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile RP.asset");
        bool foundOutline=false;
        foreach(var feature in data.rendererFeatures)
        {
            if(feature.name.Contains("AmbientOcclusion"))Assert.That(feature.isActive,Is.False);
            if(feature.name.Contains("Outline"))foundOutline|=feature.isActive;
        }
        Assert.That(foundOutline,Is.True);
    }
}
#endif
