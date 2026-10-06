using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IndianOceanAssets.ShooterSurvival;
using Object = UnityEngine.Object;

// Scene-local visual repair. Never edits a source prefab, controller or clip.
public static class RepairChapter45CityFlaggers
{
    const string ScenePath = "Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity";
    const string PrefabPath = "Assets/ShooterSurvival/Prefabs/MeshyRestStop20260925/C07_riot_police.prefab";
    const string OldController = "Assets/ShooterSurvival/Models/MeshyRestStop20260925/C08_flagger/C08_flagger.controller";
    const string NewController = "Assets/ShooterSurvival/Models/MeshyRestStop20260925/C07_riot_police/C07_riot_police.controller";
    static readonly string[] Required = { ForwardEnemyAnimationContract.Idle, ForwardEnemyAnimationContract.Walk, ForwardEnemyAnimationContract.Run,
        ForwardEnemyAnimationContract.AttackLoop, ForwardEnemyAnimationContract.AttackOnce, ForwardEnemyAnimationContract.Die };
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json")
        .GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    static string Hash(string value) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant(); }
    static string Id(Object value) => value != null ? GlobalObjectId.GetGlobalObjectIdSlow(value).ToString() : "null";
    static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    static float[] Q(Quaternion value) => new[] { value.x, value.y, value.z, value.w };
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    static IEnumerable<AnimatorState> States(AnimatorStateMachine machine)
    {
        foreach (var state in machine.states) yield return state.state;
        foreach (var child in machine.stateMachines) foreach (var state in States(child.stateMachine)) yield return state;
    }
    static object ControllerReceipt(Animator animator)
    {
        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null || controller.layers.Length == 0) throw new InvalidOperationException("Concrete imported AnimatorController required.");
        var states = States(controller.layers[0].stateMachine).ToArray();
        foreach (string required in Required)
            if (!states.Any(s => s.name == required && s.motion != null)) throw new InvalidOperationException(controller.name + " missing motion-bearing state " + required);
        if (animator.avatar == null || !animator.avatar.isValid) throw new InvalidOperationException("Replacement must keep its valid matching imported avatar.");
        return new { controller = AssetDatabase.GetAssetPath(controller), avatar = AssetDatabase.GetAssetPath(animator.avatar), animator.avatar.isHuman,
            states = Required.Select(name => new { name, motions = states.Where(s => s.name == name).Select(s => AssetDatabase.GetAssetPath(s.motion)).ToArray() }).ToArray() };
    }
    static Bounds VisualBounds(Transform visual)
    {
        bool found = false; Bounds result = default;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null; bool temporary = false;
            if (renderer is SkinnedMeshRenderer skin) { mesh = new Mesh(); skin.BakeMesh(mesh); temporary = true; }
            else mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null || mesh.vertexCount == 0) { if (temporary) Object.DestroyImmediate(mesh); continue; }
            var b = mesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                var point = renderer.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                if (!found) { result = new Bounds(point, Vector3.zero); found = true; } else result.Encapsulate(point);
            }
            if (temporary) Object.DestroyImmediate(mesh);
        }
        if (!found || result.size.y < .1f || float.IsNaN(result.size.y)) throw new InvalidOperationException("Cannot measure visual mesh bounds.");
        return result;
    }
    static object GameplayReceipt(Scene scene)
    {
        var director = All<Chapter45Director>(scene).Single();
        var actors = All<Chapter45Encounter>(scene).SelectMany(e => e.actors).Distinct().OrderBy(e => PathOf(e.transform)).ToArray();
        return new { scene = scene.path, director = EditorJsonUtility.ToJson(director), route = EditorJsonUtility.ToJson(director.route),
            encounters = All<Chapter45Encounter>(scene).OrderBy(e => PathOf(e.transform)).Select(e => new { id = Id(e), serialized = EditorJsonUtility.ToJson(e) }).ToArray(),
            actors = actors.Select(a => new { id = Id(a), path = PathOf(a.transform), active = a.gameObject.activeSelf, tag = a.tag, layer = a.gameObject.layer,
                parent = Id(a.transform.parent), position = V(a.transform.localPosition), rotation = Q(a.transform.localRotation), scale = V(a.transform.localScale),
                // Transform child membership necessarily changes. All other root components retain exact serialized configuration.
                rootComponents = a.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => new { type = c.GetType().FullName, id = Id(c), serialized = EditorJsonUtility.ToJson(c) }).ToArray() }).ToArray(),
            colliders = All<Collider>(scene).OrderBy(c => Id(c)).Select(c => new { id = Id(c), path = PathOf(c.transform), serialized = EditorJsonUtility.ToJson(c),
                position = V(c.transform.position), rotation = Q(c.transform.rotation), scale = V(c.transform.lossyScale) }).ToArray() };
    }
    static void RemapCaches(EnemyScript_space actor, Animator replacement)
    {
        // These are nonserialized runtime caches. Saved scenes resolve the sole
        // descendant animator naturally in Awake; update edit-session caches too.
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(EnemyScript_space).GetField("enemyAnimator", flags).SetValue(actor, replacement);
        var events = actor.GetComponent<EnemyEventController>();
        typeof(EnemyEventController).GetField("enemyAnimator", flags).SetValue(events, replacement);
        typeof(EnemyEventController).GetField("visualRotationOffsetCaptured", flags).SetValue(events, false);
        typeof(EnemyEventController).GetField("carryLayer", flags).SetValue(events, -1);
    }
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Idle Edit Mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve existing unsaved scene work first.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null || prefab.GetComponentsInChildren<Collider>(true).Length != 0 || prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
            throw new InvalidOperationException("Expected a visual-only C07 prefab with no gameplay components or colliders.");
        var sourceAnimator = prefab.GetComponentsInChildren<Animator>(true).Single();
        if (AssetDatabase.GetAssetPath(sourceAnimator.runtimeAnimatorController) != NewController) throw new InvalidOperationException("Unexpected replacement controller.");
        object controllerReceipt = ControllerReceipt(sourceAnimator);
        var setup = EditorSceneManager.GetSceneManagerSetup(); Scene scene = default; bool saved = false;
        string folder = "outputs/chapters45-2026-10-02/qa/flagger-repair-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
        Directory.CreateDirectory(folder);
        try
        {
            scene = EditorSceneManager.OpenScene(ScenePath);
            var encounterActors = All<Chapter45Encounter>(scene).SelectMany(e => e.actors).Distinct().ToArray();
            var selected = encounterActors.Where(a => a.GetComponentsInChildren<Animator>(true).Any(n => AssetDatabase.GetAssetPath(n.runtimeAnimatorController) == OldController)).ToArray();
            if (selected.Length != 15 && selected.Length != 0) throw new InvalidOperationException("Expected precisely the 15 demonstrated flagger actors; found " + selected.Length);
            string before = Json(GameplayReceipt(scene)); File.WriteAllText(folder + "/gameplay-before.json", before);
            File.Copy(ScenePath, folder + "/Jamsil-before-flagger-repair.unity", false);
            var replaced = new List<object>();
            foreach (var actor in selected)
            {
                var oldAnimator = actor.GetComponentsInChildren<Animator>(true).Single();
                Transform oldVisual = oldAnimator.transform;
                while (oldVisual.parent != actor.transform && oldVisual.parent != null) oldVisual = oldVisual.parent;
                if (oldVisual.parent != actor.transform || !oldVisual.name.StartsWith("C08_flagger") || oldVisual.GetComponentsInChildren<Collider>(true).Length != 0 || oldVisual.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Unexpected C08 visual boundary for " + actor.name);
                var capsule = actor.GetComponent<CapsuleCollider>(); if (capsule == null) throw new InvalidOperationException("Expected preserved root capsule.");
                var beforeBounds = VisualBounds(oldVisual); int sibling = oldVisual.GetSiblingIndex();
                var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                model.name = "C07_riot_police"; model.transform.SetParent(actor.transform, false);
                model.transform.localPosition = oldVisual.localPosition; model.transform.localRotation = oldVisual.localRotation; model.transform.localScale = oldVisual.localScale;
                model.transform.SetSiblingIndex(sibling); model.SetActive(oldVisual.gameObject.activeSelf);
                var initial = VisualBounds(model.transform); float fit = beforeBounds.size.y / initial.size.y;
                if (fit < .5f || fit > 2) throw new InvalidOperationException("Unexpected rig-height correction " + fit);
                model.transform.localScale *= fit;
                var fitted = VisualBounds(model.transform); model.transform.position += Vector3.up * (beforeBounds.min.y - fitted.min.y);
                var afterBounds = VisualBounds(model.transform); float colliderHeight = capsule.height * Mathf.Abs(actor.transform.lossyScale.y);
                if (afterBounds.size.y < colliderHeight * .6f || afterBounds.size.y > colliderHeight * 1.5f || Mathf.Abs(afterBounds.min.y - beforeBounds.min.y) > .025f)
                    throw new InvalidOperationException("Replacement model does not fit preserved collider/ground plane for " + actor.name);
                var animator = model.GetComponentsInChildren<Animator>(true).Single(); ControllerReceipt(animator);
                RemapCaches(actor, animator);
                Object.DestroyImmediate(oldVisual.gameObject);
                if (actor.GetComponentsInChildren<Animator>(true).Length != 1) throw new InvalidOperationException("Actor must resolve one matching rig animator.");
                replaced.Add(new { actor = PathOf(actor.transform), actorId = Id(actor), oldController = OldController, newController = NewController, rigHeightScale = fit,
                    before = new { center = V(beforeBounds.center), size = V(beforeBounds.size), min = V(beforeBounds.min) },
                    after = new { center = V(afterBounds.center), size = V(afterBounds.size), min = V(afterBounds.min) }, colliderHeight, rootCollider = Id(capsule) });
            }
            string after = Json(GameplayReceipt(scene)); File.WriteAllText(folder + "/gameplay-after.json", after);
            if (before != after) throw new InvalidOperationException("Gameplay/collider fingerprint changed; refusing scene save.");
            if (encounterActors.Any(a => a.GetComponentsInChildren<Animator>(true).Any(n => AssetDatabase.GetAssetPath(n.runtimeAnimatorController) == OldController)))
                throw new InvalidOperationException("An encounter flagger remains after replacement.");
            var allControllers = encounterActors.Select(a => ControllerReceipt(a.GetComponentsInChildren<Animator>(true).Single())).ToArray();
            if (selected.Length > 0) { EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Unity refused the Jamsil scene save."); }
            saved = true;
            var receipt = new { scene = ScenePath, replaced = selected.Length, visualOnly = true, sourcePrefabsChanged = false,
                beforeFingerprint = Hash(before), afterFingerprint = Hash(after), gameplayAndCollidersUnchanged = true, controllerReceipt, replacements = replaced, allEncounterControllers = allControllers,
                limits = "EditMode native imported controller/motion/bounds validation. Root still needs actual animation playback and visual evidence." };
            File.WriteAllText(folder + "/receipt.json", Json(receipt)); return new { saved = true, selected.Length, folder, receipt };
        }
        finally
        {
            // Failures discard this tool's unsaved changes; the saved original and backup remain intact.
            if (!saved && scene.IsValid() && scene.isLoaded) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
