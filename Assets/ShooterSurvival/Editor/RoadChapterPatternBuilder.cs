using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class RoadChapterPatternBuilder
{
    public const string Record = "map-concepts/road-patterns-2026-09-13";
    public const string Art = "Assets/ShooterSurvival/Models/Chapters/RoadPatterns";
    private static Material asphalt, paint, green, pink, cream, navy, steel, amber;

    [MenuItem("Tools/맵 제작 도구/패턴 개선/고속도로 곡선과 분기 적용")]
    public static void HighwayMenu() => BuildHighway();
    [MenuItem("Tools/맵 제작 도구/패턴 개선/휴게소 식당 방어전 적용")]
    public static void RestStopMenu() => BuildRestStop();

    private static Scene Open(string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s => s.isDirty))
            throw new InvalidOperationException("Saved Edit Mode scenes required.");
        Directory.CreateDirectory(Record + "/before");
        string backup = Record + "/before/" + Path.GetFileName(path);
        if (!File.Exists(backup)) File.Copy(path, backup);
        Directory.CreateDirectory(Art); AssetDatabase.Refresh(); Materials();
        return EditorSceneManager.OpenScene(path);
    }
    public static object BuildHighway()
    {
        var scene = Open(HighwaySceneBuilder.ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        if (map.GetComponent<HighwayRoute>() != null) throw new InvalidOperationException("Pattern route already authored; refine it explicitly.");
        var route = map.gameObject.AddComponent<HighwayRoute>();
        route.length = HighwaySceneBuilder.Length;
        route.centers = Enumerable.Range(0, 469).Select(i => CurvedPoint(i * 5)).ToArray();
        route.forks = new[] { new HighwayRoute.Fork { start = 80, end = 310, offset = -30 }, new HighwayRoute.Fork { start = 1680, end = 1960, offset = -32 } };
        var roads = map.Find("Roads");
        var props = map.Find("Props");
        // Transform only direct authored roots, preserving every prefab's local axes.
        foreach (var parent in new[] { props, map.Find("Enemies"), map.Find("Bonuses"), map.Find("Highway_EnemyTargets") })
            foreach (Transform child in parent)
            {
                if (child.name == "CityGround" || child.GetComponent<NoryangjinTurnSpot>() != null) continue;
                Remap(child, route);
            }
        Remap(map.Find("Highway_StageExit"), route);
        foreach (var turn in props.GetComponentsInChildren<NoryangjinTurnSpot>(true)) Object.DestroyImmediate(turn.gameObject);
        foreach (var road in roads.Cast<Transform>().ToArray()) Object.DestroyImmediate(road.gameObject);
        int serial = 0;
        for (float d = 0; d < route.length; d += 40) Road(roads, route, d, Mathf.Min(route.length, d + 40), false, "Main_" + serial++);
        foreach (var fork in route.forks)
        {
            for (float d = fork.start; d < fork.end; d += 40) Road(roads, route, d, Mathf.Min(fork.end, d + 40), true, "Bypass_" + serial++);
            Stripe(roads, route, fork.start - 50, fork.end, 2.8f, false, pink, "본선 분홍 유도선", .45f);
            Stripe(roads, route, fork.start - 50, fork.end, -2.8f, true, green, "회복 우회로 초록 유도선", .5f);
            Sign(props, route, fork.start - 42, "초록선  회복 우회로    분홍선  고속도로", "왼쪽 우회로     오른쪽 본선");
            Sign(props, route, fork.end - 25, "본선 합류", "차량 주의");
            route.Sample((fork.start + fork.end) * .5f, true, out var recovery, out var facing);
            var marker = Group(props, "Recovery_Stop"); marker.SetPositionAndRotation(recovery, Quaternion.LookRotation(facing));
            Cube(marker, "회복 쉼터 표지", new Vector3(-5.8f, 2, 0), new Vector3(.3f, 4, .3f), steel);
            Label(marker, "회복 +10%", new Vector3(-5.8f, 4, -.2f), 1.6f, Color.white);
        }
        // Short segments make the guardrail follow the curve instead of forming angular walls.
        for (float d = 4; d < route.length - 10; d += 12)
        {
            bool forkArea = route.forks.Any(f => d > f.start - 15 && d < f.end + 15);
            if (forkArea) continue;
            route.Sample(d, false, out var p, out var f);
            var rail = Group(props, "Curved_Guardrail"); rail.SetPositionAndRotation(p, Quaternion.LookRotation(f));
            foreach (int side in new[] { -1, 1 })
            { Cube(rail, "Rail", new Vector3(side * 6.85f, .85f, 0), new Vector3(.12f, .3f, 12), steel); Cube(rail, "Post", new Vector3(side * 6.85f, .42f, 0), new Vector3(.16f, .84f, .16f), steel); }
        }
        var player = Object.FindFirstObjectByType<PlayerScript>();
        route.Sample(0, false, out var start, out var direction);
        player.transform.SetPositionAndRotation(start + Vector3.up * .12f, Quaternion.LookRotation(direction));
        player.GetComponent<NoryangjinRoadHeightFollower>().Configure(roads, .12f);
        Camera.main.GetComponent<NoryangjinCameraOcclusion>().Configure(player.transform, roads);
        var hud = HUD(scene); route.hud = hud;
        Traffic(map, route, hud);
        // Reapply on the new road now, including curved activation positions.
        Physics.SyncTransforms(); map.GetComponent<EncounterPlacementController>().BeginNewRun();
        return Save(scene, new { scene = scene.name, samples = route.centers.Length, forks = route.forks.Length, roadPieces = roads.childCount, enemies = map.Find("Enemies").childCount });
    }

    public static Vector3 CurvedPoint(float distance)
    {
        var points = HighwaySceneBuilder.Points; float cursor = 0;
        for (int i = 1; i < points.Length - 1; i++)
        {
            cursor += Vector3.ProjectOnPlane(points[i] - points[i - 1], Vector3.up).magnitude;
            const float radius = 65;
            if (distance >= cursor - radius && distance <= cursor + radius)
            {
                var a = HighwaySceneBuilder.Sample(cursor - radius, out _);
                var b = HighwaySceneBuilder.Sample(cursor + radius, out _);
                float t = Mathf.InverseLerp(cursor - radius, cursor + radius, distance);
                return (1 - t) * (1 - t) * a + 2 * (1 - t) * t * points[i] + t * t * b;
            }
        }
        var p = HighwaySceneBuilder.Sample(distance, out var direction);
        // A mild S bend breaks up the long straight without abrupt entry yaw.
        cursor = 0;
        for (int i = 0; i < points.Length - 1; i++)
        {
            float span = Vector3.ProjectOnPlane(points[i + 1] - points[i], Vector3.up).magnitude;
            if (distance >= cursor + 100 && distance <= cursor + span - 100)
            {
                float t = (distance - cursor - 100) / (span - 200);
                float wave = Mathf.Sin(t * Mathf.PI); p += Vector3.Cross(Vector3.up, direction) * (wave * wave * (i % 2 == 0 ? 10 : -8)); break;
            }
            cursor += span;
        }
        return p;
    }
    public static object RefineHighwayRoadSurface()
    {
        var scene = Open(HighwaySceneBuilder.ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var route = map.GetComponent<HighwayRoute>(); var roads = map.Find("Roads");
        foreach (var road in roads.Cast<Transform>().ToArray()) Object.DestroyImmediate(road.gameObject);
        int serial = 0;
        for (float d = 0; d < route.length; d += 40) Road(roads, route, d, Mathf.Min(route.length, d + 40), false, "Main_" + serial++);
        foreach (var fork in route.forks)
        {
            for (float d = fork.start; d < fork.end; d += 40) Road(roads, route, d, Mathf.Min(fork.end, d + 40), true, "Bypass_" + serial++);
            Stripe(roads, route, fork.start - 50, fork.end, 2.8f, false, pink, "본선 분홍 유도선", .45f);
            Stripe(roads, route, fork.start - 50, fork.end, -2.8f, true, green, "회복 우회로 초록 유도선", .5f);
        }
        var player = Object.FindFirstObjectByType<PlayerScript>(); player.GetComponent<NoryangjinRoadHeightFollower>().Configure(roads,.12f);
        Camera.main.GetComponent<NoryangjinCameraOcclusion>().Configure(player.transform,roads);
        Physics.SyncTransforms(); map.GetComponent<EncounterPlacementController>().BeginNewRun();
        return Save(scene,new {scene=scene.name,surfaceRevision=2,roadPieces=roads.childCount});
    }
    private static void Remap(Transform item, HighwayRoute route)
    {
        if (item == null) return;
        float best = float.PositiveInfinity, d = 0, cursor = 0; Vector3 oldCenter = default, oldDirection = default;
        var points = HighwaySceneBuilder.Points;
        for (int i = 0; i < points.Length - 1; i++)
        {
            var delta = points[i + 1] - points[i]; float length = Vector3.ProjectOnPlane(delta, Vector3.up).magnitude;
            float t = Mathf.Clamp01(Vector3.Dot(item.position - points[i], delta) / delta.sqrMagnitude);
            var center = Vector3.Lerp(points[i], points[i + 1], t); float score = (center - item.position).sqrMagnitude;
            if (score < best) { best = score; d = cursor + t * length; oldCenter = center; oldDirection = Vector3.ProjectOnPlane(delta, Vector3.up).normalized; }
            cursor += length;
        }
        var relative = item.position - oldCenter;
        route.Sample(d, false, out var p, out var f);
        var rotation = Quaternion.LookRotation(f) * Quaternion.Inverse(Quaternion.LookRotation(oldDirection));
        item.SetPositionAndRotation(p + rotation * relative, rotation * item.rotation);
    }
    private static void Road(Transform parent, HighwayRoute route, float start, float end, bool branch, string name)
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        int steps = Mathf.CeilToInt((end - start) / 2);
        for (int i = 0; i <= steps; i++)
        {
            float d = Mathf.Lerp(start, end, (float)i / steps); route.Sample(d, branch, out var p, out var f);
            var right = Vector3.Cross(Vector3.up, f);
            foreach (float lane in new[] { -7f, 7f }) { vertices.Add(p + right * lane + Vector3.up * (branch ? .008f : 0)); uv.Add(new Vector2((lane + 7) / 5, d / 5)); }
            if (i > 0) { int n = i * 2; triangles.AddRange(new[] { n - 2, n, n - 1, n - 1, n, n + 1 }); }
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string meshPath = Art + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
        else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
        var root = Group(parent, name); root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh; root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = asphalt; root.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        foreach (float lane in new[] { -6.2f, 6.2f }) Stripe(root, route, start, end, lane, branch, paint, "Edge", .12f);
        if (!branch)
            for (float d = start; d < end - 2; d += 10)
                foreach (float lane in new[] { -1.65f, 1.65f }) Stripe(root, route, d, Mathf.Min(end, d + 4), lane, false, paint, "Lane", .12f);
    }
    private static void Stripe(Transform parent, HighwayRoute route, float start, float end, float lane, bool branch, Material material, string name, float width)
    {
        var line = Group(parent, name).gameObject.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.alignment = LineAlignment.TransformZ;
        line.transform.rotation = Quaternion.Euler(90, 0, 0); line.widthMultiplier = width; line.sharedMaterial = material;
        int count = Mathf.CeilToInt((end - start) / 2) + 1; line.positionCount = count;
        for (int i = 0; i < count; i++) { route.Sample(Mathf.Lerp(start, end, (float)i / (count - 1)), branch, out var p, out var f); line.SetPosition(i, p + Vector3.Cross(Vector3.up, f) * lane + Vector3.up * (branch ? .04f : .025f)); }
    }
    private static void Sign(Transform parent, HighwayRoute route, float distance, string title, string caption)
    {
        route.Sample(distance, false, out var p, out var f);
        var root = Group(parent, "Korean_Direction_Sign"); root.SetPositionAndRotation(p, Quaternion.LookRotation(f));
        foreach (float x in new[] { -7.7f, 7.7f }) Cube(root, "Post", new Vector3(x, 4, 0), new Vector3(.28f, 8, .28f), steel);
        Cube(root, "Green destination board", new Vector3(0, 7, 0), new Vector3(14, 2.1f, .18f), green);
        Label(root, title, new Vector3(0, 7.25f, -.12f), 1.3f, Color.white);
        Label(root, caption, new Vector3(0, 6.55f, -.12f), .85f, Color.white);
    }
    private static void Traffic(Transform map, HighwayRoute route, ChapterPatternHUD hud)
    {
        var root = Group(map, "Oncoming_Traffic"); var controller = root.gameObject.AddComponent<HighwayOncomingTraffic>(); controller.route = route; controller.hud = hud;
        controller.beats = new[] {
            new HighwayOncomingTraffic.Beat { distance = 84, title = "정면 차량 접근", lanes = new[] { 1 }, delays = new[] { 0f } },
            new HighwayOncomingTraffic.Beat { distance = 710, title = "시간차 차량 접근", lanes = new[] { 0, 1 }, delays = new[] { 0f, 1.7f } },
            new HighwayOncomingTraffic.Beat { distance = 1320, title = "공사 차로 합류", lanes = new[] { 1, 2 }, delays = new[] { 0f, 1.5f } },
            new HighwayOncomingTraffic.Beat { distance = 1700, title = "본선 차량 행렬", lanes = new[] { 0, 2, 0, 2 }, delays = new[] { 0f, 0f, 2.7f, 2.7f } },
            new HighwayOncomingTraffic.Beat { distance = 2140, title = "터널 앞 차량 접근", lanes = new[] { 0, 1 }, delays = new[] { 0f, 2f } }
        };
        controller.cars = new Transform[4]; controller.warnings = new LineRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            var car = Instance(HighwayAssetImporter.Prefabs + (i % 2 == 0 ? "/HWY_067.prefab" : "/HWY_069.prefab"), root, "Oncoming_Car_" + i);
            Fit(car.gameObject, i % 2 == 0 ? 4.3f : 5.5f, false); car.gameObject.SetActive(false); controller.cars[i] = car;
            var line = Group(root, "Lane_Warning_" + i).gameObject.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.alignment = LineAlignment.TransformZ; line.transform.rotation = Quaternion.Euler(90, 0, 0); line.widthMultiplier = .85f; line.sharedMaterial = amber; line.gameObject.SetActive(false); controller.warnings[i] = line;
        }
    }

    public static object BuildRestStop()
    {
        var scene = Open(RestStopChapterBuilder.ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        if (map.Find("FoodHall_Holdout") != null) throw new InvalidOperationException("Food hall already authored; refine explicitly.");
        var props = map.Find("Props"); var roads = map.Find("Roads");
        Vector3 center = new Vector3(240, 0, 240);
        // The workbook disables outdoor stations 11/12, replaced by this encounter.
        // Keep their IDs for editing, and relocate only scenery out of the hall footprint.
        foreach (Transform item in props)
            {
                var p = item.position;
                bool gameplay = item.name.StartsWith("RST_");
                if (gameplay || Mathf.Abs(p.x - center.x) > 38 || Mathf.Abs(p.z - center.z) > 55) continue;
                item.position += (p.x < center.x ? Vector3.left : Vector3.right) * 48;
            }
        var hall = Group(map, "FoodHall_Holdout"); hall.position = center;
        var encounter = hall.gameObject.AddComponent<RestStopHoldout>(); encounter.center = hall; encounter.hud = HUD(scene);
        // The walkable mesh is part of Roads, so the existing support resolver sees the interior.
        Floor(roads, center, new Vector2(44, 72));
        for (int z = -7; z <= 7; z++) for (int x = -4; x <= 4; x++)
            Cube(hall, "Floor tile joint", new Vector3(x * 4.8f, .035f, z * 4.8f), new Vector3(4.72f, .012f, 4.72f), (x + z) % 2 == 0 ? cream : paint);
        foreach (int side in new[] { -1, 1 })
        {
            foreach (int end in new[] { -1, 1 })
            {
                Cube(hall, "Side wall", new Vector3(side * 22, 2.2f, end * 20), new Vector3(.5f, 4.4f, 26), cream);
                Cube(hall, "Side skirting", new Vector3(side * 21.7f, .65f, end * 20), new Vector3(.12f, 1.3f, 26), navy);
                Cube(hall, "Front back wall", new Vector3(side * 14, 2.2f, end * 36), new Vector3(16, 4.4f, .5f), cream);
                Cube(hall, "Window frame", new Vector3(side * 14, 2.8f, end * 35.65f), new Vector3(12, .12f, .15f), steel);
                Cube(hall, "Window frame", new Vector3(side * 14, 1.4f, end * 35.65f), new Vector3(12, .12f, .15f), steel);
                Cube(hall, "Window mullion", new Vector3(side * 14, 2.1f, end * 35.65f), new Vector3(.1f, 1.4f, .15f), steel);
            }
        }
        Cube(hall, "Entrance fascia", new Vector3(0, 5.1f, -36), new Vector3(44, 1.6f, 1.2f), navy);
        Label(hall, "달빛 휴게소  FOOD HALL", new Vector3(0, 5.15f, -36.7f), 2.6f, new Color(1, .84f, .4f));
        Label(hall, "식당 안으로 들어가세요", new Vector3(0, 2.5f, -38), 1.35f, Color.white);
        Cube(hall, "Back fascia", new Vector3(0, 5, 36), new Vector3(44, 1.5f, 1.2f), navy);
        Label(hall, "비상구  EXIT", new Vector3(0, 4.9f, 35.2f), 2, Color.white);
        encounter.frontShutter = Cube(hall, "Entrance shutter", new Vector3(0, 2.8f, -35.6f), new Vector3(11, 5.5f, .2f), steel).transform;
        encounter.exitShutter = Cube(hall, "Emergency exit shutter", new Vector3(0, 2.8f, 35.6f), new Vector3(11, 5.5f, .2f), steel).transform;
        // Side aisles and the central cross stay clear for police and shots.
        for (int side = -1; side <= 1; side += 2)
            for (int row = 0; row < 3; row++)
            {
                var kiosk = Instance(RestStopAssetImporter.Prefabs + "/reststop_kiosk.prefab", hall, "Food counter"); kiosk.localPosition = new Vector3(side * 18, 0, -24 + row * 24); kiosk.localRotation = Quaternion.Euler(0, side < 0 ? 90 : -90, 0);
                var vending = Instance(RestStopAssetImporter.Prefabs + "/reststop_vending.prefab", hall, "Drinks"); vending.localPosition = new Vector3(side * 19, 0, -15 + row * 24);
            }
        foreach (int side in new[] { -1, 1 }) foreach (float z in new[] { -23f, -14f, 15f, 24f })
        {
            var table = Instance("Assets/ithappy/Megacity/Prefabs/Props/table_001.prefab", hall, "Dining table"); Fit(table.gameObject, 2.8f, false); table.localPosition = new Vector3(side * 10, 0, z);
            foreach (float offset in new[] { -2f, 2f }) { var chair = Instance("Assets/ithappy/Megacity/Prefabs/Props/chair_001.001.prefab", hall, "Dining chair"); Fit(chair.gameObject, 1.3f, true); chair.localPosition = new Vector3(side * 10 + offset, 0, z); chair.localRotation = Quaternion.Euler(0, offset < 0 ? 90 : -90, 0); }
        }
        encounter.entrances = new Transform[4]; encounter.entranceSignals = new Renderer[4];
        var doorPositions = new[] { new Vector3(-21, .12f, 0), new Vector3(21, .12f, 0), new Vector3(-3, .12f, -31), new Vector3(3, .12f, 31) };
        for (int i = 0; i < 4; i++)
        {
            var door = Group(hall, "Police_Door_" + i); door.localPosition = doorPositions[i]; door.rotation = Quaternion.LookRotation(center - door.position); encounter.entrances[i] = door;
            encounter.entranceSignals[i] = Cube(door, "Warning light", new Vector3(0, 3.5f, 0), new Vector3(2.5f, .4f, .5f), amber).GetComponent<Renderer>();
        }
        var pool = Group(hall, "Police_Pool"); var targets = Group(hall, "Police_Targets");
        encounter.police = new EnemyEventController[16];
        for (int i = 0; i < 16; i++)
        {
            var actor = Instance(HighwayEnemyBuilder.Folder + "/TrafficPatrol.prefab", pool, "Holdout_Police_" + i); actor.localPosition = Vector3.zero;
            var controller = actor.GetComponent<EnemyEventController>(); controller.TargetPoint = Group(targets, "Target_" + i); controller.EventMode = EnemyEventMode.MoveToTargetThenAttack; controller.MoveAnimation = EnemyMoveAnimation.Run; controller.HideWhileWaiting = true;
            actor.GetComponent<EnemyScript_space>().ConfigureRewards(false, 0);
            encounter.police[i] = controller; actor.gameObject.SetActive(false);
        }
        var player = Object.FindFirstObjectByType<PlayerScript>(); player.GetComponent<NoryangjinRoadHeightFollower>().Configure(roads, .12f);
        Camera.main.GetComponent<NoryangjinCameraOcclusion>().Configure(player.transform, roads);
        Physics.SyncTransforms(); map.GetComponent<EncounterPlacementController>().BeginNewRun();
        return Save(scene, new { scene = scene.name, holdoutSeconds = 30, policePool = encounter.police.Length, entranceCount = encounter.entrances.Length, center = center.ToString() });
    }
    private static void Floor(Transform roads, Vector3 center, Vector2 size)
    {
        var mesh = new Mesh { name = "FoodHallFloor" };
        mesh.vertices = new[] { new Vector3(-size.x / 2, .025f, -size.y / 2), new Vector3(size.x / 2, .025f, -size.y / 2), new Vector3(-size.x / 2, .025f, size.y / 2), new Vector3(size.x / 2, .025f, size.y / 2) };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 }; mesh.RecalculateNormals(); AssetDatabase.CreateAsset(mesh, Art + "/FoodHallFloor.asset");
        var floor = Group(roads, "FoodHall_Floor"); floor.position = center; floor.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh; floor.gameObject.AddComponent<MeshRenderer>().sharedMaterial = cream; floor.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
    }
    public static object RefinePresentation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        // Preserve the current in-memory scene before opening another authored scene.
        var current = SceneManager.GetActiveScene();
        if (current.isDirty)
        {
            string copy = "Assets/ShooterSurvival/Scenes/Tools/RoadPatterns_PresentationBackup_" + DateTime.UtcNow.ToString("HHmmss") + ".unity";
            if (!EditorSceneManager.SaveScene(current, copy, true)) throw new IOException("Scene backup failed");
        }
        var scene = EditorSceneManager.OpenScene(RestStopChapterBuilder.ScenePath); Materials();
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var hall = map.Find("FoodHall_Holdout");
        if (hall.Find("Presentation_v2") != null) throw new InvalidOperationException("Presentation already refined");
        var floorA = Material("InteriorFloorA", new Color(.63f,.65f,.59f));
        var floorB = Material("InteriorFloorB", new Color(.67f,.69f,.63f));
        int tile = 0;
        foreach (Transform child in hall)
        {
            if (child.name == "Police_Pool" || child.name == "Police_Targets") continue;
            var p = child.localPosition; p.x *= .6f; p.z *= .6f; child.localPosition = p;
            if (child.GetComponent<MeshFilter>() != null && !PrefabUtility.IsPartOfPrefabInstance(child))
            { var size = child.localScale; size.x *= .6f; size.z *= .6f; child.localScale = size; }
            if (child.name == "Floor tile joint") child.GetComponent<Renderer>().sharedMaterial = tile++ % 2 == 0 ? floorA : floorB;
            if (child.GetComponent<TMP_Text>() is { } text) { text.fontSize *= 9; text.rectTransform.sizeDelta = new Vector2(25, 3); }
            if (child.name == "Dining table" || child.name == "Dining chair")
            { var v = child.localPosition; v.x += Mathf.Sign(v.x) * 1.5f; child.localPosition = v; }
        }
        var floor = map.Find("Roads/FoodHall_Floor"); floor.localScale = new Vector3(.6f,1,.6f);
        foreach (var road in map.Find("Roads").Cast<Transform>().Where(r => Mathf.Abs(r.position.x-hall.position.x)<8 && Mathf.Abs(r.position.z-hall.position.z)<34))
            foreach (var kerb in road.Cast<Transform>().Where(t => t.name == "Kerb").ToArray()) Object.DestroyImmediate(kerb.gameObject);
        Group(hall,"Presentation_v2");
        foreach (var ui in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ChapterPatternHUD>(true))) RefineHUD(ui);
        Save(scene,new {scene=scene.name,interiorWidth=26.4f,interiorDepth=43.2f,presentationRevision=2});
        scene = EditorSceneManager.OpenScene(HighwaySceneBuilder.ScenePath);
        foreach (var ui in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ChapterPatternHUD>(true))) RefineHUD(ui);
        map = scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
        foreach (var sign in map.Find("Props").Cast<Transform>().Where(t=>t.name=="Korean_Direction_Sign" || t.name=="Recovery_Stop"))
            foreach (var text in sign.GetComponentsInChildren<TMP_Text>(true)) { text.fontSize*=9; text.rectTransform.sizeDelta=new Vector2(14,3); }
        return Save(scene,new {scene=scene.name,presentationRevision=2});
    }
    private static void RefineHUD(ChapterPatternHUD hud)
    {
        var rect = (RectTransform)hud.panel.transform; rect.anchorMin=new Vector2(.06f,.79f); rect.anchorMax=new Vector2(.94f,.88f);
        hud.title.fontSize=36; hud.description.fontSize=24;
        hud.title.enableAutoSizing=true;hud.title.fontSizeMin=28;hud.title.fontSizeMax=36;
        hud.description.enableAutoSizing=true;hud.description.fontSizeMin=19;hud.description.fontSizeMax=24;
    }
    private static ChapterPatternHUD HUD(Scene scene)
    {
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas").transform;
        var go = new GameObject("ChapterPatternHUD", typeof(RectTransform), typeof(ChapterPatternHUD)); go.transform.SetParent(canvas, false);
        var hud = go.GetComponent<ChapterPatternHUD>();
        var panel = new GameObject("PatternCard", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(go.transform, false);
        var root = (RectTransform)go.transform; root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        var rect = (RectTransform)panel.transform; rect.anchorMin = new Vector2(.06f, .72f); rect.anchorMax = new Vector2(.94f, .80f); rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.04f, .09f, .14f, .94f); panel.GetComponent<Image>().raycastTarget = false;
        hud.title = HudText(panel.transform, "Title", new Vector2(.04f, .47f), new Vector2(.96f, .94f), 23, new Color(1, .84f, .4f));
        hud.description = HudText(panel.transform, "Description", new Vector2(.04f, .03f), new Vector2(.96f, .49f), 14, Color.white);
        hud.panel = panel; panel.SetActive(false); return hud;
    }
    private static TMP_Text HudText(Transform parent, string name, Vector2 min, Vector2 max, int size, Color color)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); g.transform.SetParent(parent, false); var r = (RectTransform)g.transform; r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        var text = g.GetComponent<TextMeshProUGUI>(); text.font = GameUIFont.Load(); text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
    }
    private static Transform Group(Transform parent, string name) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    private static GameObject Cube(Transform parent, string name, Vector3 p, Vector3 size, Material material)
    { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = size; Object.DestroyImmediate(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = material; return g; }
    private static Transform Instance(string path, Transform parent, string name)
    { var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (prefab == null) throw new FileNotFoundException(path); var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); g.name = name; return g.transform; }
    private static void Fit(GameObject g, float dimension, bool height)
    { var b = HighwayAssetImporter.BoundsOf(g); g.transform.localScale *= dimension / (height ? b.size.y : Mathf.Max(b.size.x, b.size.z)); b = HighwayAssetImporter.BoundsOf(g); g.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z); foreach (var c in g.GetComponentsInChildren<Collider>(true)) c.enabled = false; }
    private static void Label(Transform parent, string value, Vector3 position, float size, Color color)
    { var t = Group(parent, value); t.localPosition = position; t.localRotation = Quaternion.identity; var text = t.gameObject.AddComponent<TextMeshPro>(); text.font = GameUIFont.Load(); text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(42, 3); }
    private static void Materials()
    {
        asphalt = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/Asphalt.mat");
        paint = Material("WarmWhite", new Color(.87f, .88f, .83f)); green = Material("GuidanceGreen", new Color(.06f, .65f, .3f)); pink = Material("GuidancePink", new Color(.95f, .23f, .56f));
        cream = Material("FoodHallCream", new Color(.68f, .69f, .60f)); navy = Material("FoodHallNavy", new Color(.035f, .13f, .20f)); steel = Material("Steel", new Color(.43f, .52f, .55f)); amber = Material("WarningAmber", new Color(1, .38f, .045f));
    }
    private static Material Material(string name, Color color)
    { string path = Art + "/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color, enableInstancing = true }; m.SetFloat("_Smoothness", .2f); AssetDatabase.CreateAsset(m, path); } GeneratedStylizedSurface.Apply(m); return m; }
    private static object Save(Scene scene, object report)
    {
        foreach (var root in scene.GetRootGameObjects()) foreach (var component in root.GetComponentsInChildren<Component>(true)) if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed."); AssetDatabase.SaveAssets();
        File.WriteAllText(Record + "/" + scene.name + "-authoring.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented)); return report;
    }
}
