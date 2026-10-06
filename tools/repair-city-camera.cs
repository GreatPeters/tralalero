using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

// Jamsil camera-only follow-up. Main() applies in clean idle Edit Mode.
// Measure() is read-only and can inspect a loaded Jamsil scene during native QA.
// No crown, renderer, collider, route, encounter, UI, or runtime source changes.
public static class RepairCityCamera
{
    const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity";
    const string Record = "outputs/chapters45-2026-10-02";
    const float Pitch = 7f, Fov = 70f, Aspect = 1080f / 2340f;
    static readonly Vector3 Offset = new Vector3(0, 13, -25);

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Idle Edit Mode required; do not apply during an ordinary run.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve dirty scenes before applying.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        Scene scene = default; bool saved = false;
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath);
            var camera = SceneCamera(scene);
            var player = Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.PlayerScript>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(p => p.gameObject.scene == scene);
            var follow = camera.GetComponent<StableGameplayCamera>();
            if (follow == null) throw new InvalidOperationException("Expected stable yaw-relative gameplay camera.");
            var evaluation = Measure();
            if (evaluation.rows.Where(r => r.distance >= 86 && r.distance <= 302).Any(r => !r.candidateCrown.allInFront || r.candidateCrown.top < .16f || r.candidateCrown.left < .04f || r.candidateCrown.right > .96f))
                throw new InvalidOperationException("Measured early crown would still overlap the HUD/banner region; inspect Measure().");
            string before = ProtectedState(scene);
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            Directory.CreateDirectory(Record);
            string backup = Record + "/Jamsil-before-camera-repair-" + stamp + ".unity";
            File.Copy(ScenePath, backup, false);
            var previous = new PoseSettings { position = camera.transform.position, rotation = camera.transform.rotation, offset = follow.yawRelativeOffset, pitch = follow.yawRelativeRotation.eulerAngles.x, fov = camera.fieldOfView };
            var yaw = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);
            camera.transform.SetPositionAndRotation(player.transform.position + yaw * Offset, yaw * Quaternion.Euler(Pitch, 0, 0));
            camera.fieldOfView = Fov;
            follow.Configure(player.transform);
            if (before != ProtectedState(scene)) throw new InvalidOperationException("Camera repair changed protected gameplay/collider state.");
            EditorUtility.SetDirty(camera); EditorUtility.SetDirty(follow);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Jamsil camera save failed.");
            saved = true;
            var receipt = new Receipt { scene = ScenePath, backup = backup, previous = previous,
                applied = new PoseSettings { position = camera.transform.position, rotation = camera.transform.rotation, offset = Offset, pitch = Pitch, fov = Fov },
                routeGameplayAndCollidersUnchanged = true, evaluation = evaluation,
                nativeValidationPending = "Capture ordinary play at 0/12/35/42/70 seconds. Early entire white shoe and supports must be below HUD; hero and firing corridor must remain readable. Late upper-tower crop intentionally yields to entrance focus." };
            File.WriteAllText(Record + "/city-camera-repair-" + stamp + ".json", JsonUtility.ToJson(receipt, true));
            return receipt;
        }
        finally
        {
            if (!saved && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    public static Evaluation Measure()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Load Jamsil before read-only Measure().");
        var camera = SceneCamera(scene);
        var follow = camera.GetComponent<StableGameplayCamera>();
        var director = Object.FindObjectsByType<Chapter45Director>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(d => d.gameObject.scene == scene && d.chapter == 4);
        var tower = director.transform.Find("Scenery/Jamsil_CityPolish_V1/JamsilCanonicalTower");
        var crown = tower != null ? tower.Find("GeneratedCrownAnchor/Crown_LOD0") : null;
        if (crown == null || director.route == null || follow == null) throw new InvalidOperationException("Expected polished city, generated crown and configured route.");
        var renderers = crown.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("Crown has no imported mesh renderers.");
        var crownBounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) crownBounds.Encapsulate(renderer.bounds);
        var supports = new Bounds(tower.TransformPoint(new Vector3(0, 379, 0)), new Vector3(76, 60, 28));
        var rows = new List<ProjectionRow>();
        foreach (float distance in new[] { 0f, 86f, 252f, 302f, 504f, 756f, 1008f, 1260f, 1512f, 1764f })
        {
            director.route.Sample(distance, out var routePoint, out var forward);
            var yaw = Quaternion.LookRotation(new Vector3(forward.x, 0, forward.z), Vector3.up);
            foreach (float lane in new[] { -2.4f, 0f, 2.4f })
            {
                var player = routePoint + Vector3.up * .12f + yaw * Vector3.right * lane;
                var oldPosition = player + yaw * follow.yawRelativeOffset;
                var oldRotation = yaw * follow.yawRelativeRotation;
                var newPosition = player + yaw * Offset;
                var newRotation = yaw * Quaternion.Euler(Pitch, 0, 0);
                var heroRoot = Project(player, newPosition, newRotation, Fov);
                rows.Add(new ProjectionRow { distance = distance, lane = lane,
                    currentCrown = ProjectBounds(crownBounds, oldPosition, oldRotation, camera.fieldOfView),
                    candidateCrown = ProjectBounds(crownBounds, newPosition, newRotation, Fov),
                    candidateSupports = ProjectBounds(supports, newPosition, newRotation, Fov),
                    candidateHeroRootFromTop = 1 - heroRoot.y });
            }
        }
        return new Evaluation { aspect = Aspect, candidatePitch = Pitch, candidateFov = Fov, candidateOffset = Offset,
            crownCenter = crownBounds.center, crownSize = crownBounds.size, rows = rows.ToArray(),
            interpretation = "Normalized coordinates: top=0, bottom=1. Native HP HUD ends near0.104; transient choice/title banner ends near0.153. Bounds projections are conservative geometry measurements, not a substitute for native visibility/readability review. Candidate does not change near/far planes or model transforms." };
    }

    static Camera SceneCamera(Scene scene) => Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(c => c.gameObject.scene == scene && c.CompareTag("MainCamera"));
    static string ProtectedState(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var behaviours = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c => c != null && !(c is StableGameplayCamera)).OrderBy(c => c.GetInstanceID());
        var colliders = roots.SelectMany(r => r.GetComponentsInChildren<Collider>(true)).OrderBy(c => c.GetInstanceID());
        return string.Join("\n", behaviours.Select(c => c.GetInstanceID() + ":" + EditorJsonUtility.ToJson(c))) + "\n" + string.Join("\n", colliders.Select(c => c.GetInstanceID() + ":" + EditorJsonUtility.ToJson(c) + ":" + c.transform.localToWorldMatrix.ToString()));
    }
    static Vector3 Project(Vector3 world, Vector3 position, Quaternion rotation, float fov)
    {
        var local = Quaternion.Inverse(rotation) * (world - position);
        float tan = Mathf.Tan(fov * Mathf.Deg2Rad * .5f);
        return new Vector3(.5f + local.x / (2 * local.z * tan * Aspect), .5f + local.y / (2 * local.z * tan), local.z);
    }
    static Frame ProjectBounds(Bounds bounds, Vector3 position, Quaternion rotation, float fov)
    {
        var result = new Frame { left = float.PositiveInfinity, top = float.PositiveInfinity, right = float.NegativeInfinity, bottom = float.NegativeInfinity, allInFront = true };
        foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
        {
            var point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
            var projected = Project(point, position, rotation, fov);
            result.left = Mathf.Min(result.left, projected.x); result.right = Mathf.Max(result.right, projected.x);
            result.top = Mathf.Min(result.top, 1 - projected.y); result.bottom = Mathf.Max(result.bottom, 1 - projected.y);
            result.allInFront &= projected.z > .1f;
        }
        return result;
    }
    [Serializable] public sealed class Frame { public float left, top, right, bottom; public bool allInFront; }
    [Serializable] public sealed class ProjectionRow { public float distance, lane, candidateHeroRootFromTop; public Frame currentCrown, candidateCrown, candidateSupports; }
    [Serializable] public sealed class Evaluation { public float aspect, candidatePitch, candidateFov; public Vector3 candidateOffset, crownCenter, crownSize; public ProjectionRow[] rows; public string interpretation; }
    [Serializable] public sealed class PoseSettings { public Vector3 position, offset; public Quaternion rotation; public float pitch, fov; }
    [Serializable] public sealed class Receipt { public string scene, backup, nativeValidationPending; public bool routeGameplayAndCollidersUnchanged; public PoseSettings previous, applied; public Evaluation evaluation; }
}
