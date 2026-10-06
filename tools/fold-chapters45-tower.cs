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
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Run only after InstallChapters45, RefineChapters45 and the crown installer.
// Owns ShoeTower's route and its authored world only; does not edit runtime code.
public static class FoldChapters45Tower
{
    const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity";
    const string ArtRoot = "Assets/ShooterSurvival/Models/Chapters/Chapters45";
    const string Record = "outputs/chapters45-2026-10-02";
    const string Marker = "TowerFold_Metric_V1";
    const float ArcStep = 2.5f;
    static readonly float[] Heights = { 0, 95, 235 };
    static readonly string[] Labels = { "1F  LOBBY / RETAIL", "58F  ARCHIVE / OBSERVATORY", "118F  CROWN / SHOE GALLERY" };
    static readonly List<Chapter45Route.Segment> segments = new();
    static readonly List<Span> straights = new();
    static readonly List<Move> moves = new();
    static readonly List<float> occupied = new();
    static Chapter45Route route;
    static Chapter45Director director;
    static Chapter45Choice fork;
    static Transform world, roads, scenery;
    static string stamp;
    static float progress;
    static Material ivory, stone, blue, brass, navy, green;
    [Serializable] public sealed class Move { public string name, kind; public float before, after; }
    sealed class Span { public float start, end; public int floor; }
    [Serializable] public sealed class Receipt
    {
        public string scene, backup, marker;
        public float length;
        public int segments, lifts;
        public float[] liftDistances, liftSeconds;
        public Vector3 boundsMin, boundsMax;
        public List<Move> stationMoves;
        public string[] contracts;
    }

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Idle Edit Mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve dirty scenes first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        Scene owned = default;
        bool saved = false;
        try
        {
            owned = EditorSceneManager.OpenScene(ScenePath);
            director = Object.FindObjectsByType<Chapter45Director>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(d => d.gameObject.scene == owned && d.chapter == 5);
            route = director.route; world = director.transform;
            roads = world.Find("Roads"); scenery = world.Find("Scenery");
            if (roads == null || scenery == null) throw new InvalidOperationException("Expected chapter authoring roots missing.");
            if (world.Find(Marker) != null) { saved = true; return Verify(); }
            ValidateSource();
            stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            Directory.CreateDirectory(Record);
            string backup = Record + "/ShoeTower-before-fold-" + stamp + ".unity";
            File.Copy(ScenePath, backup, false);
            segments.Clear(); straights.Clear(); moves.Clear(); occupied.Clear(); progress = 0;
            MakeSerpentine(0, 530, false);
            MakeSerpentine(1, 590, true);
            MakeCrownFloor();
            route.segments = segments.ToArray();
            fork = director.GetComponentsInChildren<Chapter45Choice>(true)
                .Single(c => c.kind == Chapter45Choice.ChoiceKind.RouteFork);
            float oldBranch = fork.branchOffset;
            fork.branchOffset = 8; // R20 turns retain a positive R12 inner corridor.
            RemapTargets(oldBranch);
            RemapHazards();
            RemapEncounters(oldBranch);
            RemapChoices(oldBranch);
            RemapGoals();
            RemapLifts();
            FoldShowroom();
            RebuildFloorArt();
            Rebind();
            ResetStartPose();
            var marker = new GameObject(Marker); marker.transform.SetParent(world, false);
            var receipt = Verify(); receipt.backup = backup; receipt.stationMoves = new List<Move>(moves);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(owned);
            if (!EditorSceneManager.SaveScene(owned)) throw new IOException("ShoeTower save failed.");
            saved = true;
            File.WriteAllText(Record + "/tower-fold-" + stamp + ".json", JsonUtility.ToJson(receipt, true));
            return receipt;
        }
        finally
        {
            // All pre-existing scenes were clean. Discard only this tool's unsaved
            // owned scene on failure, never save a partially folded scene.
            if (!saved && owned.IsValid() && owned.isLoaded) EditorSceneManager.CloseScene(owned, true);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    static void ValidateSource()
    {
        if (route == null || route.segments.Length != 3) throw new InvalidOperationException("Expected original three-deck tower; refusing an unknown route.");
        float[] lengths = { 530, 590, 530 }; float d = 0;
        for (int i = 0; i < 3; i++)
        {
            var s = route.segments[i];
            if (Mathf.Abs(s.Length - lengths[i]) > .01f || s.floor != i ||
                Vector3.Distance(s.start, new Vector3(0, Heights[i], d)) > .01f ||
                Vector3.Distance(s.end, new Vector3(0, Heights[i], d + lengths[i])) > .01f)
                throw new InvalidOperationException("Source route differs from the inspected authoring contract.");
            d += lengths[i];
        }
        if (director.GetComponentsInChildren<Chapter45Lift>(true).Length != 2)
            throw new InvalidOperationException("Exactly two source lifts required.");
        if (scenery.Find("InsideTheSneaker") == null) throw new InvalidOperationException("Preserve/install the crown showroom before folding.");
    }
    static void Line(Vector3 a, Vector3 b, int floor, bool quietCurve = false)
    {
        float length = Vector3.Distance(a, b);
        if (length < .0001f) return;
        segments.Add(new Chapter45Route.Segment { start = a, end = b, length = length, floor = floor, label = Labels[floor] });
        if (!quietCurve) straights.Add(new Span { start = progress, end = progress + length, floor = floor });
        progress += length;
    }
    static float ChordArc(float radius, float angle) => Mathf.RoundToInt(angle / ArcStep) * 2 * radius * Mathf.Sin(ArcStep * .5f * Mathf.Deg2Rad);
    static void Arc(Vector3 center, float radius, float from, float to, int floor, bool mirrorX = false)
    {
        int count = Mathf.RoundToInt(Mathf.Abs(to - from) / ArcStep);
        Vector3 Point(float angle)
        {
            var p = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, 0, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            if (mirrorX) p.x = -p.x;
            return p;
        }
        for (int i = 0; i < count; i++) Line(Point(Mathf.Lerp(from, to, (float)i / count)), Point(Mathf.Lerp(from, to, (float)(i + 1) / count)), floor, true);
    }
    static void MakeSerpentine(int floor, float length, bool mirrorX)
    {
        float leg = (length - 3 * ChordArc(20, 180)) / 4;
        float top = -60 + leg, y = Heights[floor];
        Vector3 P(float x, float z) => new Vector3(mirrorX ? -x : x, y, z);
        Line(P(-60, -60), P(-60, top), floor);
        Arc(new Vector3(-40, y, top), 20, 180, 0, floor, mirrorX);
        Line(P(-20, top), P(-20, -60), floor);
        Arc(new Vector3(0, y, -60), 20, 180, 360, floor, mirrorX);
        Line(P(20, -60), P(20, top), floor);
        Arc(new Vector3(40, y, top), 20, 180, 0, floor, mirrorX);
        Line(P(60, top), P(60, -60), floor);
    }
    static void MakeCrownFloor()
    {
        float top = (530 - 295 - 4 * ChordArc(15, 90)) / 2;
        float y = Heights[2]; Vector3 P(float x, float z) => new Vector3(x, y, z);
        Line(P(-60, -60), P(-60, top - 15), 2);
        Arc(P(-45, top - 15), 15, 180, 90, 2);
        Line(P(-45, top), P(45, top), 2);
        Arc(P(45, top - 15), 15, 90, 0, 2);
        Line(P(60, top - 15), P(60, -45), 2);
        Arc(P(45, -45), 15, 0, -90, 2);
        Line(P(45, -60), P(15, -60), 2);
        Arc(P(15, -45), 15, -90, -180, 2);
        Line(P(0, -45), P(0, 55), 2); // final100m; final80m begins z=-25.
    }

    static float SafeStation(float original, int floor, float lead, float tail, bool branch)
    {
        var candidates = new List<float>();
        foreach (var s in straights.Where(s => s.floor == floor))
        {
            float lo = s.start + lead, hi = s.end - tail;
            if (branch) { lo = Mathf.Max(lo, fork.distance + fork.transitionLength + 8); hi = Mathf.Min(hi, fork.rewardDistance - 12); }
            if (hi < lo) continue;
            candidates.Add(Mathf.Clamp(original, lo, hi)); candidates.Add(lo); candidates.Add(hi);
            foreach (float other in occupied) { if (other - 17 >= lo && other - 17 <= hi) candidates.Add(other - 17); if (other + 17 >= lo && other + 17 <= hi) candidates.Add(other + 17); }
        }
        var ordered = candidates.Where(d => !occupied.Any(o => Mathf.Abs(o - d) < 16.9f))
            .Where(d => branch || floor != 1 || d <= fork.distance - 8 || d >= fork.endDistance + 8)
            .OrderBy(d => Mathf.Abs(d - original)).ToArray();
        if (ordered.Length == 0) throw new InvalidOperationException("No straight safe station for " + original + " on floor " + floor);
        occupied.Add(ordered[0]); return ordered[0];
    }
    static void Note(string name, string kind, float before, float after)
    { moves.Add(new Move { name = name, kind = kind, before = before, after = after }); }
    static void Frame(float distance, int floor, out Vector3 center, out Quaternion yaw)
    {
        int index = route.SegmentAt(distance);
        // A lift boarding boundary belongs to its lower floor, not the next segment.
        if (route.segments[index].floor != floor) index = Array.FindLastIndex(route.segments, s => s.floor == floor);
        route.SampleSegment(index, distance, out center, out var forward); yaw = Quaternion.LookRotation(forward);
    }
    static float Offset(float d, float amount, int selection)
        => Chapter45Route.BranchOffset(d, fork.distance, fork.endDistance, selection == 0 ? -amount : amount, fork.transitionLength);
    static void MoveRoot(Transform root, float oldDistance, float newDistance, int floor, bool rootHasLocalHitbox, float branchDelta = 0)
    {
        var source = new Vector3(0, Heights[floor], oldDistance);
        Frame(newDistance, floor, out var dest, out var yaw);
        var children = root.Cast<Transform>().ToArray();
        var positions = children.Select(t => t.position).ToArray();
        var rotations = children.Select(t => t.rotation).ToArray();
        var rootOffset = rootHasLocalHitbox ? root.position - source : Vector3.zero;
        if (rootHasLocalHitbox) rootOffset.x += branchDelta;
        var oldRotation = root.rotation;
        root.SetPositionAndRotation(dest + yaw * rootOffset, yaw * oldRotation);
        for (int i = 0; i < children.Length; i++)
        {
            var offset = positions[i] - source; offset.x += branchDelta;
            children[i].SetPositionAndRotation(dest + yaw * offset, yaw * rotations[i]);
        }
        var deck = root.GetComponent<Chapter45Deck>() ?? root.gameObject.AddComponent<Chapter45Deck>(); deck.floor = floor;
    }
    static void RemapTargets(float oldBranch)
    {
        // Paired left/right panels retain one shared station. Reserve the captain first.
        var groups = director.GetComponentsInChildren<Chapter45Target>(true).GroupBy(t => Mathf.RoundToInt(t.distance * 100)).OrderByDescending(g => g.Any(t => t.captain));
        foreach (var group in groups)
        {
            var first = group.First(); float old = first.distance;
            float at = SafeStation(old, first.floor, 32, 14, first.choiceIndex >= 0);
            foreach (var target in group)
            {
                float branch = target.choiceIndex >= 0 ? Offset(at, fork.branchOffset, target.requiredChoice) - Offset(old, oldBranch, target.requiredChoice) : 0;
                MoveRoot(target.transform, old, at, target.floor, true, branch);
                target.distance = at;
                Note(target.name, "target", old, at);
            }
        }
        // Reserve the physical bonus choice's short decision area.
        foreach (var c in director.GetComponentsInChildren<Chapter45Choice>(true)) if (c.kind != Chapter45Choice.ChoiceKind.RouteFork) occupied.Add(c.distance);
    }
    static void RemapHazards()
    {
        var captain = director.GetComponentsInChildren<Chapter45Target>(true).Single(t => t.captain);
        var captainMove = moves.Single(m => m.name == captain.name && m.kind == "target");
        foreach (var hazard in director.GetComponentsInChildren<Chapter45Hazard>(true).OrderBy(h => h.distance))
        {
            float old = hazard.distance;
            float at = hazard.manualOnly ? old + captainMove.after - captainMove.before : SafeStation(old, hazard.floor, Mathf.Max(25, hazard.warningDistance + 8), 14, hazard.choiceIndex >= 0);
            MoveRoot(hazard.transform, old, at, hazard.floor, false);
            hazard.distance = at;
            Note(hazard.name, hazard.manualOnly ? "captain pressure" : "hazard", old, at);
        }
    }
    static void RemapEncounters(float oldBranch)
    {
        foreach (var encounter in director.GetComponentsInChildren<Chapter45Encounter>(true).OrderBy(e => e.stopDistance))
        {
            var actors = encounter.actors.Where(a => a != null).ToArray();
            if (actors.Length == 0) continue;
            float old = actors.Average(a => a.transform.position.z);
            float at = SafeStation(old, encounter.floor, 32, 14, encounter.choiceIndex >= 0);
            float branch = encounter.choiceIndex >= 0 ? Offset(at, fork.branchOffset, encounter.requiredChoice) - Offset(old, oldBranch, encounter.requiredChoice) : 0;
            MoveRoot(encounter.transform, old, at, encounter.floor, false, branch);
            encounter.activationDistance += at - old; encounter.stopDistance += at - old;
            foreach (var actor in actors) { var deck = actor.GetComponent<Chapter45Deck>() ?? actor.gameObject.AddComponent<Chapter45Deck>(); deck.floor = encounter.floor; }
            Note(encounter.name, "encounter", old, at);
        }
    }
    static void RemapChoices(float oldBranch)
    {
        foreach (var choice in director.GetComponentsInChildren<Chapter45Choice>(true))
        {
            var children = choice.transform.Cast<Transform>().ToArray();
            var positions = children.Select(t => t.position).ToArray(); var rotations = children.Select(t => t.rotation).ToArray();
            int floor = choice.distance < 530 ? 0 : choice.distance < 1120 ? 1 : 2;
            Frame(choice.distance, floor, out var center, out var yaw); choice.transform.SetPositionAndRotation(center, yaw);
            for (int i = 0; i < children.Length; i++)
            {
                float station = Mathf.Clamp(positions[i].z, floor == 0 ? 0 : floor == 1 ? 530 : 1120, floor == 0 ? 530 : floor == 1 ? 1120 : 1650);
                Frame(station, floor, out var at, out var frame);
                float lane = positions[i].x;
                if (choice == fork && Mathf.Abs(lane) > 10) lane += Offset(station, fork.branchOffset, lane > 0 ? 1 : 0) - Offset(station, oldBranch, lane > 0 ? 1 : 0);
                children[i].SetPositionAndRotation(at + frame * new Vector3(lane, positions[i].y - Heights[floor], 0), frame * rotations[i]);
            }
        }
    }
    static void RemapGoals()
    {
        foreach (var goal in director.GetComponentsInChildren<Chapter45Goal>(true))
        {
            float old = goal.transform.position.z;
            MoveRoot(goal.transform, old, old, goal.floor, true);
            Note(goal.name, "physical goal", old, old);
        }
    }
    static void RemapLifts()
    {
        foreach (var lift in director.GetComponentsInChildren<Chapter45Lift>(true).OrderBy(l => l.afterSegment))
        {
            int floor = lift.afterSegment; float d = floor == 0 ? 530 : 1120;
            MoveRoot(lift.transform, d, d, floor, false);
            lift.afterSegment = Array.FindLastIndex(route.segments, s => s.floor == floor); lift.duration = 6;
            Note(lift.name, "six-second vertical lift", d, d);
        }
    }
    static void FoldShowroom()
    {
        var showroom = scenery.Find("InsideTheSneaker");
        route.Sample(1650, out var end, out var forward);
        if (Vector3.Dot(forward, Vector3.forward) < .999f) throw new InvalidOperationException("Crown corridor must face +Z.");
        showroom.position += end - new Vector3(0, 235, 1650);
        var visibility = showroom.GetComponent<Chapter45SceneryGroup>() ?? showroom.gameObject.AddComponent<Chapter45SceneryGroup>();
        visibility.floor = 2; visibility.startDistance = 1510; visibility.endDistance = 1650;
    }

    static Material Art(string name) => AssetDatabase.LoadAssetAtPath<Material>(ArtRoot + "/" + name + ".mat") ?? throw new FileNotFoundException(name);
    static Transform Group(Transform parent, string name) { var g = new GameObject(name).transform; g.SetParent(parent, false); return g; }
    static GameObject Box(Transform parent, string name, Vector3 p, Vector3 size, Material mat, bool solid = false, Quaternion? yaw = null)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
        g.transform.SetPositionAndRotation(p, yaw ?? Quaternion.identity); g.transform.localScale = size;
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
        if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
    }
    static void Strip(Transform parent, Vector3 a, Vector3 b, float width, Material mat, string name)
    {
        var v = b - a; if (v.sqrMagnitude < .00001f) return;
        Box(parent, name, (a + b) * .5f, new Vector3(width, .025f, v.magnitude + .04f), mat, false, Quaternion.LookRotation(v));
    }
    static void RebuildFloorArt()
    {
        ivory = Art("Ivory"); stone = Art("WarmLimestone"); blue = Art("TowerBlue"); brass = Art("Brass"); navy = Art("Navy"); green = Art("Jade");
        foreach (var child in roads.Cast<Transform>().ToArray())
            if (child.name == "Walkable") Object.DestroyImmediate(child.gameObject);
        foreach (var child in scenery.Cast<Transform>().ToArray())
            if (child.name.StartsWith("Deck_") || child.name.StartsWith("SkylineFromDeck_")) Object.DestroyImmediate(child.gameObject);
        for (int floor = 0; floor < 3; floor++)
        {
            var g = Group(scenery, "TowerFold_Floor_" + floor);
            var visibility = g.gameObject.AddComponent<Chapter45SceneryGroup>(); visibility.floor = floor; visibility.alwaysVisible = true;
            visibility.startDistance = floor == 0 ? 0 : floor == 1 ? 530 : 1120; visibility.endDistance = floor == 0 ? 530 : floor == 1 ? 1120 : 1650;
            var deck = g.gameObject.AddComponent<Chapter45Deck>(); deck.floor = floor;
            float y = Heights[floor];
            // One actual support slab, renderer grouped with current floor only.
            Box(g, "ContinuousSupportedDeck", new Vector3(0, y - .2f, -5), new Vector3(160, .4f, 180), stone, true);
            foreach (var s in route.segments.Where(s => s.floor == floor))
            {
                Strip(g, s.start + Vector3.up * .012f, s.end + Vector3.up * .012f, 11.5f, ivory, "MainWalkInlay");
                var right = Vector3.Cross(Vector3.up, (s.end - s.start).normalized);
                foreach (int side in new[] { -1, 1 }) Strip(g, s.start + right * side * 5.5f + Vector3.up * .03f, s.end + right * side * 5.5f + Vector3.up * .03f, .09f, brass, "RouteEdge");
            }
            if (floor == 1)
            {
                for (float d = fork.distance; d < fork.endDistance; d += 1.5f)
                {
                    float next = Mathf.Min(fork.endDistance, d + 1.5f);
                    route.Sample(d, out var a, out var af); route.Sample(next, out var b, out var bf);
                    foreach (int side in new[] { 0, 1 })
                    {
                        a.y = b.y = y + .055f;
                        var aa = a + Vector3.Cross(Vector3.up, af) * Offset(d, fork.branchOffset, side);
                        var bb = b + Vector3.Cross(Vector3.up, bf) * Offset(next, fork.branchOffset, side);
                        Strip(g, aa, bb, 8, side == 0 ? green : ivory, side == 0 ? "MaintenanceCorridor" : "ArchiveCorridor");
                    }
                }
            }
            // Camera-open interior perimeter: waist-height glazing, thin frames,
            // no opaque full-height near-camera walls or overhead floor hiding.
            foreach (int side in new[] { -1, 1 })
            {
                Box(g, "GlazedPerimeterSill", new Vector3(side * 78, y + .6f, -5), new Vector3(.35f, 1.2f, 176), blue);
                Box(g, "PerimeterHandrail", new Vector3(side * 78, y + 1.3f, -5), new Vector3(.14f, .14f, 176), brass);
                for (int i = 0; i <= 8; i++) Box(g, "WindowMullion", new Vector3(side * 78, y + 5, -91 + i * 21.5f), new Vector3(.4f, 10, .4f), ivory);
            }
            foreach (float z in new[] { -93f, 83f })
            {
                Box(g, "GlazedEndSill", new Vector3(0, y + .6f, z), new Vector3(156, 1.2f, .35f), blue);
                for (int i = 0; i <= 8; i++) Box(g, "WindowMullion", new Vector3(-78 + i * 19.5f, y + 5, z), new Vector3(.4f, 10, .4f), ivory);
            }
            foreach (float z in new[] { -90f, 80f }) Box(g, "HighPerimeterBeam", new Vector3(0, y + 12, z), new Vector3(156, .5f, .7f), navy);
            MergeStatic(g, "floor" + floor);
        }
        var vista = Group(scenery, "TowerFold_CommonSeoulVista");
        var distant = vista.gameObject.AddComponent<Chapter45SceneryGroup>(); distant.floor = -1; distant.alwaysVisible = true;
        for (int i = 0; i < 28; i++)
        {
            float angle = i * Mathf.PI * 2 / 28; float radius = 190 + (i % 4) * 32; float h = 18 + (i % 7) * 9;
            var p = new Vector3(Mathf.Cos(angle) * radius, h * .5f - 2, Mathf.Sin(angle) * radius);
            Box(vista, "SeoulBlock", p, new Vector3(22 + i % 4 * 5, h, 29), i % 3 == 0 ? stone : blue);
        }
        MergeStatic(vista, "vista");
        var fins = Group(scenery, "TowerFold_ContinuousExteriorFins");
        foreach (float x in new[] { -79f, 79f }) foreach (float z in new[] { -94f, 84f })
            Box(fins, "TowerVerticalFrame", new Vector3(x, 122, z), new Vector3(.65f, 244, .65f), ivory);
        MergeStatic(fins, "fins");
    }
    static void MergeStatic(Transform parent, string key)
    {
        var filters = parent.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null).ToArray();
        foreach (var batch in filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            if (batch.Key == null) continue;
            var combines = batch.Select(f => new CombineInstance { mesh = f.sharedMesh, subMeshIndex = 0, transform = parent.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray();
            var mesh = new Mesh { name = "TowerFold " + key + " " + batch.Key.name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(combines, true, true); mesh.RecalculateBounds();
            string path = ArtRoot + "/TowerFold_" + stamp + "_" + key + "_" + batch.Key.name + ".asset";
            AssetDatabase.CreateAsset(mesh, path);
            var output = new GameObject(batch.Key.name, typeof(MeshFilter), typeof(MeshRenderer)); output.transform.SetParent(parent, false);
            output.GetComponent<MeshFilter>().sharedMesh = mesh; var renderer = output.GetComponent<MeshRenderer>(); renderer.sharedMaterial = batch.Key; renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        foreach (var f in filters) { Object.DestroyImmediate(f.GetComponent<MeshRenderer>()); Object.DestroyImmediate(f); }
    }
    static void Rebind()
    {
        director.encounters = director.GetComponentsInChildren<Chapter45Encounter>(true);
        director.choices = director.GetComponentsInChildren<Chapter45Choice>(true).OrderBy(c => c.distance).ToArray();
        director.targets = director.GetComponentsInChildren<Chapter45Target>(true);
        director.hazards = director.GetComponentsInChildren<Chapter45Hazard>(true);
        director.lifts = director.GetComponentsInChildren<Chapter45Lift>(true).OrderBy(l => l.afterSegment).ToArray();
        director.goals = director.GetComponentsInChildren<Chapter45Goal>(true);
    }
    static void ResetStartPose()
    {
        var player = Object.FindObjectsByType<PlayerScript>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(p => p.gameObject.scene == world.gameObject.scene);
        route.Sample(0, out var point, out var forward); player.transform.SetPositionAndRotation(point + Vector3.up * director.footOffset, Quaternion.LookRotation(forward));
        var camera = Camera.main;
        if (camera != null)
        {
            var yaw = Quaternion.LookRotation(forward);
            camera.transform.SetPositionAndRotation(player.transform.position + yaw * new Vector3(0, 13, -23), yaw * Quaternion.Euler(22, 0, 0));
            var follow = camera.GetComponent<StableGameplayCamera>(); if (follow != null) follow.Configure(player.transform);
        }
    }
    static Receipt Verify()
    {
        if (Mathf.Abs(route.Length - 1650) > .08f) throw new InvalidOperationException("Total metric route length changed: " + route.Length);
        var liftArray = director.GetComponentsInChildren<Chapter45Lift>(true).OrderBy(l => l.afterSegment).ToArray();
        if (liftArray.Length != 2) throw new InvalidOperationException("Lift count changed.");
        var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var hi = -lo;
        for (int i = 0; i < route.segments.Length; i++)
        {
            var s = route.segments[i];
            if (Mathf.Abs(s.Length - Vector3.Distance(s.start, s.end)) > .01f) throw new InvalidOperationException("Nonmetric route segment.");
            if (Mathf.Abs(s.start.y - s.end.y) > .001f) throw new InvalidOperationException("Combat segment is not flat.");
            foreach (var p in new[] { s.start, s.end }) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
            if (i + 1 < route.segments.Length && s.floor == route.segments[i + 1].floor && Vector3.Distance(s.end, route.segments[i + 1].start) > .01f)
                throw new InvalidOperationException("Disconnected same-floor route.");
        }
        for (int i = 0; i < 2; i++)
        {
            var lift = liftArray[i]; var a = route.segments[lift.afterSegment].end; var b = route.segments[lift.afterSegment + 1].start;
            if (Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)) > .01f || b.y <= a.y || Mathf.Abs(lift.duration - 6) > .001f)
                throw new InvalidOperationException("Lift is not a six-second vertical transfer.");
            if (Mathf.Abs(route.SegmentEnd(lift.afterSegment) - (i == 0 ? 530 : 1120)) > .08f)
                throw new InvalidOperationException("Lift progress boundary changed.");
        }
        for (float d = 1570; d <= 1650; d += 2)
        {
            route.Sample(d, out var p, out var f);
            if (Vector3.Dot(f, Vector3.forward) < .999f || Mathf.Abs(p.x) > .01f || p.z < -25.1f || p.z > 55.1f)
                throw new InvalidOperationException("Final80m is not the promised +Z crown corridor.");
        }
        if (lo.x < -70 || hi.x > 70 || lo.z < -85 || hi.z > 80) throw new InvalidOperationException("Route escaped tower footprint.");
        return new Receipt
        {
            scene = ScenePath, marker = Marker, length = route.Length, segments = route.segments.Length, lifts = 2,
            liftDistances = liftArray.Select(l => route.SegmentEnd(l.afterSegment)).ToArray(), liftSeconds = liftArray.Select(l => l.duration).ToArray(),
            boundsMin = lo, boundsMax = hi, stationMoves = new List<Move>(moves),
            contracts = new[] { "Metric progress: segment.length equals physical polyline length", "Shared support footprint160x180 atY0/95/235", "Two vertical6s lifts at530/1120", "Final80m straight+Z; original crown shell and physical offering retained", "Targets/hazards/enemies moved out of bends; station deltas recorded", "Floor renderers grouped; support colliders retained; current floor always visible", "Native gameplay/branch/turn/lift/crown and frame-cost review still required" }
        };
    }
}
