using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GeneratedStylizedSurfaceTests
{
    [Test]
    public void ConversionPreservesMapsTintUvAndTransparentRenderState()
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var texture = new Texture2D(2, 2);
        try
        {
            var color = new Color(.6f, .8f, .86f, .1f);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_BumpMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(2, 3));
            material.SetTextureOffset("_BaseMap", new Vector2(.1f, .2f));
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", 5);
            material.SetFloat("_DstBlend", 10);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", 0);
            material.renderQueue = 3010;
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            GeneratedStylizedSurface.Apply(material);
            Assert.That(material.shader.name, Is.EqualTo(GeneratedStylizedSurface.ShaderName));
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(color));
            Assert.That(material.GetTexture("_BaseMap"), Is.SameAs(texture));
            Assert.That(material.GetTexture("_BumpMap"), Is.SameAs(texture));
            Assert.That(material.GetTextureScale("_BaseMap"), Is.EqualTo(new Vector2(2, 3)));
            Assert.That(material.GetTextureOffset("_BaseMap"), Is.EqualTo(new Vector2(.1f, .2f)));
            Assert.That(material.renderQueue, Is.EqualTo(3010));
            Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1));
            Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo(5));
            Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo(10));
            Assert.That(material.GetFloat("_ZWrite"), Is.Zero);
            Assert.That(material.GetFloat("_Cull"), Is.Zero);
            Assert.That(material.GetTag("RenderType", false), Is.EqualTo("Transparent"));
            Assert.That(material.GetShaderPassEnabled("ShadowCaster"), Is.False);
            Assert.That(material.GetShaderPassEnabled("DepthOnly"), Is.False);
            Assert.That(material.IsKeywordEnabled("DR_OUTLINE_ON"), Is.False);
            string first = EditorJsonUtility.ToJson(material);
            GeneratedStylizedSurface.Apply(material);
            Assert.That(EditorJsonUtility.ToJson(material), Is.EqualTo(first), "Repeated authoring is stable");
        }
        finally { Object.DestroyImmediate(material); Object.DestroyImmediate(texture); }
    }

    [Test]
    public void AllNewSurfaceAssetsKeepStylizedShaderAndConsistentOutlineKeywords()
    {
        var paths = File.ReadAllLines("map-concepts/new-content-stylized-2026-09-17/materials.txt");
        Assert.That(paths.Length, Is.EqualTo(174));
        foreach (string path in paths)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.That(material, Is.Not.Null, path);
            Assert.That(material.shader.name, Is.EqualTo(GeneratedStylizedSurface.ShaderName), path);
            Assert.That(material.IsKeywordEnabled("_CELPRIMARYMODE_SINGLE"), Is.True, path);
            Assert.That(material.IsKeywordEnabled("DR_OUTLINE_ON"), Is.EqualTo(material.GetFloat("_OutlineEnabled") > .5f), path);
        }
    }

    [Test]
    public void EveryShippingRendererHasAnActiveOutlineFeatureForNewOpaqueSurfaces()
    {
        foreach (string path in new[] { "Assets/Settings/Mobile RP.asset", "Assets/Settings/PC RP.asset",
            "Assets/FlatKit/Demos/Common/URP Configs/[FlatKit] Example Renderer.asset" })
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>(path);
            var feature = renderer.rendererFeatures.OfType<FlatKit.ObjectOutlineRendererFeature>().Single(f => f.isActive);
            foreach (string surface in File.ReadAllLines("map-concepts/new-content-stylized-2026-09-17/materials.txt"))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(surface);
                if (material.IsKeywordEnabled("DR_OUTLINE_ON")) Assert.That(feature.materials, Does.Contain(material), path + ": " + surface);
            }
        }
    }
}
