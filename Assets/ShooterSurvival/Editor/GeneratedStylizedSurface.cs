using System;
using System.Linq;
using FlatKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Shared surface treatment for newly authored chapter art and cosmetics.</summary>
public static class GeneratedStylizedSurface
{
    public const string ShaderName = "FlatKit/Stylized Surface";

    public static void Apply(Material material)
    {
        if (material == null) throw new ArgumentNullException(nameof(material));
        var shader = Shader.Find(ShaderName);
        if (shader == null) throw new InvalidOperationException("FlatKit Stylized Surface is required.");
        var color = material.GetColor("_BaseColor");
        bool transparent = material.HasProperty("_Surface") && material.GetFloat("_Surface") > .5f;
        bool cutout = material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > .5f;
        bool roadPaint = material.name == "CenterYellow" || material.name == "RoadWhite";
        bool outline = !transparent && !cutout && !roadPaint;
        bool emission = material.IsKeywordEnabled("_EMISSION");
        int renderQueue = material.rawRenderQueue;
        material.shader = shader;
        material.renderQueue = renderQueue;
        material.SetOverrideTag("RenderType", transparent ? "Transparent" : cutout ? "TransparentCutout" : "Opaque");
        // Lit and FlatKit share texture/color/UV/blend property names. Do not copy
        // a reference material: that would overwrite each asset's authored maps.
        material.shaderKeywords = Array.Empty<string>();
        material.SetFloat("_CelPrimaryMode", 1);
        material.SetColor("_ColorDim", new Color(color.r * .72f, color.g * .72f, color.b * .72f, color.a));
        material.SetFloat("_SelfShadingSize", .45f);
        material.SetFloat("_ShadowEdgeSize", .08f);
        material.SetFloat("_Flatness", 1);
        material.SetFloat("_CelExtraEnabled", 0);
        material.SetFloat("_TextureBlendingMode", 0);
        material.SetFloat("_TextureImpact", 1);
        material.SetFloat("_BaseMapPremultiply", 0);
        material.SetFloat("_DetailMapImpact", 0);
        material.SetFloat("_SpecularEnabled", 0);
        material.SetFloat("_RimEnabled", 0);
        material.SetFloat("_GradientEnabled", 0);
        material.SetFloat("_VertexColorsEnabled", 0);
        material.SetFloat("_UnityShadowMode", roadPaint ? 0 : 1);
        material.SetFloat("_UnityShadowPower", .2f);
        material.SetFloat("_UnityShadowSharpness", 1);
        material.SetFloat("_OutlineEnabled", outline ? 1 : 0);
        material.SetFloat("_OutlineWidth", 1.2f);
        material.SetFloat("_OutlineScale", 1);
        material.SetFloat("_OutlineDepthOffset", .005f);
        material.SetFloat("_OutlineSpace", 0);
        material.SetFloat("_CameraDistanceImpact", .2f);
        material.SetColor("_OutlineColor", new Color(.035f, .045f, .055f, 1));
        material.EnableKeyword("_CELPRIMARYMODE_SINGLE");
        material.EnableKeyword("_TEXTUREBLENDINGMODE_MULTIPLY");
        material.EnableKeyword("_OUTLINESPACE_SCREEN");
        if (!roadPaint) material.EnableKeyword("_UNITYSHADOWMODE_MULTIPLY");
        if (outline) material.EnableKeyword("DR_OUTLINE_ON");
        if (material.GetTexture("_BumpMap") != null) material.EnableKeyword("_NORMALMAP");
        if (cutout) material.EnableKeyword("_ALPHATEST_ON");
        if (emission) material.EnableKeyword("_EMISSION");
        else material.SetColor("_EmissionColor", Color.black);
        material.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
        EditorUtility.SetDirty(material);
        if (outline && EditorUtility.IsPersistent(material)) RegisterOutline(material);
    }

    private static void RegisterOutline(Material material)
    {
        var pipelines = Enumerable.Range(0, QualitySettings.names.Length)
            .Select(QualitySettings.GetRenderPipelineAssetAt).OfType<UniversalRenderPipelineAsset>()
            .Concat(new[] { GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset })
            .Where(p => p != null).Distinct();
        foreach (var pipeline in pipelines)
        {
            var renderers = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            for (int i = 0; i < renderers.arraySize; i++)
            {
                var renderer = renderers.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                if (renderer == null) continue;
                var feature = renderer.rendererFeatures.OfType<ObjectOutlineRendererFeature>().FirstOrDefault();
                if (feature != null && feature.isActive &&
                    (!feature.autoReferenceMaterials || feature.materials.Contains(material))) continue;
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<ObjectOutlineRendererFeature>();
                    feature.name = "Flat Kit Per Object Outline";
                    AssetDatabase.AddObjectToAsset(feature, renderer);
                    renderer.rendererFeatures.Add(feature);
                    feature.Create();
                }
                feature.SetActive(true);
                feature.RegisterMaterial(material, true);
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                AssetDatabase.SaveAssetIfDirty(renderer);
            }
        }
    }
}
