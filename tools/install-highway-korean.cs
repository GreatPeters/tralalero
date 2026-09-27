using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Korean-expressway pass for HighWay (docs/plans/2026-09-25-reststop-korean-entry-meshy-plan.md §7).
// Reuses the rest-stop Meshy set: enemies/civilians/vehicles re-skinned, city skyscrapers replaced by
// apartment blocks, paddies, greenhouses and ridges, work zones dressed with arrow trucks and flaggers,
// oncoming traffic on the opposing carriageway, and two new dodge gimmicks (tow-truck chase, rolling tyres).
// Gameplay contract (enemies, hazards, bonuses, road tiles, colliders) must match before saving.
public static class InstallHighwayKorean
{
    const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/HighWay.unity";
    const string Marker = "KoreanHighway_20260925";
    const string Meshy = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    const string Cone = "Assets/polyperfect/Poly Universal Pack/Prefabs/City/Props City/Street Props/Cone_City.prefab";
    const string Tire = "Assets/polyperfect/Poly Universal Pack/Prefabs/City/Props City/Garbage Props/Tire_City.prefab";
    const string Evidence = "outputs/meshy-highway-2026-09-25/install/";
    static readonly System.Random Rng = new(20260926);
    static Transform root;
    static HighwayRoute route;

    static readonly Dictionary<string, string> VehicleSwap = new()
    {
        { "V01", "V01_compact_car" }, { "V02", "V02_white_sedan" }, { "V03", "V03_black_suv" }, { "V04", "V04_1ton_truck" },
        { "V05", "V05_tour_bus" }, { "V06", "V08_box_truck" },
        { "HWY_067", "V03_black_suv" }, { "HWY_068", "V05_tour_bus" }, { "HWY_069", "V08_box_truck" }, { "HWY_080", "V05_tour_bus" },
        { "HWY_081", "V07_taxi" }, { "HWY_082", "V02_white_sedan" }, { "HWY_083", "V04_1ton_truck" },
    };
    static readonly Dictionary<string, string> PeopleSwap = new()
    {
        { "H01", "A07_parking_guide" }, { "H04", "A10_cashier" }, { "H05", "A09_street_cleaner" }, { "H06", "A08_fuel_attendant" },
        { "H07", "A01_dad" }, { "H08", "C01_patrol_police" },
    };
    static readonly Dictionary<string, (string body, RestStopEnemyBehavior.Role role)> EnemySwap = new()
    {
        { "ConeMechanic", ("C02_road_worker", RestStopEnemyBehavior.Role.ConeLayer) },
        { "TrafficPatrol", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) },
        { "TollgateChief", ("C09_toll_attendant", RestStopEnemyBehavior.Role.Lobber) },
        { "TireBruiser", ("C10_tow_driver", RestStopEnemyBehavior.Role.Lobber) },
        { "AsphaltWorker", ("C05_truck_driver", RestStopEnemyBehavior.Role.Charger) },
        { "DeliveryRider", ("C07_riot_police", RestStopEnemyBehavior.Role.Shield) },
    };

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        if (map.Find(Marker) != null) throw new Exception("Already installed");
        Directory.CreateDirectory(Evidence);
        File.Copy(ScenePath, Evidence + "HighWay.before.unity", true);
        string before = Contract(map); File.WriteAllText(Evidence + "contract-before.txt", before);
        route = map.GetComponentInChildren<HighwayRoute>(true) ?? UnityEngine.Object.FindFirstObjectByType<HighwayRoute>();
        root = new GameObject(Marker).transform; root.SetParent(map, false);
        var report = new Dictionary<string, object>();
        report["hiddenCity"] = HideCity(map);
        report["enemies"] = SwapEnemies(map);
        report["vehicles"] = SwapVehicles(map);
        report["people"] = SwapPeople(map);
        BuildScenery(Group("01_Scenery"));
        report["workZones"] = BuildWorkZones(map, Group("02_WorkZones"));
        report["gimmicks"] = AddGimmicks(Group("03_Gimmicks"));
        report["traffic"] = AddTraffic(Group("04_OpposingTraffic"));
        Physics.SyncTransforms();
        string after = Contract(map); File.WriteAllText(Evidence + "contract-after.txt", after);
        if (before != after) throw new Exception("Gameplay contract changed; not saved");
        foreach (var c in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)))
            if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        var json = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        File.WriteAllText(Evidence + "install-report.json", (string)json.GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new object[] { report }));
        return report;
    }

    // Ridges stood on y≈-1 but the outer ground (CityGround) is at -6.5, so their bases floated. Idempotent.
    public static object Refine1()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var scenery = map.Find(Marker + "/01_Scenery") ?? throw new Exception("Install first");
        int moved = 0;
        foreach (Transform t in scenery)
        {
            if (t.name != "Mountain_Ridge" || t.position.y < -6) continue;
            t.position = new Vector3(t.position.x, -7f, t.position.z); t.localScale = Vector3.one * 1.35f; moved++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { moved };
    }

    // Second vehicle pass: the first pass parented Meshy visuals under non-uniformly scaled instances (skewed,
    // oversized oncoming cars). Rebuild every MeshyVisual as a sibling that follows the same parent, sized from
    // mesh bounds (valid even for inactive oncoming cars). Moving vehicles keep the old footprint length.
    public static object Refine2()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        root = map.Find(Marker) ?? throw new Exception("Install first");
        string before = Contract(map);
        int rebuilt = 0;
        foreach (var t in map.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray())
        {
            if (t == null || t.IsChildOf(root)) continue;
            var key = System.IO.Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) ?? "");
            if (!VehicleSwap.TryGetValue(key, out var id)) continue;
            var oldVisual = t.Find("MeshyVisual"); if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
            // An earlier installer nested a production vehicle inside some HWY vehicles; only the innermost gets a Meshy car.
            bool hasInner = t.GetComponentsInChildren<Transform>(true).Any(c => c != t && PrefabUtility.IsAnyPrefabInstanceRoot(c.gameObject) &&
                VehicleSwap.ContainsKey(System.IO.Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject) ?? "")));
            if (hasInner) continue;
            var outer = t; while (outer.parent != null && PrefabUtility.IsAnyPrefabInstanceRoot(outer.parent.gameObject) &&
                VehicleSwap.ContainsKey(System.IO.Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(outer.parent.gameObject) ?? ""))) outer = outer.parent;
            var host = outer.parent;
            var existing = host.Cast<Transform>().FirstOrDefault(s => s.name == t.name + "_Meshy"); if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var local = MeshBounds(t, t);
            bool across = local.size.x > local.size.z * 1.2f;
            float oldLength = Mathf.Max(local.size.x * Mathf.Abs(t.lossyScale.x), local.size.z * Mathf.Abs(t.lossyScale.z));
            var v = Place(Meshy + id + ".prefab", host, t.position, 0, t.name + "_Meshy");
            v.rotation = t.rotation * Quaternion.Euler(0, across ? 90 : 0, 0);
            v.gameObject.SetActive(outer.gameObject.activeSelf);
            float parentScale = host != null ? Mathf.Abs(host.lossyScale.x) : 1;
            bool moving = t.GetComponentInParent<HighwayHazard>() != null || t.GetComponentInParent<HighwayOncomingTraffic>() != null;
            float natural = Mathf.Max(MeshBounds(v, v).size.x, MeshBounds(v, v).size.z);
            float scale = moving && oldLength > 1f && natural > .1f ? oldLength / natural : 1f;
            v.localScale = Vector3.one * scale / Mathf.Max(.0001f, parentScale);
            rebuilt++;
        }
        if (Contract(map) != before) throw new Exception("Contract changed");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { rebuilt };
    }

    // 2026-09-26 feedback pass (same rules as RestStop Refine4): funnel rails -> oncoming side traffic,
    // water deer instead of side-crossing cars, potholes, log-truck spills, and a "졸음쉼터" pylon at each
    // recovery-branch entrance (the curved exit already exists as the green bypass).
    public static object Refine3()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        root = map.Find(Marker) ?? throw new Exception("Install first");
        if (root.Find("05_Feedback20260926") != null) throw new Exception("Refine3 already applied");
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var font = Resources.Load<TMPro.TMP_FontAsset>("UI/GmarketHarbor SDF");
        var g = Group("05_Feedback20260926");
        var report = new Dictionary<string, object>();

        int rails = 0;
        foreach (var lanes in map.GetComponentsInChildren<HighwayEncounterLanes>(true)) { foreach (var row in lanes.rows) if (row != null && row.barriers != null) { row.barriers.gameObject.SetActive(false); rails++; } lanes.enabled = false; Record(lanes); }
        report["railsHidden"] = rails;

        var trafficRoot = new GameObject("OncomingLaneTraffic").transform; trafficRoot.SetParent(g, false);
        var traffic = trafficRoot.gameObject.AddComponent<OncomingLaneTraffic>();
        traffic.carTemplates = new[] { "V02_white_sedan", "V03_black_suv", "V01_compact_car", "V07_taxi", "V04_1ton_truck", "V08_box_truck", "V11_tanker" }
            .Select(id => { var t = Place(Meshy + id + ".prefab", trafficRoot, Vector3.down * 50, 0, "Template_" + id); t.gameObject.SetActive(false); return t.gameObject; }).ToArray();
        var tow = Place(Meshy + "V09_tow_truck.prefab", trafficRoot, Vector3.down * 50, 0, "Template_Tow"); tow.gameObject.SetActive(false);
        traffic.towTruckTemplate = tow.gameObject;
        // Keep clear of the three toll plazas.
        traffic.activeRanges = new[] { new Vector2(60, 1430), new Vector2(1550, 1710), new Vector2(1830, 1930), new Vector2(2050, 2320) };
        Record(traffic);

        int deer = 0;
        foreach (var hazard in map.GetComponentsInChildren<HighwayHazard>(true).Where(h => h.GetComponent<ObstacleStats>().obstaclePattern == ObstaclePattern.HighwayTraffic && !Path(h.transform).Contains(Marker)))
        {
            foreach (var car in hazard.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("_Meshy")).ToArray()) car.gameObject.SetActive(false);
            var d = Place(Meshy + "A11_water_deer.prefab", hazard.transform, hazard.transform.position, 0, "Water_Deer");
            d.rotation = Quaternion.LookRotation(hazard.transform.right, Vector3.up);
            d.gameObject.AddComponent<DeerHop>(); deer++;
        }
        report["deer"] = deer;

        report["potholes"] = new[] { Pothole(g, font, 335, 3.6f), Pothole(g, font, 1150, -3.6f), Pothole(g, font, 2235, 0f) };
        report["logTrucks"] = new[] { LogTruck(g, 720), LogTruck(g, 1620) };

        int pylons = 0;
        foreach (var fork in route.forks)
        {
            float d = fork.start + 28;
            var p = (route.Point(d, false) + route.Point(d, true)) * .5f;
            route.Sample(d, false, out _, out var f);
            var pylon = Place(Meshy + "P01_pylon_sign.prefab", g, p, Quaternion.LookRotation(f).eulerAngles.y, "Drowsy_Shelter_Pylon");
            pylon.localScale = Vector3.one * .8f;
            var b = Bounds(pylon, false);
            var face = new GameObject("Face").transform; face.SetParent(pylon, true);
            face.SetPositionAndRotation(b.center - f * (b.extents.magnitude * .12f + .1f), Quaternion.LookRotation(f));
            face.position = new Vector3(face.position.x, p.y, face.position.z) - f * .35f;
            Text(face, font, "졸음", new Vector3(0, b.size.y * .72f, 0), 2.2f, new Color(.1f, .12f, .18f), b.size.x * .9f);
            Text(face, font, "쉼터", new Vector3(0, b.size.y * .56f, 0), 2.2f, new Color(.1f, .12f, .18f), b.size.x * .9f);
            Text(face, font, "휴식 후 출발", new Vector3(0, b.size.y * .42f, 0), .7f, new Color(.85f, .1f, .12f), b.size.x * .9f);
            pylons++;
        }
        report["drowsyShelterPylons"] = pylons;

        foreach (var c in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Component>(true)))
            if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return report;
    }

    // Pylon lettering was parented under the scaled pylon and drifted off it; rebuild it as a world-space sibling.
    public static object Refine4()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var font = Resources.Load<TMPro.TMP_FontAsset>("UI/GmarketHarbor SDF");
        int n = 0;
        foreach (var pylon in map.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Drowsy_Shelter_Pylon").ToArray())
        {
            foreach (var old in pylon.Cast<Transform>().Where(t => t.name == "Face").ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var prior = pylon.parent.Find(pylon.name + "_Face"); if (prior != null) UnityEngine.Object.DestroyImmediate(prior.gameObject);
            var b = Bounds(pylon, false);
            var forward = pylon.forward; forward.y = 0; forward.Normalize();
            // The face toward approaching traffic is the pylon's -forward side.
            var face = new GameObject(pylon.name + "_Face").transform; face.SetParent(pylon.parent, false);
            float depth = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(forward.x), 0, Mathf.Abs(forward.z))));
            face.SetPositionAndRotation(new Vector3(b.center.x, b.min.y, b.center.z) - forward * (depth + .08f), Quaternion.LookRotation(forward));
            float w = Mathf.Min(b.size.x, b.size.z) > .5f ? Mathf.Max(b.size.x, b.size.z) * .5f : 2f;
            w = Mathf.Clamp(w, 1.2f, 2.4f);
            Text(face, font, "졸음", new Vector3(0, b.size.y * .74f, 0), 1.3f, new Color(.1f, .12f, .18f), w);
            Text(face, font, "쉼터", new Vector3(0, b.size.y * .6f, 0), 1.3f, new Color(.1f, .12f, .18f), w);
            Text(face, font, "휴식 후 출발", new Vector3(0, b.size.y * .48f, 0), .42f, new Color(.85f, .1f, .12f), w);
            n++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return n;
    }

    // After tools/install-traffic-clearance.cs moved the pylons to the right shoulder: turn each pylon to face
    // approaching traffic and rebuild its lettering as a uniquely named world-space sibling (Refine4 shared one
    // name between both pylons, so one face was lost and the other drifted).
    public static object Refine5()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var font = Resources.Load<TMPro.TMP_FontAsset>("UI/GmarketHarbor SDF");
        foreach (var old in map.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Drowsy_Shelter_Pylon_Face")).ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var report = new List<string>();
        int i = 0;
        foreach (var pylon in map.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Drowsy_Shelter_Pylon").ToArray())
        {
            float d = route.NearestDistance(pylon.position);
            route.Sample(d, false, out _, out var f); f.y = 0; f.Normalize();
            pylon.rotation = Quaternion.LookRotation(f);
            var b = Bounds(pylon, false);
            float depth = Mathf.Abs(b.extents.x * f.x) + Mathf.Abs(b.extents.z * f.z);
            float width = Mathf.Clamp((Mathf.Abs(b.extents.x * f.z) + Mathf.Abs(b.extents.z * f.x)) * 1.7f, 1.2f, 3f);
            var face = new GameObject("Drowsy_Shelter_Pylon_Face_" + i++).transform; face.SetParent(pylon.parent, false);
            face.SetPositionAndRotation(new Vector3(b.center.x, b.min.y, b.center.z) - f * (depth + .08f), Quaternion.LookRotation(f));
            Text(face, font, "졸음", new Vector3(0, b.size.y * .74f, 0), 1.3f, new Color(.1f, .12f, .18f), width);
            Text(face, font, "쉼터", new Vector3(0, b.size.y * .6f, 0), 1.3f, new Color(.1f, .12f, .18f), width);
            Text(face, font, "휴식 후 출발", new Vector3(0, b.size.y * .48f, 0), .42f, new Color(.85f, .1f, .12f), width);
            report.Add($"d={d:0} size={b.size:0.0}");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return report;
    }

    // 2026-09-26 restructure, HighWay side. The highway third of RestStop moves here: the route gains a
    // 480 m straight after its old end, carrying a copy of the late-highway gameplay block (rows, bonuses,
    // tolls, deer from d 1650-1990, mapped through the route frame), a log truck, a pothole and the
    // 한울휴게소 approach (2 km / 1 km / 500 m signs, pylon, exit gore) ending at the rest-stop exit, where
    // RestStop now begins. Also from the test-mode playthrough: tunnel ribs without a tunnel hidden, speed
    // camera poles moved off the shoulder. Scene backup: outputs/restructure-2026-09-26/backup/.
    public static object Refine6()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var font = Resources.Load<TMPro.TMP_FontAsset>("UI/GmarketHarbor SDF");
        var korean = map.GetComponentsInChildren<Transform>(true).First(t => t.name == Marker);
        if (korean.Find("06_Extension20260926") != null) throw new Exception("Refine6 already applied");
        var g = new GameObject("06_Extension20260926").transform; g.SetParent(korean, false);
        var report = new Dictionary<string, object>();

        // 1. Playthrough fixes.
        int ribs = 0;
        foreach (var t in map.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Highway_Tunnel_Rib")).ToArray()) { t.gameObject.SetActive(false); Record(t.gameObject); ribs++; }
        report["tunnelRibsHidden"] = ribs;
        int poles = 0;
        foreach (var t in map.GetComponentsInChildren<Transform>(true).Where(t => t.name == "HighwayProp_53" && t.parent != null && t.parent.name == "Speed camera cluster"))
        {
            float d = route.NearestDistance(t.position); route.Sample(d, false, out var c, out var f); var right = Vector3.Cross(Vector3.up, f);
            float lane = Vector3.Dot(t.position - c, right);
            t.position += right * (Mathf.Sign(lane) * 12f - lane); Record(t); poles++;
        }
        report["speedCameraPolesMoved"] = poles;

        // 2. Route: straight extension along the final heading (centres every 5 m keep old distances).
        const float Spacing = 5f, Step = 20f; const int Copies = 24;
        float oldLength = route.length;
        if (Mathf.Abs(oldLength / (route.centers.Length - 1) - Spacing) > .01f) throw new Exception("Unexpected centre spacing");
        var endPoint = route.centers[route.centers.Length - 1];
        var dir = (endPoint - route.centers[route.centers.Length - 2]).normalized;
        int extra = Mathf.RoundToInt(Step * Copies / Spacing);
        route.centers = route.centers.Concat(Enumerable.Range(1, extra).Select(i => endPoint + dir * Spacing * i)).ToArray();
        route.length = (route.centers.Length - 1) * Spacing; Record(route);
        report["length"] = oldLength + " -> " + route.length;

        // 3. Road: the last 20 m tiles (asphalt, opposing deck, shoulders, dashes) repeat end to end.
        var roads = map.Find("Roads");
        var tiles = roads.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy)
            .Where(r => Vector3.Dot(r.bounds.min - endPoint, dir) >= -Step - .1f && Vector3.Dot(r.bounds.max - endPoint, dir) <= .1f && Vector3.ProjectOnPlane(r.bounds.center - endPoint, dir).magnitude < 32)
            .Select(r => r.gameObject).Distinct().ToList();
        foreach (var tile in tiles)
            for (int k = 1; k <= Copies; k++)
            {
                var copy = UnityEngine.Object.Instantiate(tile, tile.transform.parent); copy.name = tile.name + "_Ext" + k;
                copy.transform.position = tile.transform.position + dir * Step * k;
            }
        report["roadTilesCopied"] = tiles.Count + " x " + Copies;
        var rail = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/R04.prefab");
        var side = Vector3.Cross(Vector3.up, dir);
        for (float s = 3; s < Step * Copies; s += 6)
            foreach (var lane in new[] { 11.3f, -25.3f })
            {
                var r = ((GameObject)PrefabUtility.InstantiatePrefab(rail, g)).transform; r.name = "Ext_Guardrail";
                r.SetPositionAndRotation(endPoint + dir * s + side * lane, Quaternion.LookRotation(dir) * Quaternion.Euler(0, -90, 0));
            }

        // 4. Scenery: discrete roadside pieces from the last 60 m repeat along the extension.
        int scenery = 0;
        var containers = new List<Transform>();
        var reference = map.Find("Reference_Scenery_20260913"); if (reference != null) containers.AddRange(reference.Cast<Transform>());
        containers.AddRange(korean.GetComponentsInChildren<Transform>(true).Where(t => t.parent == korean && t != g && !t.name.Contains("Feedback") && !t.name.Contains("WorkZone")));
        foreach (var container in containers)
            foreach (var unit in container.Cast<Transform>().ToArray())
            {
                if (!unit.gameObject.activeInHierarchy) continue;
                var rs = unit.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float along = Vector3.Dot(b.center - endPoint, dir);
                if (along < -60 || along > 0 || b.size.magnitude > 90 || Vector3.ProjectOnPlane(b.center - endPoint, dir).magnitude > 150) continue;
                if (unit.GetComponentInChildren<HighwayHazard>(true) || unit.GetComponentInChildren<EnemyScript_space>(true)) continue;
                for (int k = 1; k <= 8; k++)
                {
                    var copy = UnityEngine.Object.Instantiate(unit.gameObject, container); copy.name = unit.name + "_Ext" + k;
                    copy.transform.position = unit.position + dir * 60 * k; scenery++;
                }
            }
        foreach (var tree in map.Find("Props").Cast<Transform>().Where(t => t.name.StartsWith("Highway_Tree")).ToArray())
        {
            float along = Vector3.Dot(tree.position - endPoint, dir); if (along < -60 || along > 0) continue;
            for (int k = 1; k <= 8; k++) { var copy = UnityEngine.Object.Instantiate(tree.gameObject, tree.parent); copy.name = tree.name + "_Ext" + k; copy.transform.position = tree.position + dir * 60 * k; scenery++; }
        }
        report["sceneryCopies"] = scenery;

        // 5. Gameplay: the d 1650-1990 block (rows, bonuses, deer, tolls) replayed at d 2390-2730.
        const float From = 1650, To = 1990, Shift = 740;
        var remap = new Dictionary<GameObject, GameObject>();
        Func<Transform, bool> inWindow = t =>
        {
            float d = route.NearestDistance(t.position); if (d < From || d > To) return false;
            route.Sample(d, false, out var c, out var f); return Mathf.Abs(Vector3.Dot(t.position - c, Vector3.Cross(Vector3.up, f))) < 12;
        };
        void Copy(Transform src)
        {
            if (remap.ContainsKey(src.gameObject)) return;
            float d = route.NearestDistance(src.position);
            route.Sample(d, false, out var c0, out var f0); route.Sample(d + Shift, false, out var c1, out var f1);
            var turn = Quaternion.FromToRotation(new Vector3(f0.x, 0, f0.z).normalized, new Vector3(f1.x, 0, f1.z).normalized);
            var copy = UnityEngine.Object.Instantiate(src.gameObject, src.parent); copy.name = src.name + "_Ext";
            copy.transform.SetPositionAndRotation(c1 + turn * (src.position - c0), turn * src.rotation);
            remap[src.gameObject] = copy;
        }
        var rows = map.GetComponentsInChildren<HighwayEncounterRow>(true).Where(r => r.routeDistance >= From && r.routeDistance <= To).ToArray();
        foreach (var row in rows)
        {
            Copy(row.transform); if (row.left != null) Copy(row.left.transform); if (row.right != null) Copy(row.right.transform);
            var newRow = remap[row.gameObject].GetComponent<HighwayEncounterRow>();
            newRow.routeDistance = row.routeDistance + Shift;
            if (row.left != null) newRow.left = remap[row.left.gameObject].GetComponent<EnemyScript_space>();
            if (row.right != null) newRow.right = remap[row.right.gameObject].GetComponent<EnemyScript_space>();
            foreach (var e in new[] { newRow.left, newRow.right }.Where(e => e != null))
                foreach (var m in e.GetComponents<HighwayEncounterMember>()) { m.row = newRow; Record(m); }
            Record(newRow);
        }
        foreach (Transform b in map.Find("Bonuses").Cast<Transform>().ToArray()) if (inWindow(b)) Copy(b);
        foreach (var h in map.Find("Props").Cast<Transform>().ToArray())
        {
            bool gameplay = h.GetComponent<HighwayHazard>() != null || h.name.StartsWith("HWY_TollStation") || h.GetComponent<WaterDeerCrossing>() != null;
            if (gameplay && inWindow(h)) Copy(h);
        }
        report["gameplayCopies"] = remap.Count + " (rows " + rows.Length + ")";
        foreach (var t in map.GetComponentsInChildren<OncomingLaneTraffic>(true))
        {
            var tolls = map.Find("Props").Cast<Transform>().Where(x => x.name.StartsWith("HWY_TollStation")).Select(x => route.NearestDistance(x.position)).Where(d => d > oldLength).ToArray();
            var add = new List<Vector2> { new Vector2(oldLength + 40, route.length - 30) };
            foreach (var d in tolls) add = add.SelectMany(r => d - 14 <= r.x || d + 14 >= r.y ? (d + 14 < r.x || d - 14 > r.y ? new[] { r } : new[] { new Vector2(Mathf.Max(r.x, d + 14), r.y), new Vector2(r.x, Mathf.Min(r.y, d - 14)) }.Where(v => v.y - v.x > 30).ToArray()) : new[] { new Vector2(r.x, d - 14), new Vector2(d + 14, r.y) }).ToList();
            t.activeRanges = t.activeRanges.Concat(add).ToArray(); Record(t);
            report["trafficRanges"] = string.Join(" ", t.activeRanges.Select(r => $"{r.x:0}-{r.y:0}"));
        }
        var feedback = korean.Find("05_Feedback20260926") ?? g;
        LogTruck(feedback, 2600);
        Pothole(feedback, font, 2690, .8f);

        // 6. The rest-stop approach and the exit.
        var green = Mat("Sign_Green", new Color(.05f, .42f, .25f)); var steel = Mat("Sign_Post", new Color(.55f, .57f, .6f));
        foreach (var (d, line1, line2) in new[] { (2420f, "한울휴게소", "2km"), (2620f, "한울휴게소", "1km"), (2745f, "한울휴게소", "500m") })
        {
            route.Sample(d, false, out var c, out var f); var right = Vector3.Cross(Vector3.up, f);
            var sign = new GameObject("Approach_Sign_" + line2).transform; sign.SetParent(g, false);
            sign.SetPositionAndRotation(c + right * 12.5f, Quaternion.LookRotation(f));
            foreach (var x in new[] { -1.8f, 1.8f }) Box(sign, "Post", sign.TransformPoint(new Vector3(x, 2.2f, 0)), new Vector3(.25f, 4.4f, .25f), steel);
            var panel = Box(sign, "Panel", sign.TransformPoint(new Vector3(0, 5.2f, 0)), new Vector3(5.2f, 2.2f, .2f), green); panel.rotation = sign.rotation;
            var face = new GameObject("Face").transform; face.SetParent(sign, false); face.localPosition = new Vector3(0, 0, -.12f);
            Text(face, font, line1, new Vector3(0, 5.65f, 0), .8f, Color.white, 5f);
            Text(face, font, line2 + "  ↗", new Vector3(0, 4.75f, 0), .7f, new Color(1f, .9f, .3f), 5f);
        }
        route.Sample(route.length - 40, false, out var pc, out var pf);
        var pylon = Place(Meshy + "P01_pylon_sign.prefab", g, pc + Vector3.Cross(Vector3.up, pf) * 13f, Quaternion.LookRotation(pf).eulerAngles.y, "RestStop_Pylon");
        pylon.localScale = Vector3.one * .8f;
        var pb = Bounds(pylon, false);
        var pface = new GameObject("RestStop_Pylon_Face").transform; pface.SetParent(g, false);
        pface.SetPositionAndRotation(new Vector3(pb.center.x, pb.min.y, pb.center.z) - pf * (Mathf.Abs(pb.extents.x * pf.x) + Mathf.Abs(pb.extents.z * pf.z) + .08f), Quaternion.LookRotation(pf));
        Text(pface, font, "한울", new Vector3(0, pb.size.y * .74f, 0), 1.3f, new Color(.1f, .12f, .18f), 2.4f);
        Text(pface, font, "휴게소", new Vector3(0, pb.size.y * .6f, 0), 1.1f, new Color(.1f, .12f, .18f), 2.4f);
        Text(pface, font, "주유 · 식당 · 편의점", new Vector3(0, pb.size.y * .48f, 0), .36f, new Color(.85f, .1f, .12f), 2.4f);
        var white = Mat("Gore_White", new Color(.95f, .95f, .92f));
        for (int i = 0; i < 9; i++)
        {
            route.Sample(route.length - 60 + i * 5, false, out var gc, out var gf); var gr = Vector3.Cross(Vector3.up, gf);
            var stripe = Box(g, "Exit_Gore_Stripe", gc + gr * 8.6f + Vector3.up * .04f, new Vector3(.35f, .02f, 3.2f), white);
            stripe.rotation = Quaternion.LookRotation(gf) * Quaternion.Euler(0, 40, 0);
        }
        var exit = map.Find("Highway_StageExit");
        route.Sample(route.length - 10, false, out var xc, out var xf);
        exit.SetPositionAndRotation(xc, Quaternion.LookRotation(xf)); Record(exit);
        report["stageExit"] = route.length - 10;

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return report;
    }

    // Playthrough of the extension: the ground plane (CityGround, x <= 800) and the road stopped at the finish,
    // so the last seconds showed sky under the road. Ground continues, the carriageway runs 200 m past the stage
    // exit, the rest-stop exit ramp peels off to the right before it, and a ridge closes the view.
    public static object Refine7()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var ext = map.GetComponentsInChildren<Transform>(true).First(t => t.name == "06_Extension20260926");
        if (ext.Find("Ground_East") != null) throw new Exception("Refine7 already applied");
        var ground = map.GetComponentsInChildren<Transform>(true).First(t => t.name == "CityGround");
        var east = UnityEngine.Object.Instantiate(ground.gameObject, ext); east.name = "Ground_East";
        east.transform.SetPositionAndRotation(ground.position + Vector3.right * 1590, ground.rotation); east.transform.localScale = ground.lossyScale;
        var endPoint = route.centers[route.centers.Length - 1];
        var dir = (endPoint - route.centers[route.centers.Length - 2]).normalized; var side = Vector3.Cross(Vector3.up, dir);
        var roads = map.Find("Roads");
        var tiles = roads.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy)
            .Where(r => Vector3.Dot(r.bounds.min - endPoint, dir) >= -20.1f && Vector3.Dot(r.bounds.max - endPoint, dir) <= .1f && Vector3.ProjectOnPlane(r.bounds.center - endPoint, dir).magnitude < 32)
            .Select(r => r.gameObject).Distinct().ToList();
        foreach (var tile in tiles)
            for (int k = 1; k <= 10; k++) { var c = UnityEngine.Object.Instantiate(tile, tile.transform.parent); c.name = tile.name + "_Tail" + k; c.transform.position = tile.transform.position + dir * 20 * k; }
        var rail = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/R04.prefab");
        for (float s = 3; s < 200; s += 6)
            foreach (var lane in new[] { 11.3f, -25.3f })
            {
                var r = ((GameObject)PrefabUtility.InstantiatePrefab(rail, ext)).transform; r.name = "Tail_Guardrail";
                r.SetPositionAndRotation(endPoint + dir * s + side * lane, Quaternion.LookRotation(dir) * Quaternion.Euler(0, -90, 0));
            }
        // Exit ramp: one 7 m lane diverging right at 12 degrees from 70 m before the finish.
        var main = roads.Cast<Transform>().First(t => t.name.StartsWith("Main_")).gameObject;
        var rampDir = Quaternion.Euler(0, 12, 0) * dir;
        var rampStart = endPoint - dir * 70 + side * 10.5f;
        for (int i = 0; i < 9; i++)
        {
            var tile = UnityEngine.Object.Instantiate(main, ext); tile.name = "Exit_Ramp_" + i;
            tile.transform.SetPositionAndRotation(rampStart + rampDir * (10 + i * 20), Quaternion.LookRotation(rampDir) * Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward)) * main.transform.rotation);
            var ls = main.transform.localScale; tile.transform.localScale = new Vector3(ls.x * .5f, ls.y, ls.z);
            foreach (var c in tile.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
        var meshy = Meshy + "P09_mountain_backdrop.prefab";
        Place(meshy, ext, endPoint + dir * 420 + side * 60, Quaternion.LookRotation(-dir).eulerAngles.y + 90, "Tail_Ridge");
        Place(meshy, ext, endPoint + dir * 380 - side * 120, Quaternion.LookRotation(-dir).eulerAngles.y + 90, "Tail_Ridge");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { tiles = tiles.Count, ramp = 9 };
    }

    // 2026-09-26 user feedback on the extended HighWay:
    //  1. grey squares in the lanes were viaduct pier tops flush with (z-fighting through) the road -> piers sink 0.45,
    //  2/4. two decorative systems drove the same opposing lanes and overlapped -> HighwayAmbientTraffic off,
    //       AmbientTrafficPath only, driving north as the user specified; car visuals grounded on their root,
    //  3. the recovery bypass branched left across the centre line and the opposing carriageway -> it branches
    //     right (like a Korean rest-area exit); roads, guide lines, signs and the recovery marker are rebuilt and
    //     right-side scenery in its corridor is hidden,
    //  5. too easy -> denser side traffic, more deer, two more log trucks, extra enemy rows in long gaps.
    public static object Refine8()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var font = Resources.Load<TMPro.TMP_FontAsset>("UI/GmarketHarbor SDF");
        var korean = map.GetComponentsInChildren<Transform>(true).First(t => t.name == Marker);
        if (korean.Find("07_Difficulty20260926") != null) throw new Exception("Refine8 already applied");
        var g = new GameObject("07_Difficulty20260926").transform; g.SetParent(korean, false);
        var report = new Dictionary<string, object>();
        var props = map.Find("Props"); var roads = map.Find("Roads");

        // 1. Pier tops.
        int piers = 0;
        foreach (var t in props.Cast<Transform>().Where(t => t.name.StartsWith("Highway_Polish_Support"))) { t.position += Vector3.down * .45f; Record(t); piers++; }
        report["piersLowered"] = piers;

        // 2/4. Opposing carriageway traffic.
        foreach (var a in map.GetComponentsInChildren<HighwayAmbientTraffic>(true)) { a.enabled = false; Record(a); foreach (var c in a.cars) if (c != null) { c.gameObject.SetActive(false); Record(c.gameObject); } }
        foreach (var a in map.GetComponentsInChildren<AmbientTrafficPath>(true)) { a.speed = Mathf.Abs(a.speed); Record(a); }
        int grounded = 0;
        foreach (var node in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name.EndsWith("_Meshy")).ToArray())
        {
            var root = node.parent; if (root == null) continue;
            var rs = node.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float drop = b.min.y - root.position.y;
            if (Mathf.Abs(drop) > .03f) { node.position -= Vector3.up * drop; Record(node); grounded++; }
        }
        report["vehiclesGrounded"] = grounded;

        // 3. Bypass to the right.
        var builder = typeof(RoadChapterPatternBuilder);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        var oldBypass = roads.Cast<Transform>().Where(t => t.name.StartsWith("Bypass_")).ToArray();
        var greenLine = roads.Cast<Transform>().First(t => t.name == "회복 우회로 초록 유도선").GetComponent<LineRenderer>();
        var pinkLine = roads.Cast<Transform>().First(t => t.name == "본선 분홍 유도선").GetComponent<LineRenderer>();
        var greenMat = greenLine.sharedMaterial; var pinkMat = pinkLine.sharedMaterial;
        builder.GetField("asphalt", flags).SetValue(null, oldBypass[0].GetComponent<Renderer>().sharedMaterial);
        builder.GetField("paint", flags).SetValue(null, AssetDatabase.LoadAssetAtPath<Material>(RoadChapterPatternBuilder.Art + "/WarmWhite.mat") ?? throw new Exception("WarmWhite missing"));
        builder.GetField("green", flags).SetValue(null, greenLine.sharedMaterial);
        builder.GetField("pink", flags).SetValue(null, pinkLine.sharedMaterial);
        foreach (var t in oldBypass) UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach (var t in roads.Cast<Transform>().Where(t => t.name == "회복 우회로 초록 유도선" || t.name == "본선 분홍 유도선").ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach (var fork in route.forks) fork.offset = Mathf.Abs(fork.offset);
        Record(route);
        var roadMethod = builder.GetMethod("Road", flags); var stripeMethod = builder.GetMethod("Stripe", flags);
        int serial = 900;
        foreach (var fork in route.forks)
        {
            for (float d = fork.start; d < fork.end; d += 40) roadMethod.Invoke(null, new object[] { roads, route, d, Mathf.Min(fork.end, d + 40), true, "Bypass_" + serial++ });
            stripeMethod.Invoke(null, new object[] { roads, route, fork.start - 50, fork.end, -2.8f, false, pinkMat, "본선 분홍 유도선", .45f });
            stripeMethod.Invoke(null, new object[] { roads, route, fork.start - 50, fork.end, 2.8f, true, greenMat, "회복 우회로 초록 유도선", .5f });
        }
        foreach (var tmp in map.GetComponentsInChildren<TMPro.TMP_Text>(true))
            if (tmp.text.Contains("왼쪽 우회로")) { tmp.text = "오른쪽 우회로     왼쪽 본선"; Record(tmp); }
        foreach (var marker in props.Cast<Transform>().Where(t => t.name == "Recovery_Stop").ToArray())
        {
            var fork = route.forks.OrderBy(f => Mathf.Abs((f.start + f.end) * .5f - route.NearestDistance(marker.position))).First();
            route.Sample((fork.start + fork.end) * .5f, true, out var p, out var f2);
            marker.SetPositionAndRotation(p, Quaternion.LookRotation(f2));
            foreach (Transform c in marker) { var lp = c.localPosition; c.localPosition = new Vector3(-lp.x, lp.y, lp.z); }
            Record(marker);
        }
        int cleared = 0;
        var containers = new List<Transform> { props };
        var refScenery = map.Find("Reference_Scenery_20260913"); if (refScenery != null) containers.AddRange(refScenery.Cast<Transform>());
        containers.AddRange(korean.Cast<Transform>().Where(t => t != g));
        foreach (var container in containers)
            foreach (var unit in container.Cast<Transform>().ToArray())
            {
                if (!unit.gameObject.activeInHierarchy || unit.name == "Recovery_Stop" || unit.name == "CityGround") continue;
                if (unit.GetComponentInChildren<EnemyScript_space>(true) || unit.GetComponentInChildren<HighwayHazard>(true) || unit.GetComponentInChildren<LogTruckSpill>(true) || unit.GetComponentInChildren<RoadPotholeHazard>(true) || unit.GetComponentInChildren<OncomingLaneTraffic>(true) || unit.GetComponentInChildren<AmbientTrafficPath>(true)) continue;
                var rs = unit.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.x > 60 || b.size.z > 60 || b.max.y < -1) continue;
                float d = route.NearestDistance(b.center);
                var fork = route.forks.FirstOrDefault(f => d > f.start - 5 && d < f.end + 5); if (fork == null) continue;
                route.Sample(d, false, out var c, out var fw); var right = Vector3.Cross(Vector3.up, fw);
                float lane = Vector3.Dot(b.center - c, right), half = Mathf.Abs(b.extents.x * right.x) + Mathf.Abs(b.extents.z * right.z);
                float branch = HighwayRoute.BranchOffset(d, fork.start, fork.end, fork.offset);
                if (lane < 6 || Mathf.Abs(lane - branch) > 7.5f + half) continue;
                unit.gameObject.SetActive(false); Record(unit.gameObject); cleared++;
            }
        report["bypassCorridorCleared"] = cleared;

        // 5. Difficulty.
        foreach (var t in map.GetComponentsInChildren<OncomingLaneTraffic>(true)) { t.minInterval = 2.3f; t.maxInterval = 3.9f; t.towTruckChance = .2f; Record(t); }
        var busy = new List<(float from, float to)>();
        foreach (var f in route.forks) busy.Add((f.start - 30, f.end + 30));
        foreach (var t in props.Cast<Transform>().Where(t => t.name.StartsWith("HWY_TollStation"))) { float d = route.NearestDistance(t.position); busy.Add((d - 30, d + 30)); }
        foreach (var l in map.GetComponentsInChildren<LogTruckSpill>(true)) busy.Add((l.triggerDistance - 10, l.triggerDistance + 130));
        foreach (var h in map.GetComponentsInChildren<RoadPotholeHazard>(true)) { float d = route.NearestDistance(h.transform.position); busy.Add((d - 20, d + 20)); }
        foreach (var w in map.GetComponentsInChildren<WaterDeerCrossing>(true)) { float d = route.NearestDistance(w.transform.position); busy.Add((d - 25, d + 25)); }
        bool Free(float d, float margin) => d > 40 && d < route.length - 60 && !busy.Any(r => d > r.from - margin && d < r.to + margin);

        GameObject CopyAt(Transform src, float targetD)
        {
            float d = route.NearestDistance(src.position);
            route.Sample(d, false, out var c0, out var f0); route.Sample(targetD, false, out var c1, out var f1);
            var turn = Quaternion.FromToRotation(new Vector3(f0.x, 0, f0.z).normalized, new Vector3(f1.x, 0, f1.z).normalized);
            var copy = UnityEngine.Object.Instantiate(src.gameObject, src.parent);
            copy.transform.SetPositionAndRotation(c1 + turn * (src.position - c0), turn * src.rotation);
            return copy;
        }
        var deerSource = map.GetComponentsInChildren<WaterDeerCrossing>(true).First().transform;
        var deerAdded = new List<float>();
        foreach (var d in new[] { 430f, 650f, 1010f, 1260f, 2270f, 2560f, 2765f })
        {
            if (!Free(d, 0)) continue;
            var copy = CopyAt(deerSource, d); copy.name = deerSource.name + "_More" + (int)d; deerAdded.Add(d); busy.Add((d - 25, d + 25));
        }
        report["deerAdded"] = deerAdded;
        var logsAdded = new List<float>();
        var feedback = korean.Find("05_Feedback20260926") ?? g;
        foreach (var d in new[] { 1330f, 2210f })
            if (Free(d, 0) && Free(d + 120, 0)) { LogTruck(feedback, d); logsAdded.Add(d); busy.Add((d - 10, d + 130)); }
        report["logTrucksAdded"] = logsAdded;

        var rows = map.GetComponentsInChildren<HighwayEncounterRow>(true).OrderBy(r => r.routeDistance).ToList();
        int added = 0; var rowsAt = new List<float>();
        for (int i = 0; i + 1 < rows.Count; i++)
        {
            float a = rows[i].routeDistance, b2 = rows[i + 1].routeDistance, mid = (a + b2) * .5f;
            if (b2 - a < 70 || !Free(mid, 12)) continue;
            var src = rows[i];
            var newRow = CopyAt(src.transform, mid).GetComponent<HighwayEncounterRow>(); newRow.name = src.name + "_More";
            newRow.routeDistance = mid;
            if (src.left != null) { var e = CopyAt(src.left.transform, mid + (route.NearestDistance(src.left.transform.position) - a)); e.name = src.left.name + "_More"; newRow.left = e.GetComponent<EnemyScript_space>(); }
            if (src.right != null) { var e = CopyAt(src.right.transform, mid + (route.NearestDistance(src.right.transform.position) - a)); e.name = src.right.name + "_More"; newRow.right = e.GetComponent<EnemyScript_space>(); }
            foreach (var e in new[] { newRow.left, newRow.right }.Where(e => e != null))
                foreach (var m in e.GetComponents<HighwayEncounterMember>()) { m.row = newRow; Record(m); }
            Record(newRow); added++; rowsAt.Add(mid);
        }
        report["enemyRowsAdded"] = rowsAt.Select(x => (int)x).ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return report;
    }

    // Playthrough of Refine8: the bypass tiles' edge lines crossed the main lanes diagonally where the branch
    // still overlaps the carriageway. Edge points inside the main road (|lane| < 7.3) are dropped, keeping the
    // longest outside run. The tail past the stage exit also gets its lane dashes.
    public static object Refine9()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var roads = map.Find("Roads");
        int clipped = 0, removed = 0;
        foreach (var bypass in roads.Cast<Transform>().Where(t => t.name.StartsWith("Bypass_")))
            foreach (var line in bypass.GetComponentsInChildren<LineRenderer>(true))
            {
                var pts = new Vector3[line.positionCount]; line.GetPositions(pts);
                var outside = pts.Select(p => { float d = route.NearestDistance(p); route.Sample(d, false, out var c, out var f); return Mathf.Abs(Vector3.Dot(p - c, Vector3.Cross(Vector3.up, f))) > 7.3f; }).ToArray();
                int bestStart = 0, bestLen = 0;
                for (int i = 0; i < pts.Length;) { if (!outside[i]) { i++; continue; } int j = i; while (j < pts.Length && outside[j]) j++; if (j - i > bestLen) { bestLen = j - i; bestStart = i; } i = j; }
                if (bestLen == pts.Length) continue;
                if (bestLen < 2) { UnityEngine.Object.DestroyImmediate(line.gameObject); removed++; continue; }
                line.positionCount = bestLen; line.SetPositions(pts.Skip(bestStart).Take(bestLen).ToArray()); Record(line); clipped++;
            }
        var endPoint = route.centers[route.centers.Length - 1];
        var dir = (endPoint - route.centers[route.centers.Length - 2]).normalized;
        var marks = roads.Find("Four_Lane_Markings");
        int dashes = 0;
        foreach (var r in marks.GetComponentsInChildren<Renderer>(true).Where(r => Vector3.Dot(r.bounds.center - endPoint, dir) is var a && a > -40 && a <= 0).Select(r => r.gameObject).ToArray())
            for (int k = 1; k <= 5; k++) { var c = UnityEngine.Object.Instantiate(r, r.transform.parent); c.name = r.name + "_Tail" + k; c.transform.position = r.transform.position + dir * 40 * k; dashes++; }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { clipped, removed, dashes };
    }

    // Refine6/7 copied road tiles by moving their transforms, but lane dashes and edge lines are world-space
    // LineRenderers, so every copy stayed on top of its source. Copies (names ending _ExtN / _TailN) whose points
    // still coincide with a source line are shifted by the copy offset (20 m per step along the final heading).
    public static object Refine10()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var roads = map.Find("Roads");
        var dir = (route.centers[route.centers.Length - 1] - route.centers[route.centers.Length - 2]).normalized;
        var lines = roads.GetComponentsInChildren<LineRenderer>(true).Where(l => l.useWorldSpace).ToArray();
        var sourceFirst = new HashSet<Vector3Int>();
        Vector3Int Key(Vector3 p) => Vector3Int.RoundToInt(p * 100);
        foreach (var l in lines) if (!System.Text.RegularExpressions.Regex.IsMatch(Path(l.transform), @"_(Ext|Tail)\d+(/|$)") && l.positionCount > 0) sourceFirst.Add(Key(l.GetPosition(0)));
        int shifted = 0;
        foreach (var l in lines)
        {
            var m = System.Text.RegularExpressions.Regex.Match(Path(l.transform), @"_(Ext|Tail)(\d+)(/|$)");
            if (!m.Success || l.positionCount == 0 || !sourceFirst.Contains(Key(l.GetPosition(0)))) continue;
            int k = int.Parse(m.Groups[2].Value);
            var offset = dir * 20f * k;
            var pts = new Vector3[l.positionCount]; l.GetPositions(pts);
            l.SetPositions(pts.Select(p => p + offset).ToArray()); Record(l); shifted++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { worldLines = lines.Length, shifted };
    }

    // The copied 20 m centre-line pieces on the extension left gaps: one continuous double yellow line (lane -7,
    // the median between carriageways) runs from the old route end past the stage exit.
    public static object Refine11()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        route = UnityEngine.Object.FindFirstObjectByType<HighwayRoute>(FindObjectsInactive.Include);
        var ext = map.GetComponentsInChildren<Transform>(true).First(t => t.name == "06_Extension20260926");
        var old = ext.Find("Ext_Centre_Line"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var yellow = map.Find("Roads").GetComponentsInChildren<Renderer>(true).Select(r => r.sharedMaterial).First(m => m != null && m.name.Contains("Yellow"));
        var holder = new GameObject("Ext_Centre_Line").transform; holder.SetParent(ext, false);
        var endPoint = route.centers[route.centers.Length - 1];
        var dir = (endPoint - route.centers[route.centers.Length - 2]).normalized; var side = Vector3.Cross(Vector3.up, dir);
        var start = endPoint - dir * 485; var finish = endPoint + dir * 200;
        foreach (var lane in new[] { -6.85f, -7.15f })
        {
            var line = new GameObject("Yellow").AddComponent<LineRenderer>(); line.transform.SetParent(holder, false);
            line.useWorldSpace = true; line.alignment = LineAlignment.TransformZ; line.transform.rotation = Quaternion.Euler(90, 0, 0);
            line.widthMultiplier = .16f; line.sharedMaterial = yellow; line.positionCount = 2;
            line.SetPositions(new[] { start + side * lane + Vector3.up * .03f, finish + side * lane + Vector3.up * .03f });
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return yellow.name;
    }

    static void Text(Transform parent, TMPro.TMP_FontAsset font, string text, Vector3 local, float size, Color color, float width)
    {
        var go = new GameObject("Label " + text); go.transform.SetParent(parent, false); go.transform.localPosition = local; go.transform.localRotation = Quaternion.identity;
        var tmp = go.AddComponent<TMPro.TextMeshPro>(); tmp.font = font; tmp.text = text; tmp.fontSize = size * 10; tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center; tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap; tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.rectTransform.sizeDelta = new Vector2(width, size * 2.4f);
    }

    static string Pothole(Transform parent, TMPro.TMP_FontAsset font, float d, float lane)
    {
        route.Sample(d, false, out var c, out var f); var right = Vector3.Cross(Vector3.up, f);
        var pothole = new GameObject("Road_Pothole").transform; pothole.SetParent(parent, false);
        pothole.SetPositionAndRotation(c + right * lane, Quaternion.LookRotation(f));
        Place(Meshy + "P16_pothole.prefab", pothole, pothole.position + Vector3.down * .12f, pothole.eulerAngles.y, "Pothole");
        var trigger = pothole.gameObject.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(2.4f, 2f, 2.4f); trigger.center = Vector3.up;
        pothole.gameObject.AddComponent<RoadPotholeHazard>();
        foreach (var o in new[] { new Vector3(-1.9f, 0, .6f), new Vector3(1.9f, 0, .6f), new Vector3(-1.2f, 0, 2.2f), new Vector3(1.2f, 0, 2.2f), new Vector3(0, 0, 2.6f) })
            Place(Cone, pothole, pothole.TransformPoint(o), 0, "Cone").localScale = Vector3.one * 1.6f;
        route.Sample(d - 22, false, out var sc, out var sf);
        var signRoot = new GameObject("Construction_Sign").transform; signRoot.SetParent(pothole, true);
        signRoot.SetPositionAndRotation(sc + Vector3.Cross(Vector3.up, sf) * (lane + (lane >= 0 ? 2.2f : -2.2f)) + Vector3.up * .9f, Quaternion.LookRotation(sf));
        var board = Box(signRoot, "Panel", signRoot.position, new Vector3(2.8f, 1.2f, .12f), Mat("Sign_Orange", new Color(1f, .55f, .1f))); board.rotation = signRoot.rotation;
        var face = new GameObject("Face").transform; face.SetParent(signRoot, false); face.localPosition = new Vector3(0, 0, -.08f);
        Text(face, font, "공사중", new Vector3(0, .15f, 0), .55f, Color.black, 2.6f);
        Text(face, font, "도로 파임", new Vector3(0, -.3f, 0), .35f, Color.black, 2.6f);
        return "d=" + d + " lane=" + lane;
    }

    static string LogTruck(Transform parent, float d)
    {
        var holder = new GameObject("Log_Truck_Spill_" + d).transform; holder.SetParent(parent, false);
        var spill = holder.gameObject.AddComponent<LogTruckSpill>(); spill.triggerDistance = d;
        var truck = Place(Meshy + "V12_log_truck.prefab", holder, Vector3.down * 50, 0, "Template_Truck"); truck.gameObject.SetActive(false);
        var log = Place(Meshy + "P15_log.prefab", holder, Vector3.down * 50, 0, "Template_Log"); log.gameObject.SetActive(false);
        spill.truckTemplate = truck.gameObject; spill.logTemplate = log.gameObject;
        var warn = new List<Transform>();
        for (int i = 0; i < 2; i++) { var w = Box(holder, "Log_Lane_Warning", Vector3.zero, Vector3.one, Mat("Warn", new Color(1f, .22f, .1f, .55f), true)); w.gameObject.SetActive(false); warn.Add(w); }
        spill.laneWarnings = warn.ToArray(); Record(spill);
        return "d=" + d;
    }

    static Bounds MeshBounds(Transform root, Transform space)
    {
        bool first = true; var b = new Bounds();
        void Add(Mesh m, Transform t)
        {
            if (m == null) return; var mb = m.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = space.InverseTransformPoint(t.TransformPoint(corner));
                if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
            }
        }
        foreach (var f in root.GetComponentsInChildren<MeshFilter>(true)) Add(f.sharedMesh, f.transform);
        foreach (var s in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) Add(s.sharedMesh, s.transform);
        return first ? new Bounds(Vector3.zero, Vector3.one) : b;
    }

    static string Contract(Transform map)
    {
        var lines = new List<string>();
        foreach (var e in map.GetComponentsInChildren<EnemyScript_space>(true))
        {
            var col = e.GetComponent<Collider>();
            lines.Add($"E {Path(e.transform)} {e.transform.position:F2} {e.gameObject.activeSelf} {e.HasConfiguredProjectile} {(col ? col.enabled + ":" + col.bounds.size.ToString("F2") : "none")}");
        }
        foreach (var h in map.GetComponentsInChildren<HighwayHazard>(true).Where(h => !Path(h.transform).Contains(Marker)))
        {
            var col = h.GetComponent<Collider>();
            lines.Add($"H {Path(h.transform)} {h.transform.position:F2} {h.gameObject.activeInHierarchy} {(col ? col.enabled + ":" + col.bounds.size.ToString("F2") : "none")}");
        }
        foreach (var b in map.GetComponentsInChildren<BonusWallChoicePair>(true)) lines.Add($"B {Path(b.transform)} {b.transform.position:F2}");
        foreach (Transform t in map.Find("Roads")) lines.Add($"R {t.name} {t.position:F2} {(t.GetComponent<Collider>() ? t.GetComponent<Collider>().enabled : false)}");
        return string.Join("\n", lines);
    }

    static int HideCity(Transform map)
    {
        int n = 0;
        foreach (Transform t in map.Find("Props"))
        {
            var src = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) ?? "";
            if ((src.Contains("skyscraper") || src.Contains("business_center")) && t.gameObject.activeSelf) { t.gameObject.SetActive(false); n++; }
        }
        foreach (Transform t in map.Find("Reference_Scenery_20260913"))
            if ((t.name.StartsWith("City backdrop") || t.name.StartsWith("City horizon")) && t.gameObject.activeSelf) { t.gameObject.SetActive(false); n++; }
        return n;
    }

    static object SwapEnemies(Transform map)
    {
        var template = BuildConeTemplate(map);
        var rows = new List<string>();
        foreach (var e in map.GetComponentsInChildren<EnemyScript_space>(true).ToArray())
        {
            var body = e.transform.Find("Body");
            string role = body != null ? System.IO.Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(body.gameObject) ?? "") : "";
            role = EnemySwap.Keys.FirstOrDefault(k => role.Contains(k) || e.name.Contains(k));
            if (role == null) { rows.Add(e.name + " kept (unknown role)"); continue; }
            var (id, behaviorRole) = EnemySwap[role];
            SwapBody(e.gameObject, id);
            var behavior = e.GetComponent<RestStopEnemyBehavior>() ?? e.gameObject.AddComponent<RestStopEnemyBehavior>();
            behavior.role = behaviorRole; if (behaviorRole == RestStopEnemyBehavior.Role.ConeLayer) behavior.coneTemplate = template;
            Record(behavior); rows.Add(e.name + " " + role + " -> " + id);
        }
        return rows;
    }

    static GameObject BuildConeTemplate(Transform map)
    {
        var source = map.GetComponentsInChildren<HighwayHazard>(true).First(h => h.GetComponent<ObstacleStats>().obstaclePattern == ObstaclePattern.HighwayRoadblock).gameObject;
        var copy = UnityEngine.Object.Instantiate(source, root); copy.name = "ConeLayer_RoadblockTemplate";
        foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        var hz = copy.GetComponent<HighwayHazard>(); hz.breakHealth = 40;
        var visual = new GameObject("Cones").transform; visual.SetParent(copy.transform, false);
        for (int i = -1; i <= 1; i++) Place(Cone, visual, copy.transform.position + copy.transform.right * i * .7f, 0, "Cone").localScale = Vector3.one * 1.6f;
        hz.visual = visual; copy.SetActive(false);
        return copy;
    }

    // Same body swap as the rest-stop installer (nested prefab unpack, carried hand props, Meshy bone names).
    static void SwapBody(GameObject root, string id)
    {
        var combat = root.GetComponent<EnemyScript_space>(); bool ranged = combat.HasConfiguredProjectile;
        if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(root), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var nested in root.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray())
            if (nested != null && PrefabUtility.IsOutermostPrefabInstanceRoot(nested.gameObject)) PrefabUtility.UnpackPrefabInstance(nested.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var animators = root.GetComponentsInChildren<Animator>(true); var carried = new List<Transform>();
        var so = new SerializedObject(combat);
        var held = so.FindProperty("heldProjectile").objectReferenceValue as Transform;
        var throwPoint = so.FindProperty("throwPoint").objectReferenceValue as Transform;
        foreach (var animator in animators)
        {
            var oldBones = new HashSet<Transform>(animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s => s.bones));
            foreach (var hand in animator.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Hand.R" || t.name == "RightHand"))
                foreach (var child in hand.Cast<Transform>().Where(t => !oldBones.Contains(t)).ToArray()) { child.SetParent(root.transform, true); carried.Add(child); }
        }
        foreach (var t in new[] { held, throwPoint })
            if (t != null && animators.Any(a => t.IsChildOf(a.transform)) && !carried.Contains(t)) { t.SetParent(root.transform, true); carried.Add(t); }
        var oldBody = root.transform.Find("Body");
        Quaternion rot = oldBody != null ? oldBody.localRotation : Quaternion.identity; Vector3 pos = oldBody != null ? oldBody.localPosition : Vector3.down * .1f;
        foreach (var animator in animators)
        {
            if (animator == null || carried.Any(c => c != null && animator.transform.IsChildOf(c))) continue;
            if (animator.transform == root.transform) throw new Exception("Root animator " + root.name);
            var top = animator.transform; while (top.parent != root.transform) top = top.parent;
            UnityEngine.Object.DestroyImmediate(top.name == "Body" ? top.gameObject : animator.gameObject);
        }
        foreach (var hcp in root.GetComponentsInChildren<HighwayCharacterProportions>(true)) UnityEngine.Object.DestroyImmediate(hcp);
        if (carried.Any(c => c == null)) throw new Exception("Hand prop destroyed on " + root.name);
        var body = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Meshy + id + ".prefab"), root.transform)).transform;
        body.name = "Body"; body.localPosition = pos; body.localRotation = rot;
        var anim = body.GetComponentInChildren<Animator>(); anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var right = body.GetComponentsInChildren<Transform>(true).Single(t => t.name == "RightHand"); var palm = Palm(anim, right);
        foreach (var carry in carried)
        {
            carry.SetParent(right, true); carry.localPosition = palm; carry.rotation = root.transform.rotation;
            var s = right.lossyScale; carry.localScale = new Vector3(1 / Mathf.Abs(s.x), 1 / Mathf.Abs(s.y), 1 / Mathf.Abs(s.z));
        }
        if (id == "C07_riot_police")
        {
            var left = body.GetComponentsInChildren<Transform>(true).Single(t => t.name == "LeftHand");
            var shield = Place("Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/P01.prefab", left, left.position, 0, "Riot_Shield");
            shield.localPosition = Palm(anim, left); shield.rotation = root.transform.rotation;
            var s = left.lossyScale; shield.localScale = new Vector3(1 / Mathf.Abs(s.x), 1 / Mathf.Abs(s.y), 1 / Mathf.Abs(s.z)); shield.position -= root.transform.up * .6f;
        }
        var reaction = root.GetComponent<HighwayEnemyAnimation>();
        if (reaction != null)
        {
            reaction.animator = anim;
            reaction.head = body.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Head");
            reaction.chest = body.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Spine");
            var die = anim.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == "die"); if (die != null) reaction.deathSeconds = Mathf.Max(reaction.deathSeconds, die.length);
            Record(reaction);
        }
        int bodyAnimators = root.GetComponentsInChildren<Animator>(true).Count(a => !carried.Any(c => c != null && a.transform.IsChildOf(c)));
        if (bodyAnimators != 1 || combat.HasConfiguredProjectile != ranged) throw new Exception("Actor contract " + root.name);
    }

    static Vector3 Palm(Animator animator, Transform hand)
    {
        var skin = animator.GetComponentInChildren<SkinnedMeshRenderer>(true); int index = Array.IndexOf(skin.bones, hand);
        var mesh = skin.sharedMesh; var verts = mesh.vertices; var weights = mesh.boneWeights; var p = Vector3.zero; int count = 0;
        for (int i = 0; i < verts.Length; i++)
        {
            var w = weights[i];
            float weight = (w.boneIndex0 == index ? w.weight0 : 0) + (w.boneIndex1 == index ? w.weight1 : 0) + (w.boneIndex2 == index ? w.weight2 : 0) + (w.boneIndex3 == index ? w.weight3 : 0);
            if (weight > .6f) { p += mesh.bindposes[index].MultiplyPoint3x4(verts[i]); count++; }
        }
        return count == 0 ? Vector3.zero : p / count;
    }

    // Replace a vehicle instance's visuals in place; moving/hazard vehicles keep the old footprint.
    static object SwapVehicles(Transform map)
    {
        int swapped = 0;
        foreach (var t in map.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray())
        {
            if (t == null || t.IsChildOf(root) || t.Find("MeshyVisual") != null) continue;
            var key = System.IO.Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) ?? "");
            if (!VehicleSwap.TryGetValue(key, out var id)) continue;
            var old = Bounds(t, true);
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; Record(r); }
            var v = Place(Meshy + id + ".prefab", t, t.position, 0, "MeshyVisual");
            v.rotation = t.rotation * Quaternion.Euler(0, old.size.x > old.size.z * 1.2f ? 90 : 0, 0);
            var s = t.lossyScale; v.localScale = new Vector3(1 / Mathf.Abs(s.x), 1 / Mathf.Abs(s.y), 1 / Mathf.Abs(s.z));
            bool moving = t.GetComponentInParent<HighwayHazard>() != null || t.GetComponentInParent<HighwayOncomingTraffic>() != null;
            if (moving)
            {
                float oldLength = Mathf.Max(old.size.x, old.size.z), newLength = Mathf.Max(Bounds(v, false).size.x, Bounds(v, false).size.z);
                if (newLength > .1f && oldLength > 1f) v.localScale *= oldLength / newLength; // inactive renderers report empty bounds
            }
            swapped++;
        }
        return swapped;
    }

    static object SwapPeople(Transform map)
    {
        int swapped = 0;
        foreach (var t in map.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray())
        {
            if (t == null || t.IsChildOf(root) || t.GetComponentInParent<EnemyScript_space>() != null) continue;
            var key = System.IO.Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) ?? "");
            if (!PeopleSwap.TryGetValue(key, out var id)) continue;
            if (key == "H07") id = new[] { "A01_dad", "A02_mom", "A05_student", "A04_soldier" }[swapped % 4];
            var fresh = Place(Meshy + id + ".prefab", t.parent, t.position, t.eulerAngles.y, t.name);
            fresh.gameObject.SetActive(t.gameObject.activeSelf);
            fresh.gameObject.AddComponent<RestStopCivilian>();
            t.gameObject.SetActive(false); swapped++;
        }
        return swapped;
    }

    // ---- scenery along the curved route ----------------------------------------------------
    static void BuildScenery(Transform g)
    {
        var paddyA = Mat("Paddy_Green", new Color(.46f, .62f, .3f)); var paddyB = Mat("Paddy_Young", new Color(.62f, .7f, .36f)); var bund = Mat("Paddy_Bund", new Color(.48f, .4f, .28f));
        for (float d = 20; d < route.length; d += 45)
        {
            route.Sample(d, false, out var c, out var f); var right = Vector3.Cross(Vector3.up, f);
            float ground = Mathf.Min(0, c.y);
            if (d < 520)
            {
                // Leaving the city: Korean apartment complexes on both sides.
                foreach (float side in new[] { -1f, 1f })
                    foreach (float lat in new[] { 78f, 128f })
                    {
                        if (Rng.NextDouble() < .25) continue;
                        var p = c + right * side * (lat + (float)Rng.NextDouble() * 12) + f * (float)(Rng.NextDouble() * 10); p.y = ground;
                        var a = Place(Meshy + "P13_apartment_block.prefab", g, p, Quaternion.LookRotation(-right * side).eulerAngles.y, "Apartment_Block");
                        a.localScale = Vector3.one * (.75f + (float)Rng.NextDouble() * .35f);
                    }
            }
            else if (d < 1450)
            {
                // Farmland: paddies with bunds and vinyl greenhouses.
                foreach (float side in new[] { -1f, 1f })
                {
                    for (int k = 0; k < 3; k++)
                    {
                        var p = c + right * side * (42 + k * 26) + f * 4; p.y = ground - .05f;
                        var paddy = Box(g, "Paddy", p, new Vector3(24, .06f, 40), (k + (int)(d / 45)) % 2 == 0 ? paddyA : paddyB);
                        paddy.rotation = Quaternion.LookRotation(f); paddy.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                        var edge = Box(g, "Paddy_Bund", p + Vector3.up * .08f + right * side * 12.5f, new Vector3(1, .25f, 40), bund); edge.rotation = paddy.rotation;
                    }
                    if (Rng.NextDouble() < .35)
                    {
                        var gp = c + right * side * (60 + (float)Rng.NextDouble() * 30); gp.y = ground;
                        Place(Meshy + "P14_greenhouse.prefab", g, gp, Quaternion.LookRotation(f).eulerAngles.y + 90, "Greenhouse");
                    }
                }
            }
            if ((int)(d / 45) % 3 == 0)
                foreach (float side in new[] { -1f, 1f })
                {
                    var p = c + right * side * (190 + (float)Rng.NextDouble() * 40); p.y = ground - 1;
                    Place(Meshy + "P09_mountain_backdrop.prefab", g, p, Quaternion.LookRotation(-right * side).eulerAngles.y + 90, "Mountain_Ridge");
                }
        }
    }

    static object BuildWorkZones(Transform map, Transform g)
    {
        var rows = new List<string>();
        var blocks = map.GetComponentsInChildren<HighwayHazard>(true).Where(h => h.GetComponent<ObstacleStats>().obstaclePattern == ObstaclePattern.HighwayRoadblock && !Path(h.transform).Contains(Marker))
            .OrderBy(h => route.NearestDistance(h.transform.position)).ToArray();
        for (int i = 1; i < blocks.Length; i += 4)
        {
            var h = blocks[i]; float d = route.NearestDistance(h.transform.position);
            route.Sample(d, false, out var c, out var f); var right = Vector3.Cross(Vector3.up, f);
            float lane = Vector3.Dot(h.transform.position - c, right);
            var zone = new GameObject("WorkZone_" + h.transform.parent.name).transform; zone.SetParent(g, false);
            // Arrow truck parked in the closed lane just beyond the barrier, cones tapering back toward the player.
            route.Sample(d + 14, false, out var tc, out var tf);
            Place(Meshy + "V10_arrow_truck.prefab", zone, tc + Vector3.Cross(Vector3.up, tf) * lane, Quaternion.LookRotation(tf).eulerAngles.y, "Arrow_Truck");
            for (int k = 1; k <= 5; k++)
            {
                route.Sample(d - k * 4, false, out var cc, out var cf);
                Place(Cone, zone, cc + Vector3.Cross(Vector3.up, cf) * (lane + Mathf.Sign(-lane == 0 ? 1 : -lane) * k * .45f), 0, "Taper_Cone").localScale = Vector3.one * 1.6f;
            }
            route.Sample(d - 24, false, out var fc, out var ff);
            var flagger = Place(Meshy + "C08_flagger.prefab", zone, fc + Vector3.Cross(Vector3.up, ff) * 8.6f, Quaternion.LookRotation(-ff).eulerAngles.y, "Flagger");
            var civ = flagger.gameObject.AddComponent<RestStopCivilian>(); civ.idleState = "signal";
            rows.Add(zone.name + " d=" + d.ToString("F0"));
        }
        return rows;
    }

    static object AddGimmicks(Transform g)
    {
        var rows = new List<string>();
        void Add(string name, float d, RestStopRouteGimmick.Kind kind, string template, float scale, Vector3 euler, float speed, float damage)
        {
            route.Sample(d, false, out var c, out var f);
            var t = new GameObject(name).transform; t.SetParent(g, false); t.SetPositionAndRotation(c, Quaternion.LookRotation(f));
            var gm = t.gameObject.AddComponent<RestStopRouteGimmick>(); gm.kind = kind; gm.speed = speed; gm.damageFraction = damage;
            var holder = new GameObject("Template").transform; holder.SetParent(t, false);
            var tpl = Place(template, holder, t.position, 0, "Visual"); tpl.localRotation = Quaternion.Euler(euler); tpl.localScale *= scale;
            holder.gameObject.SetActive(false); gm.visualTemplate = holder.gameObject;
            if (kind == RestStopRouteGimmick.Kind.ChaseCar) { gm.lanes = new[] { -3f, 3f }; gm.travel = 90; gm.warningSeconds = 1.4f; }
            var warnings = new List<GameObject>();
            foreach (var lane in gm.lanes)
            {
                var w = Box(t, "Lane_Warning", Vector3.zero, new Vector3(2.4f, .03f, 10), Mat("Warn", new Color(1f, .22f, .1f, .55f), true));
                w.localPosition = new Vector3(lane, .05f, kind == RestStopRouteGimmick.Kind.Rolling ? -4 : -10); w.localRotation = Quaternion.identity; w.gameObject.SetActive(false);
                warnings.Add(w.gameObject);
            }
            gm.laneWarnings = warnings.ToArray(); Record(gm); rows.Add(name + " d=" + d);
        }
        Add("Rolling_Tyres_1", 540, RestStopRouteGimmick.Kind.Rolling, Tire, 2.2f, new Vector3(0, 0, 90), 11, .15f);
        Add("Tow_Truck_Chase_1", 870, RestStopRouteGimmick.Kind.ChaseCar, Meshy + "V09_tow_truck.prefab", 1, Vector3.zero, 21, .3f);
        Add("Rolling_Tyres_2", 1250, RestStopRouteGimmick.Kind.Rolling, Tire, 2.2f, new Vector3(0, 0, 90), 12, .15f);
        Add("Tow_Truck_Chase_2", 1700, RestStopRouteGimmick.Kind.ChaseCar, Meshy + "V09_tow_truck.prefab", 1, Vector3.zero, 22, .3f);
        return rows;
    }

    static object AddTraffic(Transform g)
    {
        string[] ids = { "V02_white_sedan", "V03_black_suv", "V11_tanker", "V01_compact_car", "V07_taxi", "V08_box_truck", "V05_tour_bus", "V02_white_sedan",
                         "V04_1ton_truck", "V03_black_suv", "V09_tow_truck", "V07_taxi", "V11_tanker", "V01_compact_car", "V08_box_truck", "V02_white_sedan" };
        var cars = ids.Select((id, i) => Place(Meshy + id + ".prefab", g, Vector3.zero, 0, "Oncoming_" + i)).ToArray();
        var path = g.gameObject.AddComponent<AmbientTrafficPath>();
        path.route = route; path.cars = cars; Record(path);
        return cars.Length;
    }

    // ---- helpers -------------------------------------------------------------------------------
    static Transform Group(string name) { var t = new GameObject(name).transform; t.SetParent(root, false); return t; }
    static Transform Place(string path, Transform parent, Vector3 p, float yaw, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new Exception("Missing " + path);
        var t = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)).transform;
        t.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0)); t.name = name;
        foreach (var c in t.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        return t;
    }
    static Transform Box(Transform parent, string name, Vector3 p, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.position = p; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }
    static Material Mat(string name, Color color, bool transparent = false)
    {
        Directory.CreateDirectory("Assets/ShooterSurvival/Materials/KoreanHighway20260925");
        string path = "Assets/ShooterSurvival/Materials/KoreanHighway20260925/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (mat != null) return mat;
        mat = new Material(Shader.Find(transparent ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor", color);
        if (transparent) { mat.SetFloat("_Surface", 1); mat.SetFloat("_ZWrite", 0); mat.renderQueue = 3000; mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
        else mat.SetFloat("_Smoothness", .1f);
        mat.enableInstancing = true; AssetDatabase.CreateAsset(mat, path); return mat;
    }
    static Bounds Bounds(Transform t, bool includeDisabled)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true).Where(r => includeDisabled || r.enabled).ToArray();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.one);
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    static void Record(UnityEngine.Object o) { EditorUtility.SetDirty(o); if (PrefabUtility.IsPartOfPrefabInstance(o)) PrefabUtility.RecordPrefabInstancePropertyModifications(o); }
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
}
