using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Re-dresses the saved RestStop route as a Korean expressway rest area (docs/plans/2026-09-25-reststop-korean-entry-meshy-plan.md):
// mainline + pylon entry, parking field, canopy plaza, one long main building whose interior the route crosses
// (lobby -> food-court holdout -> restaurant -> convenience store -> restroom), fuel station, connector, mainline.
// Gameplay roots (route tiles, enemies, bonuses, hazard stations, holdout) keep their transforms and colliders;
// old scenery is disabled, not deleted. Run once:
//   unity command run_script --file tools/install-reststop-korean.cs --entry InstallRestStopKorean.Main
public static class InstallRestStopKorean
{
    const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/RestStop.unity";
    const string Marker = "KoreanRestStop_20260925";
    const string Meshy = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    const string Prod = "Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/";
    const string Evidence = "outputs/meshy-reststop-2026-09-25/install/";
    const string MatDir = "Assets/ShooterSurvival/Materials/KoreanRestStop20260925";
    const string MeshDir = "Assets/ShooterSurvival/Models/KoreanRestStop20260925";
    const string RoadCone = "Assets/polyperfect/Poly Universal Pack/Prefabs/City/Props City/Street Props/Cone_City.prefab";
    const string WetSign = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin/005_STAGE01_NRY_OBSTACLE_005_Wet_floor_safety_cone/005_STAGE01_NRY_OBSTACLE_005_Wet_floor_safety_cone.prefab";
    public const string RestStopName = "한울휴게소";

    static readonly System.Random Rng = new(20260925);
    static readonly List<Transform> Overheads = new();
    static TMP_FontAsset font;
    static Transform root;

    // ---- entry ---------------------------------------------------------
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (scene.isDirty) throw new Exception("RestStop has unsaved changes; save or discard first");
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        if (map.Find(Marker) != null) throw new Exception("Already installed");
        Directory.CreateDirectory(Evidence); Directory.CreateDirectory(MatDir); Directory.CreateDirectory(MeshDir);
        File.Copy(ScenePath, Evidence + "RestStop.before.unity", true);
        string before = Contract(scene);
        File.WriteAllText(Evidence + "contract-before.txt", before);

        font = Resources.Load<TMP_FontAsset>("UI/GmarketHarbor SDF") ?? throw new Exception("Missing game font");
        Overheads.Clear();
        root = new GameObject(Marker).transform; root.SetParent(map, false);

        var report = new Dictionary<string, object>();
        report["hiddenScenery"] = HideOldScenery(map);
        report["roadTiles"] = RepaintRoad(map);
        BuildEntry(Group("01_Entry_Mainline"));
        BuildParking(Group("02_Parking"));
        BuildPlaza(Group("03_Plaza"));
        BuildMainBuilding(Group("04_MainBuilding"), map);
        BuildFuel(Group("05_Fuel"));
        BuildConnectorAndMainline(Group("06_Connector_Mainline"));
        BuildBackdrop(Group("07_Backdrop"));
        report["civilians"] = PlaceCivilians(Group("08_Civilians"));
        report["enemies"] = ReplaceEnemies(map);
        report["gimmicks"] = ReskinGimmicks(map);
        report["newGimmicks"] = AddRouteGimmicks(Group("09_RouteGimmicks"));
        report["occluders"] = RegisterOccluders();
        report["banner"] = InstallBanner(scene, map);

        Physics.SyncTransforms();
        string after = Contract(scene);
        File.WriteAllText(Evidence + "contract-after.txt", after);
        if (before != after) throw new Exception("Gameplay contract changed; scene not saved. Compare install/contract-before/after.txt");
        foreach (var c in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)))
            if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        AssetDatabase.SaveAssets();
        var json = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        File.WriteAllText(Evidence + "install-report.json", (string)json.GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new object[] { report }));
        return report;
    }

    // Second pass after the first capture review: mainline guardrails ran across the road, and the facade faced
    // empty grass. Adds the front parking field west of the building. Idempotent via the "Refine1" marker.
    public static object Refine1()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        root = map.Find(Marker) ?? throw new Exception("Install first");
        if (root.Find("10_FrontParking") != null) throw new Exception("Refine1 already applied");
        string before = Contract(scene);
        int turned = 0;
        foreach (var t in root.Find("01_Entry_Mainline").Cast<Transform>().Where(t => t.name.EndsWith("_Guardrail")))
        { t.rotation = Quaternion.Euler(0, 90, 0); turned++; }
        var g = Group("10_FrontParking");
        string[] small = { "V01_compact_car", "V02_white_sedan", "V03_black_suv", "V07_taxi", "V02_white_sedan" };
        var lines = new List<(Vector3, float)>();
        int cars = 0;
        // Rows run north-south facing the glass facade; aisles every 20 units.
        foreach (var x in new[] { 190f, 182.5f, 158f, 150.5f, 126f, 118.5f, 94f, 86.5f })
        {
            float yaw = ((int)x % 2 == 0) ? 90 : 270;
            for (float z = 176; z <= 404; z += 3.6f)
            {
                lines.Add((new Vector3(x - 3.1f, .02f, z - 1.8f), 90));
                double fill = x > 170 ? .45 : .2; // fuller near the entrance
                if (Rng.NextDouble() < fill) { Place(Meshy + small[Rng.Next(small.Length)] + ".prefab", g, new Vector3(x, 0, z), yaw, "Parked_Car"); cars++; }
            }
        }
        StallLines(g, "Front_Stall_Lines", lines);
        foreach (var x in new[] { 170f, 138f, 106f }) for (float z = 190; z <= 400; z += 52) Place(Prod + "E06.prefab", g, new Vector3(x, 0, z), 0, "Island_Planter");
        foreach (var x in new[] { 200f, 140f, 80f }) for (float z = 200; z <= 400; z += 50) Place(Prod + "E05.prefab", g, new Vector3(x, 0, z), 0, "Lot_Light");
        var pave = Box(g, "Front_Asphalt", new Vector3(140, -.012f, 290), new Vector3(128, .02f, 240), AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat"));
        pave.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var southPave = Box(g, "South_Asphalt", new Vector3(115, -.012f, 70), new Vector3(210, .02f, 140), AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat"));
        southPave.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var ppl = new[] { ("A01_dad", new Vector3(196, 0, 240), 90f), ("A02_mom", new Vector3(197, 0, 242), 70f), ("A03_kid", new Vector3(198.5f, 0, 241), 110f), ("A05_student", new Vector3(170, 0, 300), 90f), ("A04_soldier", new Vector3(200, 0, 330), 90f), ("A06_grandma", new Vector3(199, 0, 360), 90f) };
        foreach (var (id, p, yaw) in ppl) Place(Meshy + id + ".prefab", g, p, yaw, "Civilian_" + id.Substring(4)).gameObject.AddComponent<RestStopCivilian>();
        if (Contract(scene) != before) throw new Exception("Contract changed");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { turned, cars, lines = lines.Count };
    }

    // Third pass after the first live holdout: the roof covered the raised holdout camera and the entrance sign
    // filled the approach. Roof + overhead signs now switch off while the shark is inside the building zone.
    public static object Refine2()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        root = map.Find(Marker) ?? throw new Exception("Install first");
        var building = root.Find("04_MainBuilding"); var roofGroup = building.Find("Roof"); var shell = building.Find("Shell");
        if (building.GetComponent<RestStopBuildingVisibility>() != null) throw new Exception("Refine2 already applied");
        string before = Contract(scene);
        int moved = 0;
        foreach (var sign in roofGroup.Cast<Transform>().Where(t => t.name == "Sign" && t.position.y < 7).ToArray()) { sign.SetParent(shell, true); moved++; }
        var occlusion = Camera.main.GetComponent<NoryangjinCameraOcclusion>();
        var list = new SerializedObject(occlusion).FindProperty("additionalOccluderGroups");
        var groups = Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue as Transform).Where(t => t != null && t != roofGroup).ToArray();
        occlusion.ConfigureAdditionalOccluders(groups); Record(occlusion);
        var visibility = building.gameObject.AddComponent<RestStopBuildingVisibility>();
        visibility.roof = roofGroup.GetComponentsInChildren<Renderer>(true);
        visibility.hideZone = new Bounds(new Vector3(240, 0, 315), new Vector3(72, 100, 262));
        Record(visibility);
        if (Contract(scene) != before) throw new Exception("Contract changed");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { moved, roofRenderers = visibility.roof.Length, occluderGroups = groups.Length };
    }

    // Fourth pass: after the holdout the shark exits under the food hall's far fascia, which then filled the view.
    public static object Refine3()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var visibility = map.GetComponentInChildren<RestStopBuildingVisibility>(true) ?? throw new Exception("Refine2 first");
        var hall = map.Find("Approved_Open_Hall_20260913");
        var extra = new[] { "Far food hall fascia", "Exit sign backing", "Food hall entrance facade", "Label 출입구" }.Select(n => hall.Find(n)).Where(t => t != null)
            .SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).ToArray();
        if (extra.Length == 0) throw new Exception("Hall fascia not found");
        var list = visibility.roof.ToList(); int before = list.Count;
        foreach (var r in extra) if (!list.Contains(r)) list.Add(r);
        visibility.roof = list.ToArray(); Record(visibility);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { added = list.Count - before, total = list.Count };
    }

    // 2026-09-26 feedback pass: farther holdout doors, rails -> oncoming side traffic, curved entrance ramp,
    // full-width gantries, water deer instead of side-crossing cars, potholes and a log truck.
    const string HW = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    public static object Refine4()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        root = map.Find(Marker) ?? throw new Exception("Install first");
        if (root.Find("11_Feedback20260926") != null) throw new Exception("Refine4 already applied");
        font = Resources.Load<TMP_FontAsset>("UI/GmarketHarbor SDF");
        var g = Group("11_Feedback20260926");
        var report = new Dictionary<string, object>();

        // 1. Holdout doors from 12 to 18 units.
        var hold = map.GetComponentInChildren<RestStopHoldout>(true);
        foreach (var door in hold.entrances) { var lp = door.localPosition; var flat = new Vector3(lp.x, 0, lp.z); door.localPosition = flat.normalized * 18f + Vector3.up * lp.y; Record(door); }

        // 2. Funnel rails and their invisible lane clamp are removed; side traffic replaces them.
        int rails = 0;
        foreach (var lanes in map.GetComponentsInChildren<HighwayEncounterLanes>(true)) { foreach (var row in lanes.rows) if (row != null && row.barriers != null) { row.barriers.gameObject.SetActive(false); rails++; } lanes.enabled = false; Record(lanes); }
        foreach (var rails2 in map.GetComponentsInChildren<Transform>(true).Where(t => t.name == "VisibleRestStopPassage")) { rails2.gameObject.SetActive(false); rails++; }
        report["railsHidden"] = rails;

        // 3. Runtime route frame for traffic and gimmicks.
        var poly = g.gameObject.AddComponent<RoutePolyline>();
        poly.points = new[] { new Vector3(0, 0, -420), new Vector3(0, 0, 0), new Vector3(240, 0, 0), new Vector3(240, 0, 440), new Vector3(-100, 0, 440), new Vector3(-100, 0, 780), new Vector3(220, 0, 780) };
        Record(poly);

        // 4. Curved entrance ramp: continuous arc (radius 20) replaces the first stop-and-rotate corner.
        var firstTurn = map.GetComponentsInChildren<NoryangjinTurnSpot>(true).OrderBy(t => (t.transform.position - Vector3.zero).sqrMagnitude).First();
        firstTurn.gameObject.SetActive(false); Record(firstTurn.gameObject);
        var arcStart = new GameObject("EntranceArc_Start").transform; arcStart.SetParent(g, false); arcStart.SetPositionAndRotation(new Vector3(0, 0, -20), Quaternion.identity);
        var arc = g.gameObject.AddComponent<RouteArcDriver>(); arc.start = arcStart; arc.radius = 20; arc.turnDegrees = 90; Record(arc);
        BuildEntranceRamp(g, map);
        report["entrance"] = "arc r=20 from (0,-20) to (20,0); turn spot " + firstTurn.name + " disabled";

        // 5. Left mainline lanes now carry oncoming traffic (beyond the double yellow line).
        foreach (var lane in root.GetComponentsInChildren<AmbientTrafficLane>(true))
        {
            lane.speed = -Mathf.Abs(lane.speed);
            foreach (var car in lane.cars) if (car != null) car.localRotation = Quaternion.Euler(0, 180, 0);
            if (lane.name == "Mainline_Traffic_North") lane.length = 600;
            Record(lane);
        }

        // 6. Full-width gantries (the old frame put a post in the middle of the lanes).
        int gantries = 0;
        foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Guide_Gantry").ToArray())
        {
            var p = old.position; float yaw = old.eulerAngles.y;
            var board = old.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text).ToArray();
            old.gameObject.SetActive(false);
            bool eastbound = Mathf.Abs(Mathf.DeltaAngle(yaw, 90)) < 10;
            // Span the player's road plus the extra mainline lanes: S1 x -22..9, S6 z 771..803.
            var centre = eastbound ? new Vector3(p.x, 0, 787) : new Vector3(-6.5f, 0, p.z);
            BigGantry(g, centre, eastbound ? 90 : 0, 34f, board.Length > 0 ? board[0] : RestStopName, board.Length > 1 ? board[1] : "");
            gantries++;
        }
        report["gantries"] = gantries;

        // 7. Oncoming side-lane traffic (cars from ahead; now and then a tow truck).
        var trafficRoot = new GameObject("OncomingLaneTraffic").transform; trafficRoot.SetParent(g, false);
        var traffic = trafficRoot.gameObject.AddComponent<OncomingLaneTraffic>();
        traffic.carTemplates = new[] { "V02_white_sedan", "V03_black_suv", "V01_compact_car", "V07_taxi", "V04_1ton_truck", "V08_box_truck" }
            .Select(id => { var t = Place(HW + id + ".prefab", trafficRoot, Vector3.down * 50, 0, "Template_" + id); t.gameObject.SetActive(false); return t.gameObject; }).ToArray();
        var tow = Place(HW + "V09_tow_truck.prefab", trafficRoot, Vector3.down * 50, 0, "Template_Tow"); tow.gameObject.SetActive(false);
        traffic.towTruckTemplate = tow.gameObject;
        traffic.activeRanges = new[] { new Vector2(30, 375), new Vector2(1450, 1770), new Vector2(1790, 2090) };
        Record(traffic);

        // 8. Water deer replace cars cutting in from the side on highway stretches (parking reversing cars stay).
        int deer = 0;
        foreach (var merging in map.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Merging_Car").ToArray())
        {
            var hazard = merging.GetComponentInParent<HighwayHazard>(true);
            var parent = merging.parent; merging.gameObject.SetActive(false);
            var d = Place(HW + "A11_water_deer.prefab", parent, hazard.transform.position, 0, "Water_Deer");
            d.rotation = Quaternion.LookRotation(hazard.transform.right, Vector3.up);
            d.gameObject.AddComponent<DeerHop>(); deer++;
        }
        report["deer"] = deer;

        // 9. Road-work potholes (fall in = death) with a construction sign ahead, and a log truck spill.
        report["potholes"] = new[] { Pothole(g, poly, 270, 4.2f), Pothole(g, poly, 1600, -4.2f), Pothole(g, poly, 2010, 0f) };
        report["logTruck"] = LogTruck(g, 1795);

        foreach (var c in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Component>(true)))
            if (c != null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return report;
    }

    static void BuildEntranceRamp(Transform g, Transform map)
    {
        var asphalt = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");
        var paint = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/RoadPaint.mat");
        // Curved ramp surface and edge lines, slightly above the grass/tiles.
        Ribbon(g, "Ramp_Surface", 0, 14, asphalt, .015f);
        Ribbon(g, "Ramp_Edge_Left", -6.6f, .25f, paint, .03f);
        Ribbon(g, "Ramp_Edge_Right", 6.6f, .25f, paint, .03f);
        // The mainline keeps going north past the exit.
        var lanes = new GameObject("Mainline_Continues").transform; lanes.SetParent(g, false);
        for (float z = 20; z <= 160; z += 20) { RoadTile(lanes, new Vector3(0, -.01f, z), 0); RoadTile(lanes, new Vector3(-14.2f, -.01f, z), 0); }
        for (float z = 10; z <= 160; z += 6) Place(Prod + "R04.prefab", lanes, new Vector3(-22.4f, 0, z), 90, "Median_Guardrail");
        // Painted gore chevrons where the ramp separates, and the pylon standing in the gore (as on real entrances).
        var gore = new List<(Vector3, float)>();
        for (int i = 0; i < 6; i++) gore.Add((new Vector3(8.2f + i * .8f, .03f, 2f + i * 1.6f), 40));
        StallLines(g, "Gore_Chevrons", gore);
        var pylon = map.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RestArea_Pylon");
        if (pylon != null) { pylon.position = new Vector3(12.5f, 0, 8.5f); pylon.rotation = Quaternion.Euler(0, 200, 0); }
        // Clear parked cars from the new gore/ramp corner.
        foreach (var car in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Parked_Car" && t.position.x < 20 && t.position.z < 24).ToArray()) car.gameObject.SetActive(false);
        GreenPost(g, new Vector3(-9, 0, -60), 0, "↗ " + RestStopName, "입구");
    }

    // A ribbon following the entrance arc at a lateral offset (negative = left/outside of the right turn).
    static void Ribbon(Transform parent, string name, float lateral, float width, Material mat, float y)
    {
        var verts = new List<Vector3>(); var tris = new List<int>(); int steps = 24;
        float length = Mathf.PI * 10f; // quarter circle, radius 20
        for (int i = 0; i <= steps; i++)
        {
            IndianOceanAssets.ShooterSurvival.RouteArcDriver.Evaluate(new Vector3(0, 0, -20), 0, 20, 90, length * i / steps, out var p, out var f);
            var r = Vector3.Cross(Vector3.up, f);
            verts.Add(p + r * (lateral - width / 2) + Vector3.up * y); verts.Add(p + r * (lateral + width / 2) + Vector3.up * y);
            if (i > 0) { int b = i * 2; tris.AddRange(new[] { b - 2, b, b - 1, b - 1, b, b + 1 }); }
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
        var uv = new List<Vector2>(); for (int i = 0; i <= steps; i++) { uv.Add(new Vector2(0, i)); uv.Add(new Vector2(1, i)); } mesh.SetUVs(0, uv);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = MeshDir + "/" + name + ".asset"; AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void BigGantry(Transform parent, Vector3 centre, float yaw, float span, string line1, string line2)
    {
        var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/GalvanizedSteel.mat");
        var green = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/SignGreen.mat") ?? Sign("Green");
        var root = new GameObject("Big_Gantry").transform; root.SetParent(parent, false); root.SetPositionAndRotation(centre, Quaternion.Euler(0, yaw, 0));
        foreach (float side in new[] { -1f, 1f })
        {
            var post = Box(root, "Post", Vector3.zero, new Vector3(.7f, 9.5f, .7f), steel); post.localPosition = new Vector3(side * span / 2, 4.75f, 0);
            var foot = Box(root, "Footing", Vector3.zero, new Vector3(1.6f, .6f, 1.6f), Sign("Grey")); foot.localPosition = new Vector3(side * span / 2, .3f, 0);
        }
        foreach (float h in new[] { 8.2f, 9.3f }) { var beam = Box(root, "Truss", Vector3.zero, new Vector3(span, .35f, .35f), steel); beam.localPosition = new Vector3(0, h, 0); }
        var board = Box(root, "Board", Vector3.zero, new Vector3(span * .62f, 3.6f, .2f), green); board.localPosition = new Vector3(0, 8.8f, -.35f);
        var face = new GameObject("Face").transform; face.SetParent(root, false); face.localPosition = new Vector3(0, 8.8f, -.5f);
        Label(face, line1, new Vector3(0, .6f, 0), 1.6f, Color.white, span * .6f, false);
        Label(face, line2, new Vector3(0, -.9f, 0), .85f, Color.white, span * .6f, false);
        Overheads.Add(root);
        var occlusion = Camera.main.GetComponent<NoryangjinCameraOcclusion>();
        var list = new SerializedObject(occlusion).FindProperty("additionalOccluderGroups");
        var groups = Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue as Transform).Where(t => t != null).ToList();
        groups.Add(root); occlusion.ConfigureAdditionalOccluders(groups.ToArray()); Record(occlusion);
    }

    static string Pothole(Transform parent, RoutePolyline poly, float d, float lane)
    {
        poly.Sample(d, out var c, out var f); var right = Vector3.Cross(Vector3.up, f);
        var root = new GameObject("Road_Pothole").transform; root.SetParent(parent, false);
        root.SetPositionAndRotation(c + right * lane, Quaternion.LookRotation(f));
        var hole = Place(HW + "P16_pothole.prefab", root, root.position + Vector3.down * .12f, root.eulerAngles.y, "Pothole");
        var trigger = root.gameObject.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(2.4f, 2f, 2.4f); trigger.center = Vector3.up;
        root.gameObject.AddComponent<RoadPotholeHazard>();
        // Cones behind and beside the hole (the approach stays open so a careless run can still fall in).
        foreach (var o in new[] { new Vector3(-1.9f, 0, .6f), new Vector3(1.9f, 0, .6f), new Vector3(-1.2f, 0, 2.2f), new Vector3(1.2f, 0, 2.2f), new Vector3(0, 0, 2.6f) })
            Place(RoadCone, root, root.TransformPoint(o), 0, "Cone").localScale = Vector3.one * 1.6f;
        poly.Sample(d - 22, out var sc, out var sf);
        var sign = Board(root, sc + Vector3.Cross(Vector3.up, sf) * (lane + (lane > 0 ? 2.2f : -2.2f)), new Vector3(2.8f, 1.2f, .12f), Sign("Orange"), Quaternion.LookRotation(sf).eulerAngles.y, .3f);
        Label(sign, "공사중", new Vector3(0, .15f, -.08f), .55f, Color.black, 2.6f, false);
        Label(sign, "도로 파임", new Vector3(0, -.3f, -.08f), .35f, Color.black, 2.6f, false);
        return "d=" + d + " lane=" + lane;
    }

    static string LogTruck(Transform parent, float d)
    {
        var root = new GameObject("Log_Truck_Spill").transform; root.SetParent(parent, false);
        var spill = root.gameObject.AddComponent<LogTruckSpill>();
        spill.triggerDistance = d;
        var truck = Place(HW + "V12_log_truck.prefab", root, Vector3.down * 50, 0, "Template_Truck"); truck.gameObject.SetActive(false);
        var log = Place(HW + "P15_log.prefab", root, Vector3.down * 50, 0, "Template_Log"); log.gameObject.SetActive(false);
        spill.truckTemplate = truck.gameObject; spill.logTemplate = log.gameObject;
        var warn = new List<Transform>();
        for (int i = 0; i < 2; i++) { var w = Box(root, "Log_Lane_Warning", Vector3.zero, Vector3.one, Sign("Warn")); w.gameObject.SetActive(false); warn.Add(w); }
        spill.laneWarnings = warn.ToArray(); Record(spill);
        return "d=" + d;
    }

    // Lower the big gantries so the camera (12.6 high) clears the board instead of skimming its lettering.
    // 2026-09-26 follow-up: the rest-stop entrance (parking lot before the "대형차 진입 통제" gate, d≈690-780)
    // felt empty. Families, travellers and staff on the left sidewalk and between parked cars, a few on the
    // right shoulder; none in the driving lanes (|lane| > 6). Idempotent via the group name.
    public static object Refine6()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var root = map.GetComponentsInChildren<Transform>(true).First(t => t.name == Marker);
        var old = root.Find("12_EntrancePeople"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var g = new GameObject("12_EntrancePeople").transform; g.SetParent(root, false);
        var spots = new List<(string id, Vector3 p, float yaw)>
        {
            ("A01_dad", new Vector3(233.2f, 0, 52), 0), ("A02_mom", new Vector3(234.4f, 0, 53), 0), ("A03_kid", new Vector3(233.8f, 0, 50.6f), 15),
            ("A05_student", new Vector3(232.8f, 0, 78), 180), ("A06_grandma", new Vector3(233.6f, 0, 92), 0),
            ("A04_soldier", new Vector3(232.5f, 0, 104), 20), ("A04_soldier", new Vector3(233.8f, 0, 105.2f), 340),
            ("A07_parking_guide", new Vector3(234.2f, 0, 121), 90), ("A09_street_cleaner", new Vector3(229, 0, 30), 0),
            ("A01_dad", new Vector3(227, 0, 64), 270), ("A02_mom", new Vector3(228, 0, 86), 90), ("A03_kid", new Vector3(228.6f, 0, 87.4f), 120),
            ("A05_student", new Vector3(226.5f, 0, 112), 60), ("A06_grandma", new Vector3(246.8f, 0, 60), 180),
            ("A07_parking_guide", new Vector3(246.5f, 0, 114), 270), ("A01_dad", new Vector3(247, 0, 95), 200),
        };
        foreach (var (id, p, yaw) in spots)
            Place(Meshy + id + ".prefab", g, p, yaw, "Civilian_" + id.Substring(4)).gameObject.AddComponent<RestStopCivilian>();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return spots.Count;
    }

    // 2026-09-26 follow-up: the main building interior (restaurant 286-337, store 337-389) looked bare: tall
    // blank cream dividers beside the route hid the rooms, the route floor was asphalt and the fittings were
    // primitive. Dividers go, a tiled floor covers the rooms, Meshy fittings (I01-I05, Codex concepts) replace
    // the primitive counters/tables/gondolas/fridges, fascia signs name each shop, and shoppers fill it.
    // The route doorway (x 233-247) stays clear. Idempotent via "13_Interior".
    public static object Refine7()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        root = map.Find(Marker) ?? throw new Exception("Install first");
        font = Resources.Load<TMP_FontAsset>("UI/GmarketHarbor SDF");
        var building = root.Find("04_MainBuilding");
        var old = root.Find("13_Interior"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var g = Group("13_Interior");
        var report = new Dictionary<string, object>();

        // 1. Blank dividers and the primitive fittings they replace.
        var retire = new HashSet<string> { "Divider_W", "Divider_E", "Menu_Counter", "Dining_Table", "Gondola", "Fridge", "Wall_Shelf" };
        int hidden = 0;
        foreach (var t in building.GetComponentsInChildren<Transform>(true).Where(t => retire.Contains(t.name)).ToArray()) { t.gameObject.SetActive(false); Record(t.gameObject); hidden++; }
        report["retired"] = hidden;

        // 2. Tiled floor over the restaurant, store and restroom (the atrium keeps the holdout hall floor).
        var floor = GameObject.CreatePrimitive(PrimitiveType.Quad); floor.name = "Tile_Floor"; UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.transform.SetParent(g, false);
        floor.transform.SetPositionAndRotation(new Vector3(240, .035f, (286 + 432) / 2f), Quaternion.Euler(90, 0, 0));
        floor.transform.localScale = new Vector3(67, 146, 1);
        floor.GetComponent<Renderer>().sharedMaterial = TileMaterial(67 / 1.2f, 146 / 1.2f);
        floor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // The gameplay camera sees roughly ±9 m around the route (x = 240), so everything sits 7-17 m out.
        // Meshy fittings face +Z: yaw 270 faces the route from the east side, 90 from the west side.
        // 3. Restaurant: food counters line the east side, kiosks before them, table sets on the west side.
        float counterW = Width(HW + "I03_food_counter.prefab");
        int counters = 0;
        for (float z = 287.5f + counterW / 2; z + counterW / 2 < 336; z += counterW + .4f) { Place(HW + "I03_food_counter.prefab", g, new Vector3(252.5f, 0, z), 270, "Food_Counter"); counters++; }
        foreach (var z in new[] { 294f, 306f, 318f, 330f }) Place(HW + "I05_order_kiosk.prefab", g, new Vector3(248f, 0, z), 270, "Order_Kiosk");
        foreach (var x in new[] { 231.5f, 226f, 220.5f }) for (float z = 291; z <= 333; z += 6) Place(HW + "I04_dining_set.prefab", g, new Vector3(x, 0, z), 0, "Dining_Set");
        var food = Board(g, new Vector3(255.2f, 0, 311.5f), new Vector3(40, 1.4f, .3f), Sign("Red"), 90, 5.3f);
        Label(food, "푸드코트  ·  우동  ·  돈까스  ·  한식  ·  분식", new Vector3(0, 0, -.2f), 1f, Color.white, 38, false);
        report["foodCounters"] = counters;

        // 4. Store: snack gondolas either side of the aisle facing the approach, drink fridges behind them.
        foreach (var z in new[] { 344f, 353f, 362f, 371f, 380f })
        {
            Place(HW + "I01_snack_gondola.prefab", g, new Vector3(250.2f, 0, z), 180, "Snack_Gondola");
            Place(HW + "I01_snack_gondola.prefab", g, new Vector3(229.8f, 0, z), 180, "Snack_Gondola");
        }
        float fridgeW = Width(HW + "I02_drink_fridge.prefab");
        int fridges = 0;
        for (float z = 340 + fridgeW / 2; z + fridgeW / 2 < 387; z += fridgeW + .2f)
        {
            Place(HW + "I02_drink_fridge.prefab", g, new Vector3(255.5f, 0, z), 270, "Drink_Fridge");
            Place(HW + "I02_drink_fridge.prefab", g, new Vector3(224.5f, 0, z), 90, "Drink_Fridge");
            fridges += 2;
        }
        var mart = Board(g, new Vector3(256.8f, 0, 363), new Vector3(30, 1.4f, .3f), Sign("Blue"), 90, 4.4f);
        Label(mart, "한울마트 편의점  24시", new Vector3(0, 0, -.2f), 1.05f, Color.white, 28, false);
        var martW = Board(g, new Vector3(223.2f, 0, 363), new Vector3(30, 1.4f, .3f), Sign("Blue"), 270, 4.4f);
        Label(martW, "음료 · 과자 · 라면 · 도시락", new Vector3(0, 0, -.2f), 1.05f, Color.white, 28, false);
        report["fridges"] = fridges;

        // 5. Low planters where the dividers stood mark the rooms without hiding them.
        foreach (var z in new[] { 286f, 337f, 389f })
            foreach (var x in new[] { 231f, 249f })
                Place(Prod + "F02.prefab", g, new Vector3(x, 0, z), 0, "Zone_Planter");

        // 6. People: cashiers at the counters and checkout, diners and shoppers near the aisle.
        var people = new List<(string id, Vector3 p, float yaw)>
        {
            ("A10_cashier", new Vector3(254.8f, 0, 298), 270), ("A10_cashier", new Vector3(254.8f, 0, 320), 270), ("A10_cashier", new Vector3(252.5f, 0, 343.2f), 180),
            ("A01_dad", new Vector3(249.8f, 0, 300), 270), ("A03_kid", new Vector3(249.6f, 0, 301.3f), 250), ("A05_student", new Vector3(249.2f, 0, 312), 270),
            ("A02_mom", new Vector3(233.2f, 0, 297), 90), ("A06_grandma", new Vector3(227.8f, 0, 309), 270), ("A04_soldier", new Vector3(233, 0, 322), 0), ("A04_soldier", new Vector3(233.4f, 0, 323.6f), 200),
            ("A05_student", new Vector3(232.6f, 0, 348.5f), 0), ("A02_mom", new Vector3(247.6f, 0, 357.5f), 0), ("A03_kid", new Vector3(248.6f, 0, 356.6f), 30),
            ("A01_dad", new Vector3(247.4f, 0, 375), 90), ("A06_grandma", new Vector3(232.6f, 0, 367), 180), ("A09_street_cleaner", new Vector3(233, 0, 384), 90),
        };
        // 7. Restroom hall (389-432): wash basins and vending machines along the aisle, bins by the exit.
        for (float z = 394; z <= 426; z += 3.2f) Place(Prod + "T06.prefab", g, new Vector3(231.2f, 0, z), 90, "Aisle_Sink");
        for (float z = 395; z <= 425; z += 2.6f) Place("Assets/ShooterSurvival/Prefabs/Highway/RestStop/reststop_vending.prefab", g, new Vector3(249.5f, 0, z), 270, "Vending_Machine");
        foreach (var z in new[] { 404f, 420f }) Place(HW + "P10_hand_dryer.prefab", g, new Vector3(230.4f, 1.4f, z + 1.6f), 90, "Aisle_Hand_Dryer");
        Place(HW + "P06_recycle_bins.prefab", g, new Vector3(233f, 0, 429), 90, "Recycle_Bins");
        people.AddRange(new[] { ("A04_soldier", new Vector3(232.8f, 0, 399), 270f), ("A01_dad", new Vector3(232.8f, 0, 412), 270f), ("A05_student", new Vector3(247.6f, 0, 407), 90f), ("A06_grandma", new Vector3(247.4f, 0, 420), 90f) });
        foreach (var (id, p, yaw) in people) Place(HW + id + ".prefab", g, p, yaw, "Shopper_" + id.Substring(4)).gameObject.AddComponent<RestStopCivilian>();
        report["people"] = people.Count;

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return report;
    }

    static float Width(string prefab)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
        try { var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b.size.x; }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    // Cream 60 cm tiles with grey grout (one texture = 2x2 tiles), generated once as a texture asset.
    static Material TileMaterial(float tilesX, float tilesZ)
    {
        string texPath = MatDir + "/Hall_Tile.png", matPath = MatDir + "/Hall_Tile.mat";
        if (!File.Exists(texPath))
        {
            var tex = new Texture2D(128, 128, TextureFormat.RGB24, false);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                bool grout = x % 64 < 2 || y % 64 < 2;
                float n = Mathf.PerlinNoise(x * .08f, y * .08f) * .04f;
                bool dark = (x / 64 + y / 64) % 2 == 1;
                tex.SetPixel(x, y, grout ? new Color(.62f, .62f, .6f) : dark ? new Color(.86f + n, .84f + n, .79f + n) : new Color(.93f + n, .91f + n, .86f + n));
            }
            File.WriteAllBytes(texPath, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
        }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)); mat.SetColor("_BaseColor", Color.white);
        mat.SetTextureScale("_BaseMap", new Vector2(tilesX / 2, tilesZ / 2)); mat.SetFloat("_Smoothness", .35f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // 2026-09-26 restructure (user: a third of RestStop was plain highway; hand it to HighWay and keep the
    // rest stop off the highway). The stage now starts on the exit ramp's deceleration lane (d 380) and ends
    // on the fuel-station exit road before the on-ramp (d 1430). Gameplay outside that window is deleted
    // (inactive enemies would still count in stat progression); the two remaining 90-degree turn spots become
    // continuous arcs; the exit road loses its highway guardrails and gains the fuel station, EV chargers and
    // rest-stop life inside the camera's view. Scene backup: outputs/restructure-2026-09-26/backup/.
    public const float KeepFrom = 380f, KeepTo = 1430f;
    public static object Refine8()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        root = map.Find(Marker) ?? throw new Exception("Install first");
        font = Resources.Load<TMP_FontAsset>("UI/GmarketHarbor SDF");
        var poly = UnityEngine.Object.FindFirstObjectByType<RoutePolyline>(FindObjectsInactive.Include);
        var report = new Dictionary<string, object>();
        Func<Vector3, float> D = p => poly.NearestDistance(p, Vector3.zero);
        Func<Vector3, bool> Outside = p => { float d = D(p); return d < KeepFrom || d > KeepTo; };

        // 1. Gameplay outside the window.
        var doomed = new HashSet<GameObject>();
        foreach (var e in map.GetComponentsInChildren<EnemyScript_space>(true)) if (Outside(e.transform.position)) doomed.Add(e.gameObject);
        foreach (var r in map.GetComponentsInChildren<HighwayEncounterRow>(true))
            if ((r.left == null || doomed.Contains(r.left.gameObject)) && (r.right == null || doomed.Contains(r.right.gameObject))) doomed.Add(r.gameObject);
        var bonuses = map.Find("Bonuses");
        if (bonuses != null) foreach (Transform b in bonuses) if (Outside(b.position)) doomed.Add(b.gameObject);
        foreach (var h in map.GetComponentsInChildren<HighwayHazard>(true))
        {
            var unit = h.transform; while (unit.parent != null && unit.parent.name != "Props" && unit.parent != map) unit = unit.parent;
            if (unit.parent != null && unit.parent.name == "Props" && Outside(unit.position)) doomed.Add(unit.gameObject);
        }
        foreach (var x in map.GetComponentsInChildren<RoadPotholeHazard>(true)) if (Outside(x.transform.position)) doomed.Add(x.gameObject);
        foreach (var x in map.GetComponentsInChildren<LogTruckSpill>(true)) if (x.triggerDistance < KeepFrom || x.triggerDistance > KeepTo - 60) doomed.Add(x.gameObject);
        foreach (var x in map.GetComponentsInChildren<RestStopRouteGimmick>(true)) if (Outside(x.transform.position)) doomed.Add(x.gameObject);
        var counts = doomed.GroupBy(g => g.GetComponent<EnemyScript_space>() != null ? "enemy" : g.GetComponent<HighwayEncounterRow>() != null ? "row" : g.transform.parent != null ? g.transform.parent.name : "root").ToDictionary(k => k.Key, v => v.Count());
        foreach (var g in doomed) if (g != null) UnityEngine.Object.DestroyImmediate(g);
        report["deleted"] = counts;
        foreach (var t in map.GetComponentsInChildren<OncomingLaneTraffic>(true))
        {
            t.activeRanges = t.activeRanges.Select(r => new Vector2(Mathf.Max(r.x, KeepFrom), Mathf.Min(r.y, KeepTo))).Where(r => r.y - r.x > 40).ToArray(); Record(t);
            report["trafficRanges"] = string.Join(" ", t.activeRanges.Select(r => $"{r.x:0}-{r.y:0}"));
        }

        // 2. Start on the deceleration lane; camera keeps its offset.
        var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerScript>(true)).Single();
        var start = new Vector3(0, .12f, -45);
        var delta = start - player.transform.position;
        player.transform.SetPositionAndRotation(start, Quaternion.identity); Record(player.transform);
        var cam = Camera.main.transform; cam.position += new Vector3(delta.x, 0, delta.z); Record(cam);

        // 3. Stage exit before the on-ramp turn.
        var exit = map.Find("RestStop_StageExit");
        poly.Sample(KeepTo, out var ec, out var ef);
        exit.SetPositionAndRotation(ec, Quaternion.LookRotation(ef)); Record(exit);
        var box = exit.GetComponent<BoxCollider>(); box.size = new Vector3(20, 3, 1); Record(box);

        // 4. The ramp-to-parking and building-exit corners become arcs (turn spots stay for stat ordering).
        var arcs = new List<string>();
        foreach (var (corner, entry, turn) in new[] { (new Vector3(240, 0, 0), Vector3.right, -90f), (new Vector3(240, 0, 440), Vector3.forward, -90f) })
        {
            foreach (var spot in map.GetComponentsInChildren<NoryangjinTurnSpot>(true).Where(s => Vector3.Distance(s.transform.position, corner) < 6)) { spot.gameObject.SetActive(false); Record(spot.gameObject); }
            var arcStart = new GameObject("Corner_Arc_Start_" + corner.x + "_" + corner.z).transform; arcStart.SetParent(root, false);
            arcStart.SetPositionAndRotation(corner - entry * 14, Quaternion.LookRotation(entry));
            var arc = arcStart.gameObject.AddComponent<RouteArcDriver>(); arc.start = arcStart; arc.radius = 14; arc.turnDegrees = turn; Record(arc);
            arcs.Add(corner.ToString("0"));
        }
        report["arcs"] = arcs;

        // 5. Exit road (z = 440, heading -x): rest-stop frontage instead of highway guardrails.
        int rails = 0;
        foreach (var t in map.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Exit_Guardrail")) { t.gameObject.SetActive(false); Record(t.gameObject); rails++; }
        report["exitGuardrailsHidden"] = rails;
        var fuel = map.GetComponentsInChildren<Transform>(true).First(t => t.name == "Fuel_Station"); fuel.position += new Vector3(0, 0, -8.5f); Record(fuel);
        foreach (var t in map.GetComponentsInChildren<Transform>(true).Where(t => t.name == "EV_Charger" || t.name == "Charging_Car")) { t.position += new Vector3(0, 0, 4.5f); Record(t); }
        var life = Group("14_ExitFrontage");
        foreach (var (id, p, yaw) in new[] {
            ("P04_coffee_truck", new Vector3(214, 0, 452), 180f), ("P06_recycle_bins", new Vector3(196, 0, 450.5f), 180f), ("P02_walnut_cake_stall", new Vector3(126, 0, 451), 180f),
            ("P03_fishcake_stall", new Vector3(119, 0, 451), 180f), ("P12_parasol_table", new Vector3(110, 0, 452.5f), 0f), ("P05_smoking_booth", new Vector3(40, 0, 452), 180f),
            ("V02_white_sedan", new Vector3(150, 0, 449.5f), 270f), ("V07_taxi", new Vector3(170, 0, 449.5f), 270f), ("V03_black_suv", new Vector3(58, 0, 430.5f), 90f) })
            Place(Meshy + id + ".prefab", life, p, yaw, id.Substring(4));
        foreach (var (id, p, yaw) in new[] {
            ("A01_dad", new Vector3(148, 0, 451.5f), 180f), ("A08_fuel_attendant", new Vector3(160, 0, 451), 0f), ("A08_fuel_attendant", new Vector3(166, 0, 452), 90f),
            ("A05_student", new Vector3(212, 0, 450), 0f), ("A02_mom", new Vector3(124, 0, 449.5f), 0f), ("A03_kid", new Vector3(122.8f, 0, 449.2f), 30f),
            ("A06_grandma", new Vector3(108, 0, 450), 270f), ("A04_soldier", new Vector3(80, 0, 431.2f), 90f), ("A09_street_cleaner", new Vector3(30, 0, 449.8f), 270f),
            ("A07_parking_guide", new Vector3(233, 0, 449), 270f) })
            Place(Meshy + id + ".prefab", life, p, yaw, "Civilian_" + id.Substring(4)).gameObject.AddComponent<RestStopCivilian>();
        var bye = Board(life, new Vector3(-80, 0, 450.5f), new Vector3(9, 1.8f, .25f), Sign("Green"), 270, 3f);
        Label(bye, "안녕히 가세요\n본선 합류 · 서울 42km", new Vector3(0, 0, -.16f), .6f, Color.white, 8.4f, false);

        // 6. Ramp leg (z = 0, heading +x): coach and truck bays on the right inside the camera's view.
        var bays = Group("15_CoachBays");
        float bx = 70;
        foreach (var id in new[] { "V05_tour_bus", "V08_box_truck", "V05_tour_bus", "V04_1ton_truck", "V11_tanker", "V05_tour_bus", "V08_box_truck", "V04_1ton_truck" })
        { Place(Meshy + id + ".prefab", bays, new Vector3(bx, 0, -16), 0, "Parked_" + id.Substring(4)); bx += id.StartsWith("V05") || id.StartsWith("V11") ? 8.5f : 6.5f; }
        foreach (var (id, p, yaw) in new[] { ("A04_soldier", new Vector3(96, 0, -9.5f), 0f), ("A01_dad", new Vector3(122, 0, -10), 90f), ("A05_student", new Vector3(146, 0, -9.8f), 270f), ("A07_parking_guide", new Vector3(176, 0, -9), 0f) })
            Place(Meshy + id + ".prefab", bays, p, yaw, "Civilian_" + id.Substring(4)).gameObject.AddComponent<RestStopCivilian>();
        Place(Meshy + "P05_smoking_booth.prefab", bays, new Vector3(60, 0, -11), 0, "Smoking_Booth");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return report;
    }

    // 2026-09-26 playthrough: the glass end walls and door frames either side of the building doorway and the
    // hanging room signs filled the camera for a moment on the way in and out. They join the camera's fade
    // groups (each piece is its own group so only the blocking panel fades).
    public static object Refine9()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        root = map.Find(Marker) ?? throw new Exception("Install first");
        var building = root.Find("04_MainBuilding");
        var add = building.GetComponentsInChildren<Transform>(true)
            .Where(t => (t.name == "End_Glass" || t.name == "Auto_Door_Frame") && Mathf.Abs(t.position.x - 240) < 16
                        || t.name == "Sign" && t.parent != null && t.parent.name == "Roof")
            .ToList();
        var occlusion = Camera.main.GetComponent<NoryangjinCameraOcclusion>();
        var so = new SerializedObject(occlusion);
        var groups = so.FindProperty("additionalOccluderGroups");
        var existing = Enumerable.Range(0, groups.arraySize).Select(i => groups.GetArrayElementAtIndex(i).objectReferenceValue as Transform).ToList();
        int added = 0;
        foreach (var t in add.Where(t => !existing.Contains(t)))
        {
            groups.arraySize++; groups.GetArrayElementAtIndex(groups.arraySize - 1).objectReferenceValue = t; added++;
        }
        so.ApplyModifiedPropertiesWithoutUndo(); Record(occlusion);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return new { added, total = groups.arraySize };
    }

    public static object Refine5()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        int n = 0;
        foreach (var gantry in map.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Big_Gantry"))
        {
            foreach (Transform part in gantry)
            {
                if (part.name == "Post") { part.localScale = new Vector3(.7f, 8.2f, .7f); part.localPosition = new Vector3(part.localPosition.x, 4.1f, 0); }
                else if (part.name == "Truss") part.localPosition = new Vector3(0, part.localPosition.y - 1.4f, 0);
                else if (part.name == "Board" || part.name == "Face") part.localPosition -= Vector3.up * 1.4f;
            }
            n++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        return n;
    }

    // Enemy/bonus/hazard counts, gameplay colliders and route tiles must survive the re-dress unchanged.
    static string Contract(UnityEngine.SceneManagement.Scene scene)
    {
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var lines = new List<string>();
        foreach (var e in map.GetComponentsInChildren<EnemyScript_space>(true))
        {
            var col = e.GetComponent<Collider>();
            lines.Add($"E {Path(e.transform)} {V(e.transform.position)} active={e.gameObject.activeSelf} ranged={e.HasConfiguredProjectile} col={(col ? col.enabled + ":" + col.bounds.size.ToString("F2") : "none")}");
        }
        foreach (var h in map.GetComponentsInChildren<HighwayHazard>(true).Where(h => !Path(h.transform).Contains(Marker)))
            lines.Add($"H {Path(h.transform)} {V(h.transform.position)} active={h.gameObject.activeInHierarchy} col={(h.GetComponent<Collider>() ? h.GetComponent<Collider>().enabled : false)}");
        foreach (var b in map.GetComponentsInChildren<BonusWallChoicePair>(true)) lines.Add($"B {Path(b.transform)} {V(b.transform.position)}");
        foreach (Transform t in map.Find("Roads")) lines.Add($"R {t.name} {V(t.position)} col={(t.GetComponent<Collider>() ? t.GetComponent<Collider>().enabled : false)}");
        var hold = map.GetComponentInChildren<RestStopHoldout>(true);
        lines.Add("Holdout " + (hold ? V(hold.center.position) + " police=" + hold.police.Length : "none"));
        return string.Join("\n", lines);
    }

    // ---- old scenery -----------------------------------------------------
    static int HideOldScenery(Transform map)
    {
        int hidden = 0;
        var props = map.Find("Props");
        foreach (Transform t in props)
        {
            if (t.name.StartsWith("RestStop_Block") || t.name.StartsWith("Zone_")) { t.gameObject.SetActive(false); hidden++; }
            if (t.name.StartsWith("RestStop_Deck")) foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; hidden++; }
        }
        foreach (var name in new[] { "Reference_Scenery_20260913", "ProductionInstallation20260925" })
        {
            var g = map.Find(name); if (g != null && g.gameObject.activeSelf) { g.gameObject.SetActive(false); hidden++; }
        }
        // The approved food-hall shell stays; rename its fascia from the old placeholder name.
        foreach (var label in map.GetComponentsInChildren<TMP_Text>(true))
            if (label.text.Contains("달빛 휴게소")) { label.text = label.text.Replace("달빛 휴게소", RestStopName); EditorUtility.SetDirty(label); }
        return hidden;
    }

    enum Stretch { Highway, Parking, Interior, Connector }
    static Stretch Classify(Vector3 p)
    {
        if (Mathf.Abs(p.x) < 8 && p.z < 1) return Stretch.Highway;                 // S1 mainline
        if (Mathf.Abs(p.z - 780) < 8) return Stretch.Highway;                       // S6 mainline
        if (Mathf.Abs(p.x + 100) < 8 && p.z > 450) return Stretch.Connector;       // S5 connector
        if (Mathf.Abs(p.x - 240) < 8 && p.z > 198 && p.z < 432) return Stretch.Interior;
        return Stretch.Parking;                                                    // S2, S3 south, S4
    }

    // Yaw of travel at a route point; a sign readable by the player uses this yaw (its text faces back at them).
    static float HeadingAt(Vector3 p)
    {
        if (Mathf.Abs(p.z - 780) < 30) return 90;
        if (Mathf.Abs(p.x + 100) < 30 && p.z > 430) return 0;
        if (Mathf.Abs(p.z - 440) < 30 && p.x < 250) return 270;
        if (Mathf.Abs(p.x - 240) < 30 && p.z > -5) return 0;
        if (Mathf.Abs(p.z) < 30 && p.x > 5) return 90;
        return 0;
    }

    static object RepaintRoad(Transform map)
    {
        var asphalt = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");
        var floor = map.Find("Roads/FoodHall_Floor").GetComponent<Renderer>().sharedMaterial;
        var counts = new Dictionary<string, int>();
        foreach (Transform t in map.Find("Roads"))
        {
            var r = t.GetComponent<MeshRenderer>(); if (r == null || t.name == "FoodHall_Floor") continue;
            var kind = Classify(t.position);
            var mats = r.sharedMaterials;
            if (kind == Stretch.Parking && mats.Length >= 3) { mats[1] = asphalt; mats[2] = asphalt; }
            if (kind == Stretch.Interior) for (int i = 0; i < mats.Length; i++) mats[i] = floor;
            r.sharedMaterials = mats; Record(r);
            foreach (var kerb in t.GetComponentsInChildren<Renderer>(true)) if (kerb != r && kind == Stretch.Interior) { kerb.enabled = false; Record(kerb); }
            counts[kind.ToString()] = counts.TryGetValue(kind.ToString(), out var n) ? n + 1 : 1;
        }
        return counts;
    }

    // ---- 01 entry: mainline, pylon, gantry --------------------------------
    static void BuildEntry(Transform g)
    {
        // Two more mainline lanes west of the player's lane, with harmless overtaking traffic.
        var lanes = new GameObject("Mainline_Lanes").transform; lanes.SetParent(g, false);
        for (float z = -430; z <= 10; z += 20) RoadTile(lanes, new Vector3(-14.2f, -.01f, z), 0);
        Traffic(g, "Mainline_Traffic_North", new Vector3(-14.2f, 0, -450), 0, 480, 25, new[] { "V02_white_sedan", "V03_black_suv", "V01_compact_car", "V07_taxi", "V08_box_truck", "V02_white_sedan", "V05_tour_bus", "V03_black_suv" });
        for (float z = -430; z <= 0; z += 6) Place(Prod + "R04.prefab", g, new Vector3(-22.4f, 0, z), 0, "Median_Guardrail");
        for (float z = -430; z <= -30; z += 6) Place(Prod + "R04.prefab", g, new Vector3(8.6f, 0, z), 0, "Shoulder_Guardrail");

        // Rest-area pylon, first thing the player reads ("휴게소 / 한울 / Hanul / SH-OIL").
        var pylon = Place(Meshy + "P01_pylon_sign.prefab", g, new Vector3(24, 0, -205), 0, "RestArea_Pylon");
        var pb = Bounds(pylon);
        var face = new GameObject("Pylon_Face").transform; face.SetParent(pylon, false);
        face.SetPositionAndRotation(new Vector3(pb.center.x, 0, pb.min.z - .08f), Quaternion.identity); // text faces the approaching player
        float w = pb.size.x * .8f;
        Label(face, "휴게소", new Vector3(0, pb.size.y * .9f, 0), 1.25f, new Color(.85f, .1f, .12f), w, true);
        Label(face, "한", new Vector3(0, pb.size.y * .74f, 0), 3.1f, new Color(.1f, .12f, .18f), w, true);
        Label(face, "울", new Vector3(0, pb.size.y * .6f, 0), 3.1f, new Color(.1f, .12f, .18f), w, true);
        Label(face, "Hanul", new Vector3(0, pb.size.y * .5f, 0), .9f, new Color(.25f, .28f, .34f), w, true);
        Label(face, "SH-OIL", new Vector3(0, pb.size.y * .4f, 0), 1.05f, new Color(.86f, .12f, .12f), w, true);

        // Overhead green guide sign and the exit sign before the right-hand diverge.
        GreenGantry(g, new Vector3(-3.5f, 0, -330), 0, RestStopName + "  1km", "주유 · 식당 · 편의점 · 전기충전");
        GreenPost(g, new Vector3(11, 0, -150), 0, "휴게소", RestStopName + " →");
        GreenPost(g, new Vector3(11, 0, -40), 0, "↱ 휴게소 입구", "서행");
        for (int i = 0; i < 12; i++) Place(Prod + "F04.prefab", g, new Vector3(16 + (float)Rng.NextDouble() * 18, 0, -420 + i * 32), (float)Rng.NextDouble() * 360, "Verge_Tree");
    }

    // ---- 02 parking field ---------------------------------------------------
    static void BuildParking(Transform g)
    {
        string[] small = { "V01_compact_car", "V02_white_sedan", "V03_black_suv", "V07_taxi", "V02_white_sedan", "V03_black_suv" };
        // Row along the S2 aisle (north side), noses toward the aisle, 75% occupied.
        var linesA = new List<(Vector3, float)>();
        for (float x = 14; x <= 226; x += 3.6f)
        {
            linesA.Add((new Vector3(x - 1.8f, .02f, 15.5f), 0));
            if (Rng.NextDouble() < .75) Place(Meshy + small[Rng.Next(small.Length)] + ".prefab", g, new Vector3(x, 0, 15.5f), 180, "Parked_Car");
        }
        // Field rows north of that: two double rows facing each other.
        foreach (var z in new[] { 40f, 47.5f, 80f, 87.5f, 120f, 127.5f })
        {
            float yaw = (int)(z / 10) % 2 == 0 ? 0 : 180;
            for (float x = 20; x <= 210; x += 3.6f)
            {
                linesA.Add((new Vector3(x - 1.8f, .02f, z), 0));
                double fill = z < 90 ? .55 : .3; // denser near the route, sparse toward the plaza
                if (Rng.NextDouble() < fill) Place(Meshy + small[Rng.Next(small.Length)] + ".prefab", g, new Vector3(x, 0, z), yaw, "Parked_Car");
            }
            for (float x = 20; x <= 210; x += 48) Place(Prod + "E06.prefab", g, new Vector3(x, 0, z + 3.7f), 0, "Island_Planter");
        }
        // Row along the S3 aisle (west side), noses toward the route.
        for (float z = 22; z <= 140; z += 3.6f)
        {
            linesA.Add((new Vector3(228f, .02f, z - 1.8f), 90));
            if (Rng.NextDouble() < .7) Place(Meshy + small[Rng.Next(small.Length)] + ".prefab", g, new Vector3(229.5f, 0, z), 90, "Parked_Car");
        }
        StallLines(g, "Stall_Lines", linesA);

        // Large vehicles: tour buses and trucks south of S2 and east of S3.
        for (int i = 0; i < 6; i++) Place(Meshy + "V05_tour_bus.prefab", g, new Vector3(40 + i * 7.5f, 0, -20), 0, "Tour_Bus");
        for (int i = 0; i < 4; i++) Place(Meshy + (i % 2 == 0 ? "V08_box_truck" : "V04_1ton_truck") + ".prefab", g, new Vector3(110 + i * 6.8f, 0, -17), 0, "Truck");
        for (int i = 0; i < 5; i++) Place(Meshy + (i % 3 == 0 ? "V05_tour_bus" : i % 3 == 1 ? "V08_box_truck" : "V04_1ton_truck") + ".prefab", g, new Vector3(258, 0, 30 + i * 12), 270, "Large_Parked");
        Label(Board(g, new Vector3(150, 0, -9), new Vector3(9, 1.6f, .2f), Sign("Teal"), 90, 1.6f), "대형차 주차", new Vector3(0, 0, -.12f), 1.1f, Color.white, 8.5f, false);
        Label(Board(g, new Vector3(9, 0, 9), new Vector3(7, 1.6f, .2f), Sign("Teal"), 90, 1.6f), "소형차 주차", new Vector3(0, 0, -.12f), 1.1f, Color.white, 6.5f, false);
        for (float x = 30; x <= 225; x += 40) Place(Prod + "E05.prefab", g, new Vector3(x, 0, 9.2f), 0, "Lot_Light");
        for (float z = 30; z <= 170; z += 40) Place(Prod + "E05.prefab", g, new Vector3(233, 0, z), 0, "Lot_Light");
    }

    // ---- 03 canopy plaza in front of the entrance ----------------------------
    static void BuildPlaza(Transform g)
    {
        var slab = Box(g, "Plaza_Paving", new Vector3(214, -.015f, 172), new Vector3(52, .03f, 52), Sign("Paving"));
        slab.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        // Walkway canopy over the route: arches + panels, hidden overhead while the shark passes beneath.
        var canopy = new GameObject("Walkway_Canopy").transform; canopy.SetParent(g, false); Overheads.Add(canopy);
        for (float z = 156; z <= 196; z += 8)
        {
            var arch = Place(Prod + "B10.prefab", canopy, new Vector3(240, 0, z), 0, "Canopy_Arch"); arch.localScale = new Vector3(1.9f, 1.1f, 1);
            var panel = Place(Prod + "B11.prefab", canopy, new Vector3(240, 5.4f, z + 4), 0, "Canopy_Panel"); panel.localScale = new Vector3(1.9f, 1, 1.6f);
        }
        Place(Meshy + "P02_walnut_cake_stall.prefab", g, new Vector3(226, 0, 158), 90, "Stall_WalnutCake");
        Place(Meshy + "P03_fishcake_stall.prefab", g, new Vector3(226, 0, 168), 90, "Stall_FishCake");
        Place(Meshy + "P04_coffee_truck.prefab", g, new Vector3(218, 0, 181), 0, "Coffee_Truck");
        foreach (var p in new[] { new Vector3(211, 0, 158), new Vector3(204, 0, 168), new Vector3(211, 0, 190) }) Place(Meshy + "P12_parasol_table.prefab", g, p, (float)Rng.NextDouble() * 90, "Parasol_Table");
        Place(Meshy + "P05_smoking_booth.prefab", g, new Vector3(194, 0, 150), 90, "Smoking_Booth");
        Place(Meshy + "P06_recycle_bins.prefab", g, new Vector3(230, 0, 196), 90, "Recycling");
        Place(Prod + "E04.prefab", g, new Vector3(231.5f, 0, 150), 90, "Vending");
        foreach (var z in new[] { 164f, 176f, 188f }) Place(Prod + "F01.prefab", g, new Vector3(250.5f, 0, z), 270, "Bench");
        foreach (var z in new[] { 158f, 182f }) Place(Prod + "F02.prefab", g, new Vector3(252, 0, z), 90, "Planter");
    }

    // ---- 04 main building: facade, roof, interior rooms ------------------------
    static void BuildMainBuilding(Transform g, Transform map)
    {
        const float x0 = 206, x1 = 274, z0 = 198, z1 = 432, h = 7f;
        var shell = new GameObject("Shell").transform; shell.SetParent(g, false);
        var roof = new GameObject("Roof").transform; roof.SetParent(g, false); Overheads.Add(roof);
        // West facade faces the parking field: glass bays between stone pillars.
        for (float z = z0; z < z1; z += 4)
        {
            var win = Place(Prod + "B05.prefab", shell, new Vector3(x0, 0, z + 2), 90, "Facade_Glass"); win.localScale = new Vector3(1, 1.6f, 1);
            if (((int)(z - z0) / 4) % 3 == 0) Place(Prod + "B04.prefab", shell, new Vector3(x0, 0, z), 0, "Facade_Pillar");
        }
        for (float z = z0; z < z1; z += 6) { var wall = Place(Prod + "B02.prefab", shell, new Vector3(x1, 0, z + 3), 90, "Rear_Wall"); wall.localScale = new Vector3(1, 1.75f, 1); }
        foreach (var zz in new[] { z0, z1 })
        {
            for (float x = x0; x < x1; x += 4)
            {
                if (x > 231 && x < 249) continue; // doorway on the route
                var win = Place(Prod + "B05.prefab", shell, new Vector3(x + 2, 0, zz), 0, "End_Glass"); win.localScale = new Vector3(1, 1.6f, 1);
            }
            var door = Place(Prod + "B06.prefab", shell, new Vector3(240, 0, zz), 0, "Auto_Door_Frame"); door.localScale = new Vector3(3.4f, 1.3f, 1);
        }
        // Low roof, raised food-court atrium over the approved hall (its own shell reaches 16.5).
        var roofMat = AssetDatabase.LoadAssetAtPath<GameObject>(Prod + "B08.prefab").GetComponentInChildren<Renderer>().sharedMaterial;
        Box(roof, "Roof_South", new Vector3(240, h, (z0 + 212) / 2), new Vector3(x1 - x0 + 2, .5f, 212 - z0 + 1), roofMat);
        Box(roof, "Roof_North", new Vector3(240, h, (286 + z1) / 2), new Vector3(x1 - x0 + 2, .5f, z1 - 286 + 1), roofMat);
        Box(roof, "Roof_Atrium", new Vector3(240, 17.2f, 249), new Vector3(x1 - x0 + 2, .5f, 76), roofMat);
        foreach (var side in new[] { x0 - .8f, x1 + .8f }) Box(roof, "Atrium_Clerestory", new Vector3(side, 12f, 249), new Vector3(.4f, 10, 76), Sign("Cream"));
        // Roof signs: long-side sign faces the parking field, end sign faces the approach.
        var west = Board(g, new Vector3(x0 - 1.2f, 0, 330), new Vector3(34, 4.2f, .5f), Sign("Teal"), 90, h + 2.4f);
        Label(west, RestStopName, new Vector3(0, 0, -.3f), 3.2f, Color.white, 32, false);
        var south = Board(roof, new Vector3(240, 0, z0 - 1.2f), new Vector3(30, 3.6f, .5f), Sign("Teal"), 0, h + 1.6f);
        Label(south, RestStopName, new Vector3(0, 0, -.3f), 2.8f, Color.white, 28, false);

        // Room dividers with doorway signs; the route passes the 14-unit doorway.
        foreach (var (z, name) in new[] { (286f, "식당 · 푸드코트"), (337f, "편의점"), (389f, "화장실") })
        {
            Box(shell, "Divider_W", new Vector3((x0 + 233) / 2, 1.6f, z), new Vector3(233 - x0, 3.2f, .3f), Sign("Cream"));
            Box(shell, "Divider_E", new Vector3((247 + x1) / 2, 1.6f, z), new Vector3(x1 - 247, 3.2f, .3f), Sign("Cream"));
            var sign = Board(roof, new Vector3(240, 0, z - .2f), new Vector3(12, 1.4f, .2f), Sign("Teal"), 0, 4.6f);
            Label(sign, name, new Vector3(0, 0, -.12f), 1f, Color.white, 11.5f, false);
        }
        // Lobby (198-214): info desk, ATM-like kiosk, planters.
        Place(Prod + "F02.prefab", g, new Vector3(214, 0, 206), 0, "Lobby_Planter");
        Place(Prod + "F02.prefab", g, new Vector3(266, 0, 206), 0, "Lobby_Planter");
        // Restaurant (286-337): menu counters on the rear wall, seating on the facade side, tray return.
        for (float z = 290; z < 334; z += 5.5f) Place(Prod + "Assemblies/FoodCounter.prefab", g, new Vector3(268, 0, z), 270, "Menu_Counter");
        for (float x = 212; x <= 228; x += 5.5f) for (float z = 292; z <= 330; z += 6.5f) Place(Prod + "F07.prefab", g, new Vector3(x, 0, z), 0, "Dining_Table");
        Place(Meshy + "P11_tray_return.prefab", g, new Vector3(251, 0, 332), 270, "Tray_Return");
        // Convenience store (337-389): wall shelves, gondola aisles, fridges, checkout by the doorway.
        for (float z = 341; z < 386; z += 4.2f) { Place(Prod + "S07.prefab", g, new Vector3(271.5f, 0, z), 270, "Wall_Shelf"); Place(Prod + "S08.prefab", g, new Vector3(208.8f, 0, z), 90, "Fridge"); }
        for (float z = 344; z < 384; z += 9) { Place(Prod + "S06.prefab", g, new Vector3(220, 0, z), 90, "Gondola"); Place(Prod + "S06.prefab", g, new Vector3(260, 0, z), 90, "Gondola"); }
        Place(Prod + "S03.prefab", g, new Vector3(251, 0, 341), 0, "Checkout");
        // Restroom (389-432): stalls east, urinals west, sinks and hand dryers by the doorway.
        for (float z = 394; z < 428; z += 2.6f) { Place(Prod + "T01.prefab", g, new Vector3(262, 0, z), 0, "Stall_Partition"); Place(Prod + "T02.prefab", g, new Vector3(258.5f, 0, z + 1.3f), 90, "Stall_Door"); }
        for (float z = 396; z < 428; z += 2.4f) { Place(Prod + "T04.prefab", g, new Vector3(209.5f, 0, z), 90, "Urinal"); Place(Prod + "T05.prefab", g, new Vector3(210, 0, z + 1.2f), 90, "Urinal_Screen"); }
        for (float x = 212; x < 230; x += 3.2f) Place(Prod + "T06.prefab", g, new Vector3(x, 0, 392), 0, "Sink");
        for (float x = 214; x < 230; x += 6) Place(Meshy + "P10_hand_dryer.prefab", g, new Vector3(x, 1.4f, 389.4f), 180, "Hand_Dryer");
        Place(Prod + "T09.prefab", g, new Vector3(233, 0, 390), 0, "Restroom_Sign");
    }

    // ---- 05 fuel station and exit road ------------------------------------------
    static void BuildFuel(Transform g)
    {
        Place(Prod + "Assemblies/Fuel.prefab", g, new Vector3(160, 0, 466), 180, "Fuel_Station");
        // Price pylon faces traffic heading west along the exit (text readable from +X).
        var price = Place(Meshy + "P01_pylon_sign.prefab", g, new Vector3(206, 0, 454), 270, "Fuel_Price_Pylon");
        price.localScale = Vector3.one * .6f;
        var pb = Bounds(price);
        var face = new GameObject("Price_Face").transform; face.SetParent(g, false);
        face.SetPositionAndRotation(new Vector3(pb.max.x + .06f, 0, pb.center.z), Quaternion.Euler(0, 270, 0));
        Label(face, "SH-OIL", new Vector3(0, pb.size.y * .88f, 0), 1.1f, new Color(.86f, .12f, .12f), 2.4f, true);
        Label(face, "휘발유\n1,689", new Vector3(0, pb.size.y * .66f, 0), .8f, new Color(.1f, .12f, .18f), 2.4f, true);
        Label(face, "경유\n1,549", new Vector3(0, pb.size.y * .46f, 0), .8f, new Color(.1f, .12f, .18f), 2.4f, true);
        for (int i = 0; i < 6; i++) { Place(Prod + "R10.prefab", g, new Vector3(70 + i * 7, 0, 426), 0, "EV_Charger"); if (i % 2 == 0) Place(Meshy + (i == 2 ? "V01_compact_car" : "V02_white_sedan") + ".prefab", g, new Vector3(70 + i * 7, 0, 419), 0, "Charging_Car"); }
        var ev = Board(g, new Vector3(56, 0, 424), new Vector3(6, 1.4f, .2f), Sign("Green"), 270, 2.8f);
        Label(ev, "전기차 충전", new Vector3(0, 0, -.12f), .9f, Color.white, 5.6f, false);
        for (float x = 60; x > -94; x -= 6) { Place(Prod + "R04.prefab", g, new Vector3(x, 0, 448.6f), 0, "Exit_Guardrail"); Place(Prod + "R04.prefab", g, new Vector3(x, 0, 431.4f), 0, "Exit_Guardrail"); }
        for (int i = 0; i < 8; i++) Place(Prod + "F04.prefab", g, new Vector3(50 - i * 18, 0, 462 + (float)Rng.NextDouble() * 10), (float)Rng.NextDouble() * 360, "Exit_Tree");
    }

    // ---- 06 connector and mainline merge -----------------------------------------
    static void BuildConnectorAndMainline(Transform g)
    {
        for (float z = 452; z <= 772; z += 6) { Place(Prod + "R04.prefab", g, new Vector3(-108.6f, 0, z), 90, "Connector_Guardrail"); Place(Prod + "R04.prefab", g, new Vector3(-91.4f, 0, z), 90, "Connector_Guardrail"); }
        GreenPost(g, new Vector3(-89, 0, 560), 0, "본선 합류", "서울 · 강남 42km");
        var lanes = new GameObject("Mainline_Lanes_East").transform; lanes.SetParent(g, false);
        for (float x = -110; x <= 230; x += 20) RoadTile(lanes, new Vector3(x, -.01f, 794.2f), 90);
        Traffic(g, "Mainline_Traffic_East", new Vector3(-130, 0, 794.2f), 90, 380, 25, new[] { "V03_black_suv", "V07_taxi", "V05_tour_bus", "V02_white_sedan", "V04_1ton_truck", "V01_compact_car" });
        for (float x = -110; x <= 230; x += 6) Place(Prod + "R04.prefab", g, new Vector3(x, 0, 802.4f), 0, "Median_Guardrail");
        for (float x = -60; x <= 230; x += 6) Place(Prod + "R04.prefab", g, new Vector3(x, 0, 771.4f), 0, "Shoulder_Guardrail");
        GreenGantry(g, new Vector3(60, 0, 787), 90, "서울 42km", "강남 · 노량진");
    }

    static void BuildBackdrop(Transform g)
    {
        var ridges = new[] { (new Vector3(-190, 0, -320), 90f), (new Vector3(-190, 0, -80), 90f), (new Vector3(-200, 0, 160), 90f), (new Vector3(-210, 0, 420), 90f), (new Vector3(-220, 0, 680), 90f),
            (new Vector3(-60, 0, 880), 180f), (new Vector3(80, 0, 890), 180f), (new Vector3(220, 0, 880), 180f), (new Vector3(360, 0, 120), 270f), (new Vector3(360, 0, 400), 270f), (new Vector3(100, 0, -520), 0f) };
        foreach (var (p, yaw) in ridges) Place(Meshy + "P09_mountain_backdrop.prefab", g, p, yaw, "Mountain_Ridge");
    }

    // ---- people -----------------------------------------------------------------
    static int PlaceCivilians(Transform g)
    {
        var spots = new List<(string id, Vector3 p, float yaw)>
        {
            ("A07_parking_guide", new Vector3(48, 0, 8.6f), 180), ("A07_parking_guide", new Vector3(180, 0, -8.6f), 0), ("A07_parking_guide", new Vector3(233, 0, 70), 270),
            ("A01_dad", new Vector3(92, 0, 21), 30), ("A02_mom", new Vector3(94.2f, 0, 21.8f), 10), ("A03_kid", new Vector3(93, 0, 19.6f), 200),
            ("A05_student", new Vector3(150, 0, 20), 90), ("A04_soldier", new Vector3(62, 0, -9.5f), 0), ("A04_soldier", new Vector3(64, 0, -10.2f), 20),
            ("A06_grandma", new Vector3(212, 0, 160.5f), 60), ("A01_dad", new Vector3(206, 0, 170), 200), ("A02_mom", new Vector3(207.5f, 0, 171.2f), 230), ("A03_kid", new Vector3(224, 0, 163), 270),
            ("A05_student", new Vector3(218.8f, 0, 186.5f), 180), ("A04_soldier", new Vector3(196, 0, 153), 90), ("A09_street_cleaner", new Vector3(230, 0, 175), 0),
            ("A06_grandma", new Vector3(252, 0, 170), 270), ("A01_dad", new Vector3(215, 0, 300), 90), ("A02_mom", new Vector3(221, 0, 312), 90), ("A04_soldier", new Vector3(226, 0, 322), 90),
            ("A05_student", new Vector3(263, 0, 300), 90), ("A10_cashier", new Vector3(251, 0, 343.2f), 180), ("A06_grandma", new Vector3(226, 0, 360), 0), ("A03_kid", new Vector3(254, 0, 372), 270),
            ("A09_street_cleaner", new Vector3(226, 0, 412), 90), ("A08_fuel_attendant", new Vector3(150, 0, 455), 180), ("A08_fuel_attendant", new Vector3(172, 0, 457), 180), ("A08_fuel_attendant", new Vector3(196, 0, 452), 180),
        };
        foreach (var (id, p, yaw) in spots)
        {
            var t = Place(Meshy + id + ".prefab", g, p, yaw, "Civilian_" + id.Substring(4));
            t.gameObject.AddComponent<RestStopCivilian>();
        }
        return spots.Count;
    }

    // Visual roles by route section; ranged/melee combat settings and colliders stay as authored.
    static readonly Dictionary<string, (string body, RestStopEnemyBehavior.Role role)> Roles = new()
    {
        { "RST_E01", ("C02_road_worker", RestStopEnemyBehavior.Role.ConeLayer) }, { "RST_E02", ("C02_road_worker", RestStopEnemyBehavior.Role.ConeLayer) },
        { "RST_E03", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) }, { "RST_E04", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) },
        { "RST_E05", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) },
        { "RST_E06", ("C03_hiker_woman", RestStopEnemyBehavior.Role.Swarm) }, { "RST_E07", ("C03_hiker_woman", RestStopEnemyBehavior.Role.Swarm) },
        { "RST_E08", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) }, { "RST_E09", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) },
        { "RST_E10", ("C06_snack_clerk", RestStopEnemyBehavior.Role.Lobber) }, { "RST_E11", ("C02_road_worker", RestStopEnemyBehavior.Role.ConeLayer) },
        { "RST_E12", ("C03_hiker_woman", RestStopEnemyBehavior.Role.Swarm) }, { "RST_E13", ("C06_snack_clerk", RestStopEnemyBehavior.Role.Lobber) },
        { "RST_E14", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) }, { "RST_E15", ("C05_truck_driver", RestStopEnemyBehavior.Role.Lobber) },
        { "RST_E16", ("C05_truck_driver", RestStopEnemyBehavior.Role.Charger) }, { "RST_E17", ("C02_road_worker", RestStopEnemyBehavior.Role.ConeLayer) },
        { "RST_E18", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) }, { "RST_E19", ("C07_riot_police", RestStopEnemyBehavior.Role.Shield) },
        { "RST_E20", ("C05_truck_driver", RestStopEnemyBehavior.Role.Lobber) }, { "RST_E21", ("C05_truck_driver", RestStopEnemyBehavior.Role.Charger) },
        { "RST_E22", ("C02_road_worker", RestStopEnemyBehavior.Role.ConeLayer) }, { "RST_E23", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) },
        { "RST_E24", ("C07_riot_police", RestStopEnemyBehavior.Role.Shield) }, { "RST_E25", ("C01_patrol_police", RestStopEnemyBehavior.Role.Lobber) },
    };

    static object ReplaceEnemies(Transform map)
    {
        var hold = map.GetComponentInChildren<RestStopHoldout>(true);
        var coneTemplate = BuildConeTemplate(map);
        var rows = new List<string>();
        foreach (var e in map.GetComponentsInChildren<EnemyScript_space>(true).ToArray())
        {
            string body; RestStopEnemyBehavior.Role role;
            int police = hold == null ? -1 : Array.FindIndex(hold.police, p => p != null && p.gameObject == e.gameObject);
            if (police >= 0) { body = police >= 12 ? "C07_riot_police" : "C01_patrol_police"; role = police >= 12 ? RestStopEnemyBehavior.Role.Shield : RestStopEnemyBehavior.Role.Lobber; }
            else
            {
                string key = e.name.Substring(0, Math.Min(7, e.name.Length));
                if (!Roles.TryGetValue(key, out var r)) continue;
                (body, role) = r;
                if (key == "RST_E07" && !e.name.EndsWith("_Right")) { body = "C04_hiker_leader"; role = RestStopEnemyBehavior.Role.SwarmLeader; }
            }
            SwapBody(e.gameObject, body);
            var behavior = e.GetComponent<RestStopEnemyBehavior>() ?? e.gameObject.AddComponent<RestStopEnemyBehavior>();
            behavior.role = role;
            if (role == RestStopEnemyBehavior.Role.ConeLayer) behavior.coneTemplate = coneTemplate;
            Record(behavior);
            rows.Add(e.name + " -> " + body + " / " + role);
        }
        return rows;
    }

    // A disabled copy of an authored roadblock station, re-skinned as cones; cone layers clone it at runtime.
    static GameObject BuildConeTemplate(Transform map)
    {
        var source = map.Find("Props/RST_G01").GetComponentInChildren<HighwayHazard>(true).gameObject;
        var copy = UnityEngine.Object.Instantiate(source, root);
        copy.name = "ConeLayer_RoadblockTemplate";
        foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        var hz = copy.GetComponent<HighwayHazard>(); hz.breakHealth = 40;
        var visual = new GameObject("Cones").transform; visual.SetParent(copy.transform, false);
        for (int i = -1; i <= 1; i++) Place(RoadCone, visual, copy.transform.position + copy.transform.right * i * .7f, 0, "Cone").localScale = Vector3.one * 1.6f;
        hz.visual = visual;
        copy.SetActive(false);
        return copy;
    }

    static void SwapBody(GameObject root, string id)
    {
        var combat = root.GetComponent<EnemyScript_space>();
        bool ranged = combat.HasConfiguredProjectile;
        if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(root), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        // A nested body prefab silently refuses re-parenting of its hand props; unpack every nested instance first.
        foreach (var nested in root.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray())
            if (nested != null && PrefabUtility.IsOutermostPrefabInstanceRoot(nested.gameObject))
                PrefabUtility.UnpackPrefabInstance(nested.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var animators = root.GetComponentsInChildren<Animator>(true);
        var carried = new List<Transform>();
        var held = new SerializedObject(combat).FindProperty("heldProjectile").objectReferenceValue as Transform;
        var throwPoint = new SerializedObject(combat).FindProperty("throwPoint").objectReferenceValue as Transform;
        foreach (var animator in animators)
        {
            var oldBones = new HashSet<Transform>(animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s => s.bones));
            foreach (var hand in animator.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Hand.R" || t.name == "RightHand"))
                foreach (var child in hand.Cast<Transform>().Where(t => !oldBones.Contains(t)).ToArray()) { child.SetParent(root.transform, true); carried.Add(child); }
        }
        foreach (var t in new[] { held, throwPoint })
            if (t != null && animators.Any(a => t.IsChildOf(a.transform)) && !carried.Contains(t)) { t.SetParent(root.transform, true); carried.Add(t); }
        Quaternion oldRotation = Quaternion.identity; Vector3 oldPosition = Vector3.down * .1f;
        var oldBody = root.transform.Find("Body");
        if (oldBody != null) { oldRotation = oldBody.localRotation; oldPosition = oldBody.localPosition; }
        foreach (var animator in animators)
        {
            if (animator == null) continue;
            if (carried.Any(c => c != null && animator.transform.IsChildOf(c))) continue; // animated held props travel with the new hand
            if (animator.transform == root.transform) throw new Exception("Unexpected root animator " + root.name);
            var top = TopUnder(root.transform, animator.transform);
            UnityEngine.Object.DestroyImmediate(top.name == "Body" ? top.gameObject : animator.gameObject);
        }
        if (carried.Any(c => c == null)) throw new Exception("Hand prop destroyed with old body on " + root.name);
        var body = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Meshy + id + ".prefab"), root.transform)).transform;
        body.name = "Body"; body.localPosition = oldPosition; body.localRotation = oldRotation;
        var anim = body.GetComponentInChildren<Animator>(); anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var right = body.GetComponentsInChildren<Transform>(true).Single(t => t.name == "RightHand");
        var palm = Palm(anim, right);
        foreach (var carry in carried)
        {
            carry.SetParent(right, true); carry.localPosition = palm; carry.rotation = root.transform.rotation;
            var s = right.lossyScale; carry.localScale = new Vector3(1 / Mathf.Abs(s.x), 1 / Mathf.Abs(s.y), 1 / Mathf.Abs(s.z));
        }
        if (id == "C07_riot_police")
        {
            var left = body.GetComponentsInChildren<Transform>(true).Single(t => t.name == "LeftHand");
            var shield = Place(Prod + "P01.prefab", left, left.position, 0, "Riot_Shield");
            shield.localPosition = Palm(anim, left); shield.rotation = root.transform.rotation;
            var s = left.lossyScale; shield.localScale = new Vector3(1 / Mathf.Abs(s.x), 1 / Mathf.Abs(s.y), 1 / Mathf.Abs(s.z));
            shield.position -= root.transform.up * .6f;
        }
        if (id == "C04_hiker_leader") TourFlag(right, palm);
        var reaction = root.GetComponent<HighwayEnemyAnimation>();
        if (reaction != null)
        {
            reaction.animator = anim;
            reaction.head = body.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Head");
            reaction.chest = body.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Spine");
            var die = anim.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == "die");
            if (die != null) reaction.deathSeconds = Mathf.Max(reaction.deathSeconds, die.length);
            Record(reaction);
        }
        int bodyAnimators = root.GetComponentsInChildren<Animator>(true).Count(a => !carried.Any(c => c != null && a.transform.IsChildOf(c)));
        if (bodyAnimators != 1 || combat.HasConfiguredProjectile != ranged) throw new Exception("Actor contract " + root.name);
    }

    static Transform TopUnder(Transform root, Transform t) { while (t.parent != root) t = t.parent; return t; }

    static void TourFlag(Transform hand, Vector3 palm)
    {
        var flag = new GameObject("Tour_Flag").transform; flag.SetParent(hand, false); flag.localPosition = palm;
        var s = hand.lossyScale; flag.localScale = new Vector3(1 / Mathf.Abs(s.x), 1 / Mathf.Abs(s.y), 1 / Mathf.Abs(s.z));
        Box(flag, "Pole", new Vector3(0, .5f, 0), new Vector3(.05f, 1.2f, .05f), Sign("Cream"));
        Box(flag, "Pennant", new Vector3(.28f, .95f, 0), new Vector3(.5f, .34f, .02f), Sign("Red"));
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

    // ---- gimmick re-skins ----------------------------------------------------------
    static object ReskinGimmicks(Transform map)
    {
        var rows = new List<string>();
        string[] crossing = { "V02_white_sedan", "V03_black_suv", "V01_compact_car", "V07_taxi" };
        int carIndex = 0;
        foreach (Transform station in map.Find("Props"))
        {
            if (!station.name.StartsWith("RST_G")) continue;
            var stats = station.GetComponentInChildren<ObstacleStats>(true);
            var hazard = station.GetComponentInChildren<HighwayHazard>(true);
            var p = station.position;
            if (stats.obstaclePattern == ObstaclePattern.HighwayRoadblock)
            {
                var target = hazard.visual != null ? hazard.visual : hazard.transform;
                foreach (var r in target.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; Record(r); }
                bool restroom = Classify(p) == Stretch.Interior;
                var dress = new GameObject("RestStopDress").transform; dress.SetParent(target, false);
                if (restroom)
                {
                    Place(WetSign, dress, hazard.transform.position + hazard.transform.right * -.8f, 0, "Wet_Floor_Sign").localScale = Vector3.one * 2.2f;
                    Place(WetSign, dress, hazard.transform.position + hazard.transform.right * .8f, 30, "Wet_Floor_Sign").localScale = Vector3.one * 2.2f;
                    rows.Add(station.name + " roadblock -> 청소 중 표지판");
                }
                else
                {
                    for (int i = -2; i <= 2; i++) Place(RoadCone, dress, hazard.transform.position + hazard.transform.right * i * .75f, 0, "Cone").localScale = Vector3.one * 1.6f;
                    var board = Board(dress, hazard.transform.position - Quaternion.Euler(0, HeadingAt(p), 0) * Vector3.forward * 1.4f, new Vector3(2.6f, 1.1f, .12f), Sign("Orange"), HeadingAt(p), .4f);
                    Label(board, "공사중", new Vector3(0, 0, -.08f), .6f, Color.black, 2.4f, false);
                    rows.Add(station.name + " roadblock -> 라바콘·공사중");
                }
            }
            else if (stats.obstaclePattern == ObstaclePattern.HighwayTraffic)
            {
                var target = hazard.visual != null ? hazard.visual : hazard.transform;
                foreach (var r in target.GetComponentsInChildren<Renderer>(true)) if (!r.transform.name.Contains("Warning")) { r.enabled = false; Record(r); }
                bool parking = Classify(p) == Stretch.Parking && p.z < 200 && p.x > 20;
                string id = Classify(p) == Stretch.Parking && p.z > 150 ? "V04_1ton_truck" : crossing[carIndex++ % crossing.Length];
                // Crossing moves along the hazard's +X; a reversing car faces the other way with lit reverse lamps.
                var car = Place(Meshy + id + ".prefab", target, hazard.transform.position, hazard.transform.eulerAngles.y + (parking ? 270 : 90), parking ? "Reversing_Car" : "Merging_Car");
                if (parking) ReverseLamps(car);
                rows.Add(station.name + " traffic -> " + (parking ? "후진 차량 " : "끼어드는 차량 ") + id);
            }
            else if (stats.obstaclePattern == ObstaclePattern.HighwayToll)
            {
                var label = p.z < 0 ? "차로 통제" : p.x > 200 ? "대형차 진입 통제" : p.z < 450 ? "주유소 출구" : "하이패스";
                var board = Board(station, p - Quaternion.Euler(0, HeadingAt(p), 0) * Vector3.forward * 1.2f, new Vector3(9, 1.2f, .15f), Sign(p.z > 450 ? "Blue" : "Orange"), HeadingAt(p), 4.2f);
                Label(board, label, new Vector3(0, 0, -.1f), .8f, p.z > 450 ? Color.white : Color.black, 8.5f, false);
                Overheads.Add(board);
                rows.Add(station.name + " toll -> " + label);
            }
        }
        return rows;
    }

    static void ReverseLamps(Transform car)
    {
        var b = Bounds(car);
        foreach (var side in new[] { -.3f, .3f })
        {
            var lamp = Box(car, "Reverse_Lamp", Vector3.zero, new Vector3(.45f, .28f, .08f), Sign("Lamp"));
            lamp.position = new Vector3(b.center.x, b.min.y + b.size.y * .42f, b.center.z) + car.right * side * b.size.x - car.forward * (b.extents.z + .04f);
        }
    }

    static object AddRouteGimmicks(Transform g)
    {
        var rows = new List<string>();
        void Add(string name, Vector3 at, float yaw, RestStopRouteGimmick.Kind kind, string template, float templateScale, float speed, float damage)
        {
            var t = new GameObject(name).transform; t.SetParent(g, false); t.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var gm = t.gameObject.AddComponent<RestStopRouteGimmick>();
            gm.kind = kind; gm.speed = speed; gm.damageFraction = damage;
            var tpl = Place(template, t, at, yaw, "Template"); tpl.localScale *= templateScale; tpl.gameObject.SetActive(false);
            gm.visualTemplate = tpl.gameObject;
            if (kind == RestStopRouteGimmick.Kind.ChaseCar) { gm.lanes = new[] { -3f, 3f }; gm.travel = 80; gm.warningSeconds = 1.4f; }
            var warnings = new List<GameObject>();
            foreach (var lane in gm.lanes)
            {
                var w = Box(t, "Lane_Warning", Vector3.zero, new Vector3(2.4f, .03f, 10), Sign("Warn"));
                w.localPosition = new Vector3(lane, .05f, kind == RestStopRouteGimmick.Kind.Rolling ? -4 : -10); w.gameObject.SetActive(false);
                warnings.Add(w.gameObject);
            }
            gm.laneWarnings = warnings.ToArray();
            Record(gm); rows.Add(name);
        }
        Add("Rolling_Suitcases", new Vector3(128, 0, 0), 270, RestStopRouteGimmick.Kind.Rolling, Meshy + "P08_suitcase.prefab", 1.2f, 8, .15f);
        Add("Rolling_Trays", new Vector3(240, 0, 318), 180, RestStopRouteGimmick.Kind.Rolling, Prod + "S11.prefab", 6f, 9, .12f);
        Add("Spilled_Cans", new Vector3(240, 0, 372), 180, RestStopRouteGimmick.Kind.Rolling, Prod + "S15.prefab", 2.4f, 10, .12f);
        Add("Patrol_Chase_Connector", new Vector3(-100, 0, 610), 0, RestStopRouteGimmick.Kind.ChaseCar, Meshy + "V06_patrol_car.prefab", 1, 19, .3f);
        Add("Patrol_Chase_Mainline", new Vector3(20, 0, 780), 90, RestStopRouteGimmick.Kind.ChaseCar, Meshy + "V06_patrol_car.prefab", 1, 20, .3f);
        return rows;
    }

    static object RegisterOccluders()
    {
        var occlusion = Camera.main.GetComponent<NoryangjinCameraOcclusion>();
        var so = new SerializedObject(occlusion); var list = so.FindProperty("additionalOccluderGroups");
        var groups = Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue as Transform).Where(t => t != null).ToList();
        groups.AddRange(Overheads.Where(t => !groups.Contains(t)));
        occlusion.ConfigureAdditionalOccluders(groups.ToArray());
        Record(occlusion);
        return groups.Count;
    }

    static object InstallBanner(UnityEngine.SceneManagement.Scene scene, Transform map)
    {
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas").transform;
        var hold = map.GetComponentInChildren<RestStopHoldout>(true);
        var existing = canvas.GetComponentInChildren<RestStopHoldoutBanner>(true);
        if (existing != null) return "existing";
        var go = new GameObject("RestStopHoldoutBanner", typeof(RectTransform)); go.layer = 5;
        var rt = (RectTransform)go.transform; rt.SetParent(canvas, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var banner = go.AddComponent<RestStopHoldoutBanner>();
        banner.holdout = hold;
        banner.plaque = AssetDatabase.LoadAllAssetsAtPath("Assets/ShooterSurvival/UI/ChapterThemes/RestStopPlaque.png").OfType<Sprite>().First();
        Record(banner);
        return "installed";
    }

    // ---- building blocks ------------------------------------------------------------
    static Transform Group(string name) { var t = new GameObject(name).transform; t.SetParent(root, false); return t; }

    static Transform Place(string path, Transform parent, Vector3 worldPosition, float yaw, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new Exception("Missing prefab " + path);
        var t = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)).transform;
        t.SetPositionAndRotation(worldPosition, Quaternion.Euler(0, yaw, 0));
        t.name = name;
        foreach (var c in t.GetComponentsInChildren<Collider>(true)) c.enabled = false; // decoration never blocks gameplay
        return t;
    }

    static Transform Box(Transform parent, string name, Vector3 worldPosition, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.position = worldPosition; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    // A sign panel whose child labels use local units; returns the panel transform (unscaled parent).
    static Transform Board(Transform parent, Vector3 groundPosition, Vector3 size, Material mat, float yaw, float bottom)
    {
        var holder = new GameObject("Sign").transform; holder.SetParent(parent, false);
        holder.SetPositionAndRotation(groundPosition + Vector3.up * (bottom + size.y / 2), Quaternion.Euler(0, yaw, 0));
        var panel = Box(holder, "Panel", holder.position, size, mat); panel.rotation = holder.rotation;
        return holder;
    }

    static TMP_Text Label(Transform parent, string text, Vector3 local, float size, Color color, float width, bool faceLocalMinusZ)
    {
        var go = new GameObject("Label " + text.Replace("\n", " "));
        go.transform.SetParent(parent, false); go.transform.localPosition = local; go.transform.localRotation = Quaternion.identity;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font; tmp.text = text; tmp.fontSize = size * 10; tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap; tmp.rectTransform.sizeDelta = new Vector2(width, size * 2.4f);
        tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    static void GreenGantry(Transform parent, Vector3 at, float yaw, string line1, string line2)
    {
        var frame = Place(Prod + "R06.prefab", parent, at, yaw, "Guide_Gantry");
        Overheads.Add(frame);
        var board = Board(frame, at, new Vector3(9, 2.6f, .2f), Sign("Green"), yaw, 6.4f);
        board.SetParent(frame, true);
        Label(board, line1, new Vector3(0, .45f, -.12f), 1.05f, Color.white, 8.6f, false);
        Label(board, line2, new Vector3(0, -.6f, -.12f), .62f, Color.white, 8.6f, false);
    }

    static void GreenPost(Transform parent, Vector3 at, float yaw, string line1, string line2)
    {
        var holder = Board(parent, at, new Vector3(4.6f, 2.2f, .15f), Sign("Green"), yaw, 2.4f);
        Box(holder, "Post", at + Vector3.up * 1.2f, new Vector3(.18f, 2.4f, .18f), Sign("Grey"));
        Label(holder, line1, new Vector3(0, .38f, -.1f), .75f, Color.white, 4.4f, false);
        Label(holder, line2, new Vector3(0, -.45f, -.1f), .5f, Color.white, 4.4f, false);
    }

    static void RoadTile(Transform parent, Vector3 at, float yaw)
    {
        var t = Place("Assets/ShooterSurvival/Prefabs/Highway/Roads/HighwayStraight.prefab", parent, at, yaw, "Mainline_Tile");
        foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void Traffic(Transform parent, string name, Vector3 start, float yaw, float length, float speed, string[] cars)
    {
        var lane = new GameObject(name).transform; lane.SetParent(parent, false); lane.SetPositionAndRotation(start, Quaternion.Euler(0, yaw, 0));
        var list = new List<Transform>();
        for (int i = 0; i < cars.Length; i++)
        {
            var car = Place(Meshy + cars[i] + ".prefab", lane, lane.position, yaw, "Traffic_" + cars[i]);
            car.localPosition = new Vector3(i % 2 == 0 ? -3.4f : 3.4f, 0, 0); car.localRotation = Quaternion.identity;
            list.Add(car);
        }
        var comp = lane.gameObject.AddComponent<AmbientTrafficLane>(); comp.cars = list.ToArray(); comp.length = length; comp.speed = speed; Record(comp);
    }

    static void StallLines(Transform parent, string name, List<(Vector3 start, float yaw)> lines)
    {
        var mesh = new Mesh { name = name };
        var verts = new List<Vector3>(); var tris = new List<int>();
        foreach (var (s, yaw) in lines)
        {
            var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward; var side = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            // Each stall divider is a 0.14 x 6.2 painted strip running away from the aisle.
            int b = verts.Count;
            var a = s - side * .07f; var c = s + side * .07f;
            verts.AddRange(new[] { a, c, c + dir * 6.2f, a + dir * 6.2f });
            tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
        }
        mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = MeshDir + "/" + name + ".asset"; AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/RoadPaint.mat"); r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static readonly Dictionary<string, Color> Palette = new()
    {
        { "Teal", new Color(.04f, .36f, .42f) }, { "Green", new Color(.05f, .42f, .25f) }, { "Blue", new Color(.07f, .25f, .6f) },
        { "Orange", new Color(1f, .55f, .1f) }, { "Cream", new Color(.93f, .9f, .82f) }, { "Grey", new Color(.55f, .57f, .6f) },
        { "Red", new Color(.85f, .12f, .12f) }, { "Paving", new Color(.72f, .7f, .66f) }, { "Lamp", new Color(1f, .96f, .85f) },
        { "Warn", new Color(1f, .22f, .1f, .55f) },
    };

    static Material Sign(string key)
    {
        string path = MatDir + "/" + key + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        bool lit = key == "Cream" || key == "Paving" || key == "Grey";
        mat = new Material(Shader.Find(lit ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", Palette[key]);
        if (key == "Warn")
        {
            mat.SetFloat("_Surface", 1); mat.SetFloat("_ZWrite", 0); mat.renderQueue = 3000;
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        mat.enableInstancing = true;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Bounds Bounds(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true); var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void Record(UnityEngine.Object o) { EditorUtility.SetDirty(o); if (PrefabUtility.IsPartOfPrefabInstance(o)) PrefabUtility.RecordPrefabInstancePropertyModifications(o); }
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    static string V(Vector3 v) => v.ToString("F2");
}
