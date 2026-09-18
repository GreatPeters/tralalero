using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ApplyNewStylizedMaterials
{
    public const string Manifest = "map-concepts/new-content-stylized-2026-09-17/materials.txt";
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        string backup = "tmp/backups/new-content-stylized-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var paths = File.ReadAllLines(Manifest);
        var materials = paths.Select(p => AssetDatabase.LoadAssetAtPath<Material>(p)).ToArray();
        if (materials.Any(m => m == null)) throw new InvalidOperationException("Missing manifest material.");
        foreach (var path in paths.Concat(new[]{"Assets/Settings/Mobile RP.asset", "Assets/Settings/PC RP.asset", "Assets/FlatKit/Demos/Common/URP Configs/[FlatKit] Example Renderer.asset"}))
        {
            string target = backup + "/" + path;
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(path, target);
        }
        for (int i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            var color = material.GetColor("_BaseColor");
            var texture = material.GetTexture("_BaseMap");
            var scale = material.GetTextureScale("_BaseMap");
            var offset = material.GetTextureOffset("_BaseMap");
            var normal = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            var surface = material.HasProperty("_Surface") ? material.GetFloat("_Surface") : 0;
            var cull = material.GetFloat("_Cull");
            var queue = material.renderQueue;
            GeneratedStylizedSurface.Apply(material);
            if (material.GetColor("_BaseColor") != color || material.GetTexture("_BaseMap") != texture ||
                material.GetTextureScale("_BaseMap") != scale || material.GetTextureOffset("_BaseMap") != offset ||
                material.GetTexture("_BumpMap") != normal || material.GetFloat("_Surface") != surface ||
                material.GetFloat("_Cull") != cull || material.renderQueue != queue)
                throw new InvalidOperationException("Appearance data changed: " + paths[i]);
            AssetDatabase.SaveAssetIfDirty(material);
        }
        SceneView.RepaintAll();
        return new { converted = paths.Length, backup, verification = Verify() };
    }

    public static object Verify()
    {
        var paths = File.ReadAllLines(Manifest);
        var rows = paths.Select(p => {
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m.shader.name != GeneratedStylizedSurface.ShaderName || !m.IsKeywordEnabled("_CELPRIMARYMODE_SINGLE") ||
                !m.IsKeywordEnabled("_TEXTUREBLENDINGMODE_MULTIPLY") ||
                m.IsKeywordEnabled("DR_OUTLINE_ON") != (m.GetFloat("_OutlineEnabled") > .5f) ||
                m.GetShaderPassEnabled("SRPDEFAULTUNLIT")) throw new InvalidOperationException("Invalid surface: " + p);
            return new { path=p, shader=m.shader.name, outline=m.GetFloat("_OutlineEnabled"),
                surface=m.GetFloat("_Surface"), queue=m.renderQueue, texture=AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")),
                normal=AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap")), color=m.GetColor("_BaseColor").ToString(), keywords=m.shaderKeywords };
        }).ToArray();
        return new { count=rows.Length, rows };
    }
}
