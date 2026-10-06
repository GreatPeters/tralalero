using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;

// Presentation only. Requires the metric tower fold. Does not modify route,
// encounters, rewards, hazard bodies, lift scripts/platforms, or crown objects.
public static class PolishChapters45TowerInterior
{
    const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity";
    const string Art = "Assets/ShooterSurvival/Models/Chapters/Chapters45/TowerInteriorV1";
    const string Prefabs = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    const string Record = "outputs/chapters45-2026-10-02";
    const string OwnedRoot = "TowerInterior_V1";
    static readonly float[] Heights = { 0, 95, 235 };
    static readonly string[] Names = { "01  LOBBY / RETAIL", "58  ARCHIVE / OBSERVATORY", "118  CROWN GALLERY" };
    static Chapter45Route route;
    static Chapter45Director director;
    static Chapter45Choice fork;
    static Transform root;
    static TMP_FontAsset font;
    static Material limestone, floorStone, ivory, ink, blue, brass, wood, teal, planting;
    static int clusters, reusedModels, meshCount;
    static readonly List<string> notes = new();

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Idle Edit Mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve dirty scenes first.");
        var setup = EditorSceneManager.GetSceneManagerSetup(); Scene scene = default; bool saved = false;
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath);
            director = Object.FindObjectsByType<Chapter45Director>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(d => d.gameObject.scene == scene && d.chapter == 5);
            route = director.route;
            var world = director.transform; var scenery = world.Find("Scenery");
            if (world.Find("TowerFold_Metric_V1") == null || scenery == null || route.segments.Length < 10) throw new InvalidOperationException("Apply the metric tower fold first.");
            string before = JsonUtility.ToJson(route);
            string encounterState = GameplayFingerprint();
            var crown = scenery.Find("InsideTheSneaker");
            if (crown == null) throw new InvalidOperationException("Existing crown showroom required.");
            var crownPosition = crown.position; var crownRotation = crown.rotation;
            Directory.CreateDirectory(Record); Directory.CreateDirectory(Art); AssetDatabase.Refresh();
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            string backup = Record + "/ShoeTower-before-interior-" + stamp + ".unity";
            File.Copy(ScenePath, backup, false);
            var previous = scenery.Find(OwnedRoot); if (previous != null) Object.DestroyImmediate(previous.gameObject);
            root = Group(scenery, OwnedRoot); clusters = reusedModels = meshCount = 0; notes.Clear();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");
            if (font == null) throw new FileNotFoundException("GmarketHarbor SDF font missing.");
            fork = director.GetComponentsInChildren<Chapter45Choice>(true).FirstOrDefault(c => c.kind == Chapter45Choice.ChoiceKind.RouteFork);
            MakeMaterials();
            // These renderers belong to the previous fold's plain architectural
            // pass. Their original support colliders remain enabled and intact.
            for (int floor = 0; floor < 3; floor++)
            {
                var old = scenery.Find("TowerFold_Floor_" + floor);
                if (old == null) throw new InvalidOperationException("Folded floor " + floor + " missing.");
                foreach (var renderer in old.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = false;
                BaseFloor(floor);
                DressFloor(floor);
            }
            LiftEntries();
            GroundExterior();
            if (before != JsonUtility.ToJson(route) || encounterState != GameplayFingerprint()) throw new InvalidOperationException("Presentation tool changed gameplay state.");
            if (crown.position != crownPosition || crown.rotation != crownRotation) throw new InvalidOperationException("Crown transform changed.");
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("ShoeTower save failed.");
            saved = true;
            var receipt = new Receipt
            {
                scene = ScenePath, backup = backup, clusters = clusters, reusedModels = reusedModels, meshes = meshCount,
                renderers = root.GetComponentsInChildren<Renderer>(true).Length,
                authoredTriangles = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(s => (int)f.sharedMesh.GetIndexCount(s) / 3)),
                gameplayUnchanged = true, notes = notes.ToArray()
            };
            File.WriteAllText(Record + "/tower-interior-" + stamp + ".json", JsonUtility.ToJson(receipt, true));
            return receipt;
        }
        finally
        {
            if (!saved && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
    [Serializable] sealed class Receipt
    { public string scene, backup; public int clusters, reusedModels, meshes, renderers, authoredTriangles; public bool gameplayUnchanged; public string[] notes; }
    static string GameplayFingerprint()
    {
        // Include serialized component state; renderer-only changes intentionally
        // do not enter this guard. Object references are stable during this tool.
        var types = new HashSet<Type> { typeof(Chapter45Encounter), typeof(Chapter45Target), typeof(Chapter45Hazard), typeof(Chapter45Choice), typeof(Chapter45Goal), typeof(Chapter45Lift) };
        return string.Join("\n", director.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && types.Contains(c.GetType())).OrderBy(c => c.GetInstanceID()).Select(c => JsonUtility.ToJson(c)));
    }
    static Material Mat(string name, Color color, float smooth = .18f)
    {
        string path = Art + "/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smooth); m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }
    static void MakeMaterials()
    {
        limestone = Mat("WarmLimestone", new Color(.64f, .62f, .55f));
        floorStone = Mat("CoolFloorStone", new Color(.40f, .45f, .46f));
        ivory = Mat("Porcelain", new Color(.87f, .86f, .78f), .32f);
        ink = Mat("Graphite", new Color(.055f, .085f, .11f));
        blue = Mat("RecessedBlueGlass", new Color(.15f, .29f, .35f), .48f);
        brass = Mat("ChampagneBrass", new Color(.59f, .43f, .22f), .42f);
        wood = Mat("SmokedTimber", new Color(.27f, .18f, .105f));
        teal = Mat("ArchiveTeal", new Color(.18f, .40f, .39f));
        planting = Mat("IndoorPlanting", new Color(.16f, .31f, .23f));
    }
    static Transform Group(Transform parent, string name)
    { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    static Transform Bay(string name, int floor, float from, float to, bool always = false)
    {
        var g = Group(root, name); var visibility = g.gameObject.AddComponent<Chapter45SceneryGroup>();
        visibility.floor = floor; visibility.startDistance = from; visibility.endDistance = to; visibility.alwaysVisible = always;
        return g;
    }
    static GameObject Box(Transform parent, string name, Vector3 local, Vector3 size, Material material, Quaternion? rotation = null)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
        g.transform.localPosition = local; g.transform.localRotation = rotation ?? Quaternion.identity; g.transform.localScale = size;
        Object.DestroyImmediate(g.GetComponent<Collider>()); var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = ShadowCastingMode.Off; return g;
    }
    static GameObject Cylinder(Transform parent, string name, Vector3 local, Vector3 size, Material material)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = local; g.transform.localScale = size;
        Object.DestroyImmediate(g.GetComponent<Collider>()); var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = ShadowCastingMode.Off; return g;
    }
    static TMP_Text Sign(Transform parent, string name, string text, Vector3 local, Vector2 size, float fontSize, Quaternion? rotation = null)
    {
        var g = new GameObject(name, typeof(TextMeshPro)); g.transform.SetParent(parent, false); g.transform.localPosition = local; g.transform.localRotation = rotation ?? Quaternion.identity;
        var t = g.GetComponent<TextMeshPro>(); t.font = font; t.text = text; t.fontSize = fontSize; t.color = new Color(.94f, .92f, .83f);
        t.alignment = TextAlignmentOptions.Center; t.rectTransform.sizeDelta = size; t.enableWordWrapping = false; t.outlineWidth = 0; return t;
    }
    static void SaveMesh(Mesh mesh, string name)
    {
        string path = Art + "/" + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); }
        meshCount++;
    }
    static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material[] materials)
    {
        SaveMesh(mesh, name); var asset = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "/" + name + ".asset");
        var g = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(parent, false);
        g.GetComponent<MeshFilter>().sharedMesh = asset; var r = g.GetComponent<MeshRenderer>(); r.sharedMaterials = materials; r.shadowCastingMode = ShadowCastingMode.Off; return g;
    }
    static List<Vector3> FloorPath(int floor)
    {
        var ss = route.segments.Where(s => s.floor == floor).ToArray(); var points = new List<Vector3> { ss[0].start };
        foreach (var s in ss) points.Add(s.end); return points;
    }
    static Mesh Ribbon(IReadOnlyList<Vector3> points, float[] bands, float raise)
    {
        int n = points.Count, width = bands.Length; var vertices = new Vector3[n * width]; var normals = new Vector3[vertices.Length];
        var uv = new Vector2[vertices.Length]; float along = 0;
        for (int i = 0; i < n; i++)
        {
            var previous = i == 0 ? (points[1] - points[0]).normalized : (points[i] - points[i - 1]).normalized;
            var next = i == n - 1 ? previous : (points[i + 1] - points[i]).normalized;
            var tangent = (previous + next).normalized; var right = Vector3.Cross(Vector3.up, tangent).normalized;
            float miter = 1 / Mathf.Max(.8f, Vector3.Dot(right, Vector3.Cross(Vector3.up, next).normalized));
            if (i > 0) along += Vector3.Distance(points[i - 1], points[i]);
            for (int b = 0; b < width; b++)
            { int k = i * width + b; vertices[k] = points[i] + right * bands[b] * miter + Vector3.up * raise; normals[k] = Vector3.up; uv[k] = new Vector2(bands[b], along); }
        }
        var mesh = new Mesh { name = "Continuous floor ribbon", indexFormat = IndexFormat.UInt32, vertices = vertices, normals = normals, uv = uv, subMeshCount = width - 1 };
        for (int band = 0; band < width - 1; band++)
        {
            var indices = new List<int>();
            for (int i = 0; i < n - 1; i++) { int a = i * width + band, b = a + width; indices.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            mesh.SetTriangles(indices, band);
        }
        mesh.RecalculateBounds(); return mesh;
    }
    static void BaseFloor(int floor)
    {
        float start = floor == 0 ? 0 : floor == 1 ? 530 : 1120, end = floor == 0 ? 530 : floor == 1 ? 1120 : 1650;
        var g = Bay("BaseArchitecture_" + floor, floor, start, end, true); float y = Heights[floor];
        Box(g, "SupportedFloorVisual", new Vector3(0, y - .2f, -5), new Vector3(160, .4f, 180), floor == 0 ? limestone : floorStone);
        // Broad modular stone bays make this a real floor, not an empty beige void.
        for (int x = -72; x <= 72; x += 12)
            Box(g, "StoneLongJoint", new Vector3(x, y + .006f, -5), new Vector3(.035f, .008f, 180), floorStone);
        for (int z = -89; z <= 79; z += 12)
            Box(g, "StoneCrossJoint", new Vector3(0, y + .007f, z), new Vector3(160, .008f, .035f), floorStone);
        var path = FloorPath(floor);
        MeshObject(g, "Floor" + floor + "_SeamlessRoute", Ribbon(path, new[] { -7f, -6.75f, -6.55f, -5.8f, 5.8f, 6.55f, 6.75f, 7f }, .035f),
            new[] { ink, brass, floorStone, ivory, floorStone, brass, ink });
        if (floor == 1 && fork != null)
        {
            // Begin after the two offset routes have genuinely separated. This
            // removes coplanar overlap at the branch approach and merge.
            for (int selection = 0; selection < 2; selection++)
            {
                var branch = new List<Vector3>(); float from = fork.distance + fork.transitionLength * .7f, to = fork.endDistance - fork.transitionLength * .7f;
                for (float d = from; d < to; d += 1.5f) { route.Sample(d, out var p, out var f); branch.Add(p + Vector3.Cross(Vector3.up, f) * fork.Offset(d, selection)); }
                route.Sample(to, out var last, out var lastForward); branch.Add(last + Vector3.Cross(Vector3.up, lastForward) * fork.Offset(to, selection));
                MeshObject(g, "Floor1_Branch" + selection, Ribbon(branch, new[] { -4f, -3.8f, 3.8f, 4f }, .09f), new[] { brass, selection == 0 ? teal : ivory, brass });
            }
        }
        WindowBays(g, floor);
        Merge(g, "Base" + floor);
    }
    static void WindowBays(Transform parent, int floor)
    {
        float y = Heights[floor];
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 9; i++)
            {
                float z = -83 + i * 19.5f;
                Box(parent, "DeepWindowPier", new Vector3(side * 77.4f, y + 4.8f, z), new Vector3(1.4f, 9.6f, 1), limestone);
                Box(parent, "PierInset", new Vector3(side * 76.6f, y + 4.8f, z), new Vector3(.16f, 8.4f, .42f), brass);
                if (i < 8) Box(parent, "RecessedLowerGlass", new Vector3(side * 78.6f, y + 2.9f, z + 9.75f), new Vector3(.25f, 3.4f, 18.5f), blue);
            }
            Box(parent, "WindowSeatSill", new Vector3(side * 77.3f, y + .6f, -5), new Vector3(1.6f, 1.2f, 176), ivory);
            Box(parent, "UpperGlazingHeader", new Vector3(side * 78, y + 10, -5), new Vector3(1.2f, .75f, 176), ink);
        }
        foreach (float z in new[] { -93f, 83f })
        {
            for (int i = 0; i <= 8; i++) Box(parent, "EndWindowPier", new Vector3(-77 + i * 19.25f, y + 4.8f, z), new Vector3(1.2f, 9.6f, 1.4f), limestone);
            Box(parent, "EndWindowSill", new Vector3(0, y + .6f, z), new Vector3(156, 1.2f, 1.5f), ivory);
            Box(parent, "EndWindowHeader", new Vector3(0, y + 10, z), new Vector3(156, .75f, 1.2f), ink);
        }
    }
    static bool ClearPocket(Vector3 center, float radius, int floor)
    {
        if (Mathf.Abs(center.x) + radius > 77 || center.z - radius < -91 || center.z + radius > 81) return false;
        float d = 0;
        foreach (var s in route.segments)
        {
            float next = d + s.Length;
            if (s.floor == floor)
            {
                var a = new Vector2(s.start.x, s.start.z); var b = new Vector2(s.end.x, s.end.z); var p = new Vector2(center.x, center.z); var v = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, v) / Mathf.Max(.001f, v.sqrMagnitude));
                float clearance = 7;
                if (floor == 1 && fork != null && next >= fork.distance - 10 && d <= fork.endDistance + 10) clearance = Mathf.Abs(fork.branchOffset) + fork.branchHalfWidth + 2;
                if (Vector2.Distance(p, a + v * t) < radius + clearance) return false;
            }
            d = next;
        }
        return true;
    }
    static void DressFloor(int floor)
    {
        float at = 0; int index = 0;
        foreach (var s in route.segments)
        {
            if (s.floor == floor && s.Length > 45)
            {
                foreach (float fraction in new[] { .30f, .66f })
                {
                    float d = at + s.Length * fraction;
                    if (floor == 2 && d > 1530) continue; // Existing hero crown owns the finale.
                    route.Sample(d, out var p, out var forward); var yaw = Quaternion.LookRotation(forward); var right = Vector3.Cross(Vector3.up, forward);
                    foreach (int side in new[] { -1, 1 })
                    {
                        var center = p + right * side * 16.5f;
                        if (!ClearPocket(center, 8.1f, floor)) continue;
                        var g = Bay("InteriorBay_" + floor + "_" + index, floor, d - 10, d + 10); g.SetPositionAndRotation(p, yaw);
                        bool reception = floor == 0 && index == 0;
                        if (reception) Reception(g, side);
                        else Storefront(g, side, floor, index);
                        Merge(g, "Bay" + floor + "_" + index); clusters++; index++;
                    }
                }
            }
            at += s.Length;
        }
        // A low, readable landing directory keeps the current deck legible even
        // when a room bay was rejected by the route-clearance check.
        var first = Array.FindIndex(route.segments, s => s.floor == floor);
        float station = route.SegmentStart(first) + 16;
        route.Sample(station, out var start, out var f);
        var directory = Bay("LandingDirectory_" + floor, floor, station - 20, station + 35);
        directory.SetPositionAndRotation(start, Quaternion.LookRotation(f));
        Box(directory, "DirectoryPlinth", new Vector3(9, 1.9f, 0), new Vector3(3.6f, 3.8f, .55f), ink);
        Box(directory, "DirectoryFrame", new Vector3(9, 3.75f, -.31f), new Vector3(3.6f, .10f, .08f), brass);
        Sign(directory, "DeckIdentity", Names[floor].Replace(" / ", "\n"), new Vector3(9, 2.35f, -.32f), new Vector2(3.25f, 2.2f), 3.4f);
        Sign(directory, "LiftDirection", "SKY LIFT  ↑", new Vector3(9, .6f, -.32f), new Vector2(3.2f, .7f), 2.4f);
        Merge(directory, "Directory" + floor);
    }
    static void Reception(Transform g, int side)
    {
        float x = side * 16.5f;
        Box(g, "LobbyStoneIsland", new Vector3(x, .12f, 0), new Vector3(8, .24f, 14), ink);
        Box(g, "ConciergeDesk", new Vector3(side * 13.8f, 1.1f, 0), new Vector3(2.2f, 2.2f, 8.5f), wood);
        Box(g, "FloatingMarbleCounter", new Vector3(side * 13.7f, 2.27f, 0), new Vector3(2.7f, .24f, 9), ivory);
        for (int i = 0; i < 17; i++) Box(g, "DeskFluting", new Vector3(side * 12.64f, 1.12f, -3.9f + i * .48f), new Vector3(.16f, 1.85f, .16f), brass);
        Box(g, "ReceptionBackdrop", new Vector3(side * 19.8f, 3.6f, 0), new Vector3(.55f, 7.2f, 13.5f), limestone);
        Box(g, "ConciergeNamePanel", new Vector3(side * 19.45f, 4.8f, 0), new Vector3(.10f, 2.4f, 10), ink);
        Sign(g, "ConciergeName", "SHOE TOWER\nCONCIERGE", new Vector3(side * 19.35f, 4.8f, 0), new Vector2(9.5f, 2.2f), 5, Quaternion.Euler(0, side * 90, 0));
        Model(g, "I05_order_kiosk", new Vector3(side * 13.9f, 2.4f, -2.6f), Quaternion.Euler(0, side * 90, 0), new Vector3(1.4f, 1.4f, 1.4f));
        Planter(g, new Vector3(x, 0, -5.1f)); Planter(g, new Vector3(x, 0, 5.1f));
        CeilingRaft(g, side);
    }
    static void Storefront(Transform g, int side, int floor, int index)
    {
        float x = side * 16.5f; bool cafe = floor == 0 && index % 3 == 1;
        Box(g, "RetailStoneInset", new Vector3(x, .08f, 0), new Vector3(8, .16f, 14), floor == 1 ? ink : ivory);
        Box(g, "RecessedShopBack", new Vector3(side * 20.1f, 3.25f, 0), new Vector3(.45f, 6.5f, 14), floor == 1 ? blue : wood);
        foreach (float z in new[] { -6.75f, 6.75f })
        {
            Box(g, "StorefrontStoneJamb", new Vector3(side * 12.8f, 3.55f, z), new Vector3(.65f, 7.1f, .65f), limestone);
            Box(g, "DisplayReturn", new Vector3(x, 2.7f, z), new Vector3(7.8f, 5.4f, .35f), blue);
        }
        Box(g, "StorefrontCanopy", new Vector3(x, 7.1f, 0), new Vector3(8.1f, .5f, 14.3f), ink);
        Box(g, "WarmHeaderLight", new Vector3(side * 12.65f, 6.7f, 0), new Vector3(.13f, .14f, 12.7f), ivory);
        string label = floor == 0 ? cafe ? "LOBBY CAFE" : "SOLE ATELIER" : floor == 1 ? index % 2 == 0 ? "ARCHIVE 58" : "SKY LOUNGE" : "CROWN COLLECTION";
        Sign(g, "ShopIdentity", label, new Vector3(side * 12.4f, 6.1f, 0), new Vector2(12.6f, 1.15f), 4.2f, Quaternion.Euler(0, side * 90, 0));
        if (cafe)
        {
            Model(g, "I03_food_counter", new Vector3(side * 17.8f, .18f, -2.8f), Quaternion.Euler(0, side * 90, 0), new Vector3(3.5f, 2.3f, 5.5f));
            Model(g, "I04_dining_set", new Vector3(side * 15.2f, .18f, 3.2f), Quaternion.identity, new Vector3(3.2f, 2.3f, 3.5f));
        }
        else if (floor == 1 && index % 2 == 1)
        {
            Model(g, "I04_dining_set", new Vector3(x, .18f, -2.8f), Quaternion.identity, new Vector3(5, 2.3f, 4));
            Model(g, "P08_suitcase", new Vector3(side * 13.8f, .18f, 3.6f), Quaternion.Euler(0, 25, 0), new Vector3(1.2f, 1.5f, 1));
            Planter(g, new Vector3(side * 18.4f, .18f, 4.2f));
        }
        else
        {
            foreach (float z in new[] { -3.5f, 3.5f })
            {
                Box(g, "ShoeExhibitPlinth", new Vector3(side * 16.2f, .85f, z), new Vector3(3.6f, 1.7f, 3.2f), ivory);
                Box(g, "DisplayRecessShadow", new Vector3(side * 16.2f, 1.72f, z), new Vector3(3.1f, .06f, 2.7f), ink);
                ShoeExhibit(g, new Vector3(side * 16.2f, 1.78f, z));
            }
        }
        CeilingRaft(g, side);
    }
    static void CeilingRaft(Transform g, int side)
    {
        Box(g, "RoomCeilingRaft", new Vector3(side * 17, 8.6f, 0), new Vector3(7.5f, .4f, 13.5f), limestone);
        for (int i = 0; i < 7; i++) Box(g, "CeilingTimberSlat", new Vector3(side * 17, 8.3f, -5.5f + i * 1.8f), new Vector3(6.9f, .18f, .32f), wood);
    }
    static void Planter(Transform g, Vector3 local)
    {
        Cylinder(g, "PlanterPot", local + Vector3.up * .6f, new Vector3(1.6f, .6f, 1.6f), limestone);
        Cylinder(g, "TopiaryLower", local + Vector3.up * 1.8f, new Vector3(1.7f, .6f, 1.7f), planting);
        Cylinder(g, "TopiaryUpper", local + Vector3.up * 2.65f, new Vector3(1.25f, .55f, 1.25f), planting);
    }
    static void Model(Transform parent, string name, Vector3 local, Quaternion rotation, Vector3 fit)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
        if (src == null) { notes.Add("Optional existing prop unavailable: " + name); return; }
        var g = Object.Instantiate(src); g.name = "ExistingGenerated_" + name; g.transform.SetParent(parent, false); g.transform.localPosition = local; g.transform.localRotation = rotation;
        foreach (var collider in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        var renderers = g.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) { Object.DestroyImmediate(g); return; }
        // Measure in the parent's authored local frame, including source pivots.
        Bounds bounds = LocalBounds(parent, renderers);
        float scale = Mathf.Min(fit.x / Mathf.Max(.01f, bounds.size.x), fit.y / Mathf.Max(.01f, bounds.size.y), fit.z / Mathf.Max(.01f, bounds.size.z));
        g.transform.localScale *= scale; bounds = LocalBounds(parent, renderers);
        g.transform.localPosition += local - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var r in renderers) r.shadowCastingMode = ShadowCastingMode.Off;
        reusedModels++;
    }
    static Bounds LocalBounds(Transform parent, Renderer[] renderers)
    {
        bool first = true; Bounds result = default;
        foreach (var r in renderers)
        {
            var b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                var p = parent.InverseTransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
                if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
            }
        }
        return result;
    }
    static void ShoeExhibit(Transform parent, Vector3 local)
    {
        string path = "Assets/ShooterSurvival/Models/Chapters/Chapters45/Crown/ShoeCrown_LOD2.fbx";
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (src == null) { notes.Add("Existing crown LOD2 unavailable; no fake shoe substitute inserted."); return; }
        var g = Object.Instantiate(src); g.name = "CrownStudyExhibit"; g.transform.SetParent(parent, false); g.transform.localPosition = local;
        var renderers = g.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) { Object.DestroyImmediate(g); return; }
        var bounds = LocalBounds(parent, renderers); g.transform.localScale *= 2.65f / Mathf.Max(bounds.size.x, bounds.size.z);
        bounds = LocalBounds(parent, renderers); g.transform.localPosition += local - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var r in renderers) { r.sharedMaterials = r.sharedMaterials.Select(m => m != null && m.name.Contains("Glazing") ? blue : m != null && m.name.Contains("Frames") ? brass : ivory).ToArray(); r.shadowCastingMode = ShadowCastingMode.Off; }
        foreach (var c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        reusedModels++;
    }
    static void LiftEntries()
    {
        int index = 0;
        foreach (var lift in director.GetComponentsInChildren<Chapter45Lift>(true).OrderBy(l => l.afterSegment))
        {
            float d = route.SegmentEnd(lift.afterSegment); int floor = route.segments[lift.afterSegment].floor;
            route.SampleSegment(lift.afterSegment, d, out var p, out var forward);
            var g = Bay("LiftPortal_" + index, floor, d - 70, d + 8); g.SetPositionAndRotation(p, Quaternion.LookRotation(forward));
            foreach (int side in new[] { -1, 1 })
            {
                Box(g, "LiftStoneJamb", new Vector3(side * 7, 4.1f, 1.9f), new Vector3(1.1f, 8.2f, 1.6f), ivory);
                Box(g, "LiftBrassReveal", new Vector3(side * 6.36f, 4.1f, 1), new Vector3(.16f, 7.9f, .18f), brass);
                Box(g, "LiftServiceReturn", new Vector3(side * 10.7f, 3.3f, 2.2f), new Vector3(6, 6.6f, .8f), blue);
            }
            Box(g, "LiftHeader", new Vector3(0, 8.2f, 1.9f), new Vector3(15.1f, 1.35f, 1.6f), ink);
            Box(g, "LiftHeaderStrip", new Vector3(0, 7.49f, .94f), new Vector3(13, .11f, .12f), brass);
            Sign(g, "LiftDestination", index == 0 ? "SKY LIFT  ↑  58F" : "CROWN LIFT  ↑  118F", new Vector3(0, 8.2f, 1.03f), new Vector2(13.6f, 1.15f), 5.4f);
            Box(g, "LiftArrivalMat", new Vector3(0, .04f, -7), new Vector3(11.5f, .04f, 5.5f), ink);
            for (int i = 0; i < 3; i++) Box(g, "BoardingGuidance", new Vector3(0, .075f, -8.8f + i * 1.7f), new Vector3(2.2f, .02f, .16f), brass);
            Merge(g, "Lift" + index); index++;
        }
    }
    static void GroundExterior()
    {
        var g = Bay("GroundedExterior", -1, 0, 0, true);
        Box(g, "ContinuousCityGround", new Vector3(0, -2.5f, 0), new Vector3(1500, 1, 1500), floorStone);
        Box(g, "TowerPodiumSurround", new Vector3(0, -1.8f, -5), new Vector3(240, .4f, 260), limestone);
        foreach (int side in new[] { -1, 1 })
        {
            Box(g, "PerimeterAvenue", new Vector3(side * 137, -1.55f, -5), new Vector3(19, .05f, 500), ink);
            for (int i = 0; i < 18; i++) Box(g, "AvenueMedian", new Vector3(side * 137, -1.48f, -210 + i * 24), new Vector3(.25f, .025f, 10), ivory);
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3(side * 112, -1.55f, -90 + i * 27);
                Cylinder(g, "PodiumPlanter", p + Vector3.up * .7f, new Vector3(5, .7f, 5), limestone);
                Cylinder(g, "PodiumTreeCrown", p + Vector3.up * 4.2f, new Vector3(5.2f, 2.2f, 5.2f), planting);
            }
        }
        Merge(g, "Exterior");
    }
    static void Merge(Transform parent, string key)
    {
        // Imported meshes with Read/Write disabled stay as shared imported meshes;
        // never change import settings or discard them after a failed combine.
        var filters = parent.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null && f.sharedMesh.isReadable && f.GetComponent<MeshRenderer>() != null && f.GetComponentInParent<LODGroup>() == null && f.GetComponent<TMP_Text>() == null).ToArray();
        var batches = new Dictionary<Material, List<CombineInstance>>();
        foreach (var f in filters)
        {
            var materials = f.GetComponent<MeshRenderer>().sharedMaterials;
            for (int sub = 0; sub < Mathf.Min(f.sharedMesh.subMeshCount, materials.Length); sub++)
            {
                var material = materials[sub]; if (material == null) continue;
                if (!batches.TryGetValue(material, out var list)) batches.Add(material, list = new List<CombineInstance>());
                list.Add(new CombineInstance { mesh = f.sharedMesh, subMeshIndex = sub, transform = parent.worldToLocalMatrix * f.transform.localToWorldMatrix });
            }
        }
        int index = 0;
        foreach (var batch in batches)
        {
            var mesh = new Mesh { name = "TowerInterior " + key + " " + index, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(batch.Value.ToArray(), true, true); mesh.RecalculateBounds();
            MeshObject(parent, key + "_Combined_" + index, mesh, new[] { batch.Key }); index++;
        }
        foreach (var f in filters) { Object.DestroyImmediate(f.GetComponent<MeshRenderer>()); Object.DestroyImmediate(f); }
    }
}
