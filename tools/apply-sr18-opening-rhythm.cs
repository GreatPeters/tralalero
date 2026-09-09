using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Sr18OpeningRhythm
{
    public static object Main(bool apply = false)
    {
        const string scenePath = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
        const string recordPath = "map-concepts/sr18-opening-rhythm-2026-09-10/applied.json";
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != scenePath || scene.isDirty)
            throw new InvalidOperationException("Clean SR18 Edit Mode scene required");
        if (File.Exists(recordPath)) throw new InvalidOperationException("Already applied; preserve later edits");
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var props = map.Find("Props");
        var roads = map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
        if (roads.Length != 230 || props.childCount != 715) throw new InvalidOperationException("Unexpected scene baseline");
        Transform G(int index) => props.Cast<Transform>().Single(t => t.name.StartsWith($"SR18_L_G{index:00}_T"));
        Vector3 LaneCenter(Transform group, int index) => group.position - group.right * (index % 2 == 0 ? .8f : -.8f);
        float Floor(Vector3 p)
        {
            foreach (Vector3 offset in new[] { Vector3.zero, Vector3.left * .12f, Vector3.right * .12f, Vector3.forward * .12f, Vector3.back * .12f })
                foreach (var road in roads)
                    if (road.Raycast(new Ray(p + offset + Vector3.up * 2, Vector3.down), out var hit, 4) && hit.normal.y > .7f)
                        return hit.point.y + .08f;
            throw new InvalidOperationException("No road support at " + p);
        }
        var changes = new Dictionary<Transform, Vector3>();
        var buckets = new List<object>();
        foreach (int index in new[] { 1, 4, 7 })
        {
            var group = G(index);
            var parts = group.GetComponentsInChildren<ObstacleStats>().OrderBy(p => p.name).ToArray();
            if (parts.Length != 3 || parts.Any(p => p.obstaclePattern != ObstaclePattern.Bucket))
                throw new InvalidOperationException("Unexpected bucket group: " + group.name);
            float[] along = index == 4 ? new[] { 0f, 12f, 24f } : new[] { -12f, 0f, 12f };
            float[] lateral = index == 4 ? new[] { -1.1f, 1.1f, 1.1f } :
                index == 7 ? new[] { 1.1f, -1.1f, 1.1f } : new[] { -1.1f, 1.1f, -1.1f };
            var center = LaneCenter(group, index);
            for (int i = 0; i < parts.Length; i++)
            {
                Vector3 point = center + group.forward * along[i] + group.right * lateral[i];
                point.y = Floor(point);
                foreach (float offset in new[] { -.72f, .72f }) Floor(point + group.right * offset);
                changes.Add(parts[i].transform, point);
            }
            buckets.Add(new { id = group.name, along, lateral });
        }
        var overrides = new List<object>();
        foreach (int index in new[] { 3, 6 })
        {
            var hole = G(index);
            var center = LaneCenter(hole, index);
            Vector3 point = center + hole.right * (index == 3 ? -1.9f : 1.9f);
            // Preserve this model's authored surface offset; no mesh or collider resizing.
            point.y = hole.position.y + Floor(point) - Floor(hole.position);
            foreach (float offset in new[] { -1.12f, 1.12f }) Floor(point + hole.right * offset);
            changes.Add(hole, point);
            overrides.Add(new { id = hole.name, center = new[] { point.x, point.y, point.z } });
            var pair = map.Find("Bonuses").GetComponentsInChildren<BonusWallChoicePair>().Single(p => p.name.StartsWith($"SR18_L_B{index:00}_T"));
            Vector3 reward = center - hole.forward * 25;
            reward.y = Floor(reward);
            Vector3 previous = (pair.Left.transform.position + pair.Right.transform.position) * .5f;
            foreach (var altar in new[] { pair.Left, pair.Right })
            {
                Vector3 destination = altar.transform.position + reward - previous;
                destination.y = Floor(destination);
                foreach (float offset in new[] { -1f, 1f }) Floor(destination + hole.right * offset);
                changes.Add(altar.transform, destination);
            }
            Vector3 rewardCenter = (changes[pair.Left.transform] + changes[pair.Right.transform]) * .5f;
            overrides.Add(new { id = pair.name, center = new[] { rewardCenter.x, rewardCenter.y, rewardCenter.z } });
        }
        var fill = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            .Single(i => i.name == "HealthFill");
        var preview = changes.Select(c => new { id = c.Key.name, parent = c.Key.parent.name,
            before = new[] { c.Key.position.x, c.Key.position.y, c.Key.position.z }, after = new[] { c.Value.x, c.Value.y, c.Value.z } }).ToArray();
        if (!apply) return new { applied = false, transformChanges = preview, buckets, centerOverrides = overrides };
        string backup = "tmp/backups/sr18-opening-rhythm-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "/before-layout.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Backup failed");
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("SR18 opening decisions and readable health fill");
        try
        {
            foreach (var change in changes)
            {
                Undo.RecordObject(change.Key, "Opening rhythm placement");
                change.Key.position = change.Value;
                PrefabUtility.RecordPrefabInstancePropertyModifications(change.Key);
            }
            Undo.RecordObject(fill, "Health bar fill mode");
            const string fillPath = "Assets/ShooterSurvival/Sprites/Generated/HealthFillWhite.png";
            if (!File.Exists(fillPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fillPath));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                File.WriteAllBytes(fillPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(fillPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(fillPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            fill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fillPath);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            PrefabUtility.RecordPrefabInstancePropertyModifications(fill);
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Save failed");
            var record = new { applied = true, backup, transformChanges = preview, buckets, centerOverrides = overrides,
                rewardLead = 25, roads = 230, props = 715, enemies = 25, choicePairs = 25, gimmickStations = 24, workbookChanged = false };
            // Pipeline's ephemeral compiler also sees Localization's bundled JsonConvert.
            var serializer = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json")
                .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) });
            Directory.CreateDirectory(Path.GetDirectoryName(recordPath));
            File.WriteAllText(recordPath, (string)serializer.Invoke(null, new object[] { record }));
            Undo.CollapseUndoOperations(undo);
            return record;
        }
        catch { Undo.RevertAllDownToGroup(undo); throw; }
    }
}
