#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;

public static class PolyUniversalPackUrpMaterialConverter
{
    private const string RootPath = "Assets/polyperfect/Poly Universal Pack";
    private const string ReportPath = "C:/tmp/poly_universal_pack_urp_conversion_report.txt";

    [MenuItem("Tools/Polyperfect/Convert Poly Universal Pack Materials To URP", false, 2400)]
    public static void ConvertMaterialsToUrp()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        Shader urpParticlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpLit == null || urpUnlit == null || urpParticlesUnlit == null)
        {
            Debug.LogError("URP shaders were not found. Make sure Universal Render Pipeline is installed.");
            return;
        }

        var upgraders = new List<MaterialUpgrader>
        {
            new StandardUpgrader("Standard"),
            new StandardUpgrader("Standard (Specular setup)"),
            new ParticleUpgrader("Particles/Standard Surface"),
            new ParticleUpgrader("Particles/Standard Unlit"),
            new ParticleUpgrader("Particles/VertexLit Blended")
        };

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { RootPath });
        int converted = 0;
        int skipped = 0;
        var report = new List<string>
        {
            $"Poly Universal Pack URP material conversion - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Root: {RootPath}",
            string.Empty
        };

        try
        {
            for (int i = 0; i < materialGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null)
                    continue;

                EditorUtility.DisplayProgressBar(
                    "Convert Poly Universal Pack Materials To URP",
                    path,
                    materialGuids.Length == 0 ? 1f : (float)i / materialGuids.Length);

                string oldShaderName = material.shader.name;
                if (IsUrpShader(oldShaderName) || IsSkyboxShader(oldShaderName))
                {
                    skipped++;
                    report.Add($"SKIP\t{oldShaderName}\t{path}");
                    continue;
                }

                MaterialSnapshot snapshot = MaterialSnapshot.Capture(material);
                string message = string.Empty;
                bool upgraded = MaterialUpgrader.Upgrade(material, upgraders, MaterialUpgrader.UpgradeFlags.None, ref message);

                if (!upgraded || !IsUrpShader(material.shader != null ? material.shader.name : string.Empty))
                {
                    ApplyFallbackUrpShader(material, oldShaderName, snapshot, urpLit, urpUnlit, urpParticlesUnlit);
                }

                EditorUtility.SetDirty(material);
                converted++;
                report.Add($"CONVERT\t{oldShaderName}\t{material.shader.name}\t{path}");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        File.WriteAllLines(ReportPath, report);
        Debug.Log($"Converted Poly Universal Pack materials to URP. Converted: {converted}, skipped: {skipped}, report: {ReportPath}");
    }

    private static bool IsUrpShader(string shaderName)
    {
        return shaderName.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal);
    }

    private static bool IsSkyboxShader(string shaderName)
    {
        return shaderName.StartsWith("Skybox/", StringComparison.Ordinal);
    }

    private static void ApplyFallbackUrpShader(
        Material material,
        string oldShaderName,
        MaterialSnapshot snapshot,
        Shader urpLit,
        Shader urpUnlit,
        Shader urpParticlesUnlit)
    {
        if (oldShaderName.StartsWith("Particles/", StringComparison.Ordinal))
            material.shader = urpParticlesUnlit;
        else if (oldShaderName.Contains("Unlit", StringComparison.OrdinalIgnoreCase))
            material.shader = urpUnlit;
        else
            material.shader = urpLit;

        snapshot.ApplyTo(material);

        if (snapshot.Alpha < 0.999f || oldShaderName.Contains("Transparent", StringComparison.OrdinalIgnoreCase))
        {
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }

    private readonly struct MaterialSnapshot
    {
        private readonly Color color;
        private readonly Color emissionColor;
        private readonly Texture mainTexture;
        private readonly Texture emissionTexture;
        private readonly Vector2 mainTextureScale;
        private readonly Vector2 mainTextureOffset;
        private readonly bool hasColor;
        private readonly bool hasEmissionColor;
        private readonly bool hasMainTexture;
        private readonly bool hasEmissionTexture;
        private readonly bool emissionEnabled;
        private readonly float metallic;
        private readonly float glossiness;
        private readonly bool hasMetallic;
        private readonly bool hasGlossiness;

        public float Alpha => hasColor ? color.a : 1f;

        private MaterialSnapshot(Material material)
        {
            hasColor = material.HasProperty("_Color");
            color = hasColor ? material.GetColor("_Color") : Color.white;
            hasEmissionColor = material.HasProperty("_EmissionColor");
            emissionColor = hasEmissionColor ? material.GetColor("_EmissionColor") : Color.black;
            hasMainTexture = material.HasProperty("_MainTex");
            mainTexture = hasMainTexture ? material.GetTexture("_MainTex") : null;
            mainTextureScale = hasMainTexture ? material.GetTextureScale("_MainTex") : Vector2.one;
            mainTextureOffset = hasMainTexture ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            hasEmissionTexture = material.HasProperty("_EmissionMap");
            emissionTexture = hasEmissionTexture ? material.GetTexture("_EmissionMap") : null;
            emissionEnabled = material.IsKeywordEnabled("_EMISSION");
            hasMetallic = material.HasProperty("_Metallic");
            metallic = hasMetallic ? material.GetFloat("_Metallic") : 0f;
            hasGlossiness = material.HasProperty("_Glossiness");
            glossiness = hasGlossiness ? material.GetFloat("_Glossiness") : 0.5f;
        }

        public static MaterialSnapshot Capture(Material material)
        {
            return new MaterialSnapshot(material);
        }

        public void ApplyTo(Material material)
        {
            if (hasColor && material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (hasMainTexture && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mainTexture);
                material.SetTextureScale("_BaseMap", mainTextureScale);
                material.SetTextureOffset("_BaseMap", mainTextureOffset);
            }
            if (hasEmissionColor && material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emissionColor);
            if (hasEmissionTexture && material.HasProperty("_EmissionMap"))
                material.SetTexture("_EmissionMap", emissionTexture);
            if (emissionEnabled)
                material.EnableKeyword("_EMISSION");
            if (hasMetallic && material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (hasGlossiness && material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", glossiness);
        }
    }
}
#endif
