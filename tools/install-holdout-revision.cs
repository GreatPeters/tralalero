using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
public static class InstallHoldoutRevision
{
    public static object Geometry()
    {
        var h = UnityEngine.Object.FindFirstObjectByType<RestStopHoldout>();
        string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        return UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.bounds.min.y > 3 && r.bounds.min.x < 257 && r.bounds.max.x > 223 && r.bounds.min.z < 257 && r.bounds.max.z > 223)
            .Select(r => new { path = PathOf(r.transform), center = r.bounds.center.ToString(), size = r.bounds.size.ToString(), minY = r.bounds.min.y }).ToArray();
    }
    public static object Refresh() { AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); return "Compilation requested"; }
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        const string folder = "outputs/reststop-holdout-2026-09-25";
        Directory.CreateDirectory(folder);
        if (!File.Exists(folder + "/preferences-before.tsv")) ChapterPlaytestPreferences.SnapshotAt(folder + "/preferences-before.tsv");
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "RestStop") throw new Exception("Expected existing RestStop scene");
        var h = UnityEngine.Object.FindFirstObjectByType<RestStopHoldout>();
        Undo.RecordObjects(h.entrances, "Frame holdout approaches");
        var positions = new[] { new Vector3(-12, .12f, 0), new Vector3(12, .12f, 0), new Vector3(0, .12f, -12), new Vector3(0, .12f, 12) };
        for (int i = 0; i < 4; i++)
        {
            var entry = h.entrances[i]; entry.position = h.center.position + positions[i];
            entry.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(h.center.position - entry.position, Vector3.up));
            var signal = h.entranceSignals[i].transform; Undo.RecordObject(signal, "Ground approach warning");
            signal.localPosition = new Vector3(0, .04f, 0); signal.localScale = new Vector3(1.4f, .025f, .7f);
        }
        var map = scene.GetRootGameObjects().Single(x => x.name == "Noryangjin_MapTool").transform;
        var hall = map.Find("Approved_Open_Hall_20260913");
        if (hall == null) throw new Exception("Expected approved open hall");
        Undo.RecordObject(h, "Assign defense roof cutaway");
        h.overheadOccluders = hall.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.bounds.min.y > h.center.position.y + 7 &&
                (r.transform.IsChildOf(hall.Find("Covered hall shell")) || r.transform.parent.name == "Hall ceiling light")).ToArray();
        EditorUtility.SetDirty(h);
        // Fit the existing information card below the normal health bar.
        if (h.hud != null)
        {
            var rect = (RectTransform)h.hud.panel.transform; Undo.RecordObject(rect, "Compact defense information");
            rect.anchorMin = new Vector2(.13f, .80f); rect.anchorMax = new Vector2(.87f, .87f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(rect);
        }
        foreach (var entry in h.entrances) EditorUtility.SetDirty(entry);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        var roads = scene.GetRootGameObjects().Single(x => x.name == "Noryangjin_MapTool").transform.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
        int samples = 0;
        foreach (var entry in h.entrances) for (int i = 0; i <= 20; i++)
        {
            var p = Vector3.Lerp(entry.position, h.center.position, i / 20f);
            if (!roads.Any(c => c.Raycast(new Ray(p + Vector3.up, Vector3.down), out _, 2))) throw new Exception("Unsupported approach: " + p);
            samples++;
        }
        return new { saved = scene.path, pool = h.police.Length, cutawayRenderers = h.overheadOccluders.Length, entrances = h.entrances.Select(x => x.position.ToString()).ToArray(), supportSamples = samples };
    }
    public static object RestorePreferences() => ChapterPlaytestPreferences.RestoreAt("outputs/reststop-holdout-2026-09-25/preferences-before.tsv");
}
