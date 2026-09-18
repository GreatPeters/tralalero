using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class AuditNewStylizedMaterials
{
    public static object Main()
    {
        var paths = File.ReadAllLines("tmp/stylized-new-materials.txt");
        var rows = paths.Select(path => {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            return new {path, shader=m == null ? "MISSING" : m.shader.name,
                surface=m != null && m.HasProperty("_Surface") ? m.GetFloat("_Surface") : 0,
                alpha=m != null && m.HasProperty("_AlphaClip") ? m.GetFloat("_AlphaClip") : 0,
                color=m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "",
                texture=m != null && m.HasProperty("_BaseMap") ? AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")) : ""};
        }).ToArray();
        var renderers = AssetDatabase.FindAssets("t:ScriptableRendererData", new[]{"Assets/Settings", "Assets/FlatKit"})
            .Select(AssetDatabase.GUIDToAssetPath).Select(p=>new {path=p,features=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(p).rendererFeatures.Select(f=>f == null ? "NULL" : f.GetType().Name+":"+f.isActive).ToArray()}).ToArray();
        Directory.CreateDirectory("map-concepts/new-content-stylized-2026-09-17");
        return new {playing=EditorApplication.isPlaying,rows,groups=rows.GroupBy(r=>r.shader).Select(g=>new {shader=g.Key,count=g.Count()}).ToArray(),renderers};
    }
}
