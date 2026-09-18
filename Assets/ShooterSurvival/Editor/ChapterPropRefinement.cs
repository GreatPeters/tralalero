using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class ChapterPropRefinement
{
    private const string Root = "Assets/ShooterSurvival/Models/Chapters/Props";
    private static readonly int[] Ids = { 49, 54, 57, 64, 67, 68, 69, 80 };

    public static object ImportReviewed()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        foreach (int id in Ids)
        {
            string source = Source(id);
            if (!File.Exists(source + "/fresh-inspection.json")) throw new InvalidOperationException("Fresh mesh inspection required: " + id);
            string folder = Root + "/" + id.ToString("D3");
            Directory.CreateDirectory(folder);
            File.Copy(source + "/Model.fbx", folder + "/Model.fbx", true);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var report = new List<object>();
        foreach (int id in Ids)
        {
            string fbx = Root + "/" + id.ToString("D3") + "/Model.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();
            string prefabPath = HighwayAssetImporter.Prefabs + "/HWY_" + id.ToString("D3") + ".prefab";
            string backup = "tmp/backups/chapters-polish-2026-09-12/HWY_" + id.ToString("D3") + ".prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(prefabPath, backup);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var prior = root.transform.Find("Visual");
                if (prior == null) throw new InvalidOperationException("Unexpected prop structure: " + prefabPath);
                UnityEngine.Object.DestroyImmediate(prior.gameObject);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), root.transform);
                visual.name = "Visual";
                // The imported root may carry FBX unit/axis conversion (e.g.100x).
                visual.transform.localPosition = Vector3.zero;
                var material = AssetDatabase.LoadAssetAtPath<Material>(HighwayAssetImporter.Models + "/" + id.ToString("D3") + "/Surface.mat");
                if (material == null) throw new InvalidOperationException("Existing material missing: " + id);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                var bounds = HighwayAssetImporter.BoundsOf(root);
                visual.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                bounds = HighwayAssetImporter.BoundsOf(root);
                if (Mathf.Abs(bounds.min.y) > .002f || bounds.size.y < .1f)
                    throw new InvalidOperationException("Invalid prop support/scale: " + id);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                report.Add(new { id, prefabPath, source = Source(id), size = bounds.size.ToString(), minY = bounds.min.y });
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        NoryangjinMapToolWindow.RefreshOpenWindowPaletteAssets();
        File.WriteAllText("map-concepts/chapters-polish-2026-09-12/prop-import.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static string Source(int id) => "outputs/chapters-polish-2026-09-12/props-" + (id == 49 || id == 64 ? "v5" : "v3") + "/" + id;
}
