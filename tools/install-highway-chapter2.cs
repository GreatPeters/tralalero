using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

public static class InstallHighwayChapter2
{
    [Serializable] public sealed class Baseline
    {
        public Vector3[] centres;
        public float length;
        public Vector3 cameraPosition, playerPosition, visualScale, canvasPosition, canvasScale;
    }
    const string Record = "outputs/highway-chapter2-2026-09-27";
    const string Prefabs = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/";
    const string AssetsRoot = "Assets/ShooterSurvival/Models/Highway/Chapter2_20260927";
    const string RootName = "HighwayChapter2_20260927";
    static string Json(object o) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { o });
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    static float[] V(Vector3 p) => new[] { p.x, p.y, p.z };
    static float S(string key) => HighwayChapter2Data.Value(key);
    static void Dirty(Object value)
    {
        EditorUtility.SetDirty(value);
        if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }
    static Transform Group(Transform parent, string name)
    {
        var existing = parent.Find(name); if (existing != null) return existing;
        var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "Chapter2 Highway"); go.transform.SetParent(parent, false); return go.transform;
    }
    static RectTransform RectGroup(Transform parent, string name)
    {
        var existing = parent.Find(name); if (existing != null) return existing.GetComponent<RectTransform>();
        var go = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "Chapter2 UI"); go.transform.SetParent(parent, false); return go.GetComponent<RectTransform>();
    }
    static void Disable(GameObject go)
    {
        if (!go.activeSelf) return; Undo.RecordObject(go, "Retire prior Highway layout"); go.SetActive(false); Dirty(go);
    }
    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/" + name + ".mat");
    static Material White => AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/ApprovedRoadConcepts/RoadWhite.mat");
    static Material Yellow => AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Chapters/ApprovedRoadConcepts/CenterYellow.mat");
    static Material AsphaltWithoutSeams()
    {
        string path=AssetsRoot+"/AsphaltNoSeams.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Mat("Asphalt")){name="Asphalt without internal seams"};AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_OutlineEnabled",0);material.SetFloat("_OutlineWidth",0);material.DisableKeyword("DR_OUTLINE_ON");Dirty(material);return material;
    }
    public static object RefineRoadSurfaces()
    {
        var route=Route();var material=AsphaltWithoutSeams();int count=0;
        foreach(var renderer in route.transform.Find("Roads/Chapter2Roads").GetComponentsInChildren<MeshRenderer>())
        {
            if(renderer.sharedMaterial!=null&&(renderer.sharedMaterial.name=="Asphalt"||renderer.sharedMaterial==material))
            {renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Dirty(renderer);count++;}
        }
        foreach(string name in new[]{"ForkRed","ForkGreen"})
        {
            var stripe=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/"+name+".mat");if(stripe==null)continue;
            if(stripe.HasProperty("_OutlineWidth"))stripe.SetFloat("_OutlineWidth",0);Dirty(stripe);
        }
        Save();return new{surfaces=count,internalOutlines=false};
    }
    static void Progress(string stage, int done, int total) => File.WriteAllText(Record + "/install-progress.json", Json(new { stage, done, total }));
    static void Save()
    {
        Undo.FlushUndoRecordObjects(); AssetDatabase.SaveAssets();
        var scene = SceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("HighWay save failed");
    }
    static HighwayRoute Route()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "HighWay") throw new Exception("HighWay Edit Mode required");
        return Object.FindFirstObjectByType<HighwayRoute>();
    }
    public static object Inspect()
    {
        var route = Route(); var map = route.transform; var player = Object.FindFirstObjectByType<PlayerScript>();
        var actors = map.GetComponentsInChildren<EnemyScript_space>(true);
        var source = new SerializedObject(actors[0]).FindProperty("enemyData").objectReferenceValue;
        var result = new
        {
            route.length, centres = route.centers.Select(V).ToArray(), roots = map.Cast<Transform>().Select(t => new { t.name, active = t.gameObject.activeSelf, count = t.childCount }).ToArray(),
            enemies = actors.Select(a => new { a.name, path = PathOf(a.transform) }).ToArray(),
            enemyData = AssetDatabase.GetAssetPath(source),
            bonuses = map.GetComponentsInChildren<BonusWallChoicePair>(true).Select(b => new { path = PathOf(b.transform), left = b.Left.name, right = b.Right.name, position = V(b.transform.position) }).ToArray(),
            player = new { path = PathOf(player.transform), position = V(player.transform.position), scale = V(player.transform.localScale), children = player.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Original" || t.name.ToLower().Contains("health")).Select(t => new { path = PathOf(t), scale = V(t.localScale), position = V(t.localPosition) }).ToArray() },
            camera = new { position = V(Camera.main.transform.position), rotation = V(Camera.main.transform.eulerAngles), Camera.main.fieldOfView },
            images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(i => i.sprite != null).Select(i => new { path = PathOf(i.transform), sprite = AssetDatabase.GetAssetPath(i.sprite), name = i.sprite.name }).ToArray(),
            finish = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b != null && (b.GetType().Name.Contains("Finish") || b.GetType().Name.Contains("Complete") || b.GetType().Name.Contains("EndChapter"))).Select(b => new { type = b.GetType().Name, path = PathOf(b.transform), position = V(b.transform.position) }).ToArray()
        };
        string file = Record + "/before/scene-inspection.json";
        if (!File.Exists(file)) File.WriteAllText(file, Json(result));
        string baseline = Record + "/before/native-baseline.json";
        if (!File.Exists(baseline))
        {
            var visual = player.transform.Find("Original"); var canvas = player.transform.Find("Canvas");
            File.WriteAllText(baseline, JsonUtility.ToJson(new Baseline { centres = route.centers, length = route.length,
                playerPosition = player.transform.position, cameraPosition = Camera.main.transform.position,
                visualScale = visual.localScale, canvasPosition = canvas.localPosition, canvasScale = canvas.localScale }, true));
        }
        return new { enemies = actors.Length, bonuses = map.GetComponentsInChildren<BonusWallChoicePair>(true).Length, source = AssetDatabase.GetAssetPath(source), file };
    }

    static void Quad(HighwayRoute route, bool branch, float from, float to, float left, float right, float height,
        List<Vector3> vertices, List<Vector2> uv, List<int> indices)
    {
        RoadSample(route, from, branch, out var a, out var af); RoadSample(route, to, branch, out var b, out var bf);
        var ar = Vector3.Cross(Vector3.up, af); var br = Vector3.Cross(Vector3.up, bf); int index = vertices.Count;
        vertices.Add(a + ar * left + Vector3.up * height); vertices.Add(a + ar * right + Vector3.up * height);
        vertices.Add(b + br * left + Vector3.up * height); vertices.Add(b + br * right + Vector3.up * height);
        uv.Add(new Vector2(left / 5, from / 5)); uv.Add(new Vector2(right / 5, from / 5));
        uv.Add(new Vector2(left / 5, to / 5)); uv.Add(new Vector2(right / 5, to / 5));
        indices.AddRange(new[] { index, index + 2, index + 1, index + 1, index + 2, index + 3 });
    }
    static void RoadSample(HighwayRoute route, float d, bool branch, out Vector3 point, out Vector3 forward)
    {
        route.Sample(Mathf.Clamp(d, 0, route.length), branch, out point, out forward);
        if (d < 0) point += forward * d;
        else if (d > route.length) point += forward * (d - route.length);
    }
    static Mesh StoreMesh(string name, List<Vector3> vertices, List<Vector2> uv, List<int> indices)
    {
        var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = AssetsRoot + "/" + name + ".asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); AssetDatabase.SaveAssetIfDirty(saved); return saved;
    }
    static void MeshObject(Transform parent, string name, Mesh mesh, Material material, bool collider)
    {
        var t = Group(parent, name); t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var filter = t.GetComponent<MeshFilter>(); if (filter == null) filter = t.gameObject.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
        var renderer = t.GetComponent<MeshRenderer>(); if (renderer == null) renderer = t.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        if (!collider) { renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false; }
        else { var shape = t.GetComponent<MeshCollider>(); if (shape == null) shape = t.gameObject.AddComponent<MeshCollider>(); shape.sharedMesh = mesh; }
        t.gameObject.isStatic = true; Dirty(t.gameObject);
    }
    static void Surface(Transform parent, HighwayRoute route, string name, float from, float to, float left, float right, bool branch)
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        for (float d = from; d < to; d += S("meshStep")) Quad(route, branch, d, Mathf.Min(to, d + S("meshStep")), left, right, branch ? .002f : 0, vertices, uv, indices);
        MeshObject(parent, name, StoreMesh(name, vertices, uv, indices), AsphaltWithoutSeams(), true);
    }
    static void Paint(Transform parent, HighwayRoute route, string name, float from, float to, bool branch, bool yellow)
    {
        float w = S("laneWidth"), edge = w * 1.5f;
        float oppositeNear = -edge - S("medianShoulder") * 2 - S("medianWidth");
        var continuous = yellow ? new[] { -edge, oppositeNear } : branch ? new[] { -edge, edge } : new[] { edge, oppositeNear - w * 3 };
        var dashed = yellow ? Array.Empty<float>() : branch ? new[] { -w * .5f, w * .5f } : new[] { -w * .5f, w * .5f, oppositeNear - w, oppositeNear - w * 2 };
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>(); float half = S("paintWidth") * .5f;
        foreach (float lane in continuous)
            for (float d = from; d < to; d += S("meshStep"))
                if (yellow || PaintVisible(route, d + S("meshStep") * .5f, branch, lane, Mathf.Approximately(lane, edge)))
                    Quad(route, branch, d, Mathf.Min(to, d + S("meshStep")), lane - half, lane + half, branch ? .055f : .05f, vertices, uv, indices);
        float interval = S("dashPaint") + S("dashGap");
        foreach (float lane in dashed)
            for (float start = Mathf.Floor(from / interval) * interval; start < to; start += interval)
                for (float d = Mathf.Max(start, from); d < Mathf.Min(to, start + S("dashPaint")); d += S("meshStep"))
                    if (PaintVisible(route, d + S("meshStep") * .5f, branch, lane, false))
                        Quad(route, branch, d, Mathf.Min(to, Mathf.Min(start + S("dashPaint"), d + S("meshStep"))), lane - half, lane + half, branch ? .055f : .05f, vertices, uv, indices);
        MeshObject(parent, name, StoreMesh(name, vertices, uv, indices), yellow ? Yellow : White, false);
    }
    static bool PaintVisible(HighwayRoute route, float distance, bool branch, float lane, bool outer)
    {
        route.Sample(distance, false, out var main, out var forward);
        route.Sample(distance, true, out var split, out var splitForward);
        var right=Vector3.Cross(Vector3.up, forward);
        float offset=Vector3.Dot(split-main,right);
        float lateral=Vector3.Dot(split+Vector3.Cross(Vector3.up,splitForward)*lane-main,right);
        return HighwayChapter2Rules.ShowForkPaint(branch,lateral,offset,S("laneWidth")*1.5f,outer);
    }
    public static object RefreshForkPaint()
    {
        var route=Route();EnvironmentVariableTables.Reload();var surfaces=route.transform.Find("Roads/Chapter2Roads");
        int part=0;
        for(float d=0;d<route.length;d+=S("meshSegment"))Paint(surfaces,route,"White_"+part++,d,Mathf.Min(route.length,d+S("meshSegment")),false,false);
        for(int i=0;i<route.forks.Length;i++)
        {
            var fork=route.forks[i];part=0;
            for(float d=fork.start;d<fork.end;d+=S("meshSegment"))Paint(surfaces,route,"BranchPaint"+i+"_"+part++,d,Mathf.Min(fork.end,d+S("meshSegment")),true,false);
            foreach(bool branch in new[]{false,true})
            {
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                float lane=branch?S("laneWidth"):-S("laneWidth"),half=S("guideWidth")*.5f;
                float end=Mathf.Min(route.length,fork.end+S("guideAfter"));
                for(float d=fork.start-S("guideAhead");d<end;d+=S("meshStep"))Quad(route,branch,d,Mathf.Min(end,d+S("meshStep")),lane-half,lane+half,.065f,vertices,uv,triangles);
                string name="ForkGuide_"+i+"_"+(branch?"RedRight":"GreenLeft");
                MeshObject(surfaces,name,StoreMesh(name,vertices,uv,triangles),ColorMaterial(branch?"ForkRed":"ForkGreen",branch?new Color(.85f,.06f,.08f):new Color(.04f,.68f,.30f)),false);
            }
        }
        Save();return new{forks=route.forks.Length,guideColors="left green / right red",overlapPaintClipped=true};
    }
    public static object RefreshForkGeometry()
    {
        var route=Route();EnvironmentVariableTables.Reload();Undo.RecordObject(route,"Smooth Highway split");
        route.forks[0].transitionLength=S("branchTransition");Dirty(route);
        var surfaces=route.transform.Find("Roads/Chapter2Roads");float edge=S("laneWidth")*1.5f;
        for(int i=0;i<route.forks.Length;i++)
        {
            var fork=route.forks[i];int part=0;
            for(float d=fork.start;d<fork.end;d+=S("meshSegment"))
                Surface(surfaces,route,"Branch"+i+"_"+part++,d,Mathf.Min(fork.end,d+S("meshSegment")),-edge-S("outerShoulder"),edge+S("outerShoulder"),true);
        }
        Save();return new{transition=route.forks[0].transitionLength};
    }
    public static object ClearRoadsideWalls()
    {
        var route=Route();var scenery=route.transform.Find(RootName+"/Korean city scenery");
        var roads=route.transform.Find("Roads/Chapter2Roads").GetComponentsInChildren<MeshCollider>();Physics.SyncTransforms();int moved=0,maxShift=0;
        foreach(Transform wall in scenery)
        {
            if(!wall.name.StartsWith("Sound wall "))continue;
            var parts=wall.name.Substring(11).Split('_');int side=int.Parse(parts[0]),index=int.Parse(parts[1]);float d=15+index*22;
            bool branch=side>0&&HasBranch(route,d);float lane=side<0?-25:9.3f;
            bool intersects=true;int attempt=0;
            while(intersects&&attempt<32)
            {
                Place(route,wall,d,lane+side*attempt,branch);intersects=false;
                for(int j=-5;j<=5&&!intersects;j++)
                {
                    var p=wall.TransformPoint(new Vector3(0,0,j*2.15f));
                    foreach(var road in roads)
                    {
                        var b=road.bounds;if(p.x<b.min.x||p.x>b.max.x||p.z<b.min.z||p.z>b.max.z)continue;
                        if(road.Raycast(new Ray(p+Vector3.up*10,Vector3.down),out _,20)){intersects=true;break;}
                    }
                }
                if(intersects)attempt++;
            }
            if(intersects)throw new Exception("Unable to clear wall "+wall.name);
            if(attempt>0)moved++;maxShift=Mathf.Max(maxShift,attempt);
        }
        Save();return new{wallsMoved=moved,maxShift};
    }
    static void Median(Transform parent, HighwayRoute route, string name, float from, float to)
    {
        float half = S("medianWidth") * .5f, height = S("medianHeight"), lane = -S("laneWidth") * 1.5f - S("medianShoulder") - half;
        var cross = new[] { new Vector2(-half, .025f), new Vector2(-half * .45f, height * .55f), new Vector2(-half * .45f, height), new Vector2(half * .45f, height), new Vector2(half, .025f) };
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        int steps = Mathf.CeilToInt((to - from) / S("meshStep"));
        for (int i = 0; i <= steps; i++)
        {
            float d = Mathf.Lerp(from, to, i / (float)steps); RoadSample(route, d, false, out var p, out var f); var side = Vector3.Cross(Vector3.up, f);
            foreach (var point in cross) { vertices.Add(p + side * (lane + point.x) + Vector3.up * point.y); uv.Add(new Vector2(d / 4, point.y)); }
            if (i == 0) continue;
            for (int j = 0; j < cross.Length - 1; j++) { int a = (i - 1) * cross.Length + j, b = i * cross.Length + j; indices.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
        }
        MeshObject(parent, name, StoreMesh(name, vertices, uv, indices), Mat("Concrete"), true);
    }
    public static object Geometry()
    {
        var route = Route(); var scene = SceneManager.GetActiveScene(); if (scene.isDirty) throw new Exception("Clean scene required");
        if (!File.Exists(Record + "/before/native-baseline.json")) throw new Exception("Inspect/backup first");
        EnvironmentVariableTables.Reload();
        var baseline = JsonUtility.FromJson<Baseline>(File.ReadAllText(Record + "/before/native-baseline.json"));
        Undo.RecordObject(route, "Chapter2 route"); route.centers = baseline.centres; route.length = baseline.length;
        int count = Mathf.RoundToInt(S("length") / S("meshStep")) + 1;
        var samples = new Vector3[count]; for (int i = 0; i < count; i++) samples[i] = route.Point(i * S("meshStep"));
        route.length = S("length"); route.centers = samples; route.popupBranches = true;
        route.forks = new[] { new HighwayRoute.Fork { start = S("forkAt"), end = S("mergeAt"), offset = S("branchOffset"), transitionLength = S("branchTransition") },
            new HighwayRoute.Fork { start = S("tollAt"), end = S("tollMerge"), offset = S("tollBranchOffset") } };
        route.hud?.Clear(route); Dirty(route);
        var map = route.transform; var root = Group(map, RootName); var roads = map.Find("Roads");
        foreach (Transform child in roads) if (child.name != "Chapter2Roads") Disable(child.gameObject);
        foreach (Transform child in map)
            if (child.name != "Roads" && child.name != "Enemies" && child.name != "Bonuses" && child.name != "Highway_StageExit" && child.name != RootName) Disable(child.gameObject);
        foreach (var lanes in map.GetComponents<HighwayEncounterLanes>()) { Undo.RecordObject(lanes, "Retire forced pedestrian passages"); lanes.enabled = false; Dirty(lanes); }
        Directory.CreateDirectory(AssetsRoot); AssetDatabase.Refresh();
        var surfaces = Group(roads, "Chapter2Roads"); surfaces.gameObject.SetActive(true);
        float width = S("laneWidth"), edge = width * 1.5f, near = -edge - S("medianShoulder") * 2 - S("medianWidth");
        int segment = 0, total = Mathf.CeilToInt(route.length / S("meshSegment"));
        for (float d = 0; d < route.length; d += S("meshSegment"))
        {
            float end = Mathf.Min(route.length, d + S("meshSegment"));
            Surface(surfaces, route, "Main_" + segment, d, end, -edge - S("medianShoulder"), edge + S("outerShoulder"), false);
            Surface(surfaces, route, "Opposite_" + segment, d, end, near - width * 3 - S("outerShoulder"), near + S("medianShoulder"), false);
            Paint(surfaces, route, "White_" + segment, d, end, false, false); Paint(surfaces, route, "Yellow_" + segment, d, end, false, true);
            Median(surfaces, route, "Median_" + segment, d, end); segment++; Progress("geometry", segment, total);
        }
        for (int i = 0; i < route.forks.Length; i++)
        {
            var fork = route.forks[i]; int part = 0;
            for (float d = fork.start; d < fork.end; d += S("meshSegment"))
            {
                float end = Mathf.Min(fork.end, d + S("meshSegment"));
                Surface(surfaces, route, "Branch" + i + "_" + part, d, end, -edge - S("outerShoulder"), edge + S("outerShoulder"), true);
                Paint(surfaces, route, "BranchPaint" + i + "_" + part, d, end, true, false);
                part++;
            }
        }
        var player = Object.FindFirstObjectByType<PlayerScript>(); var visual = player.transform.Find("Original"); var canvas = player.transform.Find("Canvas");
        Undo.RecordObject(visual, "Highway shark0.8"); visual.localScale = baseline.visualScale * S("sharkScale"); Dirty(visual);
        Undo.RecordObject(canvas, "Highway overhead health height"); canvas.localPosition = baseline.canvasPosition * S("sharkScale"); canvas.localScale = baseline.canvasScale * S("sharkScale"); Dirty(canvas);
        var camera = Camera.main; Undo.RecordObject(camera.transform, "Highway camera15percent"); camera.transform.position = baseline.playerPosition + (baseline.cameraPosition - baseline.playerPosition) * S("cameraScale"); Dirty(camera.transform);
        var stable = camera.GetComponent<StableGameplayCamera>(); if (stable != null) { Undo.RecordObject(stable, "Highway camera offset"); stable.Configure(player.transform); Dirty(stable); }
        var exit = map.Find("Highway_StageExit"); route.Sample(route.length - 12, false, out var ep, out var ef);
        Undo.RecordObject(exit, "Chapter2 finish position"); exit.SetPositionAndRotation(ep, Quaternion.LookRotation(ef)); Dirty(exit);
        Save(); Progress("geometry-complete", segment, total);
        return new { route.length, laneWidth = width, meshes = surfaces.GetComponentsInChildren<MeshFilter>().Length };
    }
    static Bounds LocalBounds(Transform root)
    {
        bool first = true; var result = new Bounds();
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            var bounds = renderer.localBounds; var matrix = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = matrix.MultiplyPoint3x4(corner);
                if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
            }
        }
        if (first) throw new Exception("No native model geometry in " + root.name);
        return result;
    }
    static Transform Model(Transform parent, string name, string prefabPath, float size, bool fitHeight = false)
    {
        var holder = Group(parent, name);
        if (holder.childCount > 0) return holder;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); if (prefab == null) throw new Exception("Missing prefab " + prefabPath);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder); model.SetActive(true);
        foreach (var c in model.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Dirty(c); }
        foreach (var r in model.GetComponentsInChildren<Renderer>(true)) { r.enabled = true; r.forceRenderingOff = false; Dirty(r); }
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) { t.gameObject.isStatic = false; Dirty(t.gameObject); }
        var bounds = LocalBounds(holder); model.transform.localScale *= size / (fitHeight ? bounds.size.y : bounds.size.x);
        bounds = LocalBounds(holder); model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Dirty(model.transform); return holder;
    }
    static string VehiclePrefab(HighwayVehicleKind kind) => kind switch
    {
        HighwayVehicleKind.Taxi => "V07_taxi", HighwayVehicleKind.OneTon => "V04_1ton_truck", HighwayVehicleKind.Box => "V08_box_truck",
        HighwayVehicleKind.Tanker => "V11_tanker", HighwayVehicleKind.Bus => "V05_tour_bus", HighwayVehicleKind.Police => "V02_white_sedan",
        HighwayVehicleKind.Tow => "V09_tow_truck", _ => "V02_white_sedan"
    };
    static Transform Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var t = parent.Find(name);
        if (t == null) { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; Undo.RegisterCreatedObjectUndo(go, "Chapter2 detail"); t = go.transform; t.SetParent(parent, false); Object.DestroyImmediate(go.GetComponent<Collider>()); }
        t.localPosition = position; t.localScale = size; t.GetComponent<Renderer>().sharedMaterial = material; return t;
    }
    static Material ColorMaterial(string name, Color color)
    {
        string path = AssetsRoot + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/JH/Model/Enemy/Garden_Spear/Material.001.mat")) { name = name };
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        foreach (string property in new[] { "_ColorDim", "_ColorDimSteps", "_ColorDimCurve", "_ColorDimExtra", "_ColorGradient", "_UnityShadowColor" })
            if (material.HasProperty(property)) material.SetColor(property, new Color(color.r * .72f, color.g * .72f, color.b * .72f, 1));
        AssetDatabase.CreateAsset(material, path); return material;
    }
    static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");
    static TextMeshPro WorldText(Transform parent, string name, string label, Vector3 position, Vector2 size, float fontSize, Color color)
    {
        var t = RectGroup(parent, name); var text = t.GetComponent<TextMeshPro>(); if (text == null) text = t.gameObject.AddComponent<TextMeshPro>();
        text.font = Font; text.text = label; text.fontSize = fontSize; text.color = color; text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = size; t.localPosition = position; text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
    static void Triangle(Transform parent, float front)
    {
        var root = Group(parent, "Red emergency triangle"); root.localPosition = new Vector3(0, .1f, front + 1);
        var red = ColorMaterial("EmergencyRed", new Color(.9f, .045f, .02f));
        var dark = ColorMaterial("RubberBlack", new Color(.035f, .04f, .04f));
        var white = ColorMaterial("ReflectorWhite", new Color(1, .96f, .8f));
        Cube(root, "Base", new Vector3(0, 0, 0), new Vector3(1.35f, .15f, .4f), dark);
        Cube(root, "Bottom reflector", new Vector3(0, .13f, 0), new Vector3(1.15f, .13f, .1f), red);
        var a = Cube(root, "Left reflector", new Vector3(-.29f, .6f, 0), new Vector3(.13f, 1.1f, .1f), red); a.localRotation = Quaternion.Euler(0, 0, -30);
        var b = Cube(root, "Right reflector", new Vector3(.29f, .6f, 0), new Vector3(.13f, 1.1f, .1f), red); b.localRotation = Quaternion.Euler(0, 0, 30);
        Cube(root, "Reflective centre", new Vector3(0, .35f, .03f), new Vector3(.38f, .14f, .06f), white);
    }
    static TextMeshProUGUI HealthNumber(Transform actor, Bounds body)
    {
        var canvasRoot = RectGroup(actor, "Vehicle health"); var canvas = canvasRoot.GetComponent<Canvas>(); if (canvas == null) canvas = canvasRoot.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvasRoot.localPosition = new Vector3(0, body.max.y + .75f, 0); canvasRoot.localScale = Vector3.one * .008f;
        var rect = canvasRoot.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(600, 140);
        var textRoot = RectGroup(canvasRoot, "Remaining health"); var text = textRoot.GetComponent<TextMeshProUGUI>(); if (text == null) text = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = Font; text.fontSize = 95; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(600, 140); text.text = "0";
        string path = AssetsRoot + "/VehicleNumber.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Font.material); material.SetFloat("_OutlineWidth", .16f); material.SetColor("_OutlineColor", Color.black); material.EnableKeyword("OUTLINE_ON");
            AssetDatabase.CreateAsset(material, path);
        }
        text.fontSharedMaterial = material;
        return text;
    }
    static void VehicleEffects(HighwayVehicleEnemy vehicle)
    {
        string path = AssetsRoot + "/SpeedBlur.mat"; var blur = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (blur == null)
        {
            blur = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Speed blur", renderQueue = 3000 };
            blur.SetFloat("_Surface", 1); blur.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            blur.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); blur.SetFloat("_ZWrite", 0);
            blur.SetColor("_BaseColor", new Color(.72f, .9f, 1, .10f)); blur.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); blur.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(blur, path);
        }
        vehicle.blurMaterial = blur;
        var trails = new List<TrailRenderer>();
        foreach (float x in new[] { -1.8f, 1.8f })
        {
            var t = Group(vehicle.body, "Air streak " + x); t.localPosition = new Vector3(x, 1, -vehicle.footprint.y * .6f);
            var trail = t.GetComponent<TrailRenderer>(); if (trail == null) trail = t.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = blur; trail.time = .16f; trail.startWidth = .25f; trail.endWidth = 0; trail.emitting = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false; trails.Add(trail);
        }
        vehicle.speedTrails = trails.ToArray();
        if (vehicle.kind == HighwayVehicleKind.Accident)
        {
            var smoke = vehicle.body.Find("Faint engine smoke");
            if (smoke == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Smoke Source 3D.prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, vehicle.body); go.name = "Faint engine smoke"; smoke = go.transform;
                var bounds = LocalBounds(vehicle.body); smoke.localPosition = new Vector3(0, bounds.max.y * .65f, bounds.max.z - .6f); smoke.localScale = Vector3.one * .18f;
                foreach (var script in go.GetComponentsInChildren<MonoBehaviour>(true)) { script.enabled = false; Dirty(script); }
            }
            vehicle.idleSmoke = smoke.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particle in vehicle.idleSmoke)
            {
                var main = particle.main; main.loop = true; main.startLifetime = S("smokeSeconds"); main.maxParticles = 12;
                main.startColor = new Color(.25f, .25f, .25f, .22f);
                var emission = particle.emission; emission.rateOverTime = 3; Dirty(particle);
            }
        }
        Dirty(vehicle);
    }
    public static object Actors() => BuildActors(false);
    public static object RefreshActors() => BuildActors(true);
    static object BuildActors(bool replace)
    {
        var route = Route(); if (SceneManager.GetActiveScene().isDirty) throw new Exception("Clean scene required");
        EnvironmentVariableTables.Reload(); EncounterPlacementTables.Reload();
        var rows = EncounterPlacementTables.Rows.Where(r => r.scene == "HighWay" && r.hasHighwayVehicle).ToArray();
        if (rows.Length == 0) throw new Exception("Apply chapter2 workbook first");
        var map = route.transform; var parent = map.Find("Enemies");
        foreach (Transform t in parent.Cast<Transform>().ToArray()) if (!rows.Any(r => r.id == t.name)) Undo.DestroyObjectImmediate(t.gameObject);
        var data = AssetDatabase.LoadAssetAtPath<EnemySO>("Assets/ShooterSurvival/Prefabs/Entities/SO_WalkerEnemy.asset");
        int count = 0;
        foreach (var row in rows)
        {
            var prior = parent.Find(row.id);
            if (prior != null && (replace || prior.GetComponent<HighwayVehicleEnemy>() == null)) Undo.DestroyObjectImmediate(prior.gameObject);
            var actor = Group(parent, row.id); actor.gameObject.tag = "EnemyTag";
            var vehicle = actor.GetComponent<HighwayVehicleEnemy>();
            if (vehicle == null)
            {
                float width = row.vehicleKind == HighwayVehicleKind.OneTon || row.vehicleKind == HighwayVehicleKind.Box || row.vehicleKind == HighwayVehicleKind.Tanker || row.vehicleKind == HighwayVehicleKind.Bus ? S("truckWidth") : S("vehicleWidth");
                if (row.vehicleFront && row.vehicleKind == HighwayVehicleKind.Taxi) width = S("sideCarWidth");
                string prefab=VehiclePrefab(row.vehicleKind);
                if(row.vehicleKind==HighwayVehicleKind.Sedan)prefab=new[]{"V02_white_sedan","V01_compact_car","V03_black_suv"}[count%3];
                var body = Model(actor, "Vehicle body", Prefabs + prefab + ".prefab", width);
                if (row.vehicleKind == HighwayVehicleKind.Bus) body.localScale = new Vector3(S("busWidth") / width, 1, 1);
                // Keep intact wheels/chassis. Lamps, smoke, orientation and triangle communicate the accident.
                if (row.vehicleKind == HighwayVehicleKind.Accident) body.localRotation = Quaternion.Euler(0, 90, 0);
                else if (row.vehicleFront) body.localRotation = Quaternion.Euler(0, 7, 0);
                var bounds = LocalBounds(actor);
                var rb = actor.gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                var shape = actor.gameObject.AddComponent<BoxCollider>(); shape.isTrigger = true; shape.center = bounds.center; shape.size = bounds.size;
                var number = HealthNumber(actor, bounds);
                var hit = Group(actor, "Walker-HitPos"); hit.localPosition = new Vector3(0, bounds.center.y, bounds.max.z);
                var combat = actor.gameObject.AddComponent<EnemyScript_space>(); var serialized = new SerializedObject(combat);
                serialized.FindProperty("enemyData").objectReferenceValue = data; serialized.FindProperty("dropBonusAltar").boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo();
                var eventController = actor.gameObject.AddComponent<EnemyEventController>(); eventController.HideWhileWaiting = false; eventController.enabled = false;
                vehicle = actor.gameObject.AddComponent<HighwayVehicleEnemy>(); vehicle.body = body; vehicle.healthNumber = number;
                vehicle.footprint = new Vector2(bounds.extents.x, bounds.extents.z);
                var lights = new List<Renderer>();
                if (row.vehicleKind == HighwayVehicleKind.Police || row.vehicleKind == HighwayVehicleKind.Tow || row.vehicleFront)
                {
                    var signalMaterial = Mat("Signal");
                    foreach (float x in new[] { -.6f, .6f }) lights.Add(Cube(body, "Flashing lamp " + x, new Vector3(x, bounds.max.y + .08f, 0), new Vector3(.45f, .18f, .3f), signalMaterial).GetComponent<Renderer>());
                }
                vehicle.lamps = lights.ToArray();
                if (row.vehicleKind == HighwayVehicleKind.Accident) Triangle(actor, bounds.max.z);
                if (row.vehicleKind == HighwayVehicleKind.Tow)
                {
                    var sign = WorldText(body, "Tow truck label", "견인차", new Vector3(0, bounds.max.y * .6f, bounds.max.z + .03f), new Vector2(2.7f, .8f), 4, Color.black);
                    sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
                }
            }
            Undo.RecordObject(vehicle, "Vehicle workbook placement"); vehicle.ConfigurePlacement(row);
            VehicleEffects(vehicle);
            route.Sample(row.vehicleStation, row.vehicleRoute == HighwayVehicleRoute.Open, out var p, out var f);
            actor.SetPositionAndRotation(p + Vector3.Cross(Vector3.up, f) * HighwayChapter2Data.Lane(row.vehicleLane) + Vector3.up*S("vehicleGroundClearance"), Quaternion.LookRotation(-f));
            Dirty(vehicle); Dirty(actor); count++; if (count % 10 == 0) Progress("vehicles", count, rows.Length);
        }
        Save(); Progress("vehicles-complete", count, count); return new { vehicles = count, ranged = 0 };
    }
    static void Place(HighwayRoute route, Transform t, float distance, float lane, bool branch = false, bool south = false)
    {
        route.Sample(distance, branch, out var p, out var f);
        t.SetPositionAndRotation(p + Vector3.Cross(Vector3.up, f) * lane, Quaternion.LookRotation(south ? -f : f)); Dirty(t);
    }
    static Transform Cone(Transform parent, string name)
    {
        var root = Group(parent, name);
        var orange = ColorMaterial("TrafficOrange", new Color(1, .28f, .025f));
        var rubber = ColorMaterial("RubberBlack", new Color(.035f, .04f, .04f));
        Cube(root, "Rubber foot", new Vector3(0, .07f, 0), new Vector3(.65f, .14f, .65f), rubber);
        for (int i = 0; i < 5; i++)
        {
            float width = Mathf.Lerp(.48f, .10f, i / 4f);
            Cube(root, "Taper " + i, new Vector3(0, .23f + i * .17f, 0), new Vector3(width, .17f, width), i == 2 ? White : orange);
        }
        return root;
    }
    static HighwayChapter2Controller Controller()
    {
        var route = Route(); var controller = route.GetComponent<HighwayChapter2Controller>();
        if (controller == null) controller = route.gameObject.AddComponent<HighwayChapter2Controller>();
        return controller;
    }
    public static object Events()
    {
        var route = Route(); if (SceneManager.GetActiveScene().isDirty) throw new Exception("Clean scene required");
        var root = Group(route.transform, RootName); var controller = Controller();
        var effectsRoot = Group(root, "Pooled cartoon effects"); var feedback = effectsRoot.GetComponent<HighwayChapter2Feedback>();
        if (feedback == null) feedback = effectsRoot.gameObject.AddComponent<HighwayChapter2Feedback>();
        feedback.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR Explosion 2.prefab");
        feedback.dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Smoke Poof.prefab");
        controller.feedback = feedback; Dirty(feedback);

        var traffic = Group(root, "Northbound opposite traffic"); var opposite = new List<Transform>();
        for (int i = 0; i < HighwayChapter2Data.Count("oppositeCount"); i++)
        {
            string model = i % 4 == 0 ? "V04_1ton_truck" : i % 4 == 1 ? "V03_black_suv" : i % 4 == 2 ? "V02_white_sedan" : "V07_taxi";
            var car = Model(traffic, "Northbound " + i, Prefabs + model + ".prefab", S("vehicleWidth"));
            float near = -S("laneWidth") * 1.5f - S("medianShoulder") * 2 - S("medianWidth");
            Place(route, car, (i / 3 + 1) * S("oppositeGap"), near - (i % 3 + .5f) * S("laneWidth"));
            opposite.Add(car);
        }
        controller.oppositeCars = opposite.ToArray();
        var logRoot = Group(root, "Single log event");
        controller.logTruck = Model(logRoot, "Northbound loaded truck", Prefabs + "V12_log_truck.prefab", S("truckWidth"));
        controller.singleLog = Model(logRoot, "One loose log", Prefabs + "P15_log.prefab", 3.4f);
        controller.singleLog.localScale = Vector3.one * S("logLength") / LocalBounds(controller.singleLog).size.z;
        Place(route, controller.logTruck, S("logStart") + S("crashLead"), -S("laneWidth") * 2 - S("medianShoulder") * 2 - S("medianWidth"));
        Place(route, controller.singleLog, S("logStart"), -S("laneWidth")); controller.singleLog.gameObject.SetActive(false);

        var deerRoot = Group(root, "Water deer crossing");
        Place(route, deerRoot, S("deerAt"), 0);
        var deerVisual = Model(deerRoot, "Water deer", Prefabs + "A11_water_deer.prefab", 1.45f, true);
        var hop = deerVisual.GetComponent<DeerHop>(); if (hop == null) hop = deerVisual.gameObject.AddComponent<DeerHop>();
        var crossing = deerRoot.GetComponent<WaterDeerCrossing>(); if (crossing == null) crossing = deerRoot.gameObject.AddComponent<WaterDeerCrossing>();
        crossing.deer = deerVisual; crossing.activateAhead = S("deerLead"); crossing.damageFraction = S("deerDamage");
        crossing.minSpeed = S("deerMinSpeed"); crossing.maxSpeed = S("deerMaxSpeed"); crossing.hitRadius = S("deerRadius"); crossing.spinSeconds = S("deerSpin");
        controller.deer = crossing; Dirty(crossing);
        var holeRoot = Group(root, "Fatal road pothole"); Place(route, holeRoot, S("holeAt"), HighwayChapter2Data.Lane(S("holeLane")));
        Model(holeRoot, "Pothole mesh", Prefabs + "P16_pothole.prefab", S("vehicleWidth"));
        var holeCollider = holeRoot.GetComponent<BoxCollider>(); if (holeCollider == null) holeCollider = holeRoot.gameObject.AddComponent<BoxCollider>();
        holeCollider.isTrigger = true; holeCollider.size = new Vector3(2.7f, 2, 3.2f); holeCollider.center = Vector3.up * .5f;
        controller.hole = holeRoot.GetComponent<RoadPotholeHazard>(); if (controller.hole == null) controller.hole = holeRoot.gameObject.AddComponent<RoadPotholeHazard>();
        for (int i = 0; i < 4; i++) { var cone = Cone(holeRoot, "Pothole cone " + i); cone.localPosition = new Vector3(i % 2 == 0 ? -1.7f : 1.7f, 0, i < 2 ? -2 : 2); }

        var work = Group(root, "Construction taper"); controller.workRoot = work;
        var arrowTruck = Model(work, "LED arrow work truck", Prefabs + "V10_arrow_truck.prefab", S("truckWidth"));
        Place(route, arrowTruck, S("workAt") + 20, HighwayChapter2Data.Lane(S("workLane")), false, true);
        var workerList = new List<Transform>();
        for (int i = 0; i < 2; i++)
        {
            var worker = Model(work, "Cone worker " + i, Prefabs + "C02_road_worker.prefab", 2.05f, true);
            Place(route, worker, S("workAt") + i * 8, S("laneWidth"), false, true); workerList.Add(worker);
        }
        controller.workers = workerList.ToArray();
        var flagger = Model(work, "Traffic flagger", Prefabs + "C08_flagger.prefab", 2.05f, true);
        Place(route, flagger, S("workAt") - 10, S("laneWidth") + 1.4f, false, true);
        for (int i = 0; i < 14; i++)
        {
            var cone = Cone(work, "Taper cone " + i);
            float d = S("workAt") - 28 + i * 4;
            float lane = Mathf.Lerp(S("laneWidth") * 1.8f, S("laneWidth") * .55f, Mathf.Clamp01(i / 7f));
            Place(route, cone, d, lane);
        }
        var rentals = Group(work, "Thrown cone rentals"); var thrown = new List<Transform>();
        for (int i = 0; i < 3; i++) { var cone = Cone(rentals, "Thrown cone " + i); cone.gameObject.SetActive(false); thrown.Add(cone); }
        controller.thrownCones = thrown.ToArray();

        // Retain selected original normal bonus pairs and their existing table-driven effects.
        var pairs = route.GetComponentsInChildren<BonusWallChoicePair>(true).OrderBy(p => p.Left.name, StringComparer.Ordinal).ToArray();
        float[] stations = Enumerable.Range(1, HighwayChapter2Data.Count("normalBonusCount")).Select(i => S("normalBonus" + i)).ToArray();
        var retained = new List<string>();
        for (int i = 0; i < pairs.Length; i++)
        {
            var pair = pairs[i]; bool use = i < stations.Length;
            pair.Left.gameObject.SetActive(use); pair.Right.gameObject.SetActive(use); Dirty(pair.Left.gameObject); Dirty(pair.Right.gameObject);
            if (!use) continue;
            route.Sample(stations[i], false, out var p, out var f); var side = Vector3.Cross(Vector3.up, f);
            pair.Left.transform.SetPositionAndRotation(p - side * S("normalBonusLane") + Vector3.up * .08f, Quaternion.LookRotation(f));
            pair.Right.transform.SetPositionAndRotation(p + side * S("normalBonusLane") + Vector3.up * .08f, Quaternion.LookRotation(f));
            Dirty(pair.Left.transform); Dirty(pair.Right.transform); retained.Add(pair.Left.name);
        }
        File.WriteAllLines(Record + "/normal-bonus-ids.txt", retained);
        Dirty(controller); Save(); Progress("events-complete", 1, 1);
        return new { opposite = opposite.Count, workers = workerList.Count, normalBonusPairs = retained.Count };
    }
    static void OutlineModel(Transform model)
    {
        var reference = AssetDatabase.LoadAssetAtPath<Material>("Assets/JH/Model/Enemy/Garden_Spear/Material.001.mat");
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            var originals = renderer.sharedMaterials; var converted = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                var source = originals[i]; if (source == null) continue;
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                string name = guid.Length > 0 ? guid : "native_" + source.name.Replace(" ", "_");
                string path = AssetsRoot + "/Outline_" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(reference);
                    var texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
                    material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", Color.white);
                    if (material.HasProperty("_ColorDim")) material.SetColor("_ColorDim", new Color(.86f, .86f, .88f));
                    material.enableInstancing = true; AssetDatabase.CreateAsset(material, path);
                }
                converted[i] = material;
            }
            renderer.sharedMaterials = converted; Dirty(renderer);
        }
    }
    static bool HasBranch(HighwayRoute route, float distance) => route.forks.Any(f => distance >= f.start && distance <= f.end);
    static Transform Sign(Transform parent, HighwayRoute route, string name, float d, string title, bool fork = false)
    {
        var root = Group(parent, name); Place(route, root, d, 0);
        var green = ColorMaterial("ExpresswayGreen", new Color(.035f, .28f, .16f));
        var steel = ColorMaterial("RoadSteel", new Color(.42f, .48f, .50f));
        foreach (float x in new[] { -7.3f, 7.3f }) Cube(root, "Support " + x, new Vector3(x, 4.5f, 0), new Vector3(.3f, 9, .3f), steel);
        Cube(root, "Crossbeam", new Vector3(0, 8.8f, 0), new Vector3(15.1f, .35f, .35f), steel);
        Cube(root, "Green board", new Vector3(0, 7.7f, -.1f), new Vector3(12.8f, 2.1f, .20f), green);
        WorldText(root, "Korean destination", title, new Vector3(0, 7.85f, -.24f), new Vector2(12, 1.5f), fork ? 6 : 7, Color.white);
        if (fork) WorldText(root, "Branch captions", "정체 구간                         뻥 뚫린 길", new Vector3(0, 6.9f, -.25f), new Vector2(12, .75f), 3.2f, Color.white);
        return root;
    }
    public static object Scenery()
    {
        var route = Route(); if (SceneManager.GetActiveScene().isDirty) throw new Exception("Clean scene required");
        var root = Group(route.transform, RootName); var scenery = Group(root, "Korean city scenery");
        var ground = ColorMaterial("GroundGreen", new Color(.22f, .37f, .22f));
        var bounds = new Bounds(route.centers[0], Vector3.zero); foreach (var p in route.centers) bounds.Encapsulate(p);
        var floor = Cube(scenery, "Landscape base", new Vector3(bounds.center.x, bounds.min.y - .25f, bounds.center.z), new Vector3(bounds.size.x + 500, .3f, bounds.size.z + 350), ground);
        var wallBlue = ColorMaterial("SoundWallBlue", new Color(.38f, .55f, .65f));
        var frame = ColorMaterial("RoadSteel", new Color(.42f, .48f, .50f));
        int segments = 0;
        for (float d = 15; d < route.length; d += 22)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var wall = Group(scenery, "Sound wall " + side + "_" + segments);
                Place(route, wall, d, side < 0 ? -25 : 9.3f, side > 0 && HasBranch(route, d));
                Cube(wall, "Acoustic panel", new Vector3(0, 2.0f, 0), new Vector3(.2f, 3.8f, 21.5f), wallBlue);
                Cube(wall, "Lower concrete curb", new Vector3(0, .35f, 0), new Vector3(.5f, .7f, 21.7f), Mat("Concrete"));
                for (int j = -2; j <= 2; j++) Cube(wall, "Frame " + j, new Vector3(0, 2.2f, j * 4.5f), new Vector3(.32f, 4.4f, .18f), frame);
            }
            segments++;
        }
        int apartmentCount = 0;
        for (float d = 55; d < route.length; d += 145)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var apartments = Model(scenery, "Apartment complex " + apartmentCount, Prefabs + "P13_apartment_block.prefab", 36);
                Place(route, apartments, d, side < 0 ? -45 : 30, side > 0 && HasBranch(route, d));
                apartments.localScale = new Vector3(1, apartmentCount % 3 == 0 ? 1.5f : 1, 1);
                apartmentCount++;
            }
        }
        string skylinePath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage02_Highway/065_STAGE02_HWY_BACKGROUND_004_Highway_skyline_backdrop/065_STAGE02_HWY_BACKGROUND_004_Highway_skyline_backdrop.prefab";
        for (int i = 0; i < 5; i++)
        {
            var skyline = Model(scenery, "City skyline " + i, skylinePath, 150);
            OutlineModel(skyline); Place(route, skyline, Mathf.Min(route.length - 40, 260 + i * 440), i % 2 == 0 ? -170 : 180);
        }
        // Reuse the existing Meshy scenery texture treatment, with native outlined roadside foliage.
        var trunk = ColorMaterial("TreeTrunk", new Color(.28f, .16f, .08f)); var leaf = ColorMaterial("TreeLeaf", new Color(.23f, .49f, .13f));
        for (int i = 0; i < 58; i++)
        {
            float d = 25 + i * 39;
            var tree = Group(scenery, "Roadside tree " + i); Place(route, tree, d, i % 2 == 0 ? -30 : 14, i % 2 != 0 && HasBranch(route, d));
            Cube(tree, "Trunk", new Vector3(0, 1.8f, 0), new Vector3(.55f, 3.6f, .55f), trunk);
            if (tree.Find("Crown") == null)
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name = "Crown"; crown.transform.SetParent(tree, false);
                crown.transform.localPosition = new Vector3(0, 4.3f, 0); crown.transform.localScale = new Vector3(4.2f, 4.4f, 3.8f);
                Object.DestroyImmediate(crown.GetComponent<Collider>()); crown.GetComponent<Renderer>().sharedMaterial = leaf;
            }
        }
        Sign(root, route, "Fork sign", S("forkAt") - 4, "↖                       ↗", true);
        Sign(root, route, "Rest stop approach", route.length - 85, "휴게소 진입");
        Dirty(Controller()); Save(); Progress("scenery-complete", 1, 1); return new { apartments = apartmentCount, wallSegments = segments };
    }
    static Sprite SpriteAt(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static SpriteRenderer Icon(Transform parent, string name, Sprite sprite, Vector3 position, float width)
    {
        var t = Group(parent, name); var renderer = t.GetComponent<SpriteRenderer>(); if (renderer == null) renderer = t.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; t.localPosition = position;
        if (sprite != null) t.localScale = Vector3.one * width / sprite.bounds.size.x;
        return renderer;
    }
    static Transform Card(Transform parent, string name, Vector3 position)
    {
        var root = Group(parent, name); root.localPosition = position;
        var gold = ColorMaterial("BonusGold", new Color(1, .64f, .06f));
        var cream = ColorMaterial("BonusCream", new Color(1, .96f, .80f));
        Cube(root, "Gold trim", Vector3.zero, new Vector3(3.0f, 3.8f, .18f), gold);
        Cube(root, "Cream face", new Vector3(0, 0, -.12f), new Vector3(2.78f, 3.55f, .07f), cream);
        return root;
    }
    static Transform Star(Transform parent, string name, Vector3 position)
    {
        var root = Group(parent, name); root.localPosition = position;
        var vertices = new List<Vector3> { Vector3.zero }; var uv = new List<Vector2> { Vector2.one * .5f }; var triangles = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.PI * .5f + i * Mathf.PI / 5, radius = i % 2 == 0 ? .8f : .35f;
            vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0)); uv.Add(new Vector2(.5f + Mathf.Cos(angle) * .5f, .5f + Mathf.Sin(angle) * .5f));
        }
        for (int i = 0; i < 10; i++) triangles.AddRange(new[] { 0, i + 1, (i + 1) % 10 + 1, 0, (i + 1) % 10 + 1, i + 1 });
        var filter = root.GetComponent<MeshFilter>(); if (filter == null) filter = root.gameObject.AddComponent<MeshFilter>(); filter.sharedMesh = StoreMesh("BonusStar", vertices, uv, triangles);
        var renderer = root.GetComponent<MeshRenderer>(); if (renderer == null) renderer = root.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = ColorMaterial("BonusGold", new Color(1, .64f, .06f));
        return root;
    }
    public static object Interface()
    {
        var route = Route(); if (SceneManager.GetActiveScene().isDirty) throw new Exception("Clean scene required");
        var controller = Controller(); var root = Group(route.transform, RootName);
        var uiRoot = RectGroup(root, "Chapter2 popup and news"); var ui = uiRoot.GetComponent<HighwayChapter2UI>(); if (ui == null) ui = uiRoot.gameObject.AddComponent<HighwayChapter2UI>();
        ui.font = Font; ui.enamelPanel = SpriteAt("Assets/ShooterSurvival/UI/CoastalEnamel/IvoryPanel.png");
        ui.missileIcon = SpriteAt("Assets/ShooterSurvival/Resources/WallBonusIcons/WallBonus_MissileAdd.png");
        ui.shieldIcon = SpriteAt("Assets/ShooterSurvival/UI/CoastalEnamel/SahurShield.png");
        ui.shatterIcon = SpriteAt("Assets/ShooterSurvival/Resources/WallBonusIcons/WallBonus_MissileDuration.png");
        ui.magnetIcon = SpriteAt("Assets/ShooterSurvival/UI/CoastalEnamel/Coin.png"); controller.ui = ui;

        var toll = Group(root, "Toll exit without barriers"); Place(route, toll, S("tollAt") + S("crashQueueGap"), 0);
        var blue = ColorMaterial("HipassBlue", new Color(.08f, .35f, .65f)); var cream = ColorMaterial("BoothIvory", new Color(.87f, .88f, .82f));
        Cube(toll, "Canopy roof", new Vector3(0, 8, 0), new Vector3(18.5f, .75f, 11), blue);
        Cube(toll, "Canopy front trim", new Vector3(0, 7.5f, -5.1f), new Vector3(18.6f, .22f, .3f), cream);
        controller.tollRoof = new[] { toll.Find("Canopy roof").GetComponent<Renderer>(), toll.Find("Canopy front trim").GetComponent<Renderer>() };
        foreach (float x in new[] { -8.5f, 8.5f }) Cube(toll, "Canopy pillar " + x, new Vector3(x, 3.75f, 0), new Vector3(.7f, 7.5f, .9f), cream);
        WorldText(toll, "Toll exit name", "요금소 출구", new Vector3(0, 8, -5.6f), new Vector2(15, 1.2f), 9, Color.white);
        Cube(toll, "Hipass lane board", new Vector3(-4.4f, 6.75f, -4.9f), new Vector3(5, 1.1f, .15f), blue);
        Cube(toll, "Cash lane board", new Vector3(4.4f, 6.75f, -4.9f), new Vector3(5, 1.1f, .15f), ColorMaterial("NavyBlue", new Color(.055f, .1f, .2f)));
        WorldText(toll, "Hipass text", "하이패스", new Vector3(-4.4f, 6.75f, -5.05f), new Vector2(4.8f, .9f), 5.2f, Color.white);
        WorldText(toll, "Cash text", "현금", new Vector3(4.4f, 6.75f, -5.05f), new Vector2(4.8f, .9f), 5.2f, Color.white);
        string boothPath = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage02_Highway/066_STAGE02_HWY_BUILDING_009_Tollgate_booth_module/066_STAGE02_HWY_BUILDING_009_Tollgate_booth_module.prefab";
        var booth = Model(toll, "Cash booth", boothPath, 2.3f); OutlineModel(booth); booth.localPosition = new Vector3(8.6f, 0, -2);
        var clerk = Model(toll, "Cash booth employee", Prefabs + "C09_toll_attendant.prefab", 1.9f, true); clerk.localPosition = new Vector3(8.5f, .45f, -3.4f); clerk.localRotation = Quaternion.Euler(0, 180, 0);
        var cards = new List<Transform>(); var names = new List<TMP_Text>(); var icons = new List<SpriteRenderer>();
        for (int i = 0; i < 2; i++)
        {
            var card = Card(toll, "Unique card " + i, new Vector3(i == 0 ? -4.4f : 4.4f, 4.25f, -5.7f));
            icons.Add(Icon(card, "Unique effect icon", i == 0 ? ui.missileIcon : ui.shieldIcon, new Vector3(0, .3f, -.2f), 1.6f));
            names.Add(WorldText(card, "Unique name", "유니크 보너스", new Vector3(0, -1.1f, -.22f), new Vector2(2.7f, .65f), 3.0f, new Color(.08f, .1f, .2f)));
            Star(card, "Upper star", new Vector3(0, 2.05f, -.08f)); cards.Add(card);
        }
        controller.uniqueCards = cards.ToArray(); controller.uniqueNames = names.ToArray(); controller.uniqueIcons = icons.ToArray();
        var mysteries = new List<HighwayChapter2Controller.MysteryGate>();
        for (int i = 0; i < HighwayChapter2Data.Count("mysteryCount"); i++)
        {
            var gate = Group(root, "Random bonus gate " + (i + 1)); Place(route, gate, S("mystery" + (i + 1)), 0);
            var questions = new List<TMP_Text>(); var rotating = new List<Transform>();
            for (int band = 0; band < 6; band++)
            {
                var material = ColorMaterial("Rainbow" + band, Color.HSVToRGB(band / 7f, .8f, 1));
                for (int j = 0; j < 16; j++)
                {
                    float a = j / 16f * Mathf.PI, b = (j + 1) / 16f * Mathf.PI;
                    Vector3 start = new Vector3(Mathf.Cos(a) * 5.8f, 4.5f + Mathf.Sin(a) * 1.6f + band * .12f, 0);
                    Vector3 end = new Vector3(Mathf.Cos(b) * 5.8f, 4.5f + Mathf.Sin(b) * 1.6f + band * .12f, 0);
                    var bar = Cube(gate, "Rainbow " + band + "_" + j, (start + end) * .5f, new Vector3(.14f, .14f, Vector3.Distance(start, end) + .03f), material);
                    bar.localRotation = Quaternion.LookRotation(end - start, Vector3.forward);
                }
            }
            foreach (float x in new[] { -5.8f, 5.8f })
            {
                Cube(gate, "Arch pillar " + x, new Vector3(x, 2.2f, 0), new Vector3(.25f, 4.4f, .3f), blue);
                Star(gate, "Arch star " + x, new Vector3(x, 4.7f, -.25f));
            }
            WorldText(gate, "Random bonus title", "랜덤 보너스", new Vector3(0, 5.3f, -.3f), new Vector2(10, 1.2f), 7, new Color(1, .8f, .1f));
            for (int side = 0; side < 2; side++)
            {
                var card = Card(gate, "Mystery card " + side, new Vector3(side == 0 ? -2.5f : 2.5f, 2.4f, 0));
                questions.Add(WorldText(card, "Mystery question", "?", new Vector3(0, .1f, -.22f), new Vector2(2, 2.2f), 22, new Color(.95f, .6f, .05f)));
                var sprites = new[] { SpriteAt("Assets/ShooterSurvival/Resources/WallBonusIcons/WallBonus_Health.png"), SpriteAt("Assets/ShooterSurvival/Resources/WallBonusIcons/WallBonus_Attack.png"), ui.missileIcon, ui.magnetIcon };
                for (int k = 0; k < 4; k++) rotating.Add(Icon(card, "Cycling icon " + k, sprites[k], new Vector3(k % 2 == 0 ? -.85f : .85f, k < 2 ? 1.25f : -1.25f, -.24f), .55f).transform);
            }
            mysteries.Add(new HighwayChapter2Controller.MysteryGate { root = gate, questions = questions.ToArray(), icons = rotating.ToArray() });
        }
        controller.mysteryGates = mysteries.ToArray();
        ApplyMysteryPads(controller);
        Dirty(controller); Dirty(ui); Save(); Progress("interface-complete", 1, 1);
        return new { mysteryGates = mysteries.Count, uniqueCards = cards.Count, font = Font.name };
    }
    // Bonus-wall concept 08: random gates show rainbow floor pads with a "?" hologram instead of the arch and cards.
    // The legacy renderers stay in the scene (disabled) so the controller's question/icon references remain valid.
    public static object MysteryPads()
    {
        var controller = Route().GetComponent<HighwayChapter2Controller>();
        int hidden = ApplyMysteryPads(controller); Save();
        return new { gates = controller.mysteryGates.Length, hiddenRenderers = hidden };
    }
    static int ApplyMysteryPads(HighwayChapter2Controller controller)
    {
        int hidden = 0;
        foreach (var gate in controller.mysteryGates)
        {
            if (gate?.root == null) continue;
            foreach (var renderer in gate.root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                Undo.RecordObject(renderer, "Bonus pad random gate"); renderer.enabled = false; Dirty(renderer); hidden++;
            }
            if (gate.root.GetComponent<BonusPadRandomDisplay>() == null) Dirty(Undo.AddComponent<BonusPadRandomDisplay>(gate.root.gameObject));
        }
        return hidden;
    }
    public static object CaptureRoadCards()
    {
        var route = Route(); var ui = route.GetComponent<HighwayChapter2Controller>().ui;
        var vehicles = route.GetComponentsInChildren<HighwayVehicleEnemy>(true);
        var originalStates = vehicles.ToDictionary(v => v.gameObject, v => v.gameObject.activeSelf);
        float previewStation = Mathf.Lerp(S("forkAt"), S("mergeAt"), .45f);
        try
        {
        for (int i = 0; i < 2; i++)
        {
            var keep = new HashSet<HighwayVehicleEnemy>(vehicles.Where(v => v.routeChoice == (i == 0 ? HighwayVehicleRoute.Jam : HighwayVehicleRoute.Open)
                && v.station > previewStation + 20 && v.station < previewStation + 160).OrderBy(v => v.station).Take(i == 0 ? 30 : 9));
            foreach (var vehicle in vehicles) vehicle.gameObject.SetActive(keep.Contains(vehicle));
            string file = AssetsRoot + (i == 0 ? "/JamCard.png" : "/OpenCard.png");
            var go = new GameObject("Native road card camera"); var camera = go.AddComponent<Camera>(); RenderTexture rt = null; Texture2D texture = null;
            var previous = RenderTexture.active;
            try
            {
                camera.fieldOfView = 48; camera.aspect = 4f / 3f; camera.nearClipPlane = .1f; camera.farClipPlane = 180;
                camera.clearFlags = CameraClearFlags.Skybox;
                route.Sample(previewStation, i == 1, out var p, out var f);
                go.transform.SetPositionAndRotation(p - f * 23 + Vector3.up * 15, Quaternion.LookRotation(f * 32 - Vector3.up * 10));
                var sourceSky = Camera.main.GetComponent<Skybox>(); if (sourceSky != null) go.AddComponent<Skybox>().material = sourceSky.material;
                rt = new RenderTexture(512, 384, 24); camera.targetTexture = rt; camera.Render();
                RenderTexture.active = rt;
                texture = new Texture2D(512, 384, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 512, 384), 0, 0); texture.Apply(); RenderTexture.active = previous;
                File.WriteAllBytes(file, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(go); if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); } if (texture != null) Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(file); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.mipmapEnabled = false; importer.maxTextureSize = 512; importer.SaveAndReimport();
            if (i == 0) ui.jamPicture = SpriteAt(file); else ui.openPicture = SpriteAt(file);
        }
        }
        finally { foreach (var state in originalStates) state.Key.SetActive(state.Value); }
        Dirty(ui); Save(); return new { jam = ui.jamPicture != null, open = ui.openPicture != null };
    }
    public static object Palette()
    {
        return new[] { "SoundWallBlue", "TreeLeaf", "GroundGreen" }.Select(name =>
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot + "/" + name + ".mat");
            var shader = material.shader;
            return new { name, shader = shader.name, colors = Enumerable.Range(0, shader.GetPropertyCount())
                .Where(i => shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color)
                .Select(i => new { property = shader.GetPropertyName(i), value = material.GetColor(shader.GetPropertyName(i)).ToString() }).ToArray() };
        }).ToArray();
    }
    public static object RoadMargins()
    {
        var route = Route(); var parent = route.transform.Find("Roads/Chapter2Roads");
        float width = S("laneWidth"), edge = width * 1.5f, near = -edge - S("medianShoulder") * 2 - S("medianWidth");
        foreach (int end in new[] { 0, 1 })
        {
            float a = end == 0 ? -30 : route.length, b = end == 0 ? 0 : route.length + 30;
            Surface(parent, route, "MarginMain" + end, a, b, -edge - S("medianShoulder"), edge + S("outerShoulder"), false);
            Surface(parent, route, "MarginOpposite" + end, a, b, near - width * 3 - S("outerShoulder"), near + S("medianShoulder"), false);
            Paint(parent, route, "MarginWhite" + end, a, b, false, false); Paint(parent, route, "MarginYellow" + end, a, b, false, true);
            Median(parent, route, "MarginMedian" + end, a, b);
        }
        Save(); return new { margin = 30, endSupport = true };
    }
    public static object FixPalette()
    {
        Route(); int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { AssetsRoot }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (!material.shader.name.StartsWith("FlatKit") || material.name.StartsWith("Outline_")) continue;
            Color color = material.GetColor("_BaseColor");
            foreach (string property in new[] { "_ColorDim", "_ColorDimSteps", "_ColorDimCurve", "_ColorDimExtra", "_ColorGradient", "_UnityShadowColor" })
                if (material.HasProperty(property)) material.SetColor(property, new Color(color.r * .72f, color.g * .72f, color.b * .72f, 1));
            foreach (string property in new[] { "_FlatSpecularColor", "_FlatRimColor" })
                if (material.HasProperty(property)) material.SetColor(property, color);
            EditorUtility.SetDirty(material); count++;
        }
        AssetDatabase.SaveAssets(); return new { materials = count };
    }
    public static object RefinePresentation()
    {
        var route = Route(); var controller = Controller(); var scenery = route.transform.Find(RootName + "/Korean city scenery");
        int apartment = 0;
        for (float d = 55; d < route.length; d += 145)
            foreach (int side in new[] { -1, 1 })
            {
                var t = scenery.Find("Apartment complex " + apartment++);
                if (t != null) Place(route, t, d, side < 0 ? -45 : 30, side > 0 && HasBranch(route, d));
            }
        var leaf = ColorMaterial("TreeLeaf", new Color(.23f, .49f, .13f));
        for (int i = 0; i < 58; i++)
        {
            var tree = scenery.Find("Roadside tree " + i); if (tree == null) continue;
            var old = tree.Find("Crown"); if (old != null) Disable(old.gameObject);
            var positions = new[] { new Vector3(-.85f, 4.0f, .1f), new Vector3(.95f, 4.4f, -.2f), new Vector3(.1f, 5.2f, .45f), new Vector3(.15f, 3.8f, -.8f) };
            for (int j = 0; j < positions.Length; j++)
            {
                var crown = tree.Find("Leaf cluster " + j);
                if (crown == null) { var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Leaf cluster " + j; go.transform.SetParent(tree, false); Object.DestroyImmediate(go.GetComponent<Collider>()); crown = go.transform; }
                crown.localPosition = positions[j]; crown.localScale = new Vector3(2.9f, 3.0f + (i % 3) * .18f, 2.7f); crown.GetComponent<Renderer>().sharedMaterial = leaf;
                crown.localRotation = Quaternion.Euler(i * 17, j * 41, 12);
            }
        }
        // The generated1-ton asset has an ambiguous rear. Keep its verified front on the playable road;
        // use clearly readable sedan/SUV/taxi rear silhouettes for the northbound background.
        for (int i = 0; i < controller.oppositeCars.Length; i += 4)
        {
            var holder = controller.oppositeCars[i]; foreach (Transform child in holder.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
            Model(holder.parent, holder.name, Prefabs + "V01_compact_car.prefab", S("vehicleWidth"));
        }
        Dirty(controller); Save(); return new { apartments = apartment, oppositeTrucksReplaced = (controller.oppositeCars.Length + 3) / 4 };
    }
    public static object Verify()
    {
        var route = Route(); EnvironmentVariableTables.Reload(); EncounterPlacementTables.Reload();
        var controller = route.GetComponent<HighwayChapter2Controller>(); if (controller == null) throw new Exception("Chapter2 controller missing");
        var vehicles = route.GetComponentsInChildren<HighwayVehicleEnemy>(true);
        var rows = EncounterPlacementTables.Rows.Where(r => r.scene == "HighWay" && r.hasHighwayVehicle).ToArray();
        if (vehicles.Length != rows.Length || rows.Length != HighwayChapter2Data.Count("trafficTarget")) throw new Exception("Vehicle placement count mismatch");
        foreach (var row in rows)
        {
            var v = vehicles.Single(x => x.name == row.id);
            if (v.body == null || v.healthNumber == null || v.blurMaterial == null || v.GetComponent<BoxCollider>() == null) throw new Exception("Incomplete vehicle " + row.id);
            if (v.GetComponent<EnemyScript_space>().HasConfiguredProjectile || v.body.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0) throw new Exception("Pedestrian rig remains on " + row.id);
            if (Mathf.Abs(v.station - row.vehicleStation) > .001f || v.kind != row.vehicleKind) throw new Exception("Stale vehicle metadata " + row.id);
        }
        if (!route.popupBranches || route.forks.Length != 2 || route.length != S("length")) throw new Exception("Route contract mismatch");
        if (controller.ui == null || controller.ui.font != Font || controller.ui.jamPicture == null || controller.ui.openPicture == null) throw new Exception("Popup reference missing");
        if (controller.feedback == null || controller.feedback.explosionPrefab == null || controller.mysteryGates.Length != HighwayChapter2Data.Count("mysteryCount")) throw new Exception("Event references incomplete");
        if (controller.oppositeCars.Length != HighwayChapter2Data.Count("oppositeCount") || controller.workers.Length != 2 || controller.deer == null || controller.hole == null) throw new Exception("Scene event contract mismatch");
        var surfaces = route.transform.Find("Roads/Chapter2Roads");
        if (surfaces.GetComponentsInChildren<MeshFilter>().Any(m => m.sharedMesh == null)) throw new Exception("Road mesh reference missing");
        if (route.GetComponentsInChildren<OncomingLaneTraffic>().Any(t => t.isActiveAndEnabled)
            || route.GetComponentsInChildren<LogTruckSpill>().Any(t => t.isActiveAndEnabled)
            || route.GetComponentsInChildren<HighwayHazard>().Any(t => t.isActiveAndEnabled)
            || route.GetComponentsInChildren<RestStopRouteGimmick>().Any(t => t.isActiveAndEnabled))
            throw new Exception("An old hazard controller remains active");
        if (route.GetComponentsInChildren<LineRenderer>().Any(l => l.enabled)) throw new Exception("Legacy road guide/warning line remains");
        var result = new { scene = SceneManager.GetActiveScene().path, clean = !SceneManager.GetActiveScene().isDirty,
            length = route.length, laneWidth = S("laneWidth"), vehicles = vehicles.Length, rangedEnemies = 0,
            meshes = surfaces.GetComponentsInChildren<MeshFilter>().Length, normalBonusPairs = route.GetComponentsInChildren<BonusWallChoicePair>().Length,
            randomGates = controller.mysteryGates.Length, oppositeCars = controller.oppositeCars.Length,
            playerScale = S("sharkScale"), cameraScale = S("cameraScale"), font = Font.name,
            nextScene = Object.FindFirstObjectByType<ChapterProgression>().nextScene };
        File.WriteAllText(Record + "/verified.json", Json(result)); return result;
    }
    public static object Apply()
    {
        var route = Route();
        if (route.GetComponent<HighwayChapter2Controller>() != null && File.Exists(Record + "/verified.json")) return Verify();
        Inspect(); Geometry(); Actors(); Events(); Scenery(); Interface(); RoadMargins(); FixPalette(); RefinePresentation(); RefineEffects(); RefreshForkPaint(); ClearRoadsideWalls(); RefineRoadSurfaces(); CaptureRoadCards();
        return Verify();
    }
    public static object RefineEffects()
    {
        var route = Route(); EnvironmentVariableTables.Reload(); var controller = Controller();
        controller.singleLog.localScale = Vector3.one;
        controller.singleLog.localScale = Vector3.one * S("logLength") / LocalBounds(controller.singleLog).size.z;
        controller.feedback.woodMaterial = ColorMaterial("TreeTrunk", new Color(.28f, .16f, .08f));
        controller.feedback.missileMaterial = ColorMaterial("MissileBlue", new Color(.1f, .55f, 1));
        string path = AssetsRoot + "/GoldenShieldAura.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Golden shield aura", renderQueue = 3000 };
            material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetColor("_BaseColor", new Color(1, .7f, .05f, .16f)); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(material, path);
        }
        var root = route.transform.Find(RootName); var aura = root.Find("Golden shield aura");
        if (aura == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Golden shield aura"; go.transform.SetParent(root, false); Object.DestroyImmediate(go.GetComponent<Collider>()); aura = go.transform;
        }
        aura.localScale = new Vector3(2.5f, 3.2f, 2.5f); aura.GetComponent<Renderer>().sharedMaterial = material; aura.gameObject.SetActive(false); controller.shieldAura = aura;
        var gold = AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot + "/BonusGold.mat");
        if (gold != null && gold.HasProperty("_EmissionColor")) { gold.EnableKeyword("_EMISSION"); gold.SetColor("_EmissionColor", new Color(1, .55f, .015f) * 1.4f); Dirty(gold); }
        Dirty(controller.singleLog); Dirty(controller.feedback); Dirty(controller); Save();
        return new { logLength = S("logLength"), shield = controller.shieldAura != null, pooledWoodAndMissiles = true };
    }
}
