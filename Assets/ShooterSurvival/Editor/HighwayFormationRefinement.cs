using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HighwayFormationRefinement
{
    public static object ApplyOpenScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != HighwaySceneBuilder.ScenePath)
            throw new InvalidOperationException("HighWay in Edit Mode required.");
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var enemies = map.Find("Enemies"); var floors = map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
        var entries = JObject.Parse(File.ReadAllText("map-concepts/skins-reststop-2026-09-12/encounter-layout-HighWay.json"))["placements"]
            .Where(row => (string)row["kind"] == "enemy").ToArray();
        if (entries.Length != 50) throw new InvalidOperationException("Expected50 recorded Highway enemies.");
        Physics.SyncTransforms();
        float Floor(Vector3 p)
        {
            foreach (var floor in floors)
                if (floor.Raycast(new Ray(p + Vector3.up * 1.5f, Vector3.down), out var hit, 3) && hit.normal.y > .7f) return hit.point.y + .08f;
            throw new InvalidOperationException("Formation leaves road support: " + p);
        }
        var changes = new List<object>();
        foreach (var row in entries)
        {
            string id = (string)row["id"]; int station = (int)row["station"];
            bool companion = id.EndsWith("_Right", StringComparison.Ordinal);
            float lane = station % 2 == 1 ? (companion ? 3.1f : 0) : (companion ? 0 : -3.1f);
            var center = HighwaySceneBuilder.Sample((float)row["distance"], out var forward) + Vector3.Cross(Vector3.up, forward) * lane;
            var enemy = enemies.Find(id)?.GetComponent<EnemyEventController>() ?? throw new InvalidOperationException("Missing enemy " + id);
            var right = Vector3.Cross(Vector3.up, forward);
            float span = enemy.EventMode == EnemyEventMode.PatrolBetweenStartAndTarget ? 1.2f : 0;
            var start = center - right * span * .5f; start.y = Floor(start);
            var end = center + right * span * .5f; end.y = Floor(end);
            var prior = enemy.transform.position;
            enemy.transform.SetPositionAndRotation(start, Quaternion.LookRotation(forward));
            if (enemy.HasUsableTarget) enemy.TargetPoint.position = end;
            enemy.RefreshPlacementAfterAuthoringChange(prior, true);
            changes.Add(new { id, station, lane, span, before = prior.ToString(), after = start.ToString() });
        }
        foreach (var root in scene.GetRootGameObjects()) foreach (var component in root.GetComponentsInChildren<Component>(true))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("map-concepts/chapters-polish-2026-09-12/highway-formations.json", JsonConvert.SerializeObject(changes, Formatting.Indented));
        return new { scene = scene.name, enemies = changes.Count, layout = "alternating open side", livePlay = "pending" };
    }
}
