using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Sr18ContactPairs
{
    public static object Main(bool apply = false)
    {
        const string recordPath = "map-concepts/sr18-contact-pairs-2026-09-10/applied.json";
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "Noryangjin_MapTool_Mode_SR18" || scene.isDirty)
            throw new InvalidOperationException("Clean SR18 Edit Mode required");
        if (File.Exists(recordPath)) throw new InvalidOperationException("Already applied; preserve later edits");
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var enemies = map.Find("Enemies"); var props = map.Find("Props");
        var roads = map.Find("Roads").GetComponentsInChildren<MeshCollider>();
        if (enemies.childCount != 25 || props.childCount != 715) throw new InvalidOperationException("Unexpected baseline");
        float Floor(Vector3 p)
        {
            foreach (var offset in new[] { Vector3.zero, Vector3.left * .12f, Vector3.right * .12f, Vector3.forward * .12f, Vector3.back * .12f })
                foreach (var road in roads)
                    if (road.Raycast(new Ray(p + offset + Vector3.up, Vector3.down), out var hit, 2) && hit.normal.y > .7f && Mathf.Abs(hit.point.y - p.y) < .5f)
                        return hit.point.y + .08f;
            throw new InvalidOperationException("No matching road at " + p);
        }
        var fatChanges = new Dictionary<EnemyEventController, (Vector3 start, Vector3 target)>();
        foreach (var e in enemies.GetComponentsInChildren<EnemyEventController>().Where(e => e.name.Contains("FatMan")))
        {
            var dir = Vector3.ProjectOnPlane(e.transform.forward, Vector3.up).normalized;
            var start = e.TargetPoint.position + dir * Vector3.Dot(e.transform.position - e.TargetPoint.position, dir);
            var target = e.TargetPoint.position;
            float y = Mathf.Max(Floor(start), Floor(target)); start.y = y; target.y = y;
            e.GetComponent<EnemyCornerClearance>().ValidateMovement(start, target);
            for (int i = 0; i <= 16; i++)
            {
                var point = Vector3.Lerp(start, target, i / 16f);
                Floor(point); Floor(point - e.transform.right * .6f); Floor(point + e.transform.right * .6f);
            }
            fatChanges.Add(e, (start, target));
        }
        if (fatChanges.Count != 3) throw new InvalidOperationException("Expected three FatMan placements");
        var formations = new List<(EnemyEventController source, EnemyEventActivationSpot gate, Vector3 left, Vector3 right)>();
        foreach (int index in new[] { 2, 6 })
        {
            var e = enemies.GetComponentsInChildren<EnemyEventController>().Single(e => e.name.StartsWith($"SR18_L_E{index:00}_T"));
            if (e.EventMode != EnemyEventMode.AttackLoop || e.HasUsableTarget) throw new InvalidOperationException("A stationary Normal source is required");
            if (enemies.Find(e.name + "_Right") != null) throw new InvalidOperationException("Companion already exists");
            var gate = props.GetComponentsInChildren<EnemyEventActivationSpot>().Single(s => s.Targets.Length == 1 && s.Targets[0] == e);
            var left = e.transform.position - e.transform.right * 1.1f;
            var right = e.transform.position + e.transform.right * 1.1f;
            left.y = Floor(left); right.y = Floor(right);
            foreach (var p in new[] { left, right }) { Floor(p - e.transform.right * .6f); Floor(p + e.transform.right * .6f); }
            e.GetComponent<EnemyCornerClearance>().ValidateMovement(left, right);
            formations.Add((e, gate, left, right));
        }
        var lamps = props.Cast<Transform>().Where(t => t.name.StartsWith("SR18_L_G"))
            .SelectMany(t => t.GetComponentsInChildren<ObstacleStats>()).Where(o => o.enabled && o.obstaclePattern == ObstaclePattern.Light).ToArray();
        if (lamps.Length != 8) throw new InvalidOperationException("Expected eight authored lamp hazards");
        var preview = new { fats = fatChanges.Select(c => new { id = c.Key.name, start = c.Value.start.ToString(), target = c.Value.target.ToString() }).ToArray(),
            pairs = formations.Select(f => new { id = f.source.name, left = f.left.ToString(), right = f.right.ToString() }).ToArray(), durableLamps = lamps.Length };
        if (!apply) return preview;
        string backup = "tmp/backups/sr18-contact-pairs-20260910-022738/before-layout.unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Backup failed");
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("SR18 contact hazards and Normal pairs");
        var overrides = new List<object>(); var pairRecords = new List<object>();
        void Remember(UnityEngine.Object o) { EditorUtility.SetDirty(o); PrefabUtility.RecordPrefabInstancePropertyModifications(o); }
        float[] V(Vector3 v) => new[] { v.x, v.y, v.z };
        GameObject CloneAuthored(GameObject source, Transform parent)
        {
            var clone = UnityEngine.Object.Instantiate(source, parent);
            Undo.RegisterCreatedObjectUndo(clone, "Authored companion");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source));
            PrefabUtility.ConvertToPrefabInstance(clone, prefab, new ConvertToPrefabInstanceSettings {
                componentsNotMatchedBecomesOverride = true, gameObjectsNotMatchedBecomesOverride = true,
                recordPropertyOverridesOfMatches = true, changeRootNameToAssetName = false
            }, InteractionMode.AutomatedAction);
            return clone;
        }
        try
        {
            foreach (var lamp in lamps) { Undo.RecordObject(lamp, "Durable contact hazard"); lamp.canBeShotDown = false; Remember(lamp); }
            foreach (var c in fatChanges)
            {
                var e = c.Key; var before = e.transform.position;
                Undo.RecordObjects(new UnityEngine.Object[] { e, e.transform, e.TargetPoint }, "FatMan road center");
                e.AmbushEntrySide = 0; e.transform.position = c.Value.start; e.TargetPoint.position = c.Value.target;
                e.RefreshPlacementAfterAuthoringChange(before, false);
                Remember(e); Remember(e.transform); Remember(e.TargetPoint);
                overrides.Add(new { id = e.name, center = V(c.Value.target) });
            }
            foreach (var f in formations)
            {
                var e = f.source; var original = e.transform.position;
                var companion = CloneAuthored(e.gameObject, enemies);
                companion.name = e.name + "_Right";
                companion.transform.position = f.right;
                var companionEvent = companion.GetComponent<EnemyEventController>();
                companionEvent.RefreshPlacementAfterAuthoringChange(original, false);
                var gate = CloneAuthored(f.gate.gameObject, props);
                gate.name = f.gate.name + "_Right";
                gate.GetComponent<EnemyEventActivationSpot>().Targets = new[] { companionEvent };
                Undo.RecordObject(e.transform, "Left formation member"); e.transform.position = f.left;
                e.RefreshPlacementAfterAuthoringChange(original, false);
                Remember(e.transform);
                foreach (var obj in new[] { companion, gate })
                    foreach (var component in obj.GetComponentsInChildren<Component>(true)) if (component != null) Remember(component);
                overrides.Add(new { id = e.name, center = V(f.left) });
                pairRecords.Add(new { left = e.name, right = companion.name, center = V(original), forward = V(e.transform.forward), lateral = 1.1f });
            }
            Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Save failed");
            var result = new { backup, enemies = 27, props = 717, settings = 76, durableLamps = 8, centerOverrides = overrides, formations = pairRecords,
                fatStarts = fatChanges.Select(c => new { id = c.Key.name, start = V(c.Value.start), target = V(c.Value.target) }).ToArray() };
            var serializer = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert")
                .GetMethod("SerializeObject", new[] { typeof(object) });
            Directory.CreateDirectory(Path.GetDirectoryName(recordPath));
            File.WriteAllText(recordPath, (string)serializer.Invoke(null, new object[] { result }));
            Undo.CollapseUndoOperations(undo); return result;
        }
        catch { Undo.RevertAllDownToGroup(undo); throw; }
    }
}
