using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 2026-09-26 feedback pass for HighWay and RestStop: oncoming cars drove through props, other cars and
// toll booths. This keeps the ±4.2 oncoming lanes physically clear wherever OncomingLaneTraffic runs:
//  - static props / roadblock hazards overlapping the car corridor are deactivated,
//  - potholes move to the centre lanes (their warning sign moves to the shoulder),
//  - toll plazas are cut out of the traffic ranges,
//  - the legacy HighwayOncomingTraffic (pitched Meshy cars, instant death, lane stripes) is disabled,
//  - HighWay's drowsy-shelter pylons move from the fork gore onto the right shoulder.
// Run DryRun first; Run saves both scenes. Idempotent.
public static class InstallTrafficClearance
{
    static readonly string[] Scenes = { "Assets/ShooterSurvival/Scenes/Tools/HighWay.unity", "Assets/ShooterSurvival/Scenes/Tools/RestStop.unity" };
    const float CorridorInner = 2.5f, CorridorOuter = 6.0f, TollMargin = 14f, ShoulderLane = 8.4f;

    public static object DryRun() => Scenes.Select(s => Apply(s, false)).ToArray();
    public static object Run() => Scenes.Select(s => Apply(s, true)).ToArray();

    static HighwayRoute route; static RoutePolyline poly;

    static void Frame(Vector3 p, out float d, out Vector3 centre, out Vector3 forward)
    {
        if (route != null) { d = route.NearestDistance(p); route.Sample(d, false, out centre, out forward); }
        else { d = poly.NearestDistance(p, Vector3.zero); poly.Sample(d, out centre, out forward); }
    }
    static void Sample(float d, out Vector3 c, out Vector3 f)
    {
        if (route != null) route.Sample(d, false, out c, out f); else poly.Sample(d, out c, out f);
    }

    static object Apply(string path, bool save)
    {
        var scene = EditorSceneManager.OpenScene(path);
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        poly = route == null ? UnityEngine.Object.FindFirstObjectByType<RoutePolyline>(FindObjectsInactive.Include) : null;
        var traffic = UnityEngine.Object.FindFirstObjectByType<OncomingLaneTraffic>(FindObjectsInactive.Include);
        var log = new List<string> { "== " + path };
        if (traffic == null || (route == null && poly == null)) { log.Add("no traffic/route"); return log; }

        // 1. Legacy oncoming beats: their rebuilt cars are pitched 270° and they kill on contact.
        foreach (var legacy in UnityEngine.Object.FindObjectsByType<HighwayOncomingTraffic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            legacy.enabled = false; Dirty(legacy);
            foreach (var car in legacy.cars) if (car != null) { car.gameObject.SetActive(false); Dirty(car.gameObject); }
            foreach (var w in legacy.warnings) if (w != null) { w.gameObject.SetActive(false); Dirty(w.gameObject); }
            log.Add("legacy oncoming disabled: " + legacy.name);
        }

        // 2. Toll plazas span every lane: no traffic range may cross one.
        var tolls = UnityEngine.Object.FindObjectsByType<HighwayHazard>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(h => Pattern(h) == ObstaclePattern.HighwayToll)
            .Select(h => { Frame(h.transform.position, out var d, out _, out _); return d; })
            .OrderBy(d => d).ToList();
        var ranges = traffic.activeRanges.ToList();
        foreach (var t in tolls) ranges = ranges.SelectMany(r => Cut(r, t - TollMargin, t + TollMargin)).ToList();
        ranges = ranges.Where(r => r.y - r.x >= 40).ToList();
        log.Add("tolls " + string.Join(",", tolls.Select(t => t.ToString("0")).Distinct()) + " ranges " + string.Join(" ", ranges.Select(r => $"{r.x:0}-{r.y:0}")));
        traffic.activeRanges = ranges.ToArray(); Dirty(traffic);

        // 3. Potholes move to the centre lanes; their warning sign moves to the shoulder on the same side.
        foreach (var hole in UnityEngine.Object.FindObjectsByType<RoadPotholeHazard>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var root = hole.transform;
            Frame(root.position, out var d, out var c, out var f); var right = Vector3.Cross(Vector3.up, f);
            float lane = Vector3.Dot(root.position - c, right);
            float target = Mathf.Abs(lane) < .9f ? lane : Mathf.Sign(lane) * .8f;
            root.position += right * (target - lane); Dirty(root);
            float side = Mathf.Abs(target) < .1f ? 1 : Mathf.Sign(target);
            foreach (var sign in root.Cast<Transform>().Where(t => t.name.Contains("Sign")))
            {
                Frame(sign.position, out var sd, out var sc, out var sf); var sr = Vector3.Cross(Vector3.up, sf);
                float sl = Vector3.Dot(sign.position - sc, sr);
                sign.position += sr * (side * ShoulderLane - sl); Dirty(sign);
            }
            log.Add($"pothole d={d:0} lane {lane:0.0} -> {target:0.0}");
        }

        // 4. HighWay drowsy-shelter pylons: out of the fork gore, onto the shoulder on the bypass side.
        if (route != null)
            foreach (var pylon in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.name == "Drowsy_Shelter_Pylon").ToArray())
            {
                Frame(pylon.position, out var d, out _, out _);
                var fork = route.forks.OrderBy(k => Mathf.Abs(k.start + 28 - d)).First();
                // Right shoulder: the left one borders the opposing carriageway's ambient traffic.
                float side = 1;
                float nd = fork.start - 18;
                route.Sample(nd, false, out var pc, out var pf);
                var target = pc + Vector3.Cross(Vector3.up, pf) * side * ShoulderLane;
                var delta = target - new Vector3(pylon.position.x, pc.y, pylon.position.z);
                var rot = Quaternion.LookRotation(pf) * Quaternion.Inverse(Quaternion.LookRotation(Flat(pylon.forward)));
                var face = pylon.parent.Find(pylon.name + "_Face");
                var pivot = pylon.position;
                foreach (var t in new[] { pylon, face }.Where(t => t != null))
                {
                    var local = t.position - pivot;
                    t.SetPositionAndRotation(pivot + delta + rot * local, rot * t.rotation);
                    Dirty(t);
                }
                log.Add($"pylon d={d:0} -> d={nd:0} side {side:+0;-0}");
            }

        // 5. Anything static left in the car corridor inside a traffic range goes.
        var units = new HashSet<Transform>();
        foreach (var h in UnityEngine.Object.FindObjectsByType<HighwayHazard>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var p = Pattern(h);
            if (p == ObstaclePattern.HighwayToll || p == ObstaclePattern.HighwayTraffic) continue;
            units.Add(h.transform);
        }
        foreach (var group in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                     .Where(t => t.name == "Props" || t.name.EndsWith("WorkZone_Props") || t.name.EndsWith("WorkZones") || t.GetComponent<RoadPotholeHazard>() != null))
            foreach (Transform child in group) units.Add(child);
        int removed = 0;
        foreach (var unit in units)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy || Protected(unit)) continue;
            if (unit.GetComponentInParent<HighwayHazard>() is var parentHazard && parentHazard != null && parentHazard.transform != unit && units.Contains(parentHazard.transform)) continue;
            var rs = unit.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.bounds.size.y > .2f).ToArray();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Frame(b.center, out var d, out var c, out var f);
            if (b.min.y > c.y + 3.5f || b.size.x > 30 || b.size.z > 30) continue;
            if (!traffic.activeRanges.Any(r => d >= r.x - 8 && d <= r.y + 8)) continue;
            var right = Vector3.Cross(Vector3.up, f);
            float lane = Vector3.Dot(b.center - c, right);
            float half = Mathf.Abs(b.extents.x * right.x) + Mathf.Abs(b.extents.z * right.z);
            // Roadside furniture (centre beyond the shoulder) and lane-spanning sign gantries stay.
            if (Mathf.Abs(lane) > 7.5f || half > 6f) continue;
            bool overlaps = lane + half > CorridorInner && lane - half < CorridorOuter || lane - half < -CorridorInner && lane + half > -CorridorOuter;
            if (!overlaps) continue;
            log.Add($"remove d={d:0} lane {lane:0.0}±{half:0.0} {PathOf(unit)}");
            if (save) { unit.gameObject.SetActive(false); Dirty(unit.gameObject); }
            removed++;
        }
        log.Add("removed " + removed);
        if (save)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed " + path);
        }
        return log;
    }

    // HighWay: overhead direction signs authored inside a fork had a post standing in the bypass road.
    // Slide each such sign along the mainline to just outside the fork (before its start or after its end).
    public static object Signs()
    {
        var scene = EditorSceneManager.OpenScene(Scenes[0]);
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include); poly = null;
        var log = new List<string>();
        var signs = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.name == "Korean_Direction_Sign").ToList();
        foreach (var sign in signs)
        {
            float d = route.NearestDistance(sign.position);
            var fork = route.forks.FirstOrDefault(k => d > k.start - 6 && d < k.end + 6);
            if (fork == null) continue;
            float before = fork.start - 22, after = fork.end + 22;
            float nd = d - before < after - d ? before : after;
            // A sign already announces this fork just before it: the in-fork duplicate goes (after the fork is a toll plaza).
            if (signs.Any(o => o != sign && o.gameObject.activeSelf && Mathf.Abs(route.NearestDistance(o.position) - nd) < 30))
            { sign.gameObject.SetActive(false); Dirty(sign.gameObject); log.Add($"sign d={d:0} hidden (duplicate)"); continue; }
            route.Sample(d, false, out var c0, out var f0); route.Sample(nd, false, out var c1, out var f1);
            var turn = Quaternion.FromToRotation(Flat(f0), Flat(f1));
            var local = sign.position - c0;
            sign.SetPositionAndRotation(c1 + turn * local, turn * sign.rotation); Dirty(sign);
            log.Add($"sign d={d:0} -> {nd:0} (fork {fork.start:0}-{fork.end:0})");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed");
        return log;
    }

    // 2026-09-26 follow-up (both scenes):
    //  - deer hazards: WaterDeerCrossing (continuous right-to-left run, 20% + spin) replaces the HighwayTraffic
    //    hazard behaviour; the hazard component, its collider and yellow warning line are switched off,
    //  - side traffic: the left lane (next to the yellow line) comes head-on, the right lane drives the shark's way (user flipped it),
    //  - pothole "공사중" signs stand in front of their pothole in the pothole's own lane instead of the shoulder.
    public static object Followup()
    {
        var result = new List<string>();
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
            poly = route == null ? UnityEngine.Object.FindFirstObjectByType<RoutePolyline>(FindObjectsInactive.Include) : null;
            result.Add("== " + path);
            int deer = 0;
            foreach (var hazard in UnityEngine.Object.FindObjectsByType<HighwayHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var hop = hazard.GetComponentInChildren<DeerHop>(true);
                if (Pattern(hazard) != ObstaclePattern.HighwayTraffic || hop == null) continue;
                hazard.enabled = false; Dirty(hazard);
                foreach (var c in hazard.GetComponents<Collider>()) { c.enabled = false; Dirty(c); }
                if (hazard.warning != null) { hazard.warning.SetActive(false); Dirty(hazard.warning); }
                foreach (var w in hazard.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("TrafficWarning"))) { w.gameObject.SetActive(false); Dirty(w.gameObject); }
                var crossing = hazard.GetComponent<WaterDeerCrossing>(); if (crossing == null) crossing = hazard.gameObject.AddComponent<WaterDeerCrossing>();
                crossing.deer = hop.transform; Dirty(crossing);
                deer++;
            }
            result.Add("deer crossings " + deer);
            foreach (var traffic in UnityEngine.Object.FindObjectsByType<OncomingLaneTraffic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                traffic.laneDirections = traffic.lanes.Select(l => l < 0 ? -1f : 1f).ToArray(); Dirty(traffic); // user: left lane head-on, right lane the shark's way
                result.Add("lanes " + string.Join(",", traffic.lanes) + " dirs " + string.Join(",", traffic.laneDirections));
            }
            foreach (var hole in UnityEngine.Object.FindObjectsByType<RoadPotholeHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var root = hole.transform;
                Frame(root.position, out var d, out var c, out var f); var right = Vector3.Cross(Vector3.up, f);
                float lane = Vector3.Dot(root.position - c, right);
                foreach (var sign in root.Cast<Transform>().Where(t => t.name.Contains("Sign")))
                {
                    Sample(d - 3.4f, out var sc, out var sf);
                    var sr = Vector3.Cross(Vector3.up, sf);
                    sign.SetPositionAndRotation(new Vector3(0, sign.position.y - sc.y, 0) + sc + sr * lane, Quaternion.LookRotation(sf));
                    foreach (var col in sign.GetComponentsInChildren<Collider>(true)) { col.enabled = false; Dirty(col); }
                    Dirty(sign);
                    result.Add($"pothole d={d:0} lane {lane:0.0}: sign centred in front");
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed " + path);
        }
        return result;
    }

    // 2026-09-26 playthrough: Meshy vehicle visuals rebuilt inside old vehicle hierarchies (HighWay Refine2)
    // inherited an extra 90-degree pitch and stood on their noses or lay on their sides. Each one is turned so
    // the up axis its prefab gives the mesh points up again (heading kept), then re-grounded on its old
    // footprint. Also: potholes shrink to 0.8x (trigger 1.8 m) so the gap beside one is readable.
    public static object Upright()
    {
        var result = new List<string>();
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            int fixedCount = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var node in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("_Meshy")).ToArray())
                {
                    var mf = node.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null);
                    if (mf == null) continue;
                    string meshPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                    if (!meshPath.Contains("MeshyRestStop20260925")) continue;
                    string id = Path.GetFileName(Path.GetDirectoryName(meshPath));
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/" + id + ".prefab");
                    if (prefab == null) continue;
                    var twin = prefab.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh == mf.sharedMesh);
                    if (twin == null) continue;
                    var upLocal = Quaternion.Inverse(twin.transform.rotation) * Vector3.up;
                    var currentUp = mf.transform.rotation * upLocal;
                    if (Vector3.Angle(currentUp, Vector3.up) < 15) continue;
                    var before = WorldBounds(node);
                    node.rotation = Quaternion.FromToRotation(currentUp, Vector3.up) * node.rotation;
                    var after = WorldBounds(node);
                    node.position += new Vector3(before.center.x - after.center.x, before.min.y - after.min.y, before.center.z - after.center.z);
                    Dirty(node); fixedCount++;
                }
            int holes = 0;
            foreach (var hole in UnityEngine.Object.FindObjectsByType<RoadPotholeHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var box = hole.GetComponent<BoxCollider>(); if (box != null) { box.size = new Vector3(1.8f, 2f, 1.8f); Dirty(box); }
                var model = hole.transform.Find("Pothole"); if (model != null) { model.localScale = Vector3.one * .8f; Dirty(model); }
                holes++;
            }
            result.Add(path + ": upright " + fixedCount + ", potholes " + holes);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed " + path);
        }
        return result;
    }

    // Upright() rotated vehicle visuals inside non-uniformly scaled Fit parents, which shears them flat. This
    // replaces every such visual with a clean Meshy prefab instance directly under the vehicle root (the object
    // traffic scripts move), facing the root's forward at the prefab's real size.
    public static object RebuildVehicles()
    {
        var result = new List<string>();
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            int rebuilt = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var node in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("_Meshy")).ToArray())
                {
                    var mf = node.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null);
                    if (mf == null) continue;
                    string meshPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                    if (!meshPath.Contains("MeshyRestStop20260925")) continue;
                    string id = Path.GetFileName(Path.GetDirectoryName(meshPath));
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/" + id + ".prefab");
                    if (prefab == null) continue;
                    var vroot = node; while (vroot.parent != null && (vroot.name == "ProductionVisual" || vroot.name == "Fit" || vroot.name == "Orientation" || vroot.name == "Visual" || vroot.name.EndsWith("_Meshy"))) vroot = vroot.parent;
                    if (vroot == node) continue;
                    var s = vroot.lossyScale;
                    if (Mathf.Max(s.x, s.y, s.z) / Mathf.Max(.0001f, Mathf.Min(s.x, s.y, s.z)) > 1.05f) { result.Add("skip skewed root " + vroot.name); continue; }
                    bool active = node.gameObject.activeSelf;
                    var old = WorldBounds(node);
                    var fwd = vroot.forward; fwd.y = 0; if (fwd.sqrMagnitude < .01f) fwd = Vector3.forward;
                    var inst = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, vroot)).transform;
                    inst.name = id + "_Meshy";
                    inst.SetPositionAndRotation(new Vector3(vroot.position.x, old.min.y, vroot.position.z), Quaternion.LookRotation(fwd.normalized));
                    inst.localScale = Vector3.one / s.x;
                    foreach (var c in inst.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                    inst.gameObject.SetActive(active);
                    UnityEngine.Object.DestroyImmediate(node.gameObject);
                    Dirty(inst.gameObject); rebuilt++;
                }
            result.Add(path + ": rebuilt " + rebuilt);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed " + path);
        }
        return result;
    }

    static Bounds WorldBounds(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    static bool Protected(Transform t)
    {
        if (t.GetComponentInChildren<EnemyScript_space>(true) || t.GetComponentInChildren<PlayerScript>(true)) return true;
        if (t.GetComponentInChildren<RoadPotholeHazard>(true) || t.GetComponentInChildren<LogTruckSpill>(true) || t.GetComponentInChildren<OncomingLaneTraffic>(true)) return true;
        if (t.GetComponentInChildren<DeerHop>(true) || t.GetComponentInChildren<BonusTalismanPickup>(true)) return true;
        string n = t.name;
        return n.Contains("Guardrail") || n.Contains("Bypass") || n.Contains("Road") && !n.Contains("Roadblock") || n.Contains("Gantry") || n.Contains("Pylon") || n.Contains("Pothole") || n.Contains("Support") || n.Contains("Tunnel");
    }

    static IEnumerable<Vector2> Cut(Vector2 r, float a, float b)
    {
        if (b <= r.x || a >= r.y) { yield return r; yield break; }
        if (a > r.x) yield return new Vector2(r.x, a);
        if (b < r.y) yield return new Vector2(b, r.y);
    }
    static ObstaclePattern Pattern(HighwayHazard h) { var s = h.GetComponent<ObstacleStats>(); return s != null ? s.obstaclePattern : ObstaclePattern.None; }
    static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 1e-4f ? Vector3.forward : v.normalized; }
    static void Dirty(UnityEngine.Object o) { EditorUtility.SetDirty(o); if (PrefabUtility.IsPartOfPrefabInstance(o)) PrefabUtility.RecordPrefabInstancePropertyModifications(o); }
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
