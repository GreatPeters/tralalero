#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public sealed class MobileRenderingQualityTests
{
    [Test]
    public void MobileAndFallbackPipelines_UseAtLeastFourSampleMsaa()
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
        Assert.That(mobilePipeline.msaaSampleCount, Is.GreaterThanOrEqualTo(4));

        UnityEngine.Object[] graphicsAssets =
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        Assert.That(graphicsAssets, Is.Not.Empty);
        using var graphics = new SerializedObject(graphicsAssets[0]);
        var fallbackPipeline = graphics.FindProperty("m_CustomRenderPipeline")
            .objectReferenceValue as UniversalRenderPipelineAsset;
        Assert.That(fallbackPipeline, Is.Not.Null);
        Assert.That(fallbackPipeline.msaaSampleCount, Is.GreaterThanOrEqualTo(4));
    }
}
#endif
