#if UNITY_EDITOR
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

// One explicit migration of the reviewed 25-single-altar layout, never a scene-load generator.
public static class NoryangjinSr18ChoiceLayout
{
    public const string RecordPath = "map-concepts/sr18-choices-and-corner-space-2026-09-08";
    public const float ChoiceOffset = 1.9f;
    public const float ChoiceScale = 2.25f;
    public const float MinimumGap = 24;
    private static float[] V(Vector3 p) => new[] { p.x, p.y, p.z };
    private static Vector3 Read(JToken t) => new((float)t[0], (float)t[1], (float)t[2]);

    public static string Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != NoryangjinSr18LatestEncounters.ScenePath || scene.isDirty)
            throw new InvalidOperationException("Clean SR18 Edit Mode scene required.");
        if (File.Exists(RecordPath + "/applied.json")) throw new InvalidOperationException("Already applied; preserve subsequent authoring.");
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var props = map.Find("Props"); var roads = map.Find("Roads"); var bonuses = map.Find("Bonuses"); var enemies = map.Find("Enemies");
        if (roads.childCount != 230 || props.childCount != 715 || enemies.childCount != 25 || bonuses.childCount != 25 ||
            map.GetComponentsInChildren<BonusWallChoicePair>(true).Length != 0)
            throw new InvalidOperationException("Unexpected baseline composition.");
        var original = JObject.Parse(File.ReadAllText(NoryangjinSr18LatestEncounters.RecordPath + "/placement-report.json"));
        var sections = JObject.Parse(File.ReadAllText("map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/full-flow-final.json"))["sections"].Children<JObject>().ToArray();
        var sectionsById = sections.ToDictionary(s => (string)s["id"]);
        var cumulative = new Dictionary<string, float>(); float routeLength = 0;
        foreach (var s in sections) { cumulative[(string)s["id"]] = routeLength; routeLength += Vector3.Distance(Read(s["start"]), Read(s["end"])); }
        var settings = EncounterPlacementTables.Rows.Where(r => r.scene == scene.name).ToDictionary(r => r.id);
        var protectedRoots = roads.Cast<Transform>().Concat(props.Cast<Transform>().Where(t => !t.name.StartsWith("SR18_L_")))
            .Concat(map.Find("Water").Cast<Transform>()).ToArray();
        var protectedComponents = protectedRoots.SelectMany(t => t.GetComponentsInChildren<Component>(true)).Where(c => c != null).Distinct()
            .ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
        var protectedObjects = protectedRoots.SelectMany(t => t.GetComponentsInChildren<Transform>(true))
            .ToDictionary(t => t.gameObject, t => EditorJsonUtility.ToJson(t.gameObject));
        var colliders = roads.GetComponentsInChildren<MeshCollider>(true);
        float Floor(Vector3 p, string section)
        {
            float height = NoryangjinSr18LatestEncounters.Height(section, p.x);
            foreach (var offset in new[] { Vector3.zero, Vector3.right * .12f, Vector3.left * .12f, Vector3.forward * .12f, Vector3.back * .12f })
                foreach (var c in colliders)
                    if (c.Raycast(new Ray(new Vector3(p.x, height + 3, p.z) + offset, Vector3.down), out var hit, 6) && hit.normal.y > .7f && Mathf.Abs(hit.point.y - height) < 1.1f)
                        return hit.point.y;
            throw new InvalidOperationException("No correct deck: " + section + " " + p);
        }
        // Preflight the complete route reflow before changing anything.
        var plan = new List<(JObject Row, Transform Root, Vector3 Start, Vector3 Dir, float Before, float After, Vector3 Center)>();
        float previousDistance = float.NegativeInfinity;
        foreach (var row in original["placements"].Children<JObject>().OrderBy(r => (int)r["time"]))
        {
            string kind = (string)row["kind"], id = (string)row["name"], section = (string)row["section"];
            var root = (kind == "enemy" ? enemies : kind == "bonus" ? bonuses : props).Find(id);
            if (root == null || !settings.ContainsKey(id)) throw new InvalidOperationException("Missing placement or Excel binding: " + id);
            if (Vector3.Distance(root.position, Read(row["position"])) > .03f) throw new InvalidOperationException("Placement was edited since the prior record: " + id);
            Vector3 start = Read(sectionsById[section]["start"]), end = Read(sectionsById[section]["end"]), dir = (end - start).normalized;
            Vector3 center = root.position; float minimum = kind == "object" ? 16 : 8;
            if (kind == "enemy")
            {
                var e = root.GetComponent<EnemyEventController>();
                if (e.EventMode == EnemyEventMode.PatrolBetweenStartAndTarget) center = (root.position + e.TargetPoint.position) * .5f;
                else if (e.EventMode == EnemyEventMode.AmbushMoveThenShoot) center = e.TargetPoint.position;
                minimum = 28 + (e.EventMode == EnemyEventMode.PatrolBetweenStartAndTarget ? settings[id].moveDistance * .5f : 0);
                if (e.EventMode == EnemyEventMode.AmbushMoveThenShoot) minimum = Mathf.Max(minimum, 20 + settings[id].activationLead);
            }
            float before = Vector3.Dot(center - start, dir);
            float after = Mathf.Max(before, minimum, previousDistance + MinimumGap - cumulative[section]);
            if (after > Vector3.Distance(start, end) - 8 + .01f) throw new InvalidOperationException("Reflow exceeds section: " + id);
            if (kind == "bonus" && new[] { 161, 177, 269 }.Contains((int)row["time"]) && Mathf.Abs(after - before) > .01f)
                throw new InvalidOperationException("Would move the explicitly nudged bridge choice: " + id);
            previousDistance = cumulative[section] + after;
            Floor(root.position + dir * (after - before), section);
            plan.Add((row, root, start, dir, before, after, center));
        }
        string backup = "tmp/backups/sr18-choice-layout-2026-09-08/" + DateTime.Now.ToString("HHmmss") + "/before.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup)); Directory.CreateDirectory(RecordPath);
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Backup failed");
        var report = new List<object>();
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("SR18 choices and corner response space");
        bool saved = false;
        void Record(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (c != null) { EditorUtility.SetDirty(c); PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        }
        try
        {
            foreach (var item in plan)
            {
                string kind = (string)item.Row["kind"], section = (string)item.Row["section"];
                var root = item.Root; Vector3 before = root.position;
                float delta = item.After - item.Before;
                if (delta > .005f)
                {
                    var position = root.position + item.Dir * delta;
                    position.y += Floor(position, section) - Floor(root.position, section);
                    Undo.RecordObject(root, "Reflow encounter"); root.position = position;
                    if (kind == "enemy")
                    {
                        var enemy = root.GetComponent<EnemyEventController>();
                        enemy.RefreshPlacementAfterAuthoringChange(before, false);
                        if (enemy.HasUsableTarget) { Undo.RecordObject(enemy.TargetPoint, "Move target with enemy"); enemy.TargetPoint.position += position - before; }
                    }
                }
                Vector3 center = item.Center + root.position - before;
                Vector3? rightPosition = null, gatePosition = null;
                if (kind == "bonus")
                {
                    var left = root.GetComponent<AuthoredBonusWall>();
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab");
                    var other = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bonuses);
                    Undo.RegisterCreatedObjectUndo(other, "Right bonus choice"); other.name = root.name + "_Right"; other.SetActive(true);
                    var right = other.GetComponent<AuthoredBonusWall>();
                    Vector3 lateral = Vector3.Cross(Vector3.up, item.Dir);
                    Undo.RecordObject(root, "Fit left choice to narrow road");
                    root.localScale = Vector3.one * ChoiceScale; root.position = center - lateral * ChoiceOffset;
                    other.transform.localScale = root.localScale; other.transform.SetPositionAndRotation(center + lateral * ChoiceOffset, root.rotation);
                    right.Configure(left.Rarity);
                    var marker = right.Wall.GetComponent<RuntimeBonusWall>() ?? Undo.AddComponent<RuntimeBonusWall>(right.Wall.gameObject); marker.KeepAsMapAuthoredWall();
                    var pair = Undo.AddComponent<BonusWallChoicePair>(root.gameObject);
                    Undo.RecordObject(left, "Link left choice"); pair.Configure(left, right);
                    foreach (Transform t in other.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    // Both small pedestal centers and their outer lateral footprints must stay on the actual road.
                    foreach (float side in new[] { -2.95f, -ChoiceOffset, ChoiceOffset, 2.95f }) Floor(center + lateral * side, section);
                    rightPosition = other.transform.position; Record(other);
                }
                else if (kind == "enemy")
                {
                    var enemy = root.GetComponent<EnemyEventController>();
                    var clearance = Undo.AddComponent<EnemyCornerClearance>(root.gameObject); clearance.Configure(item.Start, item.Dir);
                    clearance.ValidateMovement(root.position, enemy.HasUsableTarget ? enemy.TargetPoint.position : root.position);
                    var spot = props.GetComponentsInChildren<EnemyEventActivationSpot>(true).Single(s => s.Targets.Contains(enemy));
                    var gate = clearance.ConstrainTrigger(center - item.Dir * settings[root.name].activationLead);
                    gate.y = Floor(gate, section) + .08f;
                    Undo.RecordObject(spot.transform, "Safe post-turn activation"); spot.transform.position = gate; gatePosition = gate; Record(spot.gameObject);
                }
                Record(root.gameObject);
                report.Add(new { id = root.name, kind, section, slot = (int)item.Row["time"], before = V(before), after = V(root.position), center = V(center),
                    alongBefore = item.Before, alongAfter = item.After, routeDistance = cumulative[section] + item.After,
                    right = rightPosition.HasValue ? V(rightPosition.Value) : null, gate = gatePosition.HasValue ? V(gatePosition.Value) : null });
            }
            foreach (var pair in protectedComponents)
                if (pair.Key == null || EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("Protected road/scenery component changed");
            foreach (var pair in protectedObjects)
                if (pair.Key == null || EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("Protected road/scenery object changed");
            if (bonuses.childCount != 50 || bonuses.GetComponentsInChildren<BonusWallChoicePair>(true).Length != 25 || enemies.childCount != 25 || props.childCount != 715)
                throw new InvalidOperationException("Unexpected final counts");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Save failed");
            saved = true; Undo.CollapseUndoOperations(undo);
            File.WriteAllText(RecordPath + "/applied.json", JsonConvert.SerializeObject(new { backup, scene = scene.path, choicePoints = 25, altars = 50, enemies = 25, gimmicks = 24,
                minimumBodyDistance = 28, minimumActivationDistance = 20, minimumGimmickDistance = 16, minimumStationGap = MinimumGap, choiceScale = ChoiceScale, choiceOffset = ChoiceOffset,
                protectedRoads = 230, protectedOriginalProps = 666, events = report }, Formatting.Indented));
            return $"Saved 25 choice pairs and 25 post-turn buffers. Backup: {backup}";
        }
        catch { if (!saved) Undo.RevertAllDownToGroup(undo); throw; }
    }
}
#endif
