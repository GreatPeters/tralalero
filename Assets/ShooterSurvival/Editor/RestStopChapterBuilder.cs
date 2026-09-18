using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using IndianOceanAssets.ShooterSurvival.Analytics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// A separate chapter, authored from the same placement manifest used by Data.xlsx.
public static class RestStopChapterBuilder
{
    public const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/RestStop.unity";
    private const string Record = "map-concepts/chapters-polish-2026-09-12";
    private const string RoadPrefab = "Assets/ShooterSurvival/Prefabs/Highway/Roads/HighwayStraight.prefab";
    private static Transform roads, props, enemies, bonuses, targets;
    private static Vector3[] points;
    private static Material asphalt, concrete, paint, grass, yellow;
    private static TMP_FontAsset font;
    private static JObject layout;
    private static readonly List<Transform> occluders = new();

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s => s.isDirty))
            throw new InvalidOperationException("Save current scene work before creating RestStop.");
        if (File.Exists(ScenePath)) throw new InvalidOperationException("RestStop already exists; refine the saved scene instead of replacing it.");
        layout = JObject.Parse(File.ReadAllText(Record + "/reststop-layout.json"));
        points = layout["points"].Select(p => new Vector3((float)p[0], (float)p[1], (float)p[2])).ToArray();
        foreach (string role in layout["enemies"].Select(e => (string)e["model"]).Distinct()) RequirePrefab(ChapterMascotImporter.PrefabPath(role));
        RequirePrefab(RoadPrefab);
        foreach (string key in new[] { "reststop_hall", "reststop_fuel_canopy", "reststop_ev_charger", "reststop_restroom", "reststop_kiosk", "reststop_picnic_shelter", "reststop_vending", "reststop_wayfinding" })
            RequirePrefab(RestStopAssetImporter.Prefabs + "/" + key + ".prefab");
        if (!AssetDatabase.CopyAsset(HighwaySceneBuilder.ScenePath, ScenePath)) throw new IOException("RestStop scene copy failed.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        foreach (var child in map.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        roads = Group(map, "Roads"); props = Group(map, "Props"); enemies = Group(map, "Enemies");
        bonuses = Group(map, "Bonuses"); targets = Group(map, "RestStop_EnemyTargets");
        foreach (string name in new[] { "Water", "MapTool_Work_Grid", "MapTool_Work_Floor", "MapTool_Origin_Post" }) Group(map, name);
        asphalt = RouteMaterial("Asphalt"); concrete = RouteMaterial("Concrete"); paint = RouteMaterial("RoadPaint");
        grass = RouteMaterial("Ground"); yellow = RouteMaterial("WarningYellow");
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas");
        font = canvas.GetComponentInChildren<OpeningStoryUI>(true)?.captionText.font;
        occluders.Clear();
        BuildRoad(); BuildScenery(); BuildEncounters(); BuildBonuses(); BuildGimmicks();
        var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerScript>(true)).Single();
        player.transform.SetPositionAndRotation(points[0] + Vector3.up * .12f, Quaternion.identity);
        player.GetComponent<NoryangjinRoadHeightFollower>().Configure(roads, .12f);
        var camera = Camera.main;
        camera.GetComponent<NoryangjinCameraOcclusion>().Configure(player.transform, roads);
        camera.GetComponent<NoryangjinCameraOcclusion>().ConfigureAdditionalOccluders(occluders.ToArray());
        camera.farClipPlane = 310;
        var playerData = new SerializedObject(player);
        playerData.FindProperty("xRange").vector2Value = new Vector2(-4.4f, 4.4f);
        playerData.ApplyModifiedPropertiesWithoutUndo();
        var exit = Group(map, "RestStop_StageExit");
        exit.SetPositionAndRotation(Sample((float)layout["length"] - 10, out var direction), Quaternion.LookRotation(direction));
        exit.gameObject.tag = "GameEndTriggerTag";
        var exitCollider = exit.gameObject.AddComponent<BoxCollider>();
        exitCollider.isTrigger = true; exitCollider.center = Vector3.up * 1.5f; exitCollider.size = new Vector3(14, 3, 1);
        var chapter = canvas.GetComponent<ChapterProgression>() ?? canvas.AddComponent<ChapterProgression>();
        chapter.chapter = 3; chapter.nextScene = ""; chapter.nextChapterMovie = null;
        chapter.nextChapterTitle = ""; chapter.nextChapterCaption = "";
        var analytics = canvas.GetComponent<GameplayAnalyticsSceneContext>() ?? canvas.AddComponent<GameplayAnalyticsSceneContext>();
        analytics.Configure(3, 1, 6, "forward_march", true);
        if (map.GetComponent<EncounterPlacementController>() == null) map.gameObject.AddComponent<EncounterPlacementController>();
        OpeningWorkshopPresentation.ApplyOpening(canvas);
        chapter.transitionUI = OpeningWorkshopPresentation.BuildTransition(canvas);
        var light = scene.GetRootGameObjects().Single(g => g.name == "MapTool_DirectionalLight").GetComponent<Light>();
        light.color = new Color(1, .94f, .83f); light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(48, -32, 0);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(.65f, .79f, .83f); RenderSettings.fogStartDistance = 100; RenderSettings.fogEndDistance = 270;
        Physics.SyncTransforms();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        int enemyCount = enemies.GetComponentsInChildren<EnemyEventController>(true).Length;
        int pairCount = bonuses.GetComponentsInChildren<BonusWallChoicePair>(true).Length;
        int hazardStations = props.Cast<Transform>().Count(t => t.name.StartsWith("RST_G", StringComparison.Ordinal));
        if (enemyCount != 50 || pairCount != 25 || hazardStations != 25) throw new InvalidOperationException("RestStop encounter count mismatch.");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save RestStop.");
        var build = EditorBuildSettings.scenes.ToList();
        if (!build.Any(s => s.path == ScenePath)) build.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = build.ToArray();
        AssetDatabase.SaveAssets();
        var result = new { scene = ScenePath, length = (float)layout["length"], enemyCount, pairCount, hazardStations, roadTiles = roads.childCount, scenery = props.childCount, liveVerification = "pending" };
        File.WriteAllText(Record + "/reststop-authored.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        NoryangjinMapToolWindow.Open();
        return result;
    }

    private static Vector3 Sample(float distance, out Vector3 forward)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            var delta = points[i + 1] - points[i]; float length = delta.magnitude;
            if (distance <= length || i == points.Length - 2) { forward = delta.normalized; return points[i] + forward * Mathf.Clamp(distance, 0, length); }
            distance -= length;
        }
        throw new InvalidOperationException("Route has no segments.");
    }

    private static void BuildRoad()
    {
        int serial = 0;
        for (int segment = 0; segment < points.Length - 1; segment++)
        {
            Vector3 delta = points[segment + 1] - points[segment]; int count = Mathf.RoundToInt(delta.magnitude / 20);
            for (int tile = 0; tile < count; tile++)
            {
                var road = Instance(RoadPrefab, roads, "RST_R" + (++serial).ToString("D3"));
                road.transform.SetPositionAndRotation(Vector3.Lerp(points[segment], points[segment + 1], (tile + .5f) / count), Quaternion.LookRotation(delta));
                var renderer = road.GetComponent<MeshRenderer>(); var materials = renderer.sharedMaterials;
                if (segment == 1 || segment == 2 || segment == 5) materials[0] = concrete;
                renderer.sharedMaterials = materials;
                foreach (int side in new[] { -1, 1 }) Cube(road.transform, "Kerb", new Vector3(side * 7.1f, .1f, 0), new Vector3(.2f, .2f, 20), concrete);
            }
            if (segment >= points.Length - 2) continue;
            var corner = Instance(RoadPrefab, roads, "RST_Corner_" + segment);
            corner.transform.position = points[segment + 1]; corner.transform.localScale = new Vector3(1, 1, .7f);
            var spot = Group(props, "RestStop_Turn_" + segment);
            spot.SetPositionAndRotation(points[segment + 1], Quaternion.LookRotation(delta));
            var turn = spot.gameObject.AddComponent<NoryangjinTurnSpot>();
            turn.TargetYawDegrees = Quaternion.LookRotation(points[segment + 2] - points[segment + 1]).eulerAngles.y; turn.TurnDurationSeconds = .45f;
            var trigger = spot.GetComponent<BoxCollider>(); trigger.isTrigger = true; trigger.center = Vector3.up; trigger.size = new Vector3(14, 2, .8f);
        }
    }

    private static void BuildScenery()
    {
        Cube(props, "RestStop_Landscape", new Vector3(70, -.25f, 190), new Vector3(550, .4f, 1320), grass);
        int serial = 0;
        foreach (var zone in layout["zones"])
        {
            int index = (int)zone["index"]; float start = (float)zone["start"], length = (float)zone["length"];
            var deckPoint = Sample(start + length * .5f, out var deckForward);
            var deck = Group(props, "RestStop_Deck_" + index); deck.SetPositionAndRotation(deckPoint, Quaternion.LookRotation(deckForward));
            Cube(deck, "Forecourt", new Vector3(0, -.05f, 0), new Vector3(64, .05f, length - 30), index == 0 || index == 4 ? asphalt : concrete);
            for (float distance = start + 45; distance < start + length - 30; distance += 24)
            {
                var p = Sample(distance, out var forward); var right = Vector3.Cross(Vector3.up, forward);
                var node = Group(props, "RestStop_Block_" + (++serial)); node.SetPositionAndRotation(p, Quaternion.LookRotation(forward));
                foreach (int side in new[] { -1, 1 })
                {
                    var tree = Instance("Assets/ithappy/Megacity/Prefabs/Props/tree_012.prefab", node, "Tree");
                    FitToHeight(tree, 5.5f); tree.transform.position += p + right * (side * 27) + forward * 14 - Vector3.up * .025f;
                    if ((serial + side) % 3 == 0) PlaceHighway(65, node, p + right * (side * 8), forward);
                }
                if (index == 0 || index == 4)
                {
                    Parking(node, p, forward, serial);
                    if (serial % 3 == 0) PlaceRest(index == 0 ? "reststop_hall" : "reststop_fuel_canopy", node, p + right * 26, -right);
                    if (index == 4 && serial % 2 == 0) PlaceHighway(54, node, p - right * 24, forward);
                    if (serial % 2 == 0) PlaceAmbient("ParkingMarshal", node, p + right * 9 + forward * 3, -right);
                }
                else if (index == 1)
                {
                    PlaceRest(serial % 3 == 0 ? "reststop_hall" : "reststop_kiosk", node, p + right * 16, -right);
                    PlaceRest(serial % 2 == 0 ? "reststop_hall" : "reststop_restroom", node, p - right * 21, right);
                    PlaceRest("reststop_vending", node, p - right * 10, right);
                    PlaceAmbient("SnackChef", node, p + right * 12 + forward * 2, -right);
                    PlaceFurniture("trash_001", node, p - right * 9 - forward * 4, forward, .9f);
                }
                else if (index == 2)
                {
                    PlaceRest("reststop_picnic_shelter", node, p + right * 15, -right);
                    PlaceFurniture("table_001", node, p + right * 15, forward, 2.2f);
                    PlaceFurniture("bench_001", node, p + right * 17, -right, 2.2f);
                    PlaceFurniture("bench_001", node, p + right * 13, right, 2.2f);
                    PlaceRest("reststop_kiosk", node, p - right * 15, right);
                    PlaceAmbient("CoffeeVendor", node, p + right * 12 + forward * 2, right);
                    PlaceAmbient("SnackChef", node, p - right * 12, right);
                    Crosswalk(node);
                }
                else if (index == 3)
                {
                    for (int bay = 0; bay < 3; bay++) PlaceRest("reststop_ev_charger", node, p + right * 15 + forward * (bay * 5 - 5), -right);
                    PlaceRest("reststop_fuel_canopy", node, p - right * 20, right);
                    PlaceHighway(82, node, p + right * 11, forward);
                    if (serial % 2 == 0) PlaceAmbient("DeliveryRider", node, p - right * 13, right);
                }
                else
                {
                    PlaceRest(serial % 2 == 0 ? "reststop_restroom" : "reststop_picnic_shelter", node, p + right * 16, -right);
                    PlaceFurniture("bench_001", node, p - right * 12, right, 2.2f);
                    PlaceRest("reststop_wayfinding", node, p - right * 9 + forward * 10, right);
                    if (serial % 2 == 0) PlaceAmbient("ParkingMarshal", node, p + right * 12, -right);
                }
            }
            var signPosition = Sample(start + 35, out var direction);
            var sign = Group(props, "Zone_" + (string)zone["name"]); sign.SetPositionAndRotation(signPosition + Vector3.Cross(Vector3.up, direction) * 9 + Vector3.up * 3, Quaternion.LookRotation(-direction));
            var text = sign.gameObject.AddComponent<TextMeshPro>(); text.font = font; text.text = (string)zone["name"];
            text.fontSize = 4; text.alignment = TextAlignmentOptions.Center; text.color = new Color(.95f, .83f, .48f); text.rectTransform.sizeDelta = new Vector2(10, 2);
        }
    }

    private static void Parking(Transform parent, Vector3 p, Vector3 forward, int serial)
    {
        var right = Vector3.Cross(Vector3.up, forward);
        foreach (int side in new[] { -1, 1 }) for (int bay = 0; bay < 4; bay++)
        {
            float z = bay * 5 - 7.5f;
            Cube(parent, "ParkingLine", new Vector3(side * 14, .012f, z - 2), new Vector3(7, .018f, .12f), paint);
            if ((bay + serial) % 3 != 1) PlaceHighway(new[] { 67, 81, 82 }[(bay * 2 + serial + side + 1) % 3], parent, p + right * (side * 14) + forward * z, right * side);
        }
    }

    private static void Crosswalk(Transform parent)
    {
        for (int i = -5; i <= 5; i++) Cube(parent, "Crosswalk", new Vector3(i * 1.05f, .035f, -8), new Vector3(.62f, .02f, 3.5f), paint);
    }

    private static void BuildEncounters()
    {
        foreach (var row in layout["enemies"])
        {
            string id = (string)row["id"]; float distance = (float)row["distance"];
            var p = Sample(distance, out var forward); var right = Vector3.Cross(Vector3.up, forward);
            var enemy = Instance(ChapterMascotImporter.PrefabPath((string)row["model"]), enemies, id);
            var center = p + right * (float)row["lane"] + Vector3.up * .08f;
            bool ranged = (bool)row["ranged"]; float move = (float)row["move"];
            enemy.transform.SetPositionAndRotation(center - right * move * .5f, Quaternion.LookRotation(forward));
            var events = enemy.GetComponent<EnemyEventController>();
            if (ranged != enemy.GetComponent<EnemyScript_space>().HasConfiguredProjectile) throw new InvalidOperationException("Projectile contract differs: " + id);
            events.EventMode = ranged ? EnemyEventMode.Shoot : EnemyEventMode.PatrolBetweenStartAndTarget;
            events.PatrolAcrossRoad = !ranged; events.MoveSpeed = (float)row["speed"]; events.HideWhileWaiting = false;
            if (!ranged) { var target = Group(targets, id + "_Target"); target.position = center + right * move * .5f; events.TargetPoint = target; }
            var gate = Group(props, id + "_Activation"); gate.SetPositionAndRotation(Sample(distance - (float)row["lead"], out _), Quaternion.LookRotation(forward));
            var activation = gate.gameObject.AddComponent<EnemyEventActivationSpot>(); activation.Targets = new[] { events };
            var collider = gate.GetComponent<BoxCollider>(); collider.center = Vector3.up; collider.size = new Vector3(14, 2, .8f);
        }
    }

    private static void BuildBonuses()
    {
        foreach (var row in layout["bonuses"])
        {
            var p = Sample((float)row["distance"], out var forward); var right = Vector3.Cross(Vector3.up, forward);
            var choices = new AuthoredBonusWall[2]; string id = (string)row["id"];
            for (int side = 0; side < 2; side++)
            {
                var root = Instance("Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab", bonuses, id + (side == 0 ? "" : "_Right"));
                root.transform.SetPositionAndRotation(p + right * (side == 0 ? -3 : 3), Quaternion.LookRotation(-forward));
                root.transform.localScale = new Vector3(2.25f, 2.9f, 2.9f);
                choices[side] = root.GetComponent<AuthoredBonusWall>(); choices[side].Configure(Enum.Parse<Rarity>((string)row["rarity"]));
            }
            var pair = choices[0].GetComponent<BonusWallChoicePair>() ?? choices[0].gameObject.AddComponent<BonusWallChoicePair>();
            pair.Configure(choices[0], choices[1]);
        }
    }

    private static void BuildGimmicks()
    {
        foreach (var row in layout["gimmicks"])
        {
            string id = (string)row["id"], kind = (string)row["pattern"];
            var p = Sample((float)row["distance"], out var forward); var right = Vector3.Cross(Vector3.up, forward);
            if (kind == "HighwayToll")
            {
                var station = Group(props, id); station.SetPositionAndRotation(p, Quaternion.LookRotation(forward));
                for (int lane = 0; lane < 3; lane++)
                {
                    var gate = Instance("Assets/ShooterSurvival/Prefabs/Highway/Gimmicks/HighwayToll.prefab", station, id + "_Lane" + lane);
                    gate.transform.SetPositionAndRotation(p + right * ((lane - 1) * 3.6f), Quaternion.LookRotation(forward));
                    var hazard = gate.GetComponent<HighwayHazard>(); hazard.laneIndex = lane; hazard.cycleSeconds = (float)row["operation"];
                }
                continue;
            }
            var root = Instance("Assets/ShooterSurvival/Prefabs/Highway/Gimmicks/" + kind + ".prefab", props, id);
            root.transform.SetPositionAndRotation(p + right * (float)row["lane"], Quaternion.LookRotation(forward));
            var behavior = root.GetComponent<HighwayHazard>(); behavior.warningSeconds = (float)row["warning"];
            behavior.cycleSeconds = (float)row["operation"]; behavior.crossingDistance = (float)row["crossingDistance"];
        }
    }

    private static void PlaceRest(string key, Transform parent, Vector3 position, Vector3 facing)
    {
        var root = Instance(RestStopAssetImporter.Prefabs + "/" + key + ".prefab", parent, key);
        root.transform.SetPositionAndRotation(position - Vector3.up * .025f, Quaternion.LookRotation(facing));
        foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        if (key == "reststop_hall" || key == "reststop_fuel_canopy" || key == "reststop_picnic_shelter") occluders.Add(root.transform);
    }
    private static void PlaceAmbient(string role, Transform parent, Vector3 position, Vector3 facing)
    {
        var prefab = RequirePrefab(ChapterMascotImporter.PrefabPath(role));
        var body = prefab.transform.Find("Body");
        var actor = UnityEngine.Object.Instantiate(body.gameObject, parent);
        actor.name = "Ambient_" + role;
        actor.transform.SetPositionAndRotation(position - Vector3.up * .025f, Quaternion.LookRotation(facing));
        foreach (var point in actor.GetComponentsInChildren<Transform>(true).Where(t => t.name == "MascotThrowPoint").ToArray()) UnityEngine.Object.DestroyImmediate(point.gameObject);
        foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var rigidbody in actor.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rigidbody);
        foreach (var animator in actor.GetComponentsInChildren<Animator>(true)) animator.cullingMode = AnimatorCullingMode.CullCompletely;
    }

    public static object RefineSceneryOpenScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath) throw new InvalidOperationException("RestStop in Edit Mode required.");
        layout = JObject.Parse(File.ReadAllText(Record + "/reststop-layout.json"));
        points = layout["points"].Select(p => new Vector3((float)p[0],(float)p[1],(float)p[2])).ToArray();
        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        props = map.Find("Props");
        foreach (var child in props.Cast<Transform>().Where(t => t.name.StartsWith("RestStop_Block_",StringComparison.Ordinal) || t.name.StartsWith("RestStop_Deck_",StringComparison.Ordinal) || t.name.StartsWith("Zone_",StringComparison.Ordinal) || t.name == "RestStop_Landscape").ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        asphalt=RouteMaterial("Asphalt"); concrete=RouteMaterial("Concrete"); paint=RouteMaterial("RoadPaint"); grass=RouteMaterial("Ground"); yellow=RouteMaterial("WarningYellow");
        font=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponentInChildren<OpeningStoryUI>(true).captionText.font;
        occluders.Clear(); BuildScenery();
        Camera.main.GetComponent<NoryangjinCameraOcclusion>().ConfigureAdditionalOccluders(occluders.ToArray());
        foreach (var component in props.GetComponentsInChildren<Component>(true))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        var result = new { scene=scene.name, blocks=props.Cast<Transform>().Count(t=>t.name.StartsWith("RestStop_Block_")), ambientActors=props.GetComponentsInChildren<Animator>(true).Length, occluders=occluders.Count };
        File.WriteAllText(Record+"/reststop-scenery-refined.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        return result;
    }
    private static void PlaceHighway(int id, Transform parent, Vector3 position, Vector3 facing)
    {
        var root = Instance(HighwayAssetImporter.Prefabs + "/HWY_" + id.ToString("D3") + ".prefab", parent, "ParkProp_" + id);
        root.transform.SetPositionAndRotation(position - Vector3.up * .025f, Quaternion.LookRotation(facing));
        foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
    }
    private static void PlaceFurniture(string key, Transform parent, Vector3 position, Vector3 facing, float size)
    {
        var root = Instance("Assets/ithappy/Megacity/Prefabs/Props/" + key + ".prefab", parent, key);
        var bounds = HighwayAssetImporter.BoundsOf(root); root.transform.localScale *= size / Mathf.Max(bounds.size.x, bounds.size.z);
        root.transform.rotation = Quaternion.LookRotation(facing); bounds = HighwayAssetImporter.BoundsOf(root);
        root.transform.position += position - Vector3.up * .025f - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
    }
    private static void FitToHeight(GameObject root, float height)
    {
        var bounds = HighwayAssetImporter.BoundsOf(root); root.transform.localScale *= height / bounds.size.y;
        bounds = HighwayAssetImporter.BoundsOf(root); root.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
    }
    private static Material RouteMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/ShooterSurvival/Models/Highway/Route/" + name + ".mat");
    private static GameObject RequirePrefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new InvalidOperationException("Missing prefab: " + path);
    private static GameObject Instance(string path, Transform parent, string name)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(path), parent); root.name = name; root.SetActive(true); return root;
    }
    private static Transform Group(Transform parent, string name)
    {
        var root = new GameObject(name).transform; root.SetParent(parent, false); return root;
    }
    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube); root.name = name; root.transform.SetParent(parent, false);
        root.transform.localPosition = position; root.transform.localScale = size; root.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>()); return root;
    }
}
