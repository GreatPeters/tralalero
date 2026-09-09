#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit, one-shot migration of the reviewed old layout; not an automatic scene generator.
public static class NoryangjinSr18LatestEncounters
{
    public const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
    public const string PlanPath = "map-concepts/sr18-four-second-moving-enemies-2026-09-06/image-plan-center-left-2026-09-07.json";
    public const string RecordPath = "map-concepts/sr18-latest-elements-applied-2026-09-07";

    public static string Apply()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.isLoaded ||
            SceneManager.GetActiveScene() != scene || scene.isDirty)
            throw new InvalidOperationException("Open the clean, active SR18 scene in Edit Mode first.");
        if (File.Exists(RecordPath + "/placement-report.json"))
            throw new InvalidOperationException("Already applied; refusing to overwrite subsequent authoring.");

        var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
        var roads = map.Find("Roads"); var props = map.Find("Props");
        var enemies = map.Find("Enemies"); var bonuses = map.Find("Bonuses");
        var targets = map.Find("SR18_EnemyTargets");
        var old = JObject.Parse(File.ReadAllText("map-concepts/sr18-encounters-applied-2026-09-06/placement-report.json"));
        var owned = new List<GameObject>();
        foreach (JObject row in old["placements"])
        {
            var parent = (string)row["kind"] == "BonusWall" ? bonuses : enemies;
            var item = parent.Find((string)row["name"]);
            if (item == null || Vector3.Distance(item.position, V(row["position"])) > .02f ||
                Vector3.Distance(item.localScale, V(row["scale"])) > .02f ||
                Quaternion.Angle(item.rotation, Quaternion.Euler(0, (float)row["yaw"], 0)) > .1f)
                throw new InvalidOperationException("Old placement changed; inspect before replacing: " + row["name"]);
            owned.Add(item.gameObject);
        }
        foreach (JObject row in old["activationSpots"])
        {
            var item = props.Find((string)row["name"]);
            if (item == null || Vector3.Distance(item.position, V(row["position"])) > .02f)
                throw new InvalidOperationException("Old activation spot changed: " + row["name"]);
            owned.Add(item.gameObject);
        }
        if (roads.childCount != 230 || props.childCount != 697 || enemies.childCount != 69 ||
            bonuses.childCount != 14 || targets == null || targets.childCount != 27)
            throw new InvalidOperationException("Unexpected source composition; no replacement performed.");
        owned.Add(targets.gameObject);

        var protectedRoots = roads.Cast<Transform>().Concat(props.Cast<Transform>().Where(t => !owned.Contains(t.gameObject)))
            .Concat(map.Find("Water").Cast<Transform>()).ToArray();
        var components = protectedRoots.SelectMany(t => t.GetComponentsInChildren<Component>(true)).Where(c => c != null)
            .Distinct().ToDictionary(c => c, EditorJsonUtility.ToJson);
        var objects = protectedRoots.SelectMany(t => t.GetComponentsInChildren<Transform>(true))
            .ToDictionary(t => t.gameObject, t => EditorJsonUtility.ToJson(t.gameObject));
        var roadColliders = roads.GetComponentsInChildren<MeshCollider>(true);
        var defaults = AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>("Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset");
        var entries = (List<NoryangjinMapToolPalettePlacementEntry>)typeof(NoryangjinMapToolPaletteDefaults)
            .GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(defaults);
        var plan = JObject.Parse(File.ReadAllText(PlanPath));
        var events = plan["events"].Children<JObject>().ToArray();
        if (events.Length != 74 || events.Count(e => (string)e["kind"] == "enemy") != 25 ||
            events.Count(e => (string)e["kind"] == "bonus") != 25 || events.Count(e => (string)e["kind"] == "object") != 24)
            throw new InvalidOperationException("Unexpected plan totals.");
        var sections = JObject.Parse(File.ReadAllText("map-concepts/sr18-full-enemy-bonus-flow-2026-09-06/full-flow-final.json"))["sections"]
            .Children<JObject>().ToDictionary(s => (string)s["id"]);
        string backup = "tmp/backups/sr18-latest-elements-2026-09-07/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "/before.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup)); Directory.CreateDirectory(RecordPath);
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Backup failed.");
        string sourceHash = Hash(ScenePath);
        string map1 = Hash("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity");
        string map2 = Hash("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity");
        var rows = new List<object>(); var gates = new List<object>();
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply latest SR18 single-file encounters");
        bool saved = false;

        float Floor(Vector3 point, string section)
        {
            float expected = Height(section, point.x);
            foreach (Vector3 offset in new[] { Vector3.zero, Vector3.forward * .12f, Vector3.back * .12f, Vector3.left * .12f, Vector3.right * .12f })
                foreach (var c in roadColliders)
                    if (c.Raycast(new Ray(new Vector3(point.x, expected + 4, point.z) + offset, Vector3.down), out var hit, 8) &&
                        Mathf.Abs(hit.point.y - expected) < 1.1f && hit.normal.y > .7f) return hit.point.y;
            throw new InvalidOperationException("No matching road at " + section + " " + point);
        }
        GameObject Place(string path, string name, Transform parent, Vector3 point, Quaternion rotation, string section)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(go, "Place " + name); go.name = name;
            go.SetActive(true); // Some canonical single-part assets (Bucket) are stored inactive.
            var entry = entries.FirstOrDefault(e => e.prefabPath == path);
            go.transform.localScale = Vector3.Scale(prefab.transform.localScale, entry == null ? Vector3.one : entry.scale);
            point.y = Floor(point, section) + .08f;
            go.transform.SetPositionAndRotation(point, rotation);
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            return go;
        }
        void Record(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (c != null) { EditorUtility.SetDirty(c); PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        }
        try
        {
            foreach (var go in owned) Undo.DestroyObjectImmediate(go);
            var targetObject = new GameObject("SR18_EnemyTargets"); Undo.RegisterCreatedObjectUndo(targetObject, "Enemy targets");
            targetObject.transform.SetParent(map, false); targets = targetObject.transform;
            int enemyIndex = 0, bonusIndex = 0, gimmickIndex = 0;
            foreach (var e in events)
            {
                string section = (string)e["section"], kind = (string)e["kind"], role = (string)e["enemy_role"];
                int time = (int)e["time"];
                Vector3 start = V(sections[section]["start"]), end = V(sections[section]["end"]);
                var dir = (end - start).normalized; var right = Vector3.Cross(Vector3.up, dir);
                float length = Vector3.Distance(start, end), along = (float)e["along"];
                // Keep bodies/altars outside the corner pivot. The explicitly nudged bridge walls keep their exact XZ.
                float adjusted = Mathf.Clamp(along, 8, length - 8);
                Vector3 point = new Vector3((float)e["world_xz"][0], 0, (float)e["world_xz"][1]) + dir * (adjusted - along);
                Quaternion rotation = Quaternion.LookRotation(dir);
                GameObject go; string prefab; string subtype;
                if (kind == "enemy")
                {
                    enemyIndex++;
                    subtype = role == "ambush" ? (enemyIndex % 2 == 0 ? "Enemy_FatMan" : "Enemy_Guard") :
                        role == "patrol" ? "Enemy_YllowMan_Sword" : time == 293 ? "Enemy_Woman" :
                        enemyIndex % 3 == 0 ? "Enemy_YllowMan_Net" : "Enemy_OldMan";
                    prefab = "Assets/JH/Model/Prefab/" + subtype + ".prefab";
                    var origin = point + dir * (role == "patrol" ? -4 : role == "ambush" ? 2.5f : 0);
                    go = Place(prefab, $"SR18_L_E{enemyIndex:00}_T{time:000}_{subtype}", enemies, origin, rotation, section);
                    var controller = go.GetComponent<EnemyEventController>();
                    controller.EventMode = role == "ambush" ? EnemyEventMode.AmbushMoveThenShoot : role == "patrol" ?
                        EnemyEventMode.PatrolBetweenStartAndTarget : time == 5 ? EnemyEventMode.AttackOnce : EnemyEventMode.AttackLoop;
                    if (EnemyEventController.RequiresTarget(controller.EventMode))
                    {
                        var target = new GameObject(go.name + "_Target"); Undo.RegisterCreatedObjectUndo(target, "Movement target");
                        target.transform.SetParent(targets, false);
                        var destination = point + dir * (role == "patrol" ? 4 : 0);
                        destination.y = go.transform.position.y; target.transform.position = destination;
                        if (Mathf.Abs(Floor(destination, section) + .08f - destination.y) > .25f)
                            throw new InvalidOperationException("Moving enemy crosses a slope: " + go.name);
                        controller.TargetPoint = target.transform; controller.MoveSpeed = role == "patrol" ? 2.5f : 4;
                        controller.MoveAnimation = EnemyMoveAnimation.Run;
                    }
                    if (role == "ambush")
                    {
                        var so = new SerializedObject(go.GetComponent<EnemyScript_space>());
                        so.FindProperty("throwReleaseDelay").floatValue = .7f;
                        so.FindProperty("throwSpeed").floatValue = 14;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    float lead = time == 5 ? 0 : role == "ambush" ? 30 : 18;
                    var gatePoint = point - dir * Mathf.Min(lead, adjusted - 4);
                    var gate = Place("Assets/ShooterSurvival/Prefabs/Gameplay/Noryangjin_EnemyMovementTrigger.prefab",
                        $"SR18_L_E{enemyIndex:00}_Activation", props, gatePoint, rotation, section);
                    var box = gate.GetComponent<BoxCollider>(); box.center = new Vector3(0, 1, 0); box.size = new Vector3(4, 2, .8f); box.isTrigger = true;
                    gate.GetComponent<EnemyEventActivationSpot>().Targets = new[] { controller }; Record(gate);
                    gates.Add(new { name = gate.name, position = A(gate.transform.position), enemy = go.name, ahead = Vector3.Dot(origin - gatePoint, dir) });
                }
                else if (kind == "bonus")
                {
                    bonusIndex++; prefab = "Assets/ShooterSurvival/Prefabs/Walls/New/Box_left.prefab";
                    subtype = time == 297 ? "Unique" : bonusIndex % 4 == 0 ? "Rare" : "Normal";
                    float grade = (Height(section, point.x + dir.x * .5f) - Height(section, point.x - dir.x * .5f));
                    rotation = Quaternion.LookRotation(-dir - Vector3.up * grade, Vector3.up);
                    go = Place(prefab, $"SR18_L_B{bonusIndex:00}_T{time:000}", bonuses, point, rotation, section);
                    var altar = go.GetComponent<AuthoredBonusWall>() ?? Undo.AddComponent<AuthoredBonusWall>(go);
                    altar.Configure((Rarity)Enum.Parse(typeof(Rarity), subtype));
                    foreach (var wall in go.GetComponentsInChildren<WallScript>(true))
                    {
                        var marker = wall.GetComponent<RuntimeBonusWall>() ?? Undo.AddComponent<RuntimeBonusWall>(wall.gameObject);
                        marker.KeepAsMapAuthoredWall();
                    }
                }
                else
                {
                    gimmickIndex++;
                    bool slope = Mathf.Abs(Height(section, point.x + 1) - Height(section, point.x - 1)) > .1f;
                    subtype = slope || gimmickIndex % 3 == 1 ? "Bucket" : gimmickIndex % 3 == 2 ? "Light" : "Hole";
                    prefab = "Assets/ShooterSurvival/Prefabs/Obstacle_Real/" + (subtype == "Light" ? "Lights" : subtype) + ".prefab";
                    point += right * (gimmickIndex % 2 == 0 ? .8f : -.8f);
                    go = Place(prefab, $"SR18_L_G{gimmickIndex:00}_T{time:000}_{subtype}", props, point, rotation, section);
                    var parts = go.GetComponentsInChildren<ObstacleStats>(true);
                    foreach (var extra in parts.Skip(1)) extra.gameObject.SetActive(false);
                    var active = parts[0];
                    // Retain the canonical bundle connection while selecting exactly one existing lamp.
                    if (active.transform != go.transform)
                    {
                        Vector3 p = active.transform.position;
                        active.transform.position = new Vector3(point.x, p.y, point.z);
                    }
                    var body = active.GetComponent<BoxCollider>(); body.isTrigger = true;
                    var rigidbody = active.GetComponent<Rigidbody>();
                    if (rigidbody != null) { rigidbody.isKinematic = true; rigidbody.useGravity = false; rigidbody.detectCollisions = true; }
                    Physics.SyncTransforms();
                    float bottom = active.GetComponentInChildren<Renderer>().bounds.min.y;
                    go.transform.position += Vector3.up * (Floor(point, section) + .04f - bottom);
                    // Hole stays visibly on the timber surface; a side lane remains clear for all three types.
                }
                Record(go);
                rows.Add(new { time, kind, role, section, subtype, prefab, name = go.name, planned = e["world_xz"].ToObject<float[]>(),
                    position = A(go.transform.position), rotation = A(go.transform.eulerAngles), scale = A(go.transform.localScale), alongAdjustment = adjusted - along });
            }
            Physics.SyncTransforms();
            if (enemies.childCount != 25 || bonuses.childCount != 25 || targets.childCount != 10 || props.childCount != 715)
                throw new InvalidOperationException("Wrong installed totals.");
            foreach (var pair in components)
                if (pair.Key == null || EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("Protected map component changed.");
            foreach (var pair in objects)
                if (pair.Key == null || EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("Protected scenery changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
            saved = true; Undo.CollapseUndoOperations(group);
            if (map1 != Hash("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity") ||
                map2 != Hash("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_2.unity")) throw new IOException("Sibling scene changed.");
            string report = JsonConvert.SerializeObject(new { scene = ScenePath, plan = PlanPath, planHash = Hash(PlanPath), backup, sourceHash,
                afterHash = Hash(ScenePath), enemies = 25, bonuses = 25, gimmicks = 24, originalPropsPreserved = 666, roadsPreserved = 230,
                placements = rows, activationSpots = gates }, Formatting.Indented);
            File.WriteAllText(RecordPath + "/placement-report.json", report);
            return report;
        }
        catch { if (!saved) Undo.RevertAllDownToGroup(group); throw; }
    }

    public static float Height(string section, float x)
    {
        if (section == "S09") return Mathf.Min(Mathf.Clamp01((x + 55.7500763f) / 33.75f), Mathf.Clamp01((169.249924f - x) / 33.75f)) * 12;
        if (section == "S15") return Mathf.Min(Mathf.Clamp01((371.749939f - x) / 33.75f), Mathf.Clamp01((x - 281.749939f) / 33.75f)) * 12;
        return 0;
    }
    private static Vector3 V(JToken a) => new Vector3((float)a[0], (float)a[1], (float)a[2]);
    private static float[] A(Vector3 v) => new[] { v.x, v.y, v.z };
    private static string Hash(string path)
    {
        using var sha = System.Security.Cryptography.SHA256.Create(); using var stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
}
#endif
